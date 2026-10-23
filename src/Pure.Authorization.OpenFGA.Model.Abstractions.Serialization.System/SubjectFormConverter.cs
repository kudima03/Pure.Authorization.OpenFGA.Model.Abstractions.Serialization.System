using System.Text.Json;
using System.Text.Json.Serialization;
using Pure.Authorization.OpenFGA.Model.Abstractions.SubjectForm;
using Pure.Primitives.Abstractions.String;
using String = Pure.Primitives.String.String;

namespace Pure.Authorization.OpenFGA.Model.Abstractions.Serialization.System;

internal sealed record TypeFormJsonModel : ITypeForm
{
    public TypeFormJsonModel(IString type)
    {
        Type = type;
    }

    public IString Type { get; }
}

internal sealed record SubjectSetFormJsonModel : ISubjectSetForm
{
    public SubjectSetFormJsonModel(IString type, IString relation)
    {
        Type = type;
        Relation = relation;
    }

    public IString Type { get; }

    public IString Relation { get; }
}

internal sealed record WildcardFormJsonModel : IWildcardForm
{
    public WildcardFormJsonModel(IString type)
    {
        Type = type;
    }

    public IString Type { get; }
}

internal sealed record ConditionalFormJsonModel : IConditionalForm
{
    public ConditionalFormJsonModel(ISubjectForm form, IString condition)
    {
        Form = form;
        Condition = condition;
    }

    public ISubjectForm Form { get; }

    public IString Condition { get; }
}

/// <summary>
/// Converts <see cref="ISubjectForm"/> to and from an OpenFGA <c>RelationReference</c>,
/// one entry of <c>directly_related_user_types</c>: <c>{type}</c> is an
/// <see cref="ITypeForm"/>, <c>{type, relation}</c> an <see cref="ISubjectSetForm"/> and
/// <c>{type, wildcard: {}}</c> an <see cref="IWildcardForm"/>. A non-empty
/// <c>condition</c> wraps the form in an <see cref="IConditionalForm"/>.
/// </summary>
public sealed class SubjectFormConverter : JsonConverter<ISubjectForm>
{
    private const string Element = "relation reference";

    public override ISubjectForm Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        reader.ExpectObject(Element);

        string? type = null;
        string relation = string.Empty;
        bool wildcard = false;
        string condition = string.Empty;

        while (
            reader.NextProperty(
                out string property,
                "type",
                "relation",
                "wildcard",
                "condition"
            )
        )
        {
            switch (property)
            {
                case "type":
                    type = reader.ExpectString("property 'type'");
                    break;
                case "relation":
                    relation = reader.ExpectString("property 'relation'");
                    break;
                case "wildcard":
                    reader.ExpectObject("wildcard");
                    reader.Skip();
                    wildcard = true;
                    break;
                default:
                    condition = reader.ExpectString("property 'condition'");
                    break;
            }
        }

        IString typeName = new String(
            type ?? throw Utf8JsonReaderExtensions.MissingProperty("type", Element)
        );

        if (wildcard && relation.Length > 0)
        {
            throw new JsonException(
                "An OpenFGA relation reference cannot have both 'relation' and 'wildcard'."
            );
        }

        ISubjectForm form =
            wildcard ? new WildcardFormJsonModel(typeName)
            : relation.Length > 0
                ? new SubjectSetFormJsonModel(typeName, new String(relation))
            : new TypeFormJsonModel(typeName);

        return condition.Length > 0
            ? new ConditionalFormJsonModel(form, new String(condition))
            : form;
    }

    public override void Write(
        Utf8JsonWriter writer,
        ISubjectForm value,
        JsonSerializerOptions options
    )
    {
        writer.WriteStartObject();

        if (value is IConditionalForm conditional)
        {
            WriteUnconditional(
                writer,
                conditional.Form is IConditionalForm
                    ? throw new ArgumentException(
                        "An IConditionalForm cannot wrap another IConditionalForm: an "
                            + "OpenFGA relation reference carries at most one condition."
                    )
                    : conditional.Form
            );
            writer.WriteString("condition", conditional.Condition.TextValue);
        }
        else
        {
            WriteUnconditional(writer, value);
        }

        writer.WriteEndObject();
    }

    private static void WriteUnconditional(Utf8JsonWriter writer, ISubjectForm form)
    {
        switch (form)
        {
            case ITypeForm type:
                writer.WriteString("type", type.Type.TextValue);
                break;
            case ISubjectSetForm set:
                writer.WriteString("type", set.Type.TextValue);
                writer.WriteString("relation", set.Relation.TextValue);
                break;
            case IWildcardForm wildcard:
                writer.WriteString("type", wildcard.Type.TextValue);
                writer.WriteStartObject("wildcard");
                writer.WriteEndObject();
                break;
            default:
                throw new ArgumentException(
                    $"Cannot write {form.GetType().Name} as an OpenFGA relation reference: "
                        + "it implements none of ITypeForm, ISubjectSetForm, IWildcardForm "
                        + "and IConditionalForm."
                );
        }
    }
}
