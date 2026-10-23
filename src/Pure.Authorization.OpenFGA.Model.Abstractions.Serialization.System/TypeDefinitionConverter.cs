using System.Buffers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Pure.Authorization.OpenFGA.Model.Abstractions.Model;
using Pure.Authorization.OpenFGA.Model.Abstractions.Rewrite;
using Pure.Authorization.OpenFGA.Model.Abstractions.SubjectForm;
using Pure.Primitives.Abstractions.String;
using String = Pure.Primitives.String.String;

namespace Pure.Authorization.OpenFGA.Model.Abstractions.Serialization.System;

internal sealed record TypeDefinitionJsonModel : ITypeDefinition
{
    public TypeDefinitionJsonModel(
        IString name,
        IEnumerable<IRelationDefinition> relations
    )
    {
        Name = name;
        Relations = relations;
    }

    public IString Name { get; }

    public IEnumerable<IRelationDefinition> Relations { get; }
}

internal sealed record RelationDefinitionJsonModel : IRelationDefinition
{
    public RelationDefinitionJsonModel(IString name, IRewrite rewrite)
    {
        Name = name;
        Rewrite = rewrite;
    }

    public IString Name { get; }

    public IRewrite Rewrite { get; }
}

/// <summary>
/// Converts <see cref="ITypeDefinition"/> to and from an OpenFGA <c>TypeDefinition</c>,
/// <c>{type, relations, metadata}</c>.
/// </summary>
/// <remarks>
/// OpenFGA keeps the subject forms of a directly assignable relation out of its
/// <c>Userset</c>: the rewrite holds a bare <c>this</c>, and the forms are listed in
/// <c>metadata.relations[relation].directly_related_user_types</c>. On read every
/// <see cref="IDirect"/> of a relation gets that relation's list (empty when the metadata
/// has none). On write the forms of a relation's <see cref="IDirect"/> nodes become its
/// list; a relation without one gets an empty metadata entry, <c>{}</c>, as the OpenFGA CLI
/// emits. A relation whose <see cref="IDirect"/> nodes have different sets of forms cannot
/// be represented and throws <see cref="ArgumentException"/>. A type without relations is
/// written without <c>relations</c> and <c>metadata</c>.
/// </remarks>
public sealed class TypeDefinitionConverter : JsonConverter<ITypeDefinition>
{
    private const string Element = "type definition";

    private readonly SubjectFormConverter _forms = new SubjectFormConverter();

    public override ITypeDefinition Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        reader.ExpectObject(Element);

        string? name = null;
        List<IRelationDefinition> relations = [];
        Dictionary<string, IEnumerable<ISubjectForm>> metadata = [];

        while (reader.NextProperty(out string property, "type", "relations", "metadata"))
        {
            switch (property)
            {
                case "type":
                    name = reader.ExpectString("property 'type'");
                    break;
                case "relations":
                    reader.ExpectObject("property 'relations'");

                    while (reader.NextEntry(out string relation))
                    {
                        relations.Add(
                            new RelationDefinitionJsonModel(
                                new String(relation),
                                new UsersetReader(metadata, relation).Read(ref reader)
                            )
                        );
                    }

                    break;
                default:
                    ReadMetadata(ref reader, metadata, options);
                    break;
            }
        }

        return new TypeDefinitionJsonModel(
            new String(
                name ?? throw Utf8JsonReaderExtensions.MissingProperty("type", Element)
            ),
            relations
        );
    }

    public override void Write(
        Utf8JsonWriter writer,
        ITypeDefinition value,
        JsonSerializerOptions options
    )
    {
        writer.WriteStartObject();
        writer.WriteString("type", value.Name.TextValue);

        IRelationDefinition[] relations = [.. value.Relations];

        if (relations.Length > 0)
        {
            List<(string Relation, List<IDirect> Directs)> directs = [];

            writer.WriteStartObject("relations");

            foreach (IRelationDefinition relation in relations)
            {
                List<IDirect> relationDirects = [];
                writer.WritePropertyName(relation.Name.TextValue);
                new UsersetWriter(relationDirects).Write(writer, relation.Rewrite);
                directs.Add((relation.Name.TextValue, relationDirects));
            }

            writer.WriteEndObject();
            writer.WriteStartObject("metadata");
            writer.WriteStartObject("relations");

            foreach ((string relation, List<IDirect> relationDirects) in directs)
            {
                writer.WriteStartObject(relation);
                WriteDirectlyRelatedUserTypes(writer, relation, relationDirects, options);
                writer.WriteEndObject();
            }

            writer.WriteEndObject();
            writer.WriteEndObject();
        }

        writer.WriteEndObject();
    }

    private void ReadMetadata(
        ref Utf8JsonReader reader,
        Dictionary<string, IEnumerable<ISubjectForm>> metadata,
        JsonSerializerOptions options
    )
    {
        reader.ExpectObject("metadata");

        while (reader.NextProperty(out _, "relations"))
        {
            reader.ExpectObject("property 'relations' of the metadata");

            while (reader.NextEntry(out string relation))
            {
                reader.ExpectObject("relation metadata");

                List<ISubjectForm> forms = [];

                while (reader.NextProperty(out _, "directly_related_user_types"))
                {
                    reader.ExpectArray("property 'directly_related_user_types'");

                    while (reader.NextItem())
                    {
                        forms.Add(_forms.Read(ref reader, typeof(ISubjectForm), options));
                    }
                }

                metadata[relation] = forms;
            }
        }
    }

    private void WriteDirectlyRelatedUserTypes(
        Utf8JsonWriter writer,
        string relation,
        List<IDirect> directs,
        JsonSerializerOptions options
    )
    {
        if (directs.Count == 0)
        {
            return;
        }

        ISubjectForm[] forms = [.. directs[0].Forms];
        HashSet<string> keys = Keys(forms, options);

        if (directs.Exists(direct => !keys.SetEquals(Keys(direct.Forms, options))))
        {
            throw new ArgumentException(
                $"Relation '{relation}' has IDirect nodes with different subject forms. "
                    + "OpenFGA stores a single directly_related_user_types list per "
                    + "relation, so all of its IDirect nodes must have the same forms."
            );
        }

        writer.WriteStartArray("directly_related_user_types");

        foreach (ISubjectForm form in forms)
        {
            _forms.Write(writer, form, options);
        }

        writer.WriteEndArray();
    }

    private HashSet<string> Keys(
        IEnumerable<ISubjectForm> forms,
        JsonSerializerOptions options
    )
    {
        return [.. forms.Select(form => Key(form, options))];
    }

    private string Key(ISubjectForm form, JsonSerializerOptions options)
    {
        ArrayBufferWriter<byte> buffer = new ArrayBufferWriter<byte>();

        using (Utf8JsonWriter writer = new Utf8JsonWriter(buffer))
        {
            _forms.Write(writer, form, options);
        }

        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }
}
