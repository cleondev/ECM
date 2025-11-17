# Background workers

Supporting workers for the ECM platform live here. Each worker is a standalone .NET worker project responsible for background processes such as dispatching outbox messages, indexing documents for search, or sending notifications.

- `Tagger.Worker`: subscribes to document pipeline events and applies configurable metadata-driven rules to assign document tags.
