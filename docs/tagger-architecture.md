# Tagger Architecture Overview

This document explains how the Tagger worker wires the shared rules engine into the document pipeline to automatically assign tags.

## High-level flow

1. **Integration events**: `TaggingIntegrationEventListener` listens for `DocumentUploaded` and `OcrCompleted` events and forwards them to `TaggingEventProcessor`.
2. **Context creation**: `TaggingRuleContextFactory` builds an `IRuleContext` from the integration event using `TaggingRuleContextBuilder` plus enrichers such as `DocumentTypeContextEnricher` (adds `extension`). Common fields include `DocumentId`, `Title`, `OccurredAtUtc`, `Metadata`, `Fields`, and `EventName`.
3. **Rule set selection**: `TaggingRuleSetSelector` looks at `TaggerRules:triggers` in configuration to determine which rule set names apply to the event (for example `Tagging.DocumentUploaded`).
4. **Rule execution**: `IRuleEngine.Execute` runs every matching rule set (merged from `BuiltInRuleProvider` and `TaggerRuleProvider`). Built-in rules include:
   - `AutoDateRule` → derives a date-based tag definition (group scope) under `LOS/CreditApplication/Dates` with a calendar icon from the `OccurredAtUtc` timestamp.
   - `DocumentTypeRule` → maps the file extension to tag definitions (group scope) like `Document` or `Images` under `LOS/CreditApplication/Document Types` with file/image icons.
5. **Applying tags**: `DocumentTagAssignmentService` consumes `TagIds`/`Tags` from the rule output, ensuring namespaces and tag paths exist (creating them when necessary) before assigning labels via the ECM SDK.

## Configuration surfaces

- **Rule loading**: `TaggerRulesOptions.RuleSets` embeds JSON-like definitions inline; `TaggerRulesOptions.Files` points to external JSON files with the same schema. Both feed `TaggerRuleProvider`.
- **Triggers**: `TaggerRulesOptions.Triggers` map event names (`DocumentUploaded`, `OcrCompleted`) to rule set names. Multiple rule sets can run for the same event.
- **AppliedBy**: optional GUID persisted as the actor when `DocumentTagAssignmentService` creates or assigns tags.
- **Tag definitions**: rules can emit a `Tags` collection where each entry specifies the `scope` (`global`, `group`, `user`), optional `ownerGroupId`/`ownerUserId`, `namespaceDisplayName`, tag `name`, `iconKey`/`color`, and the `path` of ancestor segments to ensure before creating the tag.

## Adding custom rules

1. Implement `IRule` in `Tagger.Rules.Custom` and register it through a provider or JSON file.
2. Add any required context fields via an `ITaggingRuleContextEnricher` (e.g., parsing new metadata or OCR outputs) and register it with DI.
3. Reference the rule set name in `TaggerRules:triggers` so the new rule executes for the relevant events.
4. Validate by sending representative integration events and confirming tag assignments.

## Operational notes

- Rule context keys are **case-insensitive** to simplify JSON expressions.
- Empty rule set mappings are treated as no-ops; Tagger logs a debug entry and skips assignment.
- Tag names are de-duplicated before assignment; tag IDs are ignored if already present on the document.
