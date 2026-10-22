# Pure.Authorization.OpenFGA.Model.Abstractions.Serialization.System

`System.Text.Json` converters for OpenFGA authorization model abstractions in the **Pure** ecosystem.

[![.NET build & test](https://github.com/kudima03/Pure.Authorization.OpenFGA.Model.Abstractions.Serialization.System/actions/workflows/build-and-test.yml/badge.svg?branch=main)](https://github.com/kudima03/Pure.Authorization.OpenFGA.Model.Abstractions.Serialization.System/actions/workflows/build-and-test.yml)
[![Build and Deploy](https://github.com/kudima03/Pure.Authorization.OpenFGA.Model.Abstractions.Serialization.System/actions/workflows/publish-nuget.yml/badge.svg?branch=main)](https://github.com/kudima03/Pure.Authorization.OpenFGA.Model.Abstractions.Serialization.System/actions/workflows/publish-nuget.yml)
[![NuGet](https://img.shields.io/nuget/v/Pure.Authorization.OpenFGA.Model.Abstractions.Serialization.System)](https://www.nuget.org/packages/Pure.Authorization.OpenFGA.Model.Abstractions.Serialization.System)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

> **Status:** early development. The public API is coming soon; nothing has been published to NuGet yet.

## Overview

`Pure.Authorization.OpenFGA.Model.Abstractions.Serialization.System` will provide `System.Text.Json` converters between the interfaces defined in [`Pure.Authorization.OpenFGA.Model.Abstractions`](https://github.com/kudima03/Pure.Authorization.OpenFGA.Model.Abstractions) and the JSON format used by [OpenFGA](https://openfga.dev). The converters target the abstract interfaces, so any implementation of them (for example [`Pure.Authorization.OpenFGA.Model`](https://github.com/kudima03/Pure.Authorization.OpenFGA.Model)) can be written to and read from OpenFGA's JSON.

## Planned scope

### Authorization model

Reading and writing an OpenFGA authorization model document:

- `schema_version`
- `type_definitions`, each with its `type`, its `relations` and their rewrites (`this`, `computedUserset`, `tupleToUserset`, `union`, `intersection`, `difference`)
- `metadata.relations.<relation>.directly_related_user_types`, including wildcard, userset (`relation`) and conditional (`condition`) entries
- `conditions`, with their name, expression and typed parameters

### Tuple keys

Reading and writing relationship tuple keys (`user`, `relation`, `object`), including an optional `condition` with its `name` and `context`.

### Subject and object strings

Parsing and writing OpenFGA's string forms into the matching abstraction variant:

| String | Variant |
|---|---|
| `type:id` | A concrete object or user |
| `type:id#relation` | A userset |
| `type:*` | A type-bound wildcard |

## Design Principles

- **Interface-preserving** — converters target abstract interfaces, not concrete types, so any implementation serializes transparently.
- **OpenFGA wire format** — the JSON matches OpenFGA's own field names and shapes, so documents can be exchanged with an OpenFGA server or the OpenFGA CLI as-is.
- **AOT-compatible** — the package is built with `IsAotCompatible` enabled.

## Dependencies

- [`Pure.Authorization.OpenFGA.Model.Abstractions`](https://github.com/kudima03/Pure.Authorization.OpenFGA.Model.Abstractions) — read-only interfaces for the OpenFGA authorization model, tuples, subjects and objects (planned)

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

Coming soon.
