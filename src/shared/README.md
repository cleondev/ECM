# shared

Cross-cutting concerns that can be reused across services belong here.

- `Contracts/` – HTTP/GRPC contracts and DTOs exchanged between bounded contexts.
- `Messaging/` – Message schemas and integration event definitions shared across the platform.
- `Utilities/` – Shared utility classes, helpers, and cross-cutting services.
- `Extensions/` – Common extension methods and adapters.

Add concrete implementations as the solution grows.
