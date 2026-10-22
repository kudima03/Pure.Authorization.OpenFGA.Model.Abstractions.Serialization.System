using System.Text.Json;
using System.Text.Json.Serialization;
using Pure.Authorization.OpenFGA.Model.Abstractions.Model;
using Pure.Authorization.OpenFGA.Model.Abstractions.Rewrite;
using Pure.Authorization.OpenFGA.Model.Abstractions.SubjectForm;
using Pure.Primitives.Abstractions.String;
using String = Pure.Primitives.String.String;

namespace Pure.Authorization.OpenFGA.Model.Abstractions.Serialization.System.Tests;

public sealed record TypeDefinitionConverterTests
{
    private readonly JsonSerializerOptions _options;

    public TypeDefinitionConverterTests()
    {
        _options = new JsonSerializerOptions();

        foreach (
            JsonConverter converter in new AuthorizationOpenFGAModelAbstractionsConverters()
        )
        {
            _options.Converters.Add(converter);
        }
    }

    [Fact]
    public void WritesTypeWithoutRelations()
    {
        Assert.Equal(
            /*lang=json,strict*/
            """{"type":"user"}""",
            JsonSerializer.Serialize<ITypeDefinition>(new TypeDouble("user"), _options)
        );
    }

    [Fact]
    public void WritesDirect()
    {
        ITypeDefinition type = new TypeDouble(
            "document",
            new RelationDouble(
                "viewer",
                new DirectDouble(
                    new TypeFormDouble("user"),
                    new SubjectSetFormDouble("group", "member"),
                    new WildcardFormDouble("user"),
                    new ConditionalFormDouble(new TypeFormDouble("user"), "non_expired")
                )
            )
        );

        Assert.Equal(
            /*lang=json,strict*/
            """{"type":"document","relations":{"viewer":{"this":{}}},"metadata":{"relations":{"viewer":{"directly_related_user_types":[{"type":"user"},{"type":"group","relation":"member"},{"type":"user","wildcard":{}},{"type":"user","condition":"non_expired"}]}}}}""",
            JsonSerializer.Serialize(type, _options)
        );
    }

    [Fact]
    public void WritesDirectWithoutForms()
    {
        ITypeDefinition type = new TypeDouble(
            "document",
            new RelationDouble("viewer", new DirectDouble())
        );

        Assert.Equal(
            /*lang=json,strict*/
            """{"type":"document","relations":{"viewer":{"this":{}}},"metadata":{"relations":{"viewer":{"directly_related_user_types":[]}}}}""",
            JsonSerializer.Serialize(type, _options)
        );
    }

    [Fact]
    public void WritesComputedRelation()
    {
        ITypeDefinition type = new TypeDouble(
            "document",
            new RelationDouble("viewer", new ComputedRelationDouble("editor"))
        );

        Assert.Equal(
            /*lang=json,strict*/
            """{"type":"document","relations":{"viewer":{"computedUserset":{"relation":"editor"}}},"metadata":{"relations":{"viewer":{}}}}""",
            JsonSerializer.Serialize(type, _options)
        );
    }

    [Fact]
    public void WritesRelationFrom()
    {
        ITypeDefinition type = new TypeDouble(
            "document",
            new RelationDouble("viewer", new RelationFromDouble("viewer", "parent"))
        );

        Assert.Equal(
            /*lang=json,strict*/
            """{"type":"document","relations":{"viewer":{"tupleToUserset":{"tupleset":{"relation":"parent"},"computedUserset":{"relation":"viewer"}}}},"metadata":{"relations":{"viewer":{}}}}""",
            JsonSerializer.Serialize(type, _options)
        );
    }

    [Fact]
    public void WritesUnionIntersectionAndDifference()
    {
        ITypeDefinition type = new TypeDouble(
            "document",
            new RelationDouble("blocked", new DirectDouble(new TypeFormDouble("user"))),
            new RelationDouble(
                "viewer",
                new DifferenceDouble(
                    new UnionDouble(
                        new DirectDouble(new TypeFormDouble("user")),
                        new ComputedRelationDouble("editor")
                    ),
                    new IntersectionDouble(
                        new ComputedRelationDouble("blocked"),
                        new RelationFromDouble("blocked", "parent")
                    )
                )
            )
        );

        Assert.Equal(
            /*lang=json,strict*/
            """{"type":"document","relations":{"blocked":{"this":{}},"viewer":{"difference":{"base":{"union":{"child":[{"this":{}},{"computedUserset":{"relation":"editor"}}]}},"subtract":{"intersection":{"child":[{"computedUserset":{"relation":"blocked"}},{"tupleToUserset":{"tupleset":{"relation":"parent"},"computedUserset":{"relation":"blocked"}}}]}}}}},"metadata":{"relations":{"blocked":{"directly_related_user_types":[{"type":"user"}]},"viewer":{"directly_related_user_types":[{"type":"user"}]}}}}""",
            JsonSerializer.Serialize(type, _options)
        );
    }

    [Fact]
    public void WritesDirectNodesWithSameFormsOnce()
    {
        ITypeDefinition type = new TypeDouble(
            "document",
            new RelationDouble(
                "viewer",
                new IntersectionDouble(
                    new DirectDouble(
                        new TypeFormDouble("user"),
                        new SubjectSetFormDouble("group", "member")
                    ),
                    new DirectDouble(
                        new SubjectSetFormDouble("group", "member"),
                        new TypeFormDouble("user")
                    )
                )
            )
        );

        Assert.Equal(
            /*lang=json,strict*/
            """{"type":"document","relations":{"viewer":{"intersection":{"child":[{"this":{}},{"this":{}}]}}},"metadata":{"relations":{"viewer":{"directly_related_user_types":[{"type":"user"},{"type":"group","relation":"member"}]}}}}""",
            JsonSerializer.Serialize(type, _options)
        );
    }

    [Fact]
    public void ThrowsOnDirectNodesWithDifferentForms()
    {
        AssertUnrepresentable(
            new UnionDouble(
                new DirectDouble(new TypeFormDouble("user")),
                new DirectDouble(
                    new TypeFormDouble("user"),
                    new WildcardFormDouble("user")
                )
            )
        );
    }

    [Fact]
    public void ThrowsOnDirectNodesWithDifferentConditions()
    {
        AssertUnrepresentable(
            new DifferenceDouble(
                new DirectDouble(
                    new ConditionalFormDouble(new TypeFormDouble("user"), "in_region")
                ),
                new DirectDouble(
                    new ConditionalFormDouble(new TypeFormDouble("user"), "non_expired")
                )
            )
        );
    }

    [Fact]
    public void ThrowsOnUnknownRewriteWrite()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            JsonSerializer.Serialize<ITypeDefinition>(
                new TypeDouble(
                    "document",
                    new RelationDouble("viewer", new UnionDouble(new UnknownRewrite()))
                ),
                _options
            )
        );

        Assert.Equal(
            "Cannot write UnknownRewrite as an OpenFGA userset: it implements none of "
                + "IDirect, IComputedRelation, IRelationFrom, IUnion, IIntersection and "
                + "IDifference.",
            exception.Message
        );
    }

    [Fact]
    public void ThrowsOnUnknownFormWrite()
    {
        _ = Assert.Throws<ArgumentException>(() =>
            JsonSerializer.Serialize<ITypeDefinition>(
                new TypeDouble(
                    "document",
                    new RelationDouble("viewer", new DirectDouble(new UnknownForm()))
                ),
                _options
            )
        );
    }

    [Theory]
    [InlineData( /*lang=json,strict*/
        """{"type":"user"}"""
    )]
    [InlineData( /*lang=json,strict*/
        """{"type":"user","relations":{},"metadata":null}"""
    )]
    [InlineData( /*lang=json,strict*/
        """{"type":"user","relations":null,"metadata":{"relations":{}}}"""
    )]
    public void ReadsTypeWithoutRelations(string json)
    {
        ITypeDefinition type = JsonSerializer.Deserialize<ITypeDefinition>(
            json,
            _options
        )!;

        Assert.Equal("user", type.Name.TextValue);
        Assert.Empty(type.Relations);
    }

    [Fact]
    public void ReadsDirectFormsFromMetadata()
    {
        ITypeDefinition type = JsonSerializer.Deserialize<ITypeDefinition>(
            """
            {
              "type": "document",
              "relations": {
                "viewer": { "this": {} },
                "editor": { "this": {} }
              },
              "metadata": {
                "relations": {
                  "viewer": {
                    "directly_related_user_types": [
                      { "type": "user" },
                      { "type": "group", "relation": "member" },
                      { "type": "user", "wildcard": {} },
                      { "type": "user", "condition": "non_expired" }
                    ]
                  },
                  "editor": {
                    "directly_related_user_types": [{ "type": "user" }]
                  }
                }
              }
            }
            """,
            _options
        )!;

        Assert.Equal("document", type.Name.TextValue);
        Assert.Collection(
            type.Relations,
            viewer =>
            {
                Assert.Equal("viewer", viewer.Name.TextValue);
                Assert.Collection(
                    Assert.IsType<IDirect>(viewer.Rewrite, exactMatch: false).Forms,
                    form => Assert.IsType<ITypeForm>(form, exactMatch: false),
                    form => Assert.IsType<ISubjectSetForm>(form, exactMatch: false),
                    form => Assert.IsType<IWildcardForm>(form, exactMatch: false),
                    form => Assert.IsType<IConditionalForm>(form, exactMatch: false)
                );
            },
            editor =>
            {
                Assert.Equal("editor", editor.Name.TextValue);
                Assert.Equal(
                    "user",
                    Assert
                        .IsType<ITypeForm>(
                            Assert.Single(
                                Assert
                                    .IsType<IDirect>(editor.Rewrite, exactMatch: false)
                                    .Forms
                            ),
                            exactMatch: false
                        )
                        .Type.TextValue
                );
            }
        );
    }

    [Fact]
    public void ReadsMetadataBeforeRelations()
    {
        ITypeDefinition type = JsonSerializer.Deserialize<ITypeDefinition>(
            """
            {
              "metadata": {
                "module": "",
                "source_info": { "file": "model.fga" },
                "relations": {
                  "viewer": {
                    "module": "",
                    "source_info": null,
                    "directly_related_user_types": [{ "type": "user" }]
                  }
                }
              },
              "relations": { "viewer": { "this": {} } },
              "type": "document"
            }
            """,
            _options
        )!;

        IDirect direct = Assert.IsType<IDirect>(
            Assert.Single(type.Relations).Rewrite,
            exactMatch: false
        );
        Assert.Equal(
            "user",
            Assert
                .IsType<ITypeForm>(Assert.Single(direct.Forms), exactMatch: false)
                .Type.TextValue
        );
    }

    [Theory]
    [InlineData( /*lang=json,strict*/
        """{"type":"document","relations":{"viewer":{"this":{}}}}"""
    )]
    [InlineData(
        /*lang=json,strict*/
        """{"type":"document","relations":{"viewer":{"this":{}}},"metadata":{"relations":{"viewer":{}}}}"""
    )]
    [InlineData(
        /*lang=json,strict*/
        """{"type":"document","relations":{"viewer":{"this":{}}},"metadata":{"relations":{"viewer":{"directly_related_user_types":[]}}}}"""
    )]
    [InlineData(
        /*lang=json,strict*/
        """{"type":"document","relations":{"viewer":{"this":{}}},"metadata":{"relations":{"editor":{"directly_related_user_types":[{"type":"user"}]}}}}"""
    )]
    public void ReadsDirectWithoutForms(string json)
    {
        ITypeDefinition type = JsonSerializer.Deserialize<ITypeDefinition>(
            json,
            _options
        )!;

        Assert.Empty(
            Assert
                .IsType<IDirect>(Assert.Single(type.Relations).Rewrite, exactMatch: false)
                .Forms
        );
    }

    [Theory]
    [InlineData( /*lang=json,strict*/
        """{"computedUserset":{"relation":"editor"}}"""
    )]
    [InlineData( /*lang=json,strict*/
        """{"computed_userset":{"object":"","relation":"editor"}}"""
    )]
    public void ReadsComputedRelation(string userset)
    {
        IComputedRelation computed = Assert.IsType<IComputedRelation>(
            ReadRewrite(userset),
            exactMatch: false
        );

        Assert.Equal("editor", computed.Relation.TextValue);
    }

    [Theory]
    [InlineData(
        /*lang=json,strict*/
        """{"tupleToUserset":{"tupleset":{"relation":"parent"},"computedUserset":{"relation":"viewer"}}}"""
    )]
    [InlineData(
        /*lang=json,strict*/
        """{"tuple_to_userset":{"computed_userset":{"object":"","relation":"viewer"},"tupleset":{"object":"","relation":"parent"}}}"""
    )]
    public void ReadsRelationFrom(string userset)
    {
        IRelationFrom from = Assert.IsType<IRelationFrom>(
            ReadRewrite(userset),
            exactMatch: false
        );

        Assert.Equal("viewer", from.Relation.TextValue);
        Assert.Equal("parent", from.Tupleset.TextValue);
    }

    [Fact]
    public void ReadsUnion()
    {
        IUnion union = Assert.IsType<IUnion>(
            ReadRewrite(
                /*lang=json,strict*/
                """{"union":{"child":[{"this":{}},{"computedUserset":{"relation":"editor"}}]}}"""
            ),
            exactMatch: false
        );

        Assert.Collection(
            union.Children,
            child =>
                Assert.Equal(
                    "user",
                    Assert
                        .IsType<ITypeForm>(
                            Assert.Single(
                                Assert.IsType<IDirect>(child, exactMatch: false).Forms
                            ),
                            exactMatch: false
                        )
                        .Type.TextValue
                ),
            child =>
                Assert.Equal(
                    "editor",
                    Assert
                        .IsType<IComputedRelation>(child, exactMatch: false)
                        .Relation.TextValue
                )
        );
    }

    [Fact]
    public void ReadsIntersection()
    {
        IIntersection intersection = Assert.IsType<IIntersection>(
            ReadRewrite(
                /*lang=json,strict*/
                """{"intersection":{"child":[{"computedUserset":{"relation":"a"}},{"computedUserset":{"relation":"b"}}]}}"""
            ),
            exactMatch: false
        );

        Assert.Equal(
            ["a", "b"],
            intersection
                .Children.Cast<IComputedRelation>()
                .Select(child => child.Relation.TextValue)
        );
    }

    [Theory]
    [InlineData( /*lang=json,strict*/
        """{"union":{}}"""
    )]
    [InlineData( /*lang=json,strict*/
        """{"union":{"child":[]}}"""
    )]
    public void ReadsUnionWithoutChildren(string userset)
    {
        Assert.Empty(
            Assert.IsType<IUnion>(ReadRewrite(userset), exactMatch: false).Children
        );
    }

    [Fact]
    public void ReadsDifference()
    {
        IDifference difference = Assert.IsType<IDifference>(
            ReadRewrite(
                /*lang=json,strict*/
                """{"difference":{"subtract":{"computedUserset":{"relation":"blocked"}},"base":{"this":{}}}}"""
            ),
            exactMatch: false
        );

        Assert.Equal(
            "user",
            Assert
                .IsType<ITypeForm>(
                    Assert.Single(
                        Assert.IsType<IDirect>(difference.Base, exactMatch: false).Forms
                    ),
                    exactMatch: false
                )
                .Type.TextValue
        );
        Assert.Equal(
            "blocked",
            Assert
                .IsType<IComputedRelation>(difference.Subtract, exactMatch: false)
                .Relation.TextValue
        );
    }

    [Fact]
    public void ReadsUnknownAndNullUsersetPropertiesAsAbsent()
    {
        IComputedRelation computed = Assert.IsType<IComputedRelation>(
            ReadRewrite(
                /*lang=json,strict*/
                """{"this":null,"union":null,"extra":{"this":{}},"computedUserset":{"relation":"editor"}}"""
            ),
            exactMatch: false
        );

        Assert.Equal("editor", computed.Relation.TextValue);
    }

    [Theory]
    [InlineData(
        /*lang=json,strict*/
        """{"type":"document","relations":{"viewer":{"this":{}}}}""",
        /*lang=json,strict*/
        """{"type":"document","relations":{"viewer":{"this":{}}},"metadata":{"relations":{"viewer":{"directly_related_user_types":[]}}}}"""
    )]
    [InlineData(
        /*lang=json,strict*/
        """{"type":"user","relations":{},"metadata":null}""",
        /*lang=json,strict*/
        """{"type":"user"}"""
    )]
    public void RewritesToCliShape(string json, string expected)
    {
        Assert.Equal(
            expected,
            JsonSerializer.Serialize(
                JsonSerializer.Deserialize<ITypeDefinition>(json, _options),
                _options
            )
        );
    }

    [Theory]
    [InlineData("\"user\"", "Expected a JSON object for the OpenFGA type definition.")]
    [InlineData(
        "{}",
        "The OpenFGA type definition is missing the required property 'type'."
    )]
    [InlineData(
        /*lang=json,strict*/
        """{"type":1}""",
        "Expected a JSON string for the OpenFGA property 'type'."
    )]
    [InlineData(
        /*lang=json,strict*/
        """{"type":"t","relations":[]}""",
        "Expected a JSON object for the OpenFGA property 'relations'."
    )]
    [InlineData(
        /*lang=json,strict*/
        """{"type":"t","relations":{"r":1}}""",
        "Expected a JSON object for the OpenFGA userset."
    )]
    [InlineData(
        /*lang=json,strict*/
        """{"type":"t","relations":{"r":{}}}""",
        "An OpenFGA userset must have exactly one of 'this', 'computedUserset', "
            + "'tupleToUserset', 'union', 'intersection' and 'difference'."
    )]
    [InlineData(
        /*lang=json,strict*/
        """{"type":"t","relations":{"r":{"this":{},"computedUserset":{"relation":"a"}}}}""",
        "An OpenFGA userset must have exactly one of 'this', 'computedUserset', "
            + "'tupleToUserset', 'union', 'intersection' and 'difference'."
    )]
    [InlineData(
        /*lang=json,strict*/
        """{"type":"t","relations":{"r":{"this":true}}}""",
        "Expected a JSON object for the OpenFGA this."
    )]
    [InlineData(
        /*lang=json,strict*/
        """{"type":"t","relations":{"r":{"computedUserset":"a"}}}""",
        "Expected a JSON object for the OpenFGA computedUserset."
    )]
    [InlineData(
        /*lang=json,strict*/
        """{"type":"t","relations":{"r":{"computedUserset":{"object":""}}}}""",
        "The OpenFGA computedUserset is missing the required property 'relation'."
    )]
    [InlineData(
        /*lang=json,strict*/
        """{"type":"t","relations":{"r":{"computedUserset":{"relation":1}}}}""",
        "Expected a JSON string for the OpenFGA property 'relation'."
    )]
    [InlineData(
        /*lang=json,strict*/
        """{"type":"t","relations":{"r":{"tupleToUserset":[]}}}""",
        "Expected a JSON object for the OpenFGA tupleToUserset."
    )]
    [InlineData(
        /*lang=json,strict*/
        """{"type":"t","relations":{"r":{"tupleToUserset":{"computedUserset":{"relation":"a"}}}}}""",
        "The OpenFGA tupleToUserset is missing the required property 'tupleset'."
    )]
    [InlineData(
        /*lang=json,strict*/
        """{"type":"t","relations":{"r":{"tupleToUserset":{"tupleset":{"relation":"a"}}}}}""",
        "The OpenFGA tupleToUserset is missing the required property 'computedUserset'."
    )]
    [InlineData(
        /*lang=json,strict*/
        """{"type":"t","relations":{"r":{"tupleToUserset":{"tupleset":"a","computedUserset":{"relation":"a"}}}}}""",
        "Expected a JSON object for the OpenFGA tupleset."
    )]
    [InlineData(
        /*lang=json,strict*/
        """{"type":"t","relations":{"r":{"tupleToUserset":{"tupleset":{},"computedUserset":{"relation":"a"}}}}}""",
        "The OpenFGA tupleset is missing the required property 'relation'."
    )]
    [InlineData(
        /*lang=json,strict*/
        """{"type":"t","relations":{"r":{"tupleToUserset":{"tupleset":{"relation":"a"},"computedUserset":{}}}}}""",
        "The OpenFGA computedUserset is missing the required property 'relation'."
    )]
    [InlineData(
        /*lang=json,strict*/
        """{"type":"t","relations":{"r":{"union":[]}}}""",
        "Expected a JSON object for the OpenFGA union."
    )]
    [InlineData(
        /*lang=json,strict*/
        """{"type":"t","relations":{"r":{"intersection":1}}}""",
        "Expected a JSON object for the OpenFGA intersection."
    )]
    [InlineData(
        /*lang=json,strict*/
        """{"type":"t","relations":{"r":{"union":{"child":{}}}}}""",
        "Expected a JSON array for the OpenFGA property 'child'."
    )]
    [InlineData(
        /*lang=json,strict*/
        """{"type":"t","relations":{"r":{"difference":"x"}}}""",
        "Expected a JSON object for the OpenFGA difference."
    )]
    [InlineData(
        /*lang=json,strict*/
        """{"type":"t","relations":{"r":{"difference":{"subtract":{"this":{}}}}}}""",
        "The OpenFGA difference is missing the required property 'base'."
    )]
    [InlineData(
        /*lang=json,strict*/
        """{"type":"t","relations":{"r":{"difference":{"base":{"this":{}}}}}}""",
        "The OpenFGA difference is missing the required property 'subtract'."
    )]
    [InlineData(
        /*lang=json,strict*/
        """{"type":"t","metadata":[]}""",
        "Expected a JSON object for the OpenFGA metadata."
    )]
    [InlineData(
        /*lang=json,strict*/
        """{"type":"t","metadata":{"relations":[]}}""",
        "Expected a JSON object for the OpenFGA property 'relations' of the metadata."
    )]
    [InlineData(
        /*lang=json,strict*/
        """{"type":"t","metadata":{"relations":{"r":[]}}}""",
        "Expected a JSON object for the OpenFGA relation metadata."
    )]
    [InlineData(
        /*lang=json,strict*/
        """{"type":"t","metadata":{"relations":{"r":{"directly_related_user_types":{}}}}}""",
        "Expected a JSON array for the OpenFGA property 'directly_related_user_types'."
    )]
    [InlineData(
        /*lang=json,strict*/
        """{"type":"t","metadata":{"relations":{"r":{"directly_related_user_types":[{}]}}}}""",
        "The OpenFGA relation reference is missing the required property 'type'."
    )]
    public void ThrowsOnInvalidJson(string json, string message)
    {
        JsonException exception = Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<ITypeDefinition>(json, _options)
        );

        Assert.Equal(message, exception.Message);
    }

    private IRewrite ReadRewrite(string userset)
    {
        ITypeDefinition type = JsonSerializer.Deserialize<ITypeDefinition>(
            $$"""
            {
              "type": "document",
              "relations": { "viewer": {{userset}} },
              "metadata": {
                "relations": {
                  "viewer": { "directly_related_user_types": [{ "type": "user" }] }
                }
              }
            }
            """,
            _options
        )!;

        IRelationDefinition relation = Assert.Single(type.Relations);
        Assert.Equal("viewer", relation.Name.TextValue);

        return relation.Rewrite;
    }

    private void AssertUnrepresentable(IRewrite rewrite)
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            JsonSerializer.Serialize<ITypeDefinition>(
                new TypeDouble("document", new RelationDouble("viewer", rewrite)),
                _options
            )
        );

        Assert.Equal(
            "Relation 'viewer' has IDirect nodes with different subject forms. OpenFGA "
                + "stores a single directly_related_user_types list per relation, so all "
                + "of its IDirect nodes must have the same forms.",
            exception.Message
        );
    }

    private sealed record TypeDouble : ITypeDefinition
    {
        public TypeDouble(string name, params IRelationDefinition[] relations)
        {
            Name = new String(name);
            Relations = relations;
        }

        public IString Name { get; }

        public IEnumerable<IRelationDefinition> Relations { get; }
    }

    private sealed record RelationDouble : IRelationDefinition
    {
        public RelationDouble(string name, IRewrite rewrite)
        {
            Name = new String(name);
            Rewrite = rewrite;
        }

        public IString Name { get; }

        public IRewrite Rewrite { get; }
    }

    private sealed record DirectDouble : IDirect
    {
        public DirectDouble(params ISubjectForm[] forms)
        {
            Forms = forms;
        }

        public IEnumerable<ISubjectForm> Forms { get; }
    }

    private sealed record ComputedRelationDouble : IComputedRelation
    {
        public ComputedRelationDouble(string relation)
        {
            Relation = new String(relation);
        }

        public IString Relation { get; }
    }

    private sealed record RelationFromDouble : IRelationFrom
    {
        public RelationFromDouble(string relation, string tupleset)
        {
            Relation = new String(relation);
            Tupleset = new String(tupleset);
        }

        public IString Relation { get; }

        public IString Tupleset { get; }
    }

    private sealed record UnionDouble : IUnion
    {
        public UnionDouble(params IRewrite[] children)
        {
            Children = children;
        }

        public IEnumerable<IRewrite> Children { get; }
    }

    private sealed record IntersectionDouble : IIntersection
    {
        public IntersectionDouble(params IRewrite[] children)
        {
            Children = children;
        }

        public IEnumerable<IRewrite> Children { get; }
    }

    private sealed record DifferenceDouble : IDifference
    {
        public DifferenceDouble(IRewrite @base, IRewrite subtract)
        {
            Base = @base;
            Subtract = subtract;
        }

        public IRewrite Base { get; }

        public IRewrite Subtract { get; }
    }

    private sealed record UnknownRewrite : IRewrite;

    private sealed record TypeFormDouble : ITypeForm
    {
        public TypeFormDouble(string type)
        {
            Type = new String(type);
        }

        public IString Type { get; }
    }

    private sealed record SubjectSetFormDouble : ISubjectSetForm
    {
        public SubjectSetFormDouble(string type, string relation)
        {
            Type = new String(type);
            Relation = new String(relation);
        }

        public IString Type { get; }

        public IString Relation { get; }
    }

    private sealed record WildcardFormDouble : IWildcardForm
    {
        public WildcardFormDouble(string type)
        {
            Type = new String(type);
        }

        public IString Type { get; }
    }

    private sealed record ConditionalFormDouble : IConditionalForm
    {
        public ConditionalFormDouble(ISubjectForm form, string condition)
        {
            Form = form;
            Condition = new String(condition);
        }

        public ISubjectForm Form { get; }

        public IString Condition { get; }
    }

    private sealed record UnknownForm : ISubjectForm;
}
