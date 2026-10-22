# Changelog

All notable changes to Pure.Authorization.OpenFGA.Model.Abstractions.Serialization.System are documented here.

Format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

---

## [Unreleased]

### Added

- `System.Text.Json` converters between the `Pure.Authorization.OpenFGA.Model.Abstractions`
  0.1.0-preview.0.1.0 interfaces and OpenFGA's JSON:
  **`AuthorizationModelConverter`** (`AuthorizationModel`),
  **`TypeDefinitionConverter`** (`TypeDefinition`, including the relation rewrites and
  `metadata.relations.*.directly_related_user_types`),
  **`SubjectFormConverter`** (`RelationReference`),
  **`ConditionConverter`** (`Condition`),
  **`ConditionParameterTypeConverter`** (`ConditionParamTypeRef`, `TYPE_NAME_*` enum),
  **`TupleConverter`** and **`ConditionalTupleConverter`** (`TupleKey`),
  **`RelationshipConditionConverter`** (`RelationshipCondition`, with `context` kept as a
  `JsonNode`), **`SubjectConverter`** (`type:id`, `type:id#relation`, `type:*`) and
  **`ObjectConverter`** (`type:id`).
- **`AuthorizationOpenFGAModelAbstractionsConverters`** — an `IEnumerable<JsonConverter>`
  bundling all ten converters for registration with `JsonSerializerOptions`.
- Reading accepts both the OpenFGA CLI output and the OpenFGA API output (unpopulated
  fields, `null` values, the proto names `computed_userset` and `tuple_to_userset`);
  writing produces the CLI's shape, which the API accepts.
- Package validation is enabled (`EnablePackageValidation`), with no baseline until the
  first release.
