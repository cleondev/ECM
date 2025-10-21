using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ECM.Host.Swagger;

/// <summary>
/// Adds missing parameter metadata for minimal API endpoints so that Swagger UI displays query, route and body inputs.
/// </summary>
internal sealed class MinimalApiParameterOperationFilter : IOperationFilter
{
    private static readonly Regex RouteParameterRegex = new("\\{(.*?)(:|\\})", RegexOptions.Compiled);
    private static readonly NullabilityInfoContext NullabilityContext = new();

    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (operation is null || context.MethodInfo is null)
        {
            return;
        }

        var routeParameters = ExtractRouteParameters(context.ApiDescription?.RelativePath);

        foreach (var parameter in context.MethodInfo.GetParameters())
        {
            if (ShouldSkip(parameter))
            {
                continue;
            }

            if (TryHandleExplicitBinding(operation, context, parameter))
            {
                continue;
            }

            if (TryHandleAsParameters(operation, context, parameter))
            {
                continue;
            }

            if (parameter.Name is not null && routeParameters.Contains(parameter.Name, StringComparer.OrdinalIgnoreCase))
            {
                AddParameter(operation, context, parameter.Name, parameter.ParameterType, ParameterLocation.Path, required: true);
                continue;
            }

            if (IsSimpleType(parameter.ParameterType))
            {
                var required = !parameter.HasDefaultValue && !parameter.IsOptional;
                AddParameter(operation, context, parameter.Name ?? parameter.ParameterType.Name, parameter.ParameterType, ParameterLocation.Query, required);
                continue;
            }

            if (LooksLikeRequestBody(parameter))
            {
                EnsureRequestBody(operation, context, parameter.ParameterType, GetRequestContentType(parameter));
            }
        }
    }

    private static bool TryHandleExplicitBinding(OpenApiOperation operation, OperationFilterContext context, ParameterInfo parameter)
    {
        var fromRoute = parameter.GetCustomAttribute<FromRouteAttribute>();
        if (fromRoute is not null)
        {
            var name = string.IsNullOrWhiteSpace(fromRoute.Name) ? parameter.Name ?? parameter.ParameterType.Name : fromRoute.Name;
            AddParameter(operation, context, name, parameter.ParameterType, ParameterLocation.Path, required: true);
            return true;
        }

        var fromQuery = parameter.GetCustomAttribute<FromQueryAttribute>();
        if (fromQuery is not null)
        {
            var name = string.IsNullOrWhiteSpace(fromQuery.Name) ? parameter.Name ?? parameter.ParameterType.Name : fromQuery.Name;
            var required = !parameter.HasDefaultValue && !parameter.IsOptional;
            AddParameter(operation, context, name, parameter.ParameterType, ParameterLocation.Query, required);
            return true;
        }

        var fromHeader = parameter.GetCustomAttribute<FromHeaderAttribute>();
        if (fromHeader is not null)
        {
            var name = string.IsNullOrWhiteSpace(fromHeader.Name) ? parameter.Name ?? parameter.ParameterType.Name : fromHeader.Name;
            var required = !parameter.HasDefaultValue && !parameter.IsOptional;
            AddParameter(operation, context, name, parameter.ParameterType, ParameterLocation.Header, required);
            return true;
        }

        if (parameter.GetCustomAttribute<FromBodyAttribute>() is not null)
        {
            EnsureRequestBody(operation, context, parameter.ParameterType, "application/json");
            return true;
        }

        if (parameter.GetCustomAttribute<FromFormAttribute>() is not null)
        {
            EnsureRequestBody(operation, context, parameter.ParameterType, "multipart/form-data");
            return true;
        }

        return false;
    }

    private static bool TryHandleAsParameters(OpenApiOperation operation, OperationFilterContext context, ParameterInfo parameter)
    {
        if (parameter.GetCustomAttribute<AsParametersAttribute>() is null)
        {
            return false;
        }

        foreach (var property in parameter.ParameterType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!property.CanRead || property.GetCustomAttribute<FromServicesAttribute>() is not null)
            {
                continue;
            }

            var (name, location) = ResolvePropertyBinding(property);
            var required = IsPropertyRequired(property);
            AddParameter(operation, context, name, property.PropertyType, location, required);
        }

        return true;
    }

    private static (string Name, ParameterLocation Location) ResolvePropertyBinding(PropertyInfo property)
    {
        if (property.GetCustomAttribute<FromRouteAttribute>() is { } fromRoute)
        {
            var name = string.IsNullOrWhiteSpace(fromRoute.Name) ? property.Name : fromRoute.Name;
            return (name, ParameterLocation.Path);
        }

        if (property.GetCustomAttribute<FromHeaderAttribute>() is { } fromHeader)
        {
            var name = string.IsNullOrWhiteSpace(fromHeader.Name) ? property.Name : fromHeader.Name;
            return (name, ParameterLocation.Header);
        }

        if (property.GetCustomAttribute<FromQueryAttribute>() is { } fromQuery)
        {
            var name = string.IsNullOrWhiteSpace(fromQuery.Name) ? property.Name : fromQuery.Name;
            return (name, ParameterLocation.Query);
        }

        return (property.Name, ParameterLocation.Query);
    }

    private static bool IsPropertyRequired(PropertyInfo property)
    {
        if (property.GetCustomAttribute<RequiredAttribute>() is not null)
        {
            return true;
        }

        if (property.PropertyType.IsValueType)
        {
            return Nullable.GetUnderlyingType(property.PropertyType) is null;
        }

        var nullabilityInfo = NullabilityContext.Create(property);
        return nullabilityInfo.ReadState == NullabilityState.NotNull;
    }

    private static void AddParameter(OpenApiOperation operation, OperationFilterContext context, string name, Type type, ParameterLocation location, bool required)
    {
        operation.Parameters ??= new List<OpenApiParameter>();

        if (operation.Parameters.Any(parameter => string.Equals(parameter.Name, name, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        var schema = context.SchemaGenerator.GenerateSchema(type, context.SchemaRepository);

        operation.Parameters.Add(new OpenApiParameter
        {
            Name = name,
            In = location,
            Required = location == ParameterLocation.Path || required,
            Schema = schema
        });
    }

    private static void EnsureRequestBody(OpenApiOperation operation, OperationFilterContext context, Type type, string contentType)
    {
        var schema = context.SchemaGenerator.GenerateSchema(type, context.SchemaRepository);

        operation.RequestBody ??= new OpenApiRequestBody
        {
            Content = new Dictionary<string, OpenApiMediaType>()
        };

        if (!operation.RequestBody.Content.TryGetValue(contentType, out var mediaType))
        {
            mediaType = new OpenApiMediaType();
            operation.RequestBody.Content[contentType] = mediaType;
        }

        mediaType.Schema = schema;
        operation.RequestBody.Required = true;
    }

    private static string GetRequestContentType(ParameterInfo parameter)
    {
        return parameter.GetCustomAttribute<FromFormAttribute>() is not null
            ? "multipart/form-data"
            : "application/json";
    }

    private static bool LooksLikeRequestBody(ParameterInfo parameter)
    {
        if (parameter.GetCustomAttribute<FromBodyAttribute>() is not null || parameter.GetCustomAttribute<FromFormAttribute>() is not null)
        {
            return true;
        }

        var typeName = parameter.ParameterType.Name;
        return typeName.EndsWith("Request", StringComparison.OrdinalIgnoreCase) ||
               typeName.EndsWith("Command", StringComparison.OrdinalIgnoreCase) ||
               typeName.EndsWith("Payload", StringComparison.OrdinalIgnoreCase);
    }

    private static bool ShouldSkip(ParameterInfo parameter)
    {
        if (parameter.ParameterType == typeof(CancellationToken) ||
            typeof(HttpContext).IsAssignableFrom(parameter.ParameterType) ||
            typeof(ClaimsPrincipal).IsAssignableFrom(parameter.ParameterType) ||
            parameter.GetCustomAttribute<FromServicesAttribute>() is not null)
        {
            return true;
        }

        if (parameter.ParameterType.Namespace is { } @namespace && @namespace.StartsWith("Microsoft.Extensions.Logging", StringComparison.Ordinal))
        {
            return true;
        }

        if (parameter.ParameterType.IsGenericType && parameter.ParameterType.GetGenericTypeDefinition() == typeof(ILogger<>))
        {
            return true;
        }

        return false;
    }

    private static bool IsSimpleType(Type type)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;

        if (type.IsPrimitive || type.IsEnum)
        {
            return true;
        }

        return type == typeof(string) ||
               type == typeof(Guid) ||
               type == typeof(DateTime) ||
               type == typeof(DateOnly) ||
               type == typeof(TimeOnly) ||
               type == typeof(DateTimeOffset) ||
               type == typeof(decimal) ||
               type == typeof(double) ||
               type == typeof(float) ||
               type == typeof(Uri);
    }

    private static ISet<string> ExtractRouteParameters(string? template)
    {
        if (string.IsNullOrWhiteSpace(template))
        {
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        var matches = RouteParameterRegex.Matches(template);
        var names = matches
            .Select(match => match.Groups[1].Value)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Split(':', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)[0])
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return names;
    }
}
