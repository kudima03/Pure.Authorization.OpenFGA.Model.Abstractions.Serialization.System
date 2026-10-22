using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Pure.Authorization.OpenFGA.Model.Abstractions.Condition;
using Pure.Primitives.Abstractions.String;
using String = Pure.Primitives.String.String;

namespace Pure.Authorization.OpenFGA.Model.Abstractions.Serialization.System.Tests;

public sealed record ConditionConverterTests
{
    private readonly JsonSerializerOptions _options;

    public ConditionConverterTests()
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
    public void Write()
    {
        ICondition condition = new ConditionDouble(
            "non_expired",
            "current_time < grant_time + duration",
            new ParameterDouble("current_time", new ParameterTypeDouble("timestamp")),
            new ParameterDouble(
                "allowed",
                new ParameterTypeDouble("list", new ParameterTypeDouble("string"))
            )
        );

        Assert.Equal(
            /*lang=json,strict*/
            """
            {
              "name": "non_expired",
              "expression": "current_time < grant_time + duration",
              "parameters": {
                "current_time": {
                  "type_name": "TYPE_NAME_TIMESTAMP"
                },
                "allowed": {
                  "type_name": "TYPE_NAME_LIST",
                  "generic_types": [
                    {
                      "type_name": "TYPE_NAME_STRING"
                    }
                  ]
                }
              }
            }
            """,
            JsonSerializer.Serialize(condition, _options)
        );
    }

    [Fact]
    public void WritesWithoutParameters()
    {
        Assert.Equal(
            /*lang=json,strict*/
            """
            {
              "name": "always",
              "expression": "true"
            }
            """,
            JsonSerializer.Serialize<ICondition>(
                new ConditionDouble("always", "true"),
                _options
            )
        );
    }

    [Fact]
    public void Read()
    {
        ICondition condition = JsonSerializer.Deserialize<ICondition>(
            """
            {
              "name": "non_expired",
              "expression": "current_time < grant_time + duration",
              "parameters": {
                "current_time": { "type_name": "TYPE_NAME_TIMESTAMP" },
                "unused": null,
                "allowed": {
                  "type_name": "TYPE_NAME_LIST",
                  "generic_types": [{ "type_name": "TYPE_NAME_STRING" }]
                }
              },
              "metadata": { "module": "", "source_info": { "file": "model.fga" } }
            }
            """,
            _options
        )!;

        Assert.Equal("non_expired", condition.Name.TextValue);
        Assert.Equal(
            "current_time < grant_time + duration",
            condition.Expression.TextValue
        );
        Assert.Collection(
            condition.Parameters,
            parameter =>
            {
                Assert.Equal("current_time", parameter.Name.TextValue);
                Assert.Equal("timestamp", parameter.Type.Name.TextValue);
            },
            parameter =>
            {
                Assert.Equal("allowed", parameter.Name.TextValue);
                Assert.Equal("list", parameter.Type.Name.TextValue);
                Assert.Equal(
                    "string",
                    Assert.Single(parameter.Type.GenericTypes).Name.TextValue
                );
            }
        );
    }

    [Theory]
    [InlineData( /*lang=json,strict*/
        """{"name":"always","expression":"true"}"""
    )]
    [InlineData( /*lang=json,strict*/
        """{"name":"always","expression":"true","parameters":null}"""
    )]
    [InlineData( /*lang=json,strict*/
        """{"name":"always","expression":"true","parameters":{}}"""
    )]
    public void ReadsWithoutParameters(string json)
    {
        ICondition condition = JsonSerializer.Deserialize<ICondition>(json, _options)!;

        Assert.Equal("always", condition.Name.TextValue);
        Assert.Equal("true", condition.Expression.TextValue);
        Assert.Empty(condition.Parameters);
    }

    [Theory]
    [InlineData("1", "Expected a JSON object for the OpenFGA condition.")]
    [InlineData(
        /*lang=json,strict*/
        """{"expression":"true"}""",
        "The OpenFGA condition is missing the required property 'name'."
    )]
    [InlineData(
        /*lang=json,strict*/
        """{"name":"always"}""",
        "The OpenFGA condition is missing the required property 'expression'."
    )]
    [InlineData(
        /*lang=json,strict*/
        """{"name":true,"expression":"true"}""",
        "Expected a JSON string for the OpenFGA property 'name'."
    )]
    [InlineData(
        /*lang=json,strict*/
        """{"name":"always","expression":1}""",
        "Expected a JSON string for the OpenFGA property 'expression'."
    )]
    [InlineData(
        /*lang=json,strict*/
        """{"name":"always","expression":"true","parameters":[]}""",
        "Expected a JSON object for the OpenFGA property 'parameters'."
    )]
    public void ThrowsOnInvalidJson(string json, string message)
    {
        JsonException exception = Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<ICondition>(json, _options)
        );

        Assert.Equal(message, exception.Message);
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
        public ParameterDouble(string name, IConditionParameterType type)
        {
            Name = new String(name);
            Type = type;
        }

        public IString Name { get; }

        public IConditionParameterType Type { get; }
    }

    private sealed record ParameterTypeDouble : IConditionParameterType
    {
        public ParameterTypeDouble(
            string name,
            params IConditionParameterType[] genericTypes
        )
        {
            Name = new String(name);
            GenericTypes = genericTypes;
        }

        public IString Name { get; }

        public IEnumerable<IConditionParameterType> GenericTypes { get; }
    }
}
