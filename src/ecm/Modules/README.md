# Modules

Each domain capability is packaged as an independent module that implements the `IModule` contract exposed from `Abstractions`. Modules expose domain logic through an API layer when necessary and integrate with the platform through the host and shared building blocks.

Scaffold new modules by creating the corresponding `Domain`, `Application`, `Infrastructure`, and `Api` projects under the module folder.
