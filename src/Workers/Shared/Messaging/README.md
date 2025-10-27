# Messaging shared library

Common messaging utilities for worker projects are grouped here.

- `Abstractions/` contains contracts and shared models used across worker integrations.
- `Configuration/` holds strongly-typed options and helpers for binding messaging settings.
- `Kafka/` provides concrete Kafka-based implementations, including handlers and development stubs.

This structure helps keep interfaces, configuration, and protocol-specific implementations clearly separated when adding new capabilities.
