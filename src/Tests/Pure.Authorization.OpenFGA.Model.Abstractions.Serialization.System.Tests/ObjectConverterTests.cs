using System.Text.Json;
using System.Text.Json.Serialization;
using Pure.Authorization.OpenFGA.Model.Abstractions.Object;
using Pure.Primitives.Abstractions.String;
using String = Pure.Primitives.String.String;

namespace Pure.Authorization.OpenFGA.Model.Abstractions.Serialization.System.Tests;

public sealed record ObjectConverterTests
{
    private readonly JsonSerializerOptions _options;

    public ObjectConverterTests()
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
        IObject @object = new ObjectDouble("document", "2021-budget");

        Assert.Equal(
            "\"document:2021-budget\"",
            JsonSerializer.Serialize(@object, _options)
        );
    }

    [Fact]
    public void Read()
    {
        IObject @object = JsonSerializer.Deserialize<IObject>(
            "\"document:1\"",
            _options
        )!;

        Assert.Equal("document", @object.Type.TextValue);
        Assert.Equal("1", @object.Id.TextValue);
    }

    [Fact]
    public void ReadsIdContainingColon()
    {
        IObject @object = JsonSerializer.Deserialize<IObject>(
            "\"document:a:b\"",
            _options
        )!;

        Assert.Equal("document", @object.Type.TextValue);
        Assert.Equal("a:b", @object.Id.TextValue);
    }

    [Fact]
    public void RoundTrip()
    {
        IObject @object = new ObjectDouble("folder", "x");

        IObject deserialized = JsonSerializer.Deserialize<IObject>(
            JsonSerializer.Serialize(@object, _options),
            _options
        )!;

        Assert.Equal("folder", deserialized.Type.TextValue);
        Assert.Equal("x", deserialized.Id.TextValue);
    }

    [Theory]
    [InlineData("")]
    [InlineData("document")]
    [InlineData(":1")]
    [InlineData(":a:b")]
    [InlineData("document:")]
    [InlineData("document:*")]
    [InlineData("document:1#viewer")]
    [InlineData("group#member:1")]
    public void ThrowsOnMalformedObject(string text)
    {
        JsonException exception = Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<IObject>($"\"{text}\"", _options)
        );

        Assert.Equal(
            $"'{text}' is not a valid OpenFGA object; expected 'type:id'.",
            exception.Message
        );
    }

    [Fact]
    public void ThrowsOnNonString()
    {
        JsonException exception = Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<IObject>("1", _options)
        );

        Assert.Equal("Expected a JSON string for the OpenFGA object.", exception.Message);
    }

    private sealed record ObjectDouble : IObject
    {
        public ObjectDouble(string type, string id)
        {
            Type = new String(type);
            Id = new String(id);
        }

        public IString Type { get; }

        public IString Id { get; }
    }
}
