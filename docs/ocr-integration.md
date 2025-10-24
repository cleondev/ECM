# OCR Integration Guide

The ECM platform integrates with Dot OCR, an external OCR microservice, through a dedicated module and background worker.

## Components

- **ECM.Ocr module** – Provides application services, infrastructure adapters, and API endpoints (`/api/ocr/...`) that proxy Dot OCR operations such as retrieving sample results, listing boxes, and updating box values.
- **Ocr.Worker** – Kafka consumer that listens to `ecm.document.uploaded` events and triggers Dot OCR processing via the module's application layer.
- **Dot OCR service** – External HTTP service (configured through `Ocr:Dot:*` options) that performs OCR extraction.

## Configuration

Configure the Dot OCR adapter via configuration (appsettings or environment variables):

```json
"Ocr": {
  "Dot": {
    "BaseUrl": "http://localhost:7075/",
    "StartEndpoint": "api/samples",
    "SampleResultEndpointTemplate": "api/samples/{sampleId}/results",
    "BoxingResultEndpointTemplate": "api/samples/{sampleId}/boxings/{boxingId}/results",
    "BoxesEndpointTemplate": "api/samples/{sampleId}/boxes",
    "BoxValueEndpointTemplate": "api/samples/{sampleId}/boxes/{boxId}",
    "TimeoutSeconds": 100
  }
}
```

Environment variables can override each value using the `Ocr__Dot__*` prefix. Provide `Ocr__Dot__BaseUrl` at minimum; other settings are optional.

## API Endpoints

- `GET /api/ocr/samples/{sampleId}/results` – Fetch the OCR result payload for a sample from Dot OCR.
- `GET /api/ocr/samples/{sampleId}/boxes` – List recognized boxes for a sample.
- `GET /api/ocr/samples/{sampleId}/boxes/{boxId}/results` – Retrieve detailed OCR data for a specific boxing.
- `PUT /api/ocr/samples/{sampleId}/boxes/{boxId}` – Update the value/label of a box (forwarded to Dot OCR).

All endpoints proxy responses from Dot OCR, preserving JSON content for flexibility.

## Worker Behaviour

1. Subscribe to `ecm.document.uploaded` topic (Redpanda/Kafka).
2. For each message, deserialize into `DocumentUploadedIntegrationEvent` and invoke `StartOcrCommand`.
3. The module calls `DotOcrClient` to start processing; response metadata (e.g., sample id) is logged.
4. Downstream components (Dot OCR engine) emit `ecm.ocr.completed` once extraction finishes; SearchIndexer consumes that event to rebuild advanced search indices.

## Local Development

- Ensure Dot OCR service is reachable at the configured base URL.
- Provide Kafka bootstrap servers via `Kafka__BootstrapServers` for the worker.
- When running through Aspire AppHost, `appsettings.json` already ships default values; override via user-secrets or environment variables as needed.
- If the .NET SDK is unavailable locally, validation is limited to source review. Run `dotnet build` and integration tests once
  the SDK is installed to exercise the module and worker end-to-end.

## Troubleshooting

- **HTTP 4xx/5xx from Dot OCR** – Check `BaseUrl` and endpoint templates; the worker logs detailed errors when Dot OCR requests fail.
- **No OCR sample created** – Verify that incoming document events contain the expected metadata and that Dot OCR service is reachable.
- **Missing boxes/results** – Use the API endpoints above to inspect raw responses before debugging UI/business logic.
