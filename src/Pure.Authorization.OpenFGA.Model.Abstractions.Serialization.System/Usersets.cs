using System.Text.Json;
using Pure.Authorization.OpenFGA.Model.Abstractions.Rewrite;
using Pure.Authorization.OpenFGA.Model.Abstractions.SubjectForm;
using Pure.Primitives.Abstractions.String;
using String = Pure.Primitives.String.String;

namespace Pure.Authorization.OpenFGA.Model.Abstractions.Serialization.System;

/// <summary>
/// An OpenFGA <c>this</c>. Its forms are the <c>directly_related_user_types</c> listed
/// for its relation in the type definition's metadata, which may come after the relation
/// in the document; the lookup is deferred until the forms are read.
/// </summary>
internal sealed record DirectJsonModel : IDirect
{
    private readonly IReadOnlyDictionary<string, IEnumerable<ISubjectForm>> _metadata;

    private readonly string _relation;

    public DirectJsonModel(
        IReadOnlyDictionary<string, IEnumerable<ISubjectForm>> metadata,
        string relation
    )
    {
        _metadata = metadata;
        _relation = relation;
    }

    public IEnumerable<ISubjectForm> Forms => _metadata.GetValueOrDefault(_relation, []);
}

internal sealed record ComputedRelationJsonModel : IComputedRelation
{
    public ComputedRelationJsonModel(IString relation)
    {
        Relation = relation;
    }

    public IString Relation { get; }
}

internal sealed record RelationFromJsonModel : IRelationFrom
{
    public RelationFromJsonModel(IString relation, IString tupleset)
    {
        Relation = relation;
        Tupleset = tupleset;
    }

    public IString Relation { get; }

    public IString Tupleset { get; }
}

internal sealed record UnionJsonModel : IUnion
{
    public UnionJsonModel(IEnumerable<IRewrite> children)
    {
        Children = children;
    }

    public IEnumerable<IRewrite> Children { get; }
}

internal sealed record IntersectionJsonModel : IIntersection
{
    public IntersectionJsonModel(IEnumerable<IRewrite> children)
    {
        Children = children;
    }

    public IEnumerable<IRewrite> Children { get; }
}

internal sealed record DifferenceJsonModel : IDifference
{
    public DifferenceJsonModel(IRewrite @base, IRewrite subtract)
    {
        Base = @base;
        Subtract = subtract;
    }

    public IRewrite Base { get; }

    public IRewrite Subtract { get; }
}

/// <summary>
/// Reads the OpenFGA <c>Userset</c> of one relation into an <see cref="IRewrite"/> tree.
/// Both the JSON names (<c>computedUserset</c>, <c>tupleToUserset</c>) and the proto
/// names (<c>computed_userset</c>, <c>tuple_to_userset</c>) are accepted.
/// </summary>
internal sealed record UsersetReader
{
    private const string OneOf =
        "An OpenFGA userset must have exactly one of 'this', 'computedUserset', "
        + "'tupleToUserset', 'union', 'intersection' and 'difference'.";

    private readonly IReadOnlyDictionary<string, IEnumerable<ISubjectForm>> _metadata;

    private readonly string _relation;

    public UsersetReader(
        IReadOnlyDictionary<string, IEnumerable<ISubjectForm>> metadata,
        string relation
    )
    {
        _metadata = metadata;
        _relation = relation;
    }

    public IRewrite Read(ref Utf8JsonReader reader)
    {
        reader.ExpectObject("userset");

        IRewrite? rewrite = null;

        while (
            reader.NextProperty(
                out string property,
                "this",
                "computedUserset",
                "computed_userset",
                "tupleToUserset",
                "tuple_to_userset",
                "union",
                "intersection",
                "difference"
            )
        )
        {
            IRewrite next = property switch
            {
                "this" => Direct(ref reader),
                "computedUserset" or "computed_userset" => new ComputedRelationJsonModel(
                    new String(Relation(ref reader, "computedUserset"))
                ),
                "tupleToUserset" or "tuple_to_userset" => RelationFrom(ref reader),
                "union" => new UnionJsonModel(Children(ref reader, "union")),
                "intersection" => new IntersectionJsonModel(
                    Children(ref reader, "intersection")
                ),
                _ => Difference(ref reader),
            };

            rewrite = rewrite is null ? next : throw new JsonException(OneOf);
        }

        return rewrite ?? throw new JsonException(OneOf);
    }

    private static string Relation(ref Utf8JsonReader reader, string element)
    {
        reader.ExpectObject(element);

        string? relation = null;

        while (reader.NextProperty(out _, "relation"))
        {
            relation = reader.ExpectString("property 'relation'");
        }

        return relation
            ?? throw Utf8JsonReaderExtensions.MissingProperty("relation", element);
    }

    private static IRewrite RelationFrom(ref Utf8JsonReader reader)
    {
        reader.ExpectObject("tupleToUserset");

        string? tupleset = null;
        string? computed = null;

        while (
            reader.NextProperty(
                out string property,
                "tupleset",
                "computedUserset",
                "computed_userset"
            )
        )
        {
            if (property == "tupleset")
            {
                tupleset = Relation(ref reader, "tupleset");
            }
            else
            {
                computed = Relation(ref reader, "computedUserset");
            }
        }

        return new RelationFromJsonModel(
            new String(
                computed
                    ?? throw Utf8JsonReaderExtensions.MissingProperty(
                        "computedUserset",
                        "tupleToUserset"
                    )
            ),
            new String(
                tupleset
                    ?? throw Utf8JsonReaderExtensions.MissingProperty(
                        "tupleset",
                        "tupleToUserset"
                    )
            )
        );
    }

    private IRewrite Direct(ref Utf8JsonReader reader)
    {
        reader.ExpectObject("this");
        reader.Skip();

        return new DirectJsonModel(_metadata, _relation);
    }

    private List<IRewrite> Children(ref Utf8JsonReader reader, string element)
    {
        reader.ExpectObject(element);

        List<IRewrite> children = [];

        while (reader.NextProperty(out _, "child"))
        {
            reader.ExpectArray("property 'child'");

            while (reader.NextItem())
            {
                children.Add(Read(ref reader));
            }
        }

        return children;
    }

    private IRewrite Difference(ref Utf8JsonReader reader)
    {
        reader.ExpectObject("difference");

        IRewrite? @base = null;
        IRewrite? subtract = null;

        while (reader.NextProperty(out string property, "base", "subtract"))
        {
            if (property == "base")
            {
                @base = Read(ref reader);
            }
            else
            {
                subtract = Read(ref reader);
            }
        }

        return new DifferenceJsonModel(
            @base ?? throw Utf8JsonReaderExtensions.MissingProperty("base", "difference"),
            subtract
                ?? throw Utf8JsonReaderExtensions.MissingProperty(
                    "subtract",
                    "difference"
                )
        );
    }
}

/// <summary>
/// Writes an <see cref="IRewrite"/> tree as an OpenFGA <c>Userset</c>, collecting the
/// <see cref="IDirect"/> nodes it meets so that their forms can be written to the
/// relation's metadata.
/// </summary>
internal sealed record UsersetWriter
{
    private readonly ICollection<IDirect> _directs;

    public UsersetWriter(ICollection<IDirect> directs)
    {
        _directs = directs;
    }

    public void Write(Utf8JsonWriter writer, IRewrite rewrite)
    {
        writer.WriteStartObject();

        switch (rewrite)
        {
            case IDirect direct:
                _directs.Add(direct);
                writer.WriteStartObject("this");
                writer.WriteEndObject();
                break;
            case IComputedRelation computed:
                WriteRelation(writer, "computedUserset", computed.Relation);
                break;
            case IRelationFrom from:
                writer.WriteStartObject("tupleToUserset");
                WriteRelation(writer, "tupleset", from.Tupleset);
                WriteRelation(writer, "computedUserset", from.Relation);
                writer.WriteEndObject();
                break;
            case IUnion union:
                WriteChildren(writer, "union", union.Children);
                break;
            case IIntersection intersection:
                WriteChildren(writer, "intersection", intersection.Children);
                break;
            case IDifference difference:
                writer.WriteStartObject("difference");
                writer.WritePropertyName("base");
                Write(writer, difference.Base);
                writer.WritePropertyName("subtract");
                Write(writer, difference.Subtract);
                writer.WriteEndObject();
                break;
            default:
                throw new ArgumentException(
                    $"Cannot write {rewrite.GetType().Name} as an OpenFGA userset: it "
                        + "implements none of IDirect, IComputedRelation, IRelationFrom, "
                        + "IUnion, IIntersection and IDifference."
                );
        }

        writer.WriteEndObject();
    }

    private static void WriteRelation(
        Utf8JsonWriter writer,
        string property,
        IString relation
    )
    {
        writer.WriteStartObject(property);
        writer.WriteString("relation", relation.TextValue);
        writer.WriteEndObject();
    }

    private void WriteChildren(
        Utf8JsonWriter writer,
        string property,
        IEnumerable<IRewrite> children
    )
    {
        writer.WriteStartObject(property);
        writer.WriteStartArray("child");

        foreach (IRewrite child in children)
        {
            Write(writer, child);
        }

        writer.WriteEndArray();
        writer.WriteEndObject();
    }
}
