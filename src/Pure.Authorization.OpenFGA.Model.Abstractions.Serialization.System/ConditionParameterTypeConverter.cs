using System.Text.Json;
using System.Text.Json.Serialization;
using Pure.Authorization.OpenFGA.Model.Abstractions.Condition;
using Pure.Primitives.Abstractions.String;
using String = Pure.Primitives.String.String;

namespace Pure.Authorization.OpenFGA.Model.Abstractions.Serialization.System;

internal sealed record ConditionParameterTypeJsonModel : IConditionParameterType
{
    public ConditionParameterTypeJsonModel(
        IString name,
        IEnumerable<IConditionParameterType> genericTypes
    )
    {
        Name = name;
        GenericTypes = genericTypes;
    }

    public IString Name { get; }

    public IEnumerable<IConditionParameterType> GenericTypes { get; }
}

/// <summary>
/// Converts <see cref="IConditionParameterType"/> to and from an OpenFGA
/// <c>ConditionParamTypeRef</c>, <c>{type_name, generic_types}</c>. The
/// <c>type_name</c> enum value <c>TYPE_NAME_&lt;NAME&gt;</c> maps to the lower-case
/// <see cref="IConditionParameterType.Name"/> (e.g. <c>TYPE_NAME_IPADDRESS</c> to
/// <c>ipaddress</c>).
/// </summary>
public sealed class ConditionParameterTypeConverter
    : JsonConverter<IConditionParameterType>
{
    private const string Element = "condition parameter type";

    private const string Prefix = "TYPE_NAME_";

    private static readonly string[] Names =
    [
        "any",
        "bool",
        "string",
        "int",
        "uint",
        "double",
        "duration",
        "timestamp",
        "map",
        "list",
        "ipaddress",
    ];

    public override IConditionParameterType Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        reader.ExpectObject(Element);

        string? typeName = null;
        List<IConditionParameterType> genericTypes = [];

        while (reader.NextProperty(out string property, "type_name", "generic_types"))
        {
            if (property == "type_name")
            {
                typeName = reader.ExpectString("property 'type_name'");
            }
            else
            {
                reader.ExpectArray("property 'generic_types'");

                while (reader.NextItem())
                {
                    genericTypes.Add(Read(ref reader, typeToConvert, options));
                }
            }
        }

        string enumValue =
            typeName
            ?? throw Utf8JsonReaderExtensions.MissingProperty("type_name", Element);

        string name =
            Names.FirstOrDefault(candidate =>
                Prefix + candidate.ToUpperInvariant() == enumValue
            )
            ?? throw new JsonException(
                $"'{enumValue}' is not a supported OpenFGA condition parameter type_name."
            );

        return new ConditionParameterTypeJsonModel(new String(name), genericTypes);
    }

    public override void Write(
        Utf8JsonWriter writer,
        IConditionParameterType value,
        JsonSerializerOptions options
    )
    {
        string name = value.Name.TextValue;

        if (!Names.Contains(name))
        {
            throw new ArgumentException(
                $"'{name}' is not an OpenFGA condition parameter type name; expected one of "
                    + $"{string.Join(", ", Names)}."
            );
        }

        writer.WriteStartObject();
        writer.WriteString("type_name", Prefix + name.ToUpperInvariant());

        IConditionParameterType[] genericTypes = [.. value.GenericTypes];

        if (genericTypes.Length > 0)
        {
            writer.WriteStartArray("generic_types");

            foreach (IConditionParameterType genericType in genericTypes)
            {
                Write(writer, genericType, options);
            }

            writer.WriteEndArray();
        }

        writer.WriteEndObject();
    }
}
