using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Pure.Authorization.OpenFGA.Model.Abstractions.Tuple;
using Pure.Primitives.Abstractions.String;
using String = Pure.Primitives.String.String;

namespace Pure.Authorization.OpenFGA.Model.Abstractions.Serialization.System;

internal sealed record RelationshipConditionJsonModel : IRelationshipCondition
{
    public RelationshipConditionJsonModel(IString name, JsonNode context)
    {
        Name = name;
        Context = context;
    }

    public IString Name { get; }

    public JsonNode Context { get; }
}

/// <summary>
/// Converts <see cref="IRelationshipCondition"/> to and from an OpenFGA
/// <c>RelationshipCondition</c>, <c>{name, context}</c>. The <c>context</c> object is
/// kept as a <see cref="JsonNode"/> and written back as is; a missing or <c>null</c>
/// <c>context</c> reads as an empty object.
/// </summary>
public sealed class RelationshipConditionConverter : JsonConverter<IRelationshipCondition>
{
    private const string Element = "relationship condition";

    public override IRelationshipCondition Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        reader.ExpectObject(Element);

        string? name = null;
        JsonNode context = new JsonObject();

        while (reader.NextProperty(out string property, "name", "context"))
        {
            if (property == "name")
            {
                name = reader.ExpectString("property 'name'");
            }
            else
            {
                reader.ExpectObject("property 'context'");
                context = JsonNode.Parse(ref reader)!;
            }
        }

        return new RelationshipConditionJsonModel(
            new String(
                name ?? throw Utf8JsonReaderExtensions.MissingProperty("name", Element)
            ),
            context
        );
    }

    public override void Write(
        Utf8JsonWriter writer,
        IRelationshipCondition value,
        JsonSerializerOptions options
    )
    {
        writer.WriteStartObject();
        writer.WriteString("name", value.Name.TextValue);
        writer.WritePropertyName("context");
        value.Context.WriteTo(writer, options);
        writer.WriteEndObject();
    }
}
