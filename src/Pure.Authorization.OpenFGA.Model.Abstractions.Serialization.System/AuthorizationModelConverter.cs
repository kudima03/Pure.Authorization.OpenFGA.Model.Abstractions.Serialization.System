using System.Text.Json;
using System.Text.Json.Serialization;
using Pure.Authorization.OpenFGA.Model.Abstractions.Condition;
using Pure.Authorization.OpenFGA.Model.Abstractions.Model;
using Pure.Primitives.Abstractions.String;
using String = Pure.Primitives.String.String;

namespace Pure.Authorization.OpenFGA.Model.Abstractions.Serialization.System;

internal sealed record AuthorizationModelJsonModel : IAuthorizationModel
{
    public AuthorizationModelJsonModel(
        IString schemaVersion,
        IEnumerable<ITypeDefinition> types,
        IEnumerable<ICondition> conditions
    )
    {
        SchemaVersion = schemaVersion;
        Types = types;
        Conditions = conditions;
    }

    public IString SchemaVersion { get; }

    public IEnumerable<ITypeDefinition> Types { get; }

    public IEnumerable<ICondition> Conditions { get; }
}

/// <summary>
/// Converts <see cref="IAuthorizationModel"/> to and from an OpenFGA
/// <c>AuthorizationModel</c>, <c>{schema_version, type_definitions, conditions}</c>, as
/// written by the OpenFGA CLI and accepted by the <c>WriteAuthorizationModel</c> API.
/// Other properties, such as the <c>id</c> returned by the API, are ignored on read.
/// <c>conditions</c> is written only when the model has conditions.
/// </summary>
public sealed class AuthorizationModelConverter : JsonConverter<IAuthorizationModel>
{
    private const string Element = "authorization model";

    private readonly TypeDefinitionConverter _types = new TypeDefinitionConverter();

    private readonly ConditionConverter _conditions = new ConditionConverter();

    public override IAuthorizationModel Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        reader.ExpectObject(Element);

        string? schemaVersion = null;
        List<ITypeDefinition> types = [];
        List<ICondition> conditions = [];

        while (
            reader.NextProperty(
                out string property,
                "schema_version",
                "type_definitions",
                "conditions"
            )
        )
        {
            switch (property)
            {
                case "schema_version":
                    schemaVersion = reader.ExpectString("property 'schema_version'");
                    break;
                case "type_definitions":
                    reader.ExpectArray("property 'type_definitions'");

                    while (reader.NextItem())
                    {
                        types.Add(
                            _types.Read(ref reader, typeof(ITypeDefinition), options)
                        );
                    }

                    break;
                default:
                    reader.ExpectObject("property 'conditions'");

                    while (reader.NextEntry(out _))
                    {
                        conditions.Add(
                            _conditions.Read(ref reader, typeof(ICondition), options)
                        );
                    }

                    break;
            }
        }

        return new AuthorizationModelJsonModel(
            new String(
                schemaVersion
                    ?? throw Utf8JsonReaderExtensions.MissingProperty(
                        "schema_version",
                        Element
                    )
            ),
            types,
            conditions
        );
    }

    public override void Write(
        Utf8JsonWriter writer,
        IAuthorizationModel value,
        JsonSerializerOptions options
    )
    {
        writer.WriteStartObject();
        writer.WriteString("schema_version", value.SchemaVersion.TextValue);
        writer.WriteStartArray("type_definitions");

        foreach (ITypeDefinition type in value.Types)
        {
            _types.Write(writer, type, options);
        }

        writer.WriteEndArray();

        ICondition[] conditions = [.. value.Conditions];

        if (conditions.Length > 0)
        {
            writer.WriteStartObject("conditions");

            foreach (ICondition condition in conditions)
            {
                writer.WritePropertyName(condition.Name.TextValue);
                _conditions.Write(writer, condition, options);
            }

            writer.WriteEndObject();
        }

        writer.WriteEndObject();
    }
}
