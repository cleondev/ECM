# Tagger Worker

The **Tagger.Worker** service listens to document pipeline events (upload completion and OCR completion) and applies configurable rules that automatically assign tag labels to documents. Rules are evaluated against the metadata emitted with each document event, which can include uploader information, file extensions, OCR output summaries, document classifications, and group memberships.

## Configuration

Rules are supplied through the worker configuration under the `TaggingRules` section. The schema is:

```json
{
  "TaggingRules": {
    "appliedBy": "optional GUID recorded as the actor",
    "rules": [
      {
        "name": "display name",
        "description": "optional notes",
        "enabled": true,
        "tagId": "GUID of the tag label to apply",
        "trigger": "All | DocumentUploaded | OcrCompleted",
        "match": "All | Any",
        "conditions": [
          {
            "field": "metadata key (e.g. extension, uploadedBy, classification)",
            "operator": "Equals | NotEquals | Contains | NotContains | StartsWith | EndsWith | In | NotIn | Regex",
            "value": "single comparison value",
            "values": ["optional array of comparison values for In/NotIn"]
          }
        ]
      }
    ]
  }
}
```

### Example

```json
{
  "TaggingRules": {
    "appliedBy": "00000000-0000-0000-0000-000000000001",
    "rules": [
      {
        "name": "HR PDFs",
        "tagId": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
        "trigger": "DocumentUploaded",
        "match": "All",
        "conditions": [
          { "field": "extension", "operator": "Equals", "value": ".pdf" },
          { "field": "uploadedBy", "operator": "Equals", "value": "hr-team" }
        ]
      },
      {
        "name": "Invoices via OCR",
        "tagId": "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
        "trigger": "OcrCompleted",
        "match": "Any",
        "conditions": [
          { "field": "classification", "operator": "Equals", "values": ["invoice", "receipt"] },
          { "field": "content", "operator": "Contains", "value": "amount due" }
        ]
      }
    ]
  }
}
```

When a rule evaluates to `true`, the worker calls the internal document tagging command so that tags are persisted in the Document module.
