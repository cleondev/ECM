# ECM Rules Architecture

This document summarizes the shared rules infrastructure used across ECM workers and services. The same abstractions power the Tagger worker and can be reused by other components that need pluggable decision logic.

## Building blocks

- **`IRule`** – contract with `Match` and `Apply` methods. The first decides whether the rule is applicable for a given `IRuleContext`; the second writes outputs to `IRuleOutput` when matched.
- **`IRuleContext` / `IRuleContextFactory`** – read-only view over contextual data. Factories create contexts from dictionaries so callers can normalize inputs before rule execution.
- **`IRuleOutput`** – mutable dictionary-like target that rules use to surface values. Tagger relies on `TagIds` and `TagNames`, but any key can be emitted for downstream consumers.
- **`IRuleSet`** – named collection of rules. Names allow multiple providers to contribute rules to the same logical set (for example, built-in and JSON-supplied rules for `Tagging.DocumentUploaded`).
- **`IRuleProvider`** – supplies rule sets to the engine. Providers can be code-based (`BuiltInRuleProvider`) or file-based (`TaggerRuleProvider` via JSON definitions).
- **`IRuleEngine`** – executes a named rule set against a context and returns the aggregated output. Engines combine all rule sets from registered providers that share the same name.

## Execution flow

1. **Context preparation**: call sites gather inputs into a dictionary and build an `IRuleContext` using `IRuleContextFactory`. The context acts as a normalized, case-insensitive lookup.
2. **Rule set resolution**: the caller selects the rule set name(s) to run. Multiple providers may contribute rules to the same name; the engine merges them transparently.
3. **Evaluation**: `IRuleEngine.Execute(ruleSetName, context)` iterates through each rule. For every rule that `Match` returns `true`, `Apply` is invoked to write to the shared `IRuleOutput`.
4. **Consumption**: the caller inspects `IRuleOutput` to drive side effects (for example, Tagger turns `TagIds`/`TagNames` into document labels).

## JSON rule format

The JSON provider (`Ecm.Rules.Providers.Json`) consumes definitions shaped like the snippet below. Names are case-insensitive and merged when duplicated across files or inline configuration.

```json
[
  {
    "name": "My.RuleSet",
    "rules": [
      {
        "name": "Display name",
        "condition": "field == \"value\"",
        "set": {
          "CustomKey": "CustomValue"
        }
      }
    ]
  }
]
```

- `condition` expressions support logical/comparison operators against context keys.
- The `set` block writes arbitrary keys to the output. Downstream code decides how to use them.

## Extensibility guidelines

- Prefer **small, single-purpose rules** and compose behavior by grouping them under the same rule set name.
- Add **enrichers or builders** to normalize inputs before evaluation rather than duplicating parsing logic inside each rule.
- Keep rules **side-effect free**; they should only read the context and write to the output. Any mutations (like tagging documents) belong to the caller.
- Use **unit tests** for custom rules by constructing an `IRuleContext` with representative dictionaries and asserting against `IRuleOutput`.
