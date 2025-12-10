# Tagger rule set mapping

This note summarizes where a custom rule like `AutoDateRule` gets associated with a rule set and how that rule set is triggered for a tagging event.

## Where rules join rule sets
- Built-in rules are grouped into rule sets inside `BuiltInRuleProvider`. Each call to `CreateRuleSet` wraps the shared rules (`AutoDateRule`, `DocumentTypeRule`) inside a named `RuleSet` such as `Tagging.DocumentUploaded` or `Tagging.OcrCompleted`. The rule set name is how the engine distinguishes which group of rules to run.

## How rule sets are mapped to events
- `TaggerRuleTriggersSetup` seeds the configuration with a default mapping from each integration event name (`DocumentUploaded`, `OcrCompleted`) to the corresponding rule set names. This ensures that, even without custom configuration, each event has a rule set associated with it.
- `TaggingRuleSetSelector` reads the current configuration (`TaggerRulesOptions.Triggers`) at runtime, filters for triggers whose `Event` matches the incoming integration event, and returns the distinct rule set names to evaluate. Those names are then passed to the rule engine so the correct grouped rules run.

Together, the provider assigns rules to named rule sets, the setup class maps events to those rule sets, and the selector resolves the mapping at runtime when an event arrives.
