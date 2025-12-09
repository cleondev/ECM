# Tagger Worker

The **Tagger.Worker** service listens to document pipeline events (upload completion and OCR completion) and applies configurable rules that automatically assign tag labels to documents. Rules are evaluated against the metadata emitted with each document event, which can include uploader information, file extensions, OCR output summaries, document classifications, and group memberships.

## Configuration

Rules are supplied through the worker configuration under the `TaggerRules` section. The schema is:

```json
{
  "TaggerRules": {
    "appliedBy": "optional GUID recorded as the actor",
    "files": ["optional absolute or relative paths to rule files"],
    "ruleSets": [
      {
        "name": "Tagging.DocumentUploaded | Tagging.OcrCompleted",
        "rules": [
          {
            "name": "display name",
            "condition": "rule expression using context fields (case-insensitive)",
            "set": {
              "TagIds": ["list of tag IDs to apply"]
            }
          }
        ]
      }
    ]
  }
}
```

Rule sets map directly to tagger events. Use `Tagging.DocumentUploaded` to react to uploads and `Tagging.OcrCompleted` for OCR events. A rule definition sets output values; to apply tags, populate `TagIds` with the GUIDs of labels to assign. Conditions support `&&`/`||` composition with equality and comparison operators.

### Example

```json
{
  "TaggerRules": {
    "appliedBy": "00000000-0000-0000-0000-000000000001",
    "ruleSets": [
      {
        "name": "Tagging.DocumentUploaded",
        "rules": [
          {
            "name": "HR PDFs",
            "condition": "extension == \".pdf\" && uploadedBy == \"hr-team\"",
            "set": {
              "TagIds": ["aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"]
            }
          }
        ]
      },
      {
        "name": "Tagging.OcrCompleted",
        "rules": [
          {
            "name": "Invoices via OCR",
            "condition": "classification == \"invoice\" || classification == \"receipt\"",
            "set": {
              "TagIds": ["bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"]
            }
          }
        ]
      }
    ]
  }
}
```

When a rule evaluates to `true`, the worker calls the internal document tagging command so that tags are persisted in the Document module.

## Rule layout inside Tagger

The Tagger worker now ships with a set of default rule assets to make it easier to manage and extend automatic tagging:

- `src/Workers/Tagger/RulesConfiguration/JsonRules/CustomerTag.json`: sample JSON rules that can be overridden or extended per deployment.
- `src/Workers/Tagger/Rules/AutoDate.cs`: a built-in rule that emits a tag for the document upload date.
- `src/Workers/Tagger/Rules/DocumentType.cs`: built-in rules that map file extensions to friendly tag names such as `Document` or `Images`.

The default JSON file is copied to the worker output, and the rule providers automatically merge it with any additional paths configured through `TaggerRules:files`. Custom rules remain alongside these folders so that they are easy to locate and update.
