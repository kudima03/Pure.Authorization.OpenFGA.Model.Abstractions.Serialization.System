using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Pure.Authorization.OpenFGA.Model.Abstractions.Tuple;
using Pure.Primitives.Abstractions.String;
using String = Pure.Primitives.String.String;

namespace Pure.Authorization.OpenFGA.Model.Abstractions.Serialization.System.Tests;

public sealed record RelationshipConditionConverterTests
{
    private const string Context =
        """{"grant_time":"2023-01-01T00:00:00Z","limit":1.50,"nested":{"list":[1,"x",true,null,{"k":[]}],"empty":{}}}""";

    private readonly JsonSerializerOptions _options;

    public RelationshipConditionConverterTests()
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
    public void Write()
    {
        IRelationshipCondition condition = new RelationshipConditionDouble(
            "non_expired",
            JsonNode.Parse(Context)!
        );

        Assert.Equal(
            $$"""{"name":"non_expired","context":{{Context}}}""",
            JsonSerializer.Serialize(condition, _options)
        );
    }

    [Fact]
    public void Read()
    {
        IRelationshipCondition condition =
            JsonSerializer.Deserialize<IRelationshipCondition>(
                $$"""{"context":{{Context}},"name":"non_expired"}""",
                _options
            )!;

        Assert.Equal("non_expired", condition.Name.TextValue);
        Assert.Equal(Context, condition.Context.ToJsonString());
    }

    [Theory]
    [InlineData( /*lang=json,strict*/
        """{"name":"non_expired"}"""
    )]
    [InlineData( /*lang=json,strict*/
        """{"name":"non_expired","context":null}"""
    )]
    public void ReadsMissingContextAsEmptyObject(string json)
    {
        IRelationshipCondition condition =
            JsonSerializer.Deserialize<IRelationshipCondition>(json, _options)!;

        _ = Assert.IsType<JsonObject>(condition.Context);
        Assert.Equal("{}", condition.Context.ToJsonString());
    }

    [Fact]
    public void RoundTrip()
    {
        string json = $$"""{"name":"non_expired","context":{{Context}}}""";

        Assert.Equal(
            json,
            JsonSerializer.Serialize(
                JsonSerializer.Deserialize<IRelationshipCondition>(json, _options),
                _options
            )
        );
    }

    [Theory]
    [InlineData("[]", "Expected a JSON object for the OpenFGA relationship condition.")]
    [InlineData(
        /*lang=json,strict*/
        """{"context":{}}""",
        "The OpenFGA relationship condition is missing the required property 'name'."
    )]
    [InlineData(
        /*lang=json,strict*/
        """{"name":1}""",
        "Expected a JSON string for the OpenFGA property 'name'."
    )]
    [InlineData(
        /*lang=json,strict*/
        """{"name":"c","context":[1]}""",
        "Expected a JSON object for the OpenFGA property 'context'."
    )]
    public void ThrowsOnInvalidJson(string json, string message)
    {
        JsonException exception = Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<IRelationshipCondition>(json, _options)
        );

        Assert.Equal(message, exception.Message);
    }

    private sealed record RelationshipConditionDouble : IRelationshipCondition
    {
        public RelationshipConditionDouble(string name, JsonNode context)
        {
            Name = new String(name);
            Context = context;
        }

        public IString Name { get; }

        public JsonNode Context { get; }
    }
}
