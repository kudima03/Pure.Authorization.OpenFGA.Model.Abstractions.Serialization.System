using System.Text.Json;
using System.Text.Json.Serialization;
using Pure.Authorization.OpenFGA.Model.Abstractions.Object;
using Pure.Primitives.Abstractions.String;
using Pure.Primitives.String;
using Pure.Primitives.String.Operations;
using String = Pure.Primitives.String.String;

namespace Pure.Authorization.OpenFGA.Model.Abstractions.Serialization.System;

internal sealed record ObjectJsonModel : IObject
{
    public ObjectJsonModel(IString type, IString id)
    {
        Type = type;
        Id = id;
    }

    public IString Type { get; }

    public IString Id { get; }
}

/// <summary>
/// The OpenFGA string form of an object, <c>type:id</c>. The id is everything after the
/// first colon; it may not be empty, may not be the wildcard <c>*</c> and may not contain
/// <c>#</c>.
/// </summary>
internal sealed record ObjectText
{
    private readonly IString _text;

    public ObjectText(IString text)
    {
        _text = text;
    }

    public bool Valid()
    {
        string text = _text.TextValue;
        int colon = text.IndexOf(':');

        return colon > 0
            && colon < text.Length - 1
            && !text.Contains('#')
            && text[(colon + 1)..] != "*";
    }

    public IObject Value()
    {
        return new ObjectJsonModel(
            new TextBefore(_text, new ColonString()),
            new TextAfter(_text, new ColonString())
        );
    }
}

/// <summary>
/// Converts <see cref="IObject"/> to and from its OpenFGA string form, <c>type:id</c>
/// (e.g. <c>document:1</c>).
/// </summary>
public sealed class ObjectConverter : JsonConverter<IObject>
{
    public override IObject Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        string text = reader.ExpectString("object");
        ObjectText objectText = new ObjectText(new String(text));

        return objectText.Valid()
            ? objectText.Value()
            : throw new JsonException(
                $"'{text}' is not a valid OpenFGA object; expected 'type:id'."
            );
    }

    public override void Write(
        Utf8JsonWriter writer,
        IObject value,
        JsonSerializerOptions options
    )
    {
        writer.WriteStringValue(new ColonJoinedString(value.Type, value.Id).TextValue);
    }
}
