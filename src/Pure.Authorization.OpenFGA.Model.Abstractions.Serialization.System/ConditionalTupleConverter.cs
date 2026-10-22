using System.Text.Json;
using System.Text.Json.Serialization;
using Pure.Authorization.OpenFGA.Model.Abstractions.Tuple;

namespace Pure.Authorization.OpenFGA.Model.Abstractions.Serialization.System;

/// <summary>
/// Converts <see cref="IConditionalTuple"/> to and from an OpenFGA <c>TupleKey</c> that
/// carries a <c>condition</c>. Reading a tuple key without one fails.
/// </summary>
public sealed class ConditionalTupleConverter : JsonConverter<IConditionalTuple>
{
    private readonly TupleConverter _tuples = new TupleConverter();

    public override IConditionalTuple Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        return _tuples.Read(ref reader, typeof(ITuple), options) as IConditionalTuple
            ?? throw Utf8JsonReaderExtensions.MissingProperty(
                "condition",
                "conditional tuple key"
            );
    }

    public override void Write(
        Utf8JsonWriter writer,
        IConditionalTuple value,
        JsonSerializerOptions options
    )
    {
        _tuples.Write(writer, value, options);
    }
}
