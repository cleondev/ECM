# AppGateway → ECM.Host Document Responsibility Refactor

## Goals
- Keep AppGateway focused on transport concerns (routing, coarse request shape validation, authn) while delegating business rules to ECM.Host.
- Centralize document business logic and validation inside the Document module (application/domain layers) to avoid duplication.
- Relocate user-context utilities (e.g., resolving OwnerId/CreatedBy) to the Document module so they are owned and tested alongside document workflows.

## Proposed layering
```
src/
  AppGateway/
    Api (controllers/endpoints, FluentValidation/DataAnnotations for shape)
    Infrastructure (HTTP clients to ECM.Host, auth middleware)
    Contracts (DTOs shared with clients only)
  ECM.Host/
    Document/
      Contracts (request/response models shared with gateway)
      Application (handlers/services orchestrating document use cases)
      Domain (entities, value objects, policies, invariants)
      Infrastructure (persistence, file storage, message bus adapters)
```
- **Contracts**: pure DTOs without ASP.NET Core attributes. Gateways adapt inbound payloads to these DTOs.
- **Application**: entry points for use cases (upload, update metadata, permission checks). Coordinates domain objects and infrastructure. Contains validations that require business rules or persistence.
- **Domain**: document aggregate, permission policies, status transitions. Contains guard logic and invariants.
- **Infrastructure**: EF Core repositories, file storage providers, audit/logging sinks.

## DocumentUserContextResolver relocation
- Move `DocumentUserContextResolver` into the Document application layer (e.g., `ECM.Host/Document/Application/UserContext/DocumentUserContextResolver.cs`).
- Depend on a **cross-cutting abstraction** that the host can supply, e.g., `ICurrentPrincipalAccessor` or `ICurrentUserProvider` that exposes `ClaimsPrincipal Current`.
- AppGateway should **not** set document ownership values. Instead, it forwards the principal (via token) and request DTOs as-is.
- ECM.Host uses the resolver inside application services when ownership defaults are needed.

### Interface sketch
```csharp
public interface ICurrentUserProvider
{
    ClaimsPrincipal Current { get; }
    Guid? TryGetUserId();
    string? TryGetDisplayName();
}

public interface IDocumentUserContextResolver
{
    DocumentUserContext Resolve(DocumentCommandContext commandContext);
}

public sealed record DocumentUserContext(Guid? OwnerId, Guid? CreatedBy, string? DisplayName);
```
- `ICurrentUserProvider` lives in a shared kernel/infrastructure abstraction (e.g., `ECM.Host/Common/Security`). Host registers an implementation that reads `IHttpContextAccessor`.
- `DocumentUserContextResolver` remains Document-specific; it interprets claims for document use cases.

## OwnerId/CreatedBy handling design
- Keep gateway validation minimal: ensure request shape is valid JSON and required transport fields exist (e.g., file is present). Do **not** attempt to backfill ownership fields.
- In the Document application layer, introduce an **upload command handler** that composes ownership as part of its pipeline.

### Command flow
1. **Controller/Endpoint (AppGateway)**: accepts upload request; maps to `UploadDocumentRequest` DTO (contracts). Does not mutate ownership fields.
2. **Gateway client call**: forwards JWT/access token to ECM.Host.
3. **ECM.Host API**: maps request to `UploadDocumentCommand` and passes to mediator/service.
4. **Application handler**: calls `IDocumentUserContextResolver` to compute final ownership based on request payload + current principal.
5. **Domain**: enforces ownership rules and creates the aggregate.

### Resolver logic sketch
```csharp
public sealed class DocumentUserContextResolver : IDocumentUserContextResolver
{
    private readonly ICurrentUserProvider _currentUser;

    public DocumentUserContextResolver(ICurrentUserProvider currentUser)
    {
        _currentUser = currentUser;
    }

    public DocumentUserContext Resolve(DocumentCommandContext commandContext)
    {
        var claimsUserId = _currentUser.TryGetUserId();

        var ownerId = commandContext.OwnerId ?? claimsUserId
            ?? throw new ValidationException("OwnerId is required for this operation.");

        var createdBy = commandContext.CreatedBy ?? claimsUserId
            ?? throw new ValidationException("CreatedBy is required for this operation.");

        return new DocumentUserContext(ownerId, createdBy, _currentUser.TryGetDisplayName());
    }
}

public sealed record DocumentCommandContext(Guid? OwnerId, Guid? CreatedBy);
```
- The resolver is invoked inside command handlers (e.g., `UploadDocumentHandler`).
- Validation exceptions stay in the application layer; controllers translate them to HTTP responses.

## Usage in application handler
```csharp
public sealed class UploadDocumentHandler : IRequestHandler<UploadDocumentCommand, UploadDocumentResult>
{
    private readonly IDocumentUserContextResolver _userContextResolver;
    private readonly IDocumentRepository _repository;

    public UploadDocumentHandler(
        IDocumentUserContextResolver userContextResolver,
        IDocumentRepository repository)
    {
        _userContextResolver = userContextResolver;
        _repository = repository;
    }

    public async Task<UploadDocumentResult> Handle(UploadDocumentCommand request, CancellationToken ct)
    {
        var userContext = _userContextResolver.Resolve(new DocumentCommandContext(request.OwnerId, request.CreatedBy));

        var document = Document.Create(
            title: request.Title,
            ownerId: userContext.OwnerId,
            createdBy: userContext.CreatedBy,
            file: request.File,
            metadata: request.Metadata);

        await _repository.AddAsync(document, ct);

        return new UploadDocumentResult(document.Id, document.Version);
    }
}
```

## Validation split
- **Gateway**: basic request shape (e.g., non-empty filename, max file size before upload). No ownership defaults.
- **ECM.Host Application**: semantic validation (ownership required, permission checks against principal, document status transitions).
- **Domain**: invariants (e.g., cannot transition to Archived from Draft without Review).

## Registration and testing
- Register `ICurrentUserProvider` in `ECM.Host` using `IHttpContextAccessor` to read claims; reuse in other modules.
- Register `IDocumentUserContextResolver` in the Document module's DI extension (e.g., `services.AddDocumentModule()`).
- Unit-test resolver logic with fake `ICurrentUserProvider` to cover combinations: request null vs. explicit IDs, missing claims, mismatched IDs, etc.

## Migration steps
1. Move `DocumentUserContextResolver` to the Document application layer and adjust namespaces.
2. Introduce `ICurrentUserProvider` abstraction and register in ECM.Host.
3. Update upload/update handlers in Document to call the resolver; remove ownership backfill from AppGateway.
4. Trim AppGateway validators to transport-only checks; ensure gateway contracts remain ASP.NET-free.
5. Add tests for ownership resolution and application validation; remove duplicated checks from AppGateway.
