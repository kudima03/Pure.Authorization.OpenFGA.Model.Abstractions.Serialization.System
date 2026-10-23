using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Pure.Authorization.OpenFGA.Model.Abstractions.Object;
using Pure.Authorization.OpenFGA.Model.Abstractions.Subject;
using Pure.Authorization.OpenFGA.Model.Abstractions.Tuple;
using Pure.Primitives.Abstractions.String;
using String = Pure.Primitives.String.String;

namespace Pure.Authorization.OpenFGA.Model.Abstractions.Serialization.System.Tests;

public sealed record TupleConverterTests
{
    private const string Context =
        """{"ip":"10.0.0.1","window":{"from":"2024-01-01T00:00:00Z","days":[1,2,3],"meta":{"x":null}}}""";

    private readonly JsonSerializerOptions _options;

    public TupleConverterTests()
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
        ITuple tuple = new TupleDouble(
            new ObjectDouble("user", "anne"),
            "viewer",
            new ObjectDouble("document", "1")
        );

        Assert.Equal(
            /*lang=json,strict*/
            """{"user":"user:anne","relation":"viewer","object":"document:1"}""",
            JsonSerializer.Serialize(tuple, _options)
        );
    }

    [Fact]
    public void WritesSubjectSetAndWildcardSubjects()
    {
        ITuple set = new TupleDouble(
            new SubjectSetDouble(new ObjectDouble("group", "eng"), "member"),
            "viewer",
            new ObjectDouble("document", "1")
        );
        ITuple wildcard = new TupleDouble(
            new TypedWildcardDouble("user"),
            "viewer",
            new ObjectDouble("document", "1")
        );

        Assert.Equal(
            /*lang=json,strict*/
            """{"user":"group:eng#member","relation":"viewer","object":"document:1"}""",
            JsonSerializer.Serialize(set, _options)
        );
        Assert.Equal(
            /*lang=json,strict*/
            """{"user":"user:*","relation":"viewer","object":"document:1"}""",
            JsonSerializer.Serialize(wildcard, _options)
        );
    }

    [Fact]
    public void WritesConditionalTuple()
    {
        ITuple tuple = new ConditionalTupleDouble(
            new TupleDouble(
                new ObjectDouble("user", "anne"),
                "viewer",
                new ObjectDouble("document", "1")
            ),
            "in_network",
            JsonNode.Parse(Context)!
        );

        Assert.Equal(
            $$$"""{"user":"user:anne","relation":"viewer","object":"document:1","condition":{"name":"in_network","context":{{{Context}}}}}""",
            JsonSerializer.Serialize(tuple, _options)
        );
    }

    [Fact]
    public void ThrowsOnUnknownSubjectWrite()
    {
        ITuple tuple = new TupleDouble(
            new UnknownSubject(),
            "viewer",
            new ObjectDouble("document", "1")
        );

        _ = Assert.Throws<ArgumentException>(() =>
            JsonSerializer.Serialize(tuple, _options)
        );
    }

    [Theory]
    [InlineData( /*lang=json,strict*/
        """{"user":"user:anne","relation":"viewer","object":"document:1"}"""
    )]
    [InlineData(
        /*lang=json,strict*/
        """{"object":"document:1","condition":null,"relation":"viewer","user":"user:anne","timestamp":{"a":1}}"""
    )]
    public void Read(string json)
    {
        ITuple tuple = JsonSerializer.Deserialize<ITuple>(json, _options)!;

        Assert.IsNotType<IConditionalTuple>(tuple, exactMatch: false);
        IObject subject = Assert.IsType<IObject>(tuple.Subject, exactMatch: false);
        Assert.Equal("user", subject.Type.TextValue);
        Assert.Equal("anne", subject.Id.TextValue);
        Assert.Equal("viewer", tuple.Relation.TextValue);
        Assert.Equal("document", tuple.Object.Type.TextValue);
        Assert.Equal("1", tuple.Object.Id.TextValue);
    }

    [Fact]
    public void ReadsSubjectSetSubject()
    {
        ITuple tuple = JsonSerializer.Deserialize<ITuple>(
            """{"user":"group:eng#member","relation":"viewer","object":"document:1"}""",
            _options
        )!;

        ISubjectSet set = Assert.IsType<ISubjectSet>(tuple.Subject, exactMatch: false);
        Assert.Equal("group", set.Object.Type.TextValue);
        Assert.Equal("eng", set.Object.Id.TextValue);
        Assert.Equal("member", set.Relation.TextValue);
    }

    [Fact]
    public void ReadsTypedWildcardSubject()
    {
        ITuple tuple = JsonSerializer.Deserialize<ITuple>(
            """{"user":"user:*","relation":"viewer","object":"document:1"}""",
            _options
        )!;

        Assert.Equal(
            "user",
            Assert.IsType<ITypedWildcard>(tuple.Subject, exactMatch: false).Type.TextValue
        );
    }

    [Fact]
    public void ReadsConditionalTuple()
    {
        ITuple tuple = JsonSerializer.Deserialize<ITuple>(
            $$$"""{"user":"user:anne","relation":"viewer","object":"document:1","condition":{"name":"in_network","context":{{{Context}}}}}""",
            _options
        )!;

        IConditionalTuple conditional = Assert.IsType<IConditionalTuple>(
            tuple,
            exactMatch: false
        );
        IObject subject = Assert.IsType<IObject>(conditional.Subject, exactMatch: false);
        Assert.Equal("user", subject.Type.TextValue);
        Assert.Equal("anne", subject.Id.TextValue);
        Assert.Equal("viewer", conditional.Relation.TextValue);
        Assert.Equal("document", conditional.Object.Type.TextValue);
        Assert.Equal("1", conditional.Object.Id.TextValue);
        Assert.Equal("in_network", conditional.Condition.Name.TextValue);
        Assert.Equal(Context, conditional.Condition.Context.ToJsonString());
    }

    [Theory]
    [InlineData( /*lang=json,strict*/
        """{"user":"user:anne","relation":"viewer","object":"document:1"}"""
    )]
    [InlineData(
        /*lang=json,strict*/
        """{"user":"group:eng#member","relation":"viewer","object":"document:1"}"""
    )]
    [InlineData( /*lang=json,strict*/
        """{"user":"user:*","relation":"viewer","object":"document:1"}"""
    )]
    [InlineData(
        /*lang=json,strict*/
        """{"user":"user:anne","relation":"viewer","object":"document:1","condition":{"name":"c","context":{"a":{"b":[1,{"c":"d"}]}}}}"""
    )]
    public void RoundTrip(string json)
    {
        Assert.Equal(
            json,
            JsonSerializer.Serialize(
                JsonSerializer.Deserialize<ITuple>(json, _options),
                _options
            )
        );
    }

    [Theory]
    [InlineData("\"user:anne\"", "Expected a JSON object for the OpenFGA tuple key.")]
    [InlineData(
        /*lang=json,strict*/
        """{"relation":"viewer","object":"document:1"}""",
        "The OpenFGA tuple key is missing the required property 'user'."
    )]
    [InlineData(
        /*lang=json,strict*/
        """{"user":"user:anne","object":"document:1"}""",
        "The OpenFGA tuple key is missing the required property 'relation'."
    )]
    [InlineData(
        /*lang=json,strict*/
        """{"user":"user:anne","relation":"viewer"}""",
        "The OpenFGA tuple key is missing the required property 'object'."
    )]
    [InlineData(
        /*lang=json,strict*/
        """{"user":"user:anne","relation":1,"object":"document:1"}""",
        "Expected a JSON string for the OpenFGA property 'relation'."
    )]
    [InlineData(
        /*lang=json,strict*/
        """{"user":"anne","relation":"viewer","object":"document:1"}""",
        "'anne' is not a valid OpenFGA subject; expected 'type:id', 'type:id#relation' or 'type:*'."
    )]
    [InlineData(
        /*lang=json,strict*/
        """{"user":"user:anne","relation":"viewer","object":"document:*"}""",
        "'document:*' is not a valid OpenFGA object; expected 'type:id'."
    )]
    [InlineData(
        /*lang=json,strict*/
        """{"user":"user:anne","relation":"viewer","object":"document:1","condition":"c"}""",
        "Expected a JSON object for the OpenFGA relationship condition."
    )]
    public void ThrowsOnInvalidJson(string json, string message)
    {
        JsonException exception = Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<ITuple>(json, _options)
        );

        Assert.Equal(message, exception.Message);
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

    private sealed record TupleDouble : ITuple
    {
        public TupleDouble(ISubject subject, string relation, IObject @object)
        {
            Subject = subject;
            Relation = new String(relation);
            Object = @object;
        }

        public ISubject Subject { get; }

        public IString Relation { get; }

        public IObject Object { get; }
    }

    private sealed record ConditionalTupleDouble : IConditionalTuple
    {
        private readonly ITuple _tuple;

        public ConditionalTupleDouble(ITuple tuple, string name, JsonNode context)
        {
            _tuple = tuple;
            Condition = new RelationshipConditionDouble(name, context);
        }

        public ISubject Subject => _tuple.Subject;

        public IString Relation => _tuple.Relation;

        public IObject Object => _tuple.Object;

        public IRelationshipCondition Condition { get; }
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
