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
using ECM.Workflow.Domain.Definitions;
using ECM.Workflow.Domain.Instances;
using ECM.Workflow.Infrastructure.Camunda.Dto;
using ECM.Workflow.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ECM.Workflow.Infrastructure.Camunda;

internal sealed class CamundaWorkflowRepository : IWorkflowRepository
{
    private readonly HttpClient _client;
    private readonly ILogger<CamundaWorkflowRepository> _logger;

    public CamundaWorkflowRepository(HttpClient client, IOptions<CamundaOptions> options, ILogger<CamundaWorkflowRepository> logger)
    {
        _client = client;
        _logger = logger;

        var baseUrl = options.Value.BaseUrl?.TrimEnd('/') ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(baseUrl) && _client.BaseAddress is null)
        {
            _client.BaseAddress = new Uri(baseUrl, UriKind.Absolute);
        }
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

        using var response = await _client.PostAsJsonAsync($"process-definition/key/{Uri.EscapeDataString(definition.Key)}/start", payload, cancellationToken);
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
        using var response = await _client.GetAsync("process-instance?active=true", cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Failed to query Camunda process instances. Status {StatusCode}. Response {Body}", response.StatusCode, body);
            return Array.Empty<WorkflowInstance>();
        }

        var instances = await response.Content.ReadFromJsonAsync<List<ProcessInstanceDto>>(cancellationToken: cancellationToken)
            ?? [];

        if (instances.Count == 0)
        {
            return Array.Empty<WorkflowInstance>();
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

    private async Task<WorkflowDefinition?> GetDefinitionByKeyAsync(string key, CancellationToken cancellationToken)
    {
        using var response = await _client.GetAsync($"process-definition/key/{Uri.EscapeDataString(key)}", cancellationToken);
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

    private async Task<WorkflowDefinition?> GetDefinitionByIdAsync(string id, CancellationToken cancellationToken)
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

    private WorkflowInstance MapToDomain(ProcessInstanceDto dto, WorkflowDefinition definition, Guid documentId, DateTimeOffset startedAt)
    {
        var internalId = TryParseGuid(dto.Id, out var guid)
            ? guid
            : CreateDeterministicGuid(dto.Id);

        return new WorkflowInstance(internalId, documentId, definition, WorkflowStatus.Running, startedAt, dto.Id);
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

    private readonly Dictionary<string, WorkflowDefinition> _definitionIdCache = new(StringComparer.OrdinalIgnoreCase);
}
