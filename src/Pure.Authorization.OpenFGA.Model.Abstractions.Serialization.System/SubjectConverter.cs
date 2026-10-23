using System.Text.Json;
using System.Text.Json.Serialization;
using Pure.Authorization.OpenFGA.Model.Abstractions.Object;
using Pure.Authorization.OpenFGA.Model.Abstractions.Subject;
using Pure.Primitives.Abstractions.String;
using Pure.Primitives.String;
using Pure.Primitives.String.Operations;
using String = Pure.Primitives.String.String;

namespace Pure.Authorization.OpenFGA.Model.Abstractions.Serialization.System;

internal sealed record SubjectSetJsonModel : ISubjectSet
{
    public SubjectSetJsonModel(IObject @object, IString relation)
    {
        Object = @object;
        Relation = relation;
    }

    public IObject Object { get; }

    public IString Relation { get; }
}

internal sealed record TypedWildcardJsonModel : ITypedWildcard
{
    public TypedWildcardJsonModel(IString type)
    {
        Type = type;
    }

    public IString Type { get; }
}

/// <summary>
/// The OpenFGA string form of a subject: <c>type:*</c> (a typed wildcard),
/// <c>type:id#relation</c> (a subject set) or <c>type:id</c> (an object).
/// </summary>
internal sealed record SubjectText
{
    private readonly IString _text;

    public SubjectText(IString text)
    {
        _text = text;
    }

    public bool Valid()
    {
        string text = _text.TextValue;
        int hash = text.IndexOf('#');

        if (hash < 0)
        {
            return Wildcard() || new ObjectText(_text).Valid();
        }

        string relation = text[(hash + 1)..];

        return new ObjectText(new TextBefore(_text, new HashString())).Valid()
            && relation.Length > 0
            && relation.IndexOfAny([':', '#']) < 0;
    }

    public ISubject Value()
    {
        return _text.TextValue.Contains('#')
                ? new SubjectSetJsonModel(
                    new ObjectText(new TextBefore(_text, new HashString())).Value(),
                    new TextAfter(_text, new HashString())
                )
            : Wildcard()
                ? new TypedWildcardJsonModel(new TextBefore(_text, new ColonString()))
            : new ObjectText(_text).Value();
    }

    private bool Wildcard()
    {
        string text = _text.TextValue;
        int colon = text.IndexOf(':');

        return colon > 0 && text[(colon + 1)..] == "*";
    }
}

/// <summary>
/// Converts <see cref="ISubject"/> to and from its OpenFGA string form (the <c>user</c>
/// of a tuple key): <c>type:*</c> is an <see cref="ITypedWildcard"/>,
/// <c>type:id#relation</c> an <see cref="ISubjectSet"/> and <c>type:id</c> an
/// <see cref="IObject"/>.
/// </summary>
public sealed class SubjectConverter : JsonConverter<ISubject>
{
    public override ISubject Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        string text = reader.ExpectString("subject");
        SubjectText subjectText = new SubjectText(new String(text));

        return subjectText.Valid()
            ? subjectText.Value()
            : throw new JsonException(
                $"'{text}' is not a valid OpenFGA subject; expected 'type:id', "
                    + "'type:id#relation' or 'type:*'."
            );
    }

    public override void Write(
        Utf8JsonWriter writer,
        ISubject value,
        JsonSerializerOptions options
    )
    {
        IString text = value switch
        {
            IObject @object => new ColonJoinedString(@object.Type, @object.Id),
            ISubjectSet set => new ConcatenatedString(
                new ColonJoinedString(set.Object.Type, set.Object.Id),
                new HashString(),
                set.Relation
            ),
            ITypedWildcard wildcard => new ColonJoinedString(
                wildcard.Type,
                new AsteriskString()
            ),
            _ => throw new ArgumentException(
                $"Cannot write {value.GetType().Name} as an OpenFGA subject: it implements "
                    + "none of IObject, ISubjectSet and ITypedWildcard."
            ),
        };

        writer.WriteStringValue(text.TextValue);
    }
}
