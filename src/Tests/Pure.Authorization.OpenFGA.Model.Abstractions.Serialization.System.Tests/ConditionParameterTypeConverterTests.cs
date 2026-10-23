using System.Text.Json;
using System.Text.Json.Serialization;
using Pure.Authorization.OpenFGA.Model.Abstractions.Condition;
using Pure.Primitives.Abstractions.String;
using String = Pure.Primitives.String.String;

namespace Pure.Authorization.OpenFGA.Model.Abstractions.Serialization.System.Tests;

public sealed record ConditionParameterTypeConverterTests
{
    private readonly JsonSerializerOptions _options;

    public ConditionParameterTypeConverterTests()
    {
        _options = new JsonSerializerOptions();

        foreach (
            JsonConverter converter in new AuthorizationOpenFGAModelAbstractionsConverters()
        )
        {
            _options.Converters.Add(converter);
        }
    }

    [Theory]
    [InlineData("any", "TYPE_NAME_ANY")]
    [InlineData("bool", "TYPE_NAME_BOOL")]
    [InlineData("string", "TYPE_NAME_STRING")]
    [InlineData("int", "TYPE_NAME_INT")]
    [InlineData("uint", "TYPE_NAME_UINT")]
    [InlineData("double", "TYPE_NAME_DOUBLE")]
    [InlineData("duration", "TYPE_NAME_DURATION")]
    [InlineData("timestamp", "TYPE_NAME_TIMESTAMP")]
    [InlineData("map", "TYPE_NAME_MAP")]
    [InlineData("list", "TYPE_NAME_LIST")]
    [InlineData("ipaddress", "TYPE_NAME_IPADDRESS")]
    public void Write(string name, string typeName)
    {
        Assert.Equal(
            $$"""{"type_name":"{{typeName}}"}""",
            JsonSerializer.Serialize<IConditionParameterType>(
                new ParameterTypeDouble(name),
                _options
            )
        );
    }

    [Theory]
    [InlineData("any", "TYPE_NAME_ANY")]
    [InlineData("bool", "TYPE_NAME_BOOL")]
    [InlineData("string", "TYPE_NAME_STRING")]
    [InlineData("int", "TYPE_NAME_INT")]
    [InlineData("uint", "TYPE_NAME_UINT")]
    [InlineData("double", "TYPE_NAME_DOUBLE")]
    [InlineData("duration", "TYPE_NAME_DURATION")]
    [InlineData("timestamp", "TYPE_NAME_TIMESTAMP")]
    [InlineData("map", "TYPE_NAME_MAP")]
    [InlineData("list", "TYPE_NAME_LIST")]
    [InlineData("ipaddress", "TYPE_NAME_IPADDRESS")]
    public void Read(string name, string typeName)
    {
        IConditionParameterType type =
            JsonSerializer.Deserialize<IConditionParameterType>(
                $$"""{"type_name":"{{typeName}}"}""",
                _options
            )!;

        Assert.Equal(name, type.Name.TextValue);
        Assert.Empty(type.GenericTypes);
    }

    [Fact]
    public void WritesGenericTypes()
    {
        IConditionParameterType type = new ParameterTypeDouble(
            "map",
            new ParameterTypeDouble("list", new ParameterTypeDouble("int"))
        );

        Assert.Equal(
            /*lang=json,strict*/
            """{"type_name":"TYPE_NAME_MAP","generic_types":[{"type_name":"TYPE_NAME_LIST","generic_types":[{"type_name":"TYPE_NAME_INT"}]}]}""",
            JsonSerializer.Serialize(type, _options)
        );
    }

    [Fact]
    public void ReadsGenericTypes()
    {
        IConditionParameterType type =
            JsonSerializer.Deserialize<IConditionParameterType>(
                """{"type_name":"TYPE_NAME_MAP","generic_types":[{"type_name":"TYPE_NAME_LIST","generic_types":[{"type_name":"TYPE_NAME_INT"}]},{"type_name":"TYPE_NAME_STRING"}]}""",
                _options
            )!;

        Assert.Equal("map", type.Name.TextValue);
        Assert.Collection(
            type.GenericTypes,
            list =>
            {
                Assert.Equal("list", list.Name.TextValue);
                Assert.Equal("int", Assert.Single(list.GenericTypes).Name.TextValue);
            },
            text => Assert.Equal("string", text.Name.TextValue)
        );
    }

    [Theory]
    [InlineData( /*lang=json,strict*/
        """{"type_name":"TYPE_NAME_BOOL","generic_types":[]}"""
    )]
    [InlineData( /*lang=json,strict*/
        """{"generic_types":null,"type_name":"TYPE_NAME_BOOL","x":{"y":1}}"""
    )]
    public void ReadsEmptyGenericTypes(string json)
    {
        IConditionParameterType type =
            JsonSerializer.Deserialize<IConditionParameterType>(json, _options)!;

        Assert.Equal("bool", type.Name.TextValue);
        Assert.Empty(type.GenericTypes);
    }

    [Theory]
    [InlineData("TYPE_NAME_UNSPECIFIED")]
    [InlineData("TYPE_NAME_FLOAT")]
    [InlineData("TYPE_NAME_bool")]
    [InlineData("type_name_bool")]
    [InlineData("bool")]
    [InlineData("")]
    public void ThrowsOnUnknownTypeName(string typeName)
    {
        JsonException exception = Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<IConditionParameterType>(
                $$"""{"type_name":"{{typeName}}"}""",
                _options
            )
        );

        Assert.Equal(
            $"'{typeName}' is not a supported OpenFGA condition parameter type_name.",
            exception.Message
        );
    }

    [Theory]
    [InlineData("[]", "Expected a JSON object for the OpenFGA condition parameter type.")]
    [InlineData(
        /*lang=json,strict*/
        """{"generic_types":[]}""",
        "The OpenFGA condition parameter type is missing the required property 'type_name'."
    )]
    [InlineData(
        /*lang=json,strict*/
        """{"type_name":2}""",
        "Expected a JSON string for the OpenFGA property 'type_name'."
    )]
    [InlineData(
        /*lang=json,strict*/
        """{"type_name":"TYPE_NAME_LIST","generic_types":{}}""",
        "Expected a JSON array for the OpenFGA property 'generic_types'."
    )]
    public void ThrowsOnInvalidJson(string json, string message)
    {
        JsonException exception = Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<IConditionParameterType>(json, _options)
        );

        Assert.Equal(message, exception.Message);
    }

    [Theory]
    [InlineData("float")]
    [InlineData("BOOL")]
    [InlineData("TYPE_NAME_BOOL")]
    public void ThrowsOnUnknownNameWrite(string name)
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            JsonSerializer.Serialize<IConditionParameterType>(
                new ParameterTypeDouble(name),
                _options
            )
        );

        Assert.Equal(
            $"'{name}' is not an OpenFGA condition parameter type name; expected one of "
                + "any, bool, string, int, uint, double, duration, timestamp, map, list, "
                + "ipaddress.",
            exception.Message
        );
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
