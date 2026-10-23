# Pure.Authorization.OpenFGA.Model.Abstractions.Serialization.System

`System.Text.Json` converters for OpenFGA authorization model abstractions in the **Pure** ecosystem.

[![.NET build & test](https://github.com/kudima03/Pure.Authorization.OpenFGA.Model.Abstractions.Serialization.System/actions/workflows/build-and-test.yml/badge.svg?branch=main)](https://github.com/kudima03/Pure.Authorization.OpenFGA.Model.Abstractions.Serialization.System/actions/workflows/build-and-test.yml)
[![Build and Deploy](https://github.com/kudima03/Pure.Authorization.OpenFGA.Model.Abstractions.Serialization.System/actions/workflows/publish-nuget.yml/badge.svg?branch=main)](https://github.com/kudima03/Pure.Authorization.OpenFGA.Model.Abstractions.Serialization.System/actions/workflows/publish-nuget.yml)
[![NuGet](https://img.shields.io/nuget/v/Pure.Authorization.OpenFGA.Model.Abstractions.Serialization.System)](https://www.nuget.org/packages/Pure.Authorization.OpenFGA.Model.Abstractions.Serialization.System)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

## Overview

`Pure.Authorization.OpenFGA.Model.Abstractions.Serialization.System` provides `System.Text.Json` converters between the interfaces defined in [`Pure.Authorization.OpenFGA.Model.Abstractions`](https://github.com/kudima03/Pure.Authorization.OpenFGA.Model.Abstractions/tree/0.1.0-preview.0.1.0) and the JSON format used by [OpenFGA](https://openfga.dev): authorization models, tuple keys, and subject and object strings. The converters target the abstract interfaces, so any implementation of them can be written to OpenFGA's JSON; reading produces internal sealed records that implement the same interfaces.

## Converters

| Class | Converts | OpenFGA JSON |
|---|---|---|
| `AuthorizationModelConverter` | `IAuthorizationModel` | `AuthorizationModel` |
| `TypeDefinitionConverter` | `ITypeDefinition` | `TypeDefinition` |
| `SubjectFormConverter` | `ISubjectForm` | `RelationReference` |
| `ConditionConverter` | `ICondition` | `Condition` |
| `ConditionParameterTypeConverter` | `IConditionParameterType` | `ConditionParamTypeRef` |
| `TupleConverter` | `ITuple` | `TupleKey` |
| `ConditionalTupleConverter` | `IConditionalTuple` | `TupleKey` with a `condition` |
| `RelationshipConditionConverter` | `IRelationshipCondition` | `RelationshipCondition` |
| `SubjectConverter` | `ISubject` | `type:id`, `type:id#relation` or `type:*` |
| `ObjectConverter` | `IObject` | `type:id` |
| `AuthorizationOpenFGAModelAbstractionsConverters` | Enumerable collection of all ten converters above | |

A relation's rewrite (`IRewrite`) has no converter of its own: OpenFGA stores the forms of its `IDirect` nodes in the type definition's metadata, so rewrites are read and written by `TypeDefinitionConverter`.

## JSON Mapping

### Authorization model

| OpenFGA JSON | Interface |
|---|---|
| `schema_version` | `IAuthorizationModel.SchemaVersion` |
| `type_definitions[]` | `IAuthorizationModel.Types` |
| `conditions{name: Condition}` | `IAuthorizationModel.Conditions` |
| `type_definitions[].type` | `ITypeDefinition.Name` |
| `type_definitions[].relations{name: Userset}` | `ITypeDefinition.Relations` (`IRelationDefinition.Name`, `IRelationDefinition.Rewrite`) |
| `type_definitions[].metadata.relations[name].directly_related_user_types[]` | `IDirect.Forms` of the relation's `IDirect` nodes |
| `conditions[name].name`, `conditions[name].expression` | `ICondition.Name`, `ICondition.Expression` |
| `conditions[name].parameters{name: ConditionParamTypeRef}` | `ICondition.Parameters` (`IConditionParameter.Name`, `IConditionParameter.Type`) |
| `type_name`: `TYPE_NAME_BOOL`, `TYPE_NAME_STRING`, `TYPE_NAME_INT`, `TYPE_NAME_UINT`, `TYPE_NAME_DOUBLE`, `TYPE_NAME_DURATION`, `TYPE_NAME_TIMESTAMP`, `TYPE_NAME_IPADDRESS`, `TYPE_NAME_ANY`, `TYPE_NAME_MAP`, `TYPE_NAME_LIST` | `IConditionParameterType.Name`: `bool`, `string`, `int`, `uint`, `double`, `duration`, `timestamp`, `ipaddress`, `any`, `map`, `list` |
| `generic_types[]` | `IConditionParameterType.GenericTypes` |

### Rewrites (`Userset`)

| OpenFGA JSON | Interface |
|---|---|
| `{"this": {}}` | `IDirect` |
| `{"computedUserset": {"relation": r}}` | `IComputedRelation` (`Relation` = `r`) |
| `{"tupleToUserset": {"tupleset": {"relation": t}, "computedUserset": {"relation": r}}}` | `IRelationFrom` (`Relation` = `r`, `Tupleset` = `t`) |
| `{"union": {"child": [...]}}` | `IUnion` |
| `{"intersection": {"child": [...]}}` | `IIntersection` |
| `{"difference": {"base": ..., "subtract": ...}}` | `IDifference` |

### Subject forms (`RelationReference`)

| OpenFGA JSON | Interface |
|---|---|
| `{"type": "user"}` | `ITypeForm` |
| `{"type": "group", "relation": "member"}` | `ISubjectSetForm` |
| `{"type": "user", "wildcard": {}}` | `IWildcardForm` |
| any of the above with a non-empty `"condition": "c"` | `IConditionalForm` wrapping it (`Condition` = `c`) |

### Tuple keys, subjects and objects

| OpenFGA JSON | Interface |
|---|---|
| `user` | `ITuple.Subject` |
| `relation` | `ITuple.Relation` |
| `object` | `ITuple.Object` |
| `condition{name, context}` | `IConditionalTuple.Condition` (`IRelationshipCondition.Name`, `IRelationshipCondition.Context`) |
| `"document:1"` | `IObject` (`Type` before the first `:`, `Id` after it) |
| `"group:eng#member"` | `ISubjectSet` |
| `"user:*"` | `ITypedWildcard` |

## Wire Format

- **Field names.** OpenFGA's JSON is the protobuf JSON mapping of [`openfga/api`](https://github.com/openfga/api/blob/main/openfga/v1/authzmodel.proto). Fields declared with a `json_name` are snake_case (`schema_version`, `type_definitions`, `directly_related_user_types`, `type_name`, `generic_types`); fields without one use the default lowerCamelCase JSON name (`computedUserset`, `tupleToUserset`). The OpenFGA HTTP API and the OpenFGA CLI use the same names, and this package writes exactly those. On read it also accepts the proto names `computed_userset` and `tuple_to_userset`, as OpenFGA's own parser does.
- **CLI vs API.** The two differ only in empty values. The CLI (`fga model transform`) omits them. The API emits them (`"metadata": null`, `"relations": {}`, `"directly_related_user_types": []`, `"condition": ""`, `"module": ""`, `"source_info": null`) and adds the model `id`. Reading accepts both: a `null` value is treated as absent, an empty `relation` or `condition` in a relation reference is treated as absent, and properties the abstractions do not model (`id`, `module`, `source_info`, a condition's `metadata`) are skipped.
- **Writing.** The output follows the CLI's shape, which the `WriteAuthorizationModel` API accepts as is. A type without relations is written without `relations` and `metadata`, a relation without an `IDirect` gets an empty metadata entry (`{}`), and `conditions`, `parameters` and `generic_types` are written only when non-empty. A tuple key is written as `{user, relation, object, condition}`, the shape the `Write` and `Check` APIs take. Its `context` is always written; an absent or `null` `context` reads as an empty object.
- **`IDirect` forms.** On read, every `IDirect` node of a relation gets that relation's `directly_related_user_types` (empty when the metadata has no entry, as in schema 1.0 models). On write, the forms of a relation's `IDirect` nodes become its list. A relation whose `IDirect` nodes have different sets of forms cannot be represented in OpenFGA's JSON, so writing it throws `ArgumentException`.
- **Errors.** Malformed JSON on read throws `JsonException` with a message naming the OpenFGA element: a missing required property, a value of the wrong JSON type, an unknown `type_name`, a malformed subject or object string, or a userset with no operator or more than one. A value that cannot be written throws `ArgumentException`: an implementation outside a closed variant set (`IRewrite`, `ISubjectForm`, `ISubject`), an unknown condition parameter type name, an `IConditionalForm` wrapping another, or the `IDirect` case above.
- **Escaping.** Strings are escaped by the options' `Encoder`. The default encoder escapes characters such as `<` and `+` in CEL expressions as Unicode escape sequences, which OpenFGA reads as the same characters. Set `Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping` to keep them as is, as the CLI does.

## Design Principles

- **Interface-preserving** — converters target abstract interfaces, not concrete types, so any implementation serializes transparently.
- **Internal DTOs** — reading produces internal sealed records; no internal types leak into the public API.
- **OpenFGA wire format** — the JSON matches OpenFGA's own field names and shapes, so documents can be exchanged with an OpenFGA server or the OpenFGA CLI as is.
- **AOT-compatible** — the package is built with `IsAotCompatible` enabled; the converters read and write with `Utf8JsonReader` and `Utf8JsonWriter` and use no reflection-based serialization.

## Dependencies

- [`Pure.Authorization.OpenFGA.Model.Abstractions`](https://github.com/kudima03/Pure.Authorization.OpenFGA.Model.Abstractions/tree/0.1.0-preview.0.1.0) — read-only interfaces for the OpenFGA authorization model, tuples, subjects and objects
- [`Pure.Primitives`](https://github.com/kudima03/Pure.Primitives/tree/3.7.0) — the `IString` implementation behind read values
- [`Pure.Primitives.String.Operations`](https://github.com/kudima03/Pure.Primitives.String.Operations/tree/1.7.0) — slicing and joining of `type:id#relation` strings

## Target Frameworks

- .NET 7
- .NET 8
- .NET 9
- .NET 10

## Installation

```
dotnet add package Pure.Authorization.OpenFGA.Model.Abstractions.Serialization.System
```

## Usage

```csharp
JsonSerializerOptions options = new JsonSerializerOptions();

foreach (JsonConverter converter in new AuthorizationOpenFGAModelAbstractionsConverters())
{
    options.Converters.Add(converter);
}

// An authorization model, e.g. the output of `fga model transform`.
IAuthorizationModel model = JsonSerializer.Deserialize<IAuthorizationModel>(
    File.ReadAllText("model.json"),
    options
)!;
string json = JsonSerializer.Serialize(model, options);

// A tuple key. One with a condition reads as an IConditionalTuple.
ITuple tuple = JsonSerializer.Deserialize<ITuple>(
    """{"user":"group:eng#member","relation":"viewer","object":"document:1"}""",
    options
)!;
ISubjectSet members = (ISubjectSet)tuple.Subject;
```
