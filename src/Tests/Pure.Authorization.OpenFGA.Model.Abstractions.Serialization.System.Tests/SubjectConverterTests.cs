using System.Text.Json;
using System.Text.Json.Serialization;
using Pure.Authorization.OpenFGA.Model.Abstractions.Object;
using Pure.Authorization.OpenFGA.Model.Abstractions.Subject;
using Pure.Primitives.Abstractions.String;
using String = Pure.Primitives.String.String;

namespace Pure.Authorization.OpenFGA.Model.Abstractions.Serialization.System.Tests;

public sealed record SubjectConverterTests
{
    private readonly JsonSerializerOptions _options;

    public SubjectConverterTests()
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
    public void WritesObject()
    {
        ISubject subject = new ObjectDouble("user", "anne");

        Assert.Equal("\"user:anne\"", JsonSerializer.Serialize(subject, _options));
    }

    [Fact]
    public void WritesSubjectSet()
    {
        ISubject subject = new SubjectSetDouble(
            new ObjectDouble("group", "eng"),
            "member"
        );

        Assert.Equal("\"group:eng#member\"", JsonSerializer.Serialize(subject, _options));
    }

    [Fact]
    public void WritesTypedWildcard()
    {
        ISubject subject = new TypedWildcardDouble("user");

        Assert.Equal("\"user:*\"", JsonSerializer.Serialize(subject, _options));
    }

    [Fact]
    public void ThrowsOnUnknownSubjectWrite()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            JsonSerializer.Serialize<ISubject>(new UnknownSubject(), _options)
        );

        Assert.Equal(
            "Cannot write UnknownSubject as an OpenFGA subject: it implements none of "
                + "IObject, ISubjectSet and ITypedWildcard.",
            exception.Message
        );
    }

    [Fact]
    public void ReadsObject()
    {
        ISubject subject = JsonSerializer.Deserialize<ISubject>(
            "\"user:anne\"",
            _options
        )!;

        IObject @object = Assert.IsType<IObject>(subject, exactMatch: false);
        Assert.Equal("user", @object.Type.TextValue);
        Assert.Equal("anne", @object.Id.TextValue);
    }

    [Fact]
    public void ReadsObjectWithColonInId()
    {
        ISubject subject = JsonSerializer.Deserialize<ISubject>(
            "\"user:a:b\"",
            _options
        )!;

        IObject @object = Assert.IsType<IObject>(subject, exactMatch: false);
        Assert.Equal("user", @object.Type.TextValue);
        Assert.Equal("a:b", @object.Id.TextValue);
    }

    [Fact]
    public void ReadsSubjectSet()
    {
        ISubject subject = JsonSerializer.Deserialize<ISubject>(
            "\"group:eng#member\"",
            _options
        )!;

        ISubjectSet set = Assert.IsType<ISubjectSet>(subject, exactMatch: false);
        Assert.Equal("group", set.Object.Type.TextValue);
        Assert.Equal("eng", set.Object.Id.TextValue);
        Assert.Equal("member", set.Relation.TextValue);
    }

    [Fact]
    public void ReadsTypedWildcard()
    {
        ISubject subject = JsonSerializer.Deserialize<ISubject>("\"user:*\"", _options)!;

        ITypedWildcard wildcard = Assert.IsType<ITypedWildcard>(
            subject,
            exactMatch: false
        );
        Assert.Equal("user", wildcard.Type.TextValue);
    }

    [Theory]
    [InlineData("user:anne")]
    [InlineData("group:eng#member")]
    [InlineData("user:*")]
    public void RoundTrip(string text)
    {
        string json = $"\"{text}\"";

        Assert.Equal(
            json,
            JsonSerializer.Serialize(
                JsonSerializer.Deserialize<ISubject>(json, _options),
                _options
            )
        );
    }

    [Theory]
    [InlineData("")]
    [InlineData("user")]
    [InlineData(":anne")]
    [InlineData("user:")]
    [InlineData("*")]
    [InlineData(":*")]
    [InlineData(":a:*")]
    [InlineData("#user:*")]
    [InlineData("#member")]
    [InlineData("group#member")]
    [InlineData("group:eng#")]
    [InlineData("group:eng#mem#ber")]
    [InlineData("group:eng#mem:ber")]
    [InlineData("group:eng#:member")]
    [InlineData("group:*#member")]
    [InlineData("group:#member")]
    public void ThrowsOnMalformedSubject(string text)
    {
        JsonException exception = Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<ISubject>($"\"{text}\"", _options)
        );

        Assert.Equal(
            $"'{text}' is not a valid OpenFGA subject; expected 'type:id', "
                + "'type:id#relation' or 'type:*'.",
            exception.Message
        );
    }

    [Fact]
    public void ThrowsOnNonString()
    {
        JsonException exception = Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<ISubject>("{}", _options)
        );

        Assert.Equal(
            "Expected a JSON string for the OpenFGA subject.",
            exception.Message
        );
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

    private sealed record SubjectSetDouble : ISubjectSet
    {
        public SubjectSetDouble(IObject @object, string relation)
        {
            Object = @object;
            Relation = new String(relation);
        }

        public IObject Object { get; }

        public IString Relation { get; }
    }

    private sealed record TypedWildcardDouble : ITypedWildcard
    {
        public TypedWildcardDouble(string type)
        {
            Type = new String(type);
        }

        public IString Type { get; }
    }

    private sealed record UnknownSubject : ISubject;
}
