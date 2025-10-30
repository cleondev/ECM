using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ECM.BuildingBlocks.Application;
using ECM.Workflow.Application.Workflows;
using ECM.Workflow.Application.Workflows.Commands;
using ECM.Workflow.Application.Workflows.Tasks;
using ECM.Workflow.Domain.Definitions;
using ECM.Workflow.Domain.Instances;
using ECM.Workflow.Domain.Tasks;
using ECM.Workflow.Infrastructure.Camunda.Dto;
using ECM.Workflow.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ECM.Workflow.Infrastructure.Camunda;

internal sealed class CamundaWorkflowRepository : IWorkflowRepository
{
    private readonly HttpClient _client;
    private readonly ILogger<CamundaWorkflowRepository> _logger;
    private readonly string? _tenantId;

    public CamundaWorkflowRepository(HttpClient client, IOptions<CamundaOptions> options, ILogger<CamundaWorkflowRepository> logger)
    {
        _client = client;
        _logger = logger;

        var camundaOptions = options.Value;

        var baseUrl = camundaOptions.BaseUrl?.TrimEnd('/') ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(baseUrl) && _client.BaseAddress is null)
        {
            _client.BaseAddress = new Uri(baseUrl, UriKind.Absolute);
        }

        _tenantId = string.IsNullOrWhiteSpace(camundaOptions.TenantId)
            ? null
            : camundaOptions.TenantId.Trim();
    }

    public async Task<OperationResult<WorkflowInstance>> StartAsync(Guid documentId, string definitionKey, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(definitionKey))
        {
            return OperationResult<WorkflowInstance>.Failure("Workflow definition is required.");
        }

        var definition = await GetDefinitionByKeyAsync(definitionKey, cancellationToken);
        if (definition is null)
        {
            return OperationResult<WorkflowInstance>.Failure("Workflow definition not found in Camunda.");
        }

        var payload = new StartProcessInstanceRequestDto
        {
            BusinessKey = documentId.ToString(),
            Variables = new Dictionary<string, CamundaVariableDto>
            {
                ["documentId"] = new(documentId.ToString(), "String")
            }
        };

        var startEndpoint = BuildDefinitionEndpoint(definition.Key, suffix: "start");
        using var response = await _client.PostAsJsonAsync(startEndpoint, payload, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Failed to start workflow definition {DefinitionKey}. Status {StatusCode}. Response {Body}", definitionKey, response.StatusCode, body);
            return OperationResult<WorkflowInstance>.Failure("Failed to start workflow instance in Camunda.");
        }

        var instanceDto = await response.Content.ReadFromJsonAsync<ProcessInstanceDto>(cancellationToken: cancellationToken);
        if (instanceDto is null)
        {
            return OperationResult<WorkflowInstance>.Failure("Camunda returned an empty response.");
        }

        var workflowInstance = MapToDomain(instanceDto, definition, documentId, DateTimeOffset.UtcNow);
        return OperationResult<WorkflowInstance>.Success(workflowInstance);
    }

    public async Task<IReadOnlyCollection<WorkflowInstance>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        var activeEndpoint = string.IsNullOrEmpty(_tenantId)
            ? "process-instance?active=true"
            : $"process-instance?active=true&tenantIdIn={Uri.EscapeDataString(_tenantId)}";

        using var response = await _client.GetAsync(activeEndpoint, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Failed to query Camunda process instances. Status {StatusCode}. Response {Body}", response.StatusCode, body);
            return [];
        }

        var instances = await response.Content.ReadFromJsonAsync<List<ProcessInstanceDto>>(cancellationToken: cancellationToken)
            ?? [];

        if (instances.Count == 0)
        {
            return [];
        }

        var definitionCache = new Dictionary<string, WorkflowDefinition>(StringComparer.OrdinalIgnoreCase);
        var results = new List<WorkflowInstance>(instances.Count);

        foreach (var dto in instances)
        {
            if (string.IsNullOrWhiteSpace(dto.BusinessKey) || !Guid.TryParse(dto.BusinessKey, out var documentId))
            {
                continue;
            }

            if (!definitionCache.TryGetValue(dto.DefinitionId, out var definition))
            {
                definition = await GetDefinitionByIdAsync(dto.DefinitionId, cancellationToken);
                if (definition is null)
                {
                    continue;
                }

                definitionCache[dto.DefinitionId] = definition;
            }

            var startedAt = await GetStartTimeAsync(dto.Id, cancellationToken) ?? DateTimeOffset.UtcNow;
            results.Add(MapToDomain(dto, definition, documentId, startedAt));
        }

        return results;
    }

    public async Task<IReadOnlyCollection<WorkflowDefinition>> GetDefinitionsAsync(CancellationToken cancellationToken = default)
    {
        var endpoint = string.IsNullOrEmpty(_tenantId)
            ? "process-definition?sortBy=key&sortOrder=asc"
            : $"process-definition?tenantIdIn={Uri.EscapeDataString(_tenantId)}&sortBy=key&sortOrder=asc";

        using var response = await _client.GetAsync(endpoint, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Failed to list Camunda process definitions. Status {StatusCode}. Response {Body}", response.StatusCode, body);
            return [];
        }

        var dtos = await response.Content.ReadFromJsonAsync<List<ProcessDefinitionDto>>(cancellationToken: cancellationToken)
            ?? [];

        var definitions = new List<WorkflowDefinition>(dtos.Count);
        foreach (var dto in dtos)
        {
            var definition = MapDefinition(dto);
            _definitionIdCache[definition.Id] = definition;
            definitions.Add(definition);
        }

        return definitions;
    }

    public Task<WorkflowDefinition?> GetDefinitionByIdAsync(string definitionId, CancellationToken cancellationToken = default)
        => GetDefinitionByIdInternalAsync(definitionId, cancellationToken);

    public async Task<WorkflowInstance?> GetInstanceByExternalIdAsync(string instanceId, CancellationToken cancellationToken = default)
    {
        using var response = await _client.GetAsync($"process-instance/{Uri.EscapeDataString(instanceId)}", cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning("Camunda instance lookup {InstanceId} failed. Status {StatusCode}. Response {Body}", instanceId, response.StatusCode, body);
            return null;
        }

        var dto = await response.Content.ReadFromJsonAsync<ProcessInstanceDto>(cancellationToken: cancellationToken);
        if (dto is null)
        {
            return null;
        }

        var definition = await GetDefinitionByIdAsync(dto.DefinitionId, cancellationToken);
        if (definition is null)
        {
            return null;
        }

        var startedAt = await GetStartTimeAsync(dto.Id, cancellationToken) ?? DateTimeOffset.UtcNow;
        var completedAt = dto.Ended is true ? startedAt : (DateTimeOffset?)null;
        var documentId = TryParseDocumentId(dto.BusinessKey);

        var status = dto.Ended is true
            ? WorkflowStatus.Completed
            : WorkflowStatus.Running;

        return MapToDomain(dto, definition, documentId ?? Guid.Empty, startedAt, status, completedAt);
    }

    public async Task<OperationResult> CancelInstanceAsync(string instanceId, string? reason, CancellationToken cancellationToken = default)
    {
        var endpoint = $"process-instance/{Uri.EscapeDataString(instanceId)}";
        var requestUri = $"{endpoint}?skipIoMappings=true&skipCustomListeners=true";

        using var response = await _client.DeleteAsync(requestUri, cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            return OperationResult.Success();
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        _logger.LogError("Failed to cancel Camunda instance {InstanceId}. Status {StatusCode}. Response {Body}", instanceId, response.StatusCode, body);
        return OperationResult.Failure("Unable to cancel workflow instance in Camunda.");
    }

    public async Task<IReadOnlyCollection<WorkflowTask>> GetTasksAsync(WorkflowTaskQuery query, CancellationToken cancellationToken = default)
    {
        var endpoint = BuildTaskQuery(query);
        using var response = await _client.GetAsync(endpoint, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Failed to list Camunda tasks. Status {StatusCode}. Response {Body}", response.StatusCode, body);
            return [];
        }

        var taskDtos = await response.Content.ReadFromJsonAsync<List<TaskDto>>(cancellationToken: cancellationToken)
            ?? [];

        var tasks = new List<WorkflowTask>(taskDtos.Count);
        foreach (var dto in taskDtos)
        {
            var task = await MapTaskAsync(dto, includeVariables: false, cancellationToken);
            if (task is not null)
            {
                tasks.Add(task);
            }
        }

        return tasks;
    }

    public async Task<WorkflowTask?> GetTaskAsync(string taskId, CancellationToken cancellationToken = default)
    {
        using var response = await _client.GetAsync($"task/{Uri.EscapeDataString(taskId)}", cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning("Camunda task lookup {TaskId} failed. Status {StatusCode}. Response {Body}", taskId, response.StatusCode, body);
            return null;
        }

        var dto = await response.Content.ReadFromJsonAsync<TaskDto>(cancellationToken: cancellationToken);
        if (dto is null)
        {
            return null;
        }

        return await MapTaskAsync(dto, includeVariables: true, cancellationToken);
    }

    public async Task<OperationResult> ClaimTaskAsync(string taskId, string assigneeId, CancellationToken cancellationToken = default)
    {
        var payload = new TaskClaimRequestDto(assigneeId);
        using var response = await _client.PostAsJsonAsync($"task/{Uri.EscapeDataString(taskId)}/claim", payload, cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            return OperationResult.Success();
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        _logger.LogError("Failed to claim Camunda task {TaskId} for {Assignee}. Status {StatusCode}. Response {Body}", taskId, assigneeId, response.StatusCode, body);
        return OperationResult.Failure("Unable to claim workflow task in Camunda.");
    }

    public async Task<OperationResult> CompleteTaskAsync(CompleteWorkflowTaskCommand command, CancellationToken cancellationToken = default)
    {
        var variables = new Dictionary<string, CamundaVariableDto>
        {
            ["action"] = new(command.Action, "String")
        };

        if (!string.IsNullOrWhiteSpace(command.Comment))
        {
            variables["comment"] = new(command.Comment, "String");
        }

        if (command.Outputs is not null)
        {
            foreach (var (key, value) in command.Outputs)
            {
                if (string.IsNullOrWhiteSpace(key))
                {
                    continue;
                }

                variables[key] = new(value, DetermineVariableType(value));
            }
        }

        var payload = new TaskCompleteRequestDto(variables);
        using var response = await _client.PostAsJsonAsync($"task/{Uri.EscapeDataString(command.TaskId)}/complete", payload, cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            return OperationResult.Success();
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        _logger.LogError("Failed to complete Camunda task {TaskId}. Status {StatusCode}. Response {Body}", command.TaskId, response.StatusCode, body);
        return OperationResult.Failure("Unable to complete workflow task in Camunda.");
    }

    public async Task<OperationResult> ReassignTaskAsync(string taskId, string assigneeId, CancellationToken cancellationToken = default)
    {
        var payload = new TaskAssigneeRequestDto(assigneeId);
        using var response = await _client.PostAsJsonAsync($"task/{Uri.EscapeDataString(taskId)}/assignee", payload, cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            return OperationResult.Success();
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        _logger.LogError("Failed to reassign Camunda task {TaskId} to {Assignee}. Status {StatusCode}. Response {Body}", taskId, assigneeId, response.StatusCode, body);
        return OperationResult.Failure("Unable to reassign workflow task in Camunda.");
    }

    private async Task<WorkflowDefinition?> GetDefinitionByKeyAsync(string key, CancellationToken cancellationToken)
    {
        var endpoint = BuildDefinitionEndpoint(key);
        using var response = await _client.GetAsync(endpoint, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning("Camunda definition lookup by key {DefinitionKey} failed. Status {StatusCode}. Response {Body}", key, response.StatusCode, body);
            return null;
        }

        var dto = await response.Content.ReadFromJsonAsync<ProcessDefinitionDto>(cancellationToken: cancellationToken);
        if (dto is null)
        {
            return null;
        }

        var definition = MapDefinition(dto);
        _definitionIdCache[definition.Id] = definition;
        return definition;
    }

    private async Task<WorkflowDefinition?> GetDefinitionByIdInternalAsync(string id, CancellationToken cancellationToken)
    {
        if (_definitionIdCache.TryGetValue(id, out var cached))
        {
            return cached;
        }

        using var response = await _client.GetAsync($"process-definition/{Uri.EscapeDataString(id)}", cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning("Camunda definition lookup by id {DefinitionId} failed. Status {StatusCode}. Response {Body}", id, response.StatusCode, body);
            return null;
        }

        var dto = await response.Content.ReadFromJsonAsync<ProcessDefinitionDto>(cancellationToken: cancellationToken);
        var definition = dto is null ? null : MapDefinition(dto);
        if (definition is not null)
        {
            _definitionIdCache[id] = definition;
        }

        return definition;
    }

    private async Task<DateTimeOffset?> GetStartTimeAsync(string processInstanceId, CancellationToken cancellationToken)
    {
        using var response = await _client.GetAsync($"history/process-instance/{Uri.EscapeDataString(processInstanceId)}", cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var dto = await response.Content.ReadFromJsonAsync<ProcessInstanceHistoryDto>(cancellationToken: cancellationToken);
        return dto?.StartTime;
    }

    private WorkflowInstance MapToDomain(ProcessInstanceDto dto, WorkflowDefinition definition, Guid documentId, DateTimeOffset startedAt, WorkflowStatus status = WorkflowStatus.Running, DateTimeOffset? completedAt = null)
    {
        var internalId = TryParseGuid(dto.Id, out var guid)
            ? guid
            : CreateDeterministicGuid(dto.Id);

        var instance = new WorkflowInstance(internalId, documentId, definition, status, startedAt, dto.Id);

        if (status is WorkflowStatus.Completed && completedAt.HasValue)
        {
            instance.MarkCompleted(completedAt.Value);
        }
        else if (status is WorkflowStatus.Cancelled && completedAt.HasValue)
        {
            instance.MarkCancelled(completedAt.Value);
        }

        return instance;
    }

    private static WorkflowDefinition MapDefinition(ProcessDefinitionDto dto)
        => new(dto.Id, dto.Key, dto.Name ?? dto.Key, dto.Version);

    private static bool TryParseGuid(string value, out Guid guid)
        => Guid.TryParse(value, out guid);

    private static Guid CreateDeterministicGuid(string value)
    {
        using var provider = MD5.Create();
        var hash = provider.ComputeHash(Encoding.UTF8.GetBytes(value));
        return new Guid(hash);
    }

    private string BuildDefinitionEndpoint(string definitionKey, string? suffix = null)
    {
        var baseEndpoint = string.IsNullOrEmpty(_tenantId)
            ? $"process-definition/key/{Uri.EscapeDataString(definitionKey)}"
            : $"process-definition/key/{Uri.EscapeDataString(definitionKey)}/tenant-id/{Uri.EscapeDataString(_tenantId)}";

        if (string.IsNullOrEmpty(suffix))
        {
            return baseEndpoint;
        }

        return $"{baseEndpoint}/{suffix}";
    }

    private async Task<WorkflowTask?> MapTaskAsync(TaskDto dto, bool includeVariables, CancellationToken cancellationToken)
    {
        var definition = await GetDefinitionByIdAsync(dto.ProcessDefinitionId, cancellationToken);
        if (definition is null)
        {
            return null;
        }

        var documentId = await GetDocumentIdForInstanceAsync(dto.ProcessInstanceId, cancellationToken);
        var createdAt = dto.Created ?? DateTimeOffset.UtcNow;

        IReadOnlyDictionary<string, object?>? variables = null;
        if (includeVariables)
        {
            variables = await GetTaskVariablesAsync(dto.Id, cancellationToken);
        }

        return new WorkflowTask(
            dto.Id,
            documentId,
            dto.Name ?? dto.Id,
            definition.Key,
            dto.ProcessInstanceId,
            dto.Assignee,
            WorkflowTaskState.Open,
            createdAt,
            dto.FormKey,
            variables);
    }

    private async Task<IReadOnlyDictionary<string, object?>> GetTaskVariablesAsync(string taskId, CancellationToken cancellationToken)
    {
        using var response = await _client.GetAsync($"task/{Uri.EscapeDataString(taskId)}/variables", cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return new Dictionary<string, object?>();
        }

        var dto = await response.Content.ReadFromJsonAsync<Dictionary<string, CamundaVariableDto>>(cancellationToken: cancellationToken)
            ?? [];

        var variables = new Dictionary<string, object?>(dto.Count, StringComparer.OrdinalIgnoreCase);
        foreach (var (key, value) in dto)
        {
            variables[key] = value.Value;
        }

        return variables;
    }

    private async Task<Guid?> GetDocumentIdForInstanceAsync(string processInstanceId, CancellationToken cancellationToken)
    {
        if (_instanceDocumentCache.TryGetValue(processInstanceId, out var cached))
        {
            return cached;
        }

        using var response = await _client.GetAsync($"process-instance/{Uri.EscapeDataString(processInstanceId)}", cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var dto = await response.Content.ReadFromJsonAsync<ProcessInstanceDto>(cancellationToken: cancellationToken);
        var documentId = TryParseDocumentId(dto?.BusinessKey);
        _instanceDocumentCache[processInstanceId] = documentId;
        return documentId;
    }

    private static Guid? TryParseDocumentId(string? businessKey)
    {
        if (string.IsNullOrWhiteSpace(businessKey))
        {
            return null;
        }

        return Guid.TryParse(businessKey, out var documentId) ? documentId : null;
    }

    private string BuildTaskQuery(WorkflowTaskQuery query)
    {
        var parameters = new List<string>
        {
            "sortBy=created",
            "sortOrder=asc"
        };

        if (!string.IsNullOrWhiteSpace(_tenantId))
        {
            parameters.Add($"tenantIdIn={Uri.EscapeDataString(_tenantId)}");
        }

        if (!string.IsNullOrWhiteSpace(query.AssigneeId))
        {
            parameters.Add($"assignee={Uri.EscapeDataString(query.AssigneeId)}");
        }

        if (query.DocumentId.HasValue)
        {
            parameters.Add($"processInstanceBusinessKey={Uri.EscapeDataString(query.DocumentId.Value.ToString())}");
        }

        if (!string.IsNullOrWhiteSpace(query.State) && query.State.Equals("done", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogDebug("Historic task lookup is not implemented; returning active tasks instead for filter {State}.", query.State);
        }

        return parameters.Count > 0
            ? $"task?{string.Join("&", parameters)}"
            : "task";
    }

    private static string DetermineVariableType(object? value)
        => value switch
        {
            null => "Null",
            string => "String",
            bool => "Boolean",
            int or long => "Long",
            double or float or decimal => "Double",
            Guid => "String",
            DateTime or DateTimeOffset => "String",
            _ => "Json"
        };

    private readonly Dictionary<string, WorkflowDefinition> _definitionIdCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Guid?> _instanceDocumentCache = new(StringComparer.OrdinalIgnoreCase);
}
