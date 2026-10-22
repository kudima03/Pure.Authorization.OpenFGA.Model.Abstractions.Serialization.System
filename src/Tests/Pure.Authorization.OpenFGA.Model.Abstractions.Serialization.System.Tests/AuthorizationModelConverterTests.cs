using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Pure.Authorization.OpenFGA.Model.Abstractions.Condition;
using Pure.Authorization.OpenFGA.Model.Abstractions.Model;
using Pure.Authorization.OpenFGA.Model.Abstractions.Rewrite;
using Pure.Authorization.OpenFGA.Model.Abstractions.SubjectForm;
using Pure.Primitives.Abstractions.String;
using String = Pure.Primitives.String.String;

namespace Pure.Authorization.OpenFGA.Model.Abstractions.Serialization.System.Tests;

public sealed record AuthorizationModelConverterTests
{
    private readonly JsonSerializerOptions _options;

    public AuthorizationModelConverterTests()
    {
        _options = new JsonSerializerOptions();

        foreach (
            JsonConverter converter in new AuthorizationOpenFGAModelAbstractionsConverters()
        )
        {
            _options.Converters.Add(converter);
        }

        _options.WriteIndented = true;
        _options.NewLine = "\n";
        _options.Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
    }

    [Fact]
    public void RoundTripsCliModel()
    {
        string fixture = Fixture();

        IAuthorizationModel model = JsonSerializer.Deserialize<IAuthorizationModel>(
            fixture,
            _options
        )!;
        string written = JsonSerializer.Serialize(model, _options);
        string rewritten = JsonSerializer.Serialize(
            JsonSerializer.Deserialize<IAuthorizationModel>(written, _options),
            _options
        );

        Assert.True(
            JsonNode.DeepEquals(JsonNode.Parse(fixture), JsonNode.Parse(written))
        );
        Assert.Equal(written, rewritten);
    }

    [Fact]
    public void ReadsCliModel()
    {
        IAuthorizationModel model = JsonSerializer.Deserialize<IAuthorizationModel>(
            Fixture(),
            _options
        )!;

        Assert.Equal("1.1", model.SchemaVersion.TextValue);
        Assert.Empty(model.Conditions);
        Assert.Equal(
            [
                "user",
                "client",
                "organization",
                "group",
                "datasource",
                "schema",
                "table",
                "column",
                "folder",
                "chart",
                "diagram",
                "query",
                "workspace",
                "widget",
            ],
            model.Types.Select(type => type.Name.TextValue)
        );

        ITypeDefinition table = model.Types.Single(type =>
            type.Name.TextValue == "table"
        );
        IDifference canRead = Assert.IsType<IDifference>(
            table
                .Relations.Single(relation => relation.Name.TextValue == "can_read")
                .Rewrite,
            exactMatch: false
        );
        Assert.Equal(
            "reader",
            Assert
                .IsType<IComputedRelation>(canRead.Base, exactMatch: false)
                .Relation.TextValue
        );
        Assert.Equal(
            "blocked",
            Assert
                .IsType<IComputedRelation>(canRead.Subtract, exactMatch: false)
                .Relation.TextValue
        );

        IUnion reader = Assert.IsType<IUnion>(
            table
                .Relations.Single(relation => relation.Name.TextValue == "reader")
                .Rewrite,
            exactMatch: false
        );
        Assert.Collection(
            reader.Children,
            child =>
                Assert.Equal(
                    ["user", "group#member", "organization#member"],
                    Assert.IsType<IDirect>(child, exactMatch: false).Forms.Select(Text)
                ),
            child =>
            {
                IRelationFrom from = Assert.IsType<IRelationFrom>(
                    child,
                    exactMatch: false
                );
                Assert.Equal("reader", from.Relation.TextValue);
                Assert.Equal("parent", from.Tupleset.TextValue);
            },
            child =>
                Assert.Equal(
                    "steward",
                    Assert
                        .IsType<IComputedRelation>(child, exactMatch: false)
                        .Relation.TextValue
                )
        );
    }

    [Fact]
    public void ReadsApiResponseModel()
    {
        IAuthorizationModel model = JsonSerializer.Deserialize<IAuthorizationModel>(
            """
            {
              "id": "01HVMMBCMGZNT3SED4Z17ECXCA",
              "schema_version": "1.1",
              "type_definitions": [
                { "type": "user", "relations": {}, "metadata": null },
                {
                  "type": "document",
                  "relations": { "viewer": { "this": {} } },
                  "metadata": {
                    "relations": {
                      "viewer": {
                        "directly_related_user_types": [
                          { "type": "user", "condition": "non_expired" }
                        ],
                        "module": "",
                        "source_info": null
                      }
                    },
                    "module": "",
                    "source_info": null
                  }
                }
              ],
              "conditions": {
                "non_expired": {
                  "name": "non_expired",
                  "expression": "current_time < expires_at",
                  "parameters": {
                    "current_time": { "type_name": "TYPE_NAME_TIMESTAMP", "generic_types": [] },
                    "expires_at": { "type_name": "TYPE_NAME_TIMESTAMP", "generic_types": [] }
                  },
                  "metadata": null
                }
              }
            }
            """,
            _options
        )!;

        Assert.Equal("1.1", model.SchemaVersion.TextValue);
        Assert.Equal(
            ["user", "document"],
            model.Types.Select(type => type.Name.TextValue)
        );

        IConditionalForm form = Assert.IsType<IConditionalForm>(
            Assert.Single(
                Assert
                    .IsType<IDirect>(
                        Assert.Single(model.Types.Last().Relations).Rewrite,
                        exactMatch: false
                    )
                    .Forms
            ),
            exactMatch: false
        );
        Assert.Equal("non_expired", form.Condition.TextValue);

        ICondition condition = Assert.Single(model.Conditions);
        Assert.Equal("non_expired", condition.Name.TextValue);
        Assert.Equal("current_time < expires_at", condition.Expression.TextValue);
        Assert.Equal(
            ["current_time", "expires_at"],
            condition.Parameters.Select(parameter => parameter.Name.TextValue)
        );
    }

    [Theory]
    [InlineData( /*lang=json,strict*/
        """{"schema_version":"1.1"}"""
    )]
    [InlineData( /*lang=json,strict*/
        """{"schema_version":"1.1","type_definitions":null,"conditions":null}"""
    )]
    [InlineData( /*lang=json,strict*/
        """{"schema_version":"1.1","type_definitions":[],"conditions":{}}"""
    )]
    public void ReadsEmptyModel(string json)
    {
        IAuthorizationModel model = JsonSerializer.Deserialize<IAuthorizationModel>(
            json,
            _options
        )!;

        Assert.Equal("1.1", model.SchemaVersion.TextValue);
        Assert.Empty(model.Types);
        Assert.Empty(model.Conditions);
    }

    [Fact]
    public void Write()
    {
        IAuthorizationModel model = new ModelDouble(
            "1.1",
            [
                new TypeDouble("user"),
                new TypeDouble(
                    "document",
                    new RelationDouble(
                        "viewer",
                        new DirectDouble(
                            new ConditionalFormDouble(
                                new TypeFormDouble("user"),
                                "non_expired"
                            )
                        )
                    )
                ),
            ],
            [
                new ConditionDouble(
                    "non_expired",
                    "current_time < expires_at",
                    new ParameterDouble("current_time", "timestamp"),
                    new ParameterDouble("expires_at", "timestamp")
                ),
            ]
        );

        Assert.Equal(
            /*lang=json,strict*/
            """
            {
              "schema_version": "1.1",
              "type_definitions": [
                {
                  "type": "user"
                },
                {
                  "type": "document",
                  "relations": {
                    "viewer": {
                      "this": {}
                    }
                  },
                  "metadata": {
                    "relations": {
                      "viewer": {
                        "directly_related_user_types": [
                          {
                            "type": "user",
                            "condition": "non_expired"
                          }
                        ]
                      }
                    }
                  }
                }
              ],
              "conditions": {
                "non_expired": {
                  "name": "non_expired",
                  "expression": "current_time < expires_at",
                  "parameters": {
                    "current_time": {
                      "type_name": "TYPE_NAME_TIMESTAMP"
                    },
                    "expires_at": {
                      "type_name": "TYPE_NAME_TIMESTAMP"
                    }
                  }
                }
              }
            }
            """,
            JsonSerializer.Serialize(model, _options)
        );
    }

    [Fact]
    public void WritesWithoutConditions()
    {
        Assert.Equal(
            /*lang=json,strict*/
            """
            {
              "schema_version": "1.1",
              "type_definitions": []
            }
            """,
            JsonSerializer.Serialize<IAuthorizationModel>(
                new ModelDouble("1.1", [], []),
                _options
            )
        );
    }

    [Theory]
    [InlineData("[]", "Expected a JSON object for the OpenFGA authorization model.")]
    [InlineData(
        /*lang=json,strict*/
        """{"type_definitions":[]}""",
        "The OpenFGA authorization model is missing the required property 'schema_version'."
    )]
    [InlineData(
        /*lang=json,strict*/
        """{"schema_version":1.1}""",
        "Expected a JSON string for the OpenFGA property 'schema_version'."
    )]
    [InlineData(
        /*lang=json,strict*/
        """{"schema_version":"1.1","type_definitions":{}}""",
        "Expected a JSON array for the OpenFGA property 'type_definitions'."
    )]
    [InlineData(
        /*lang=json,strict*/
        """{"schema_version":"1.1","conditions":[]}""",
        "Expected a JSON object for the OpenFGA property 'conditions'."
    )]
    [InlineData(
        /*lang=json,strict*/
        """{"schema_version":"1.1","type_definitions":[1]}""",
        "Expected a JSON object for the OpenFGA type definition."
    )]
    [InlineData(
        /*lang=json,strict*/
        """{"schema_version":"1.1","conditions":{"c":1}}""",
        "Expected a JSON object for the OpenFGA condition."
    )]
    public void ThrowsOnInvalidJson(string json, string message)
    {
        JsonException exception = Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<IAuthorizationModel>(json, _options)
        );

        Assert.Equal(message, exception.Message);
    }

    private static string Fixture()
    {
        using Stream stream =
            typeof(AuthorizationModelConverterTests).Assembly.GetManifestResourceStream(
                "model.json"
            )!;
        using StreamReader reader = new StreamReader(stream);

        return reader.ReadToEnd();
    }

    private static string Text(ISubjectForm form)
    {
        return form switch
        {
            ITypeForm type => type.Type.TextValue,
            ISubjectSetForm set => $"{set.Type.TextValue}#{set.Relation.TextValue}",
            _ => throw new ArgumentException(form.GetType().Name),
        };
    }

    private sealed record ModelDouble : IAuthorizationModel
    {
        public ModelDouble(
            string schemaVersion,
            IEnumerable<ITypeDefinition> types,
            IEnumerable<ICondition> conditions
        )
        {
            SchemaVersion = new String(schemaVersion);
            Types = types;
            Conditions = conditions;
        }

        public IString SchemaVersion { get; }

        public IEnumerable<ITypeDefinition> Types { get; }

        public IEnumerable<ICondition> Conditions { get; }
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

    private sealed record TypeFormDouble : ITypeForm
    {
        public TypeFormDouble(string type)
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

    private sealed record ConditionDouble : ICondition
    {
        public ConditionDouble(
            string name,
            string expression,
            params IConditionParameter[] parameters
        )
        {
            Name = new String(name);
            Expression = new String(expression);
            Parameters = parameters;
        }

        public IString Name { get; }

        public IString Expression { get; }

        public IEnumerable<IConditionParameter> Parameters { get; }
    }

    private sealed record ParameterDouble : IConditionParameter
    {
        public ParameterDouble(string name, string type)
        {
            Name = new String(name);
            Type = new ParameterTypeDouble(type);
        }

        public IString Name { get; }

        public IConditionParameterType Type { get; }
    }

    private sealed record ParameterTypeDouble : IConditionParameterType
    {
        public ParameterTypeDouble(string name)
        {
            Name = new String(name);
            GenericTypes = [];
        }

        public IString Name { get; }

        public IEnumerable<IConditionParameterType> GenericTypes { get; }
    }
}
