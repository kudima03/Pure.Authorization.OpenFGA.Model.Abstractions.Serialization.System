using System.Text.Json;
using System.Text.Json.Serialization;
using Pure.Authorization.OpenFGA.Model.Abstractions.Object;
using Pure.Authorization.OpenFGA.Model.Abstractions.Subject;
using Pure.Authorization.OpenFGA.Model.Abstractions.Tuple;
using Pure.Primitives.Abstractions.String;
using String = Pure.Primitives.String.String;

namespace Pure.Authorization.OpenFGA.Model.Abstractions.Serialization.System;

internal sealed record TupleJsonModel : ITuple
{
    public TupleJsonModel(ISubject subject, IString relation, IObject @object)
    {
        Subject = subject;
        Relation = relation;
        Object = @object;
    }

    public ISubject Subject { get; }

    public IString Relation { get; }

    public IObject Object { get; }
}

internal sealed record ConditionalTupleJsonModel : IConditionalTuple
{
    private readonly ITuple _tuple;

    public ConditionalTupleJsonModel(ITuple tuple, IRelationshipCondition condition)
    {
        _tuple = tuple;
        Condition = condition;
    }

    public ISubject Subject => _tuple.Subject;

    public IString Relation => _tuple.Relation;

    public IObject Object => _tuple.Object;

    public IRelationshipCondition Condition { get; }
}

/// <summary>
/// Converts <see cref="ITuple"/> to and from an OpenFGA <c>TupleKey</c>,
/// <c>{user, relation, object, condition}</c>. A tuple key with a <c>condition</c> reads
/// as an <see cref="IConditionalTuple"/>, and an <see cref="IConditionalTuple"/> is
/// written with its <c>condition</c>.
/// </summary>
public sealed class TupleConverter : JsonConverter<ITuple>
{
    private const string Element = "tuple key";

    private readonly SubjectConverter _subjects = new SubjectConverter();

    private readonly ObjectConverter _objects = new ObjectConverter();

    private readonly RelationshipConditionConverter _conditions =
        new RelationshipConditionConverter();

    public override ITuple Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        reader.ExpectObject(Element);

        ISubject? subject = null;
        string? relation = null;
        IObject? @object = null;
        IRelationshipCondition? condition = null;

        while (
            reader.NextProperty(
                out string property,
                "user",
                "relation",
                "object",
                "condition"
            )
        )
        {
            switch (property)
            {
                case "user":
                    subject = _subjects.Read(ref reader, typeof(ISubject), options);
                    break;
                case "relation":
                    relation = reader.ExpectString("property 'relation'");
                    break;
                case "object":
                    @object = _objects.Read(ref reader, typeof(IObject), options);
                    break;
                default:
                    condition = _conditions.Read(
                        ref reader,
                        typeof(IRelationshipCondition),
                        options
                    );
                    break;
            }
        }

        ITuple tuple = new TupleJsonModel(
            subject ?? throw Utf8JsonReaderExtensions.MissingProperty("user", Element),
            new String(
                relation
                    ?? throw Utf8JsonReaderExtensions.MissingProperty("relation", Element)
            ),
            @object ?? throw Utf8JsonReaderExtensions.MissingProperty("object", Element)
        );

        return condition is null
            ? tuple
            : new ConditionalTupleJsonModel(tuple, condition);
    }

    public override void Write(
        Utf8JsonWriter writer,
        ITuple value,
        JsonSerializerOptions options
    )
    {
        writer.WriteStartObject();
        writer.WritePropertyName("user");
        _subjects.Write(writer, value.Subject, options);
        writer.WriteString("relation", value.Relation.TextValue);
        writer.WritePropertyName("object");
        _objects.Write(writer, value.Object, options);

        if (value is IConditionalTuple conditional)
        {
            writer.WritePropertyName("condition");
            _conditions.Write(writer, conditional.Condition, options);
        }

        writer.WriteEndObject();
    }
}
