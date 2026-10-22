using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Pure.Authorization.OpenFGA.Model.Abstractions.Object;
using Pure.Authorization.OpenFGA.Model.Abstractions.Subject;
using Pure.Authorization.OpenFGA.Model.Abstractions.Tuple;
using Pure.Primitives.Abstractions.String;
using String = Pure.Primitives.String.String;

namespace Pure.Authorization.OpenFGA.Model.Abstractions.Serialization.System.Tests;

public sealed record ConditionalTupleConverterTests
{
    private const string Json =
        """{"user":"group:eng#member","relation":"viewer","object":"document:1","condition":{"name":"in_network","context":{"cidr":"10.0.0.0/8","nested":{"a":[1,2]}}}}""";

    private readonly JsonSerializerOptions _options;

    public ConditionalTupleConverterTests()
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
        IConditionalTuple tuple = new ConditionalTupleDouble(
            new SubjectSetDouble(new ObjectDouble("group", "eng"), "member"),
            "viewer",
            new ObjectDouble("document", "1"),
            "in_network",
            JsonNode.Parse("""{"cidr":"10.0.0.0/8","nested":{"a":[1,2]}}""")!
        );

        Assert.Equal(Json, JsonSerializer.Serialize(tuple, _options));
    }

    [Fact]
    public void Read()
    {
        IConditionalTuple tuple = JsonSerializer.Deserialize<IConditionalTuple>(
            Json,
            _options
        )!;

        ISubjectSet subject = Assert.IsType<ISubjectSet>(
            tuple.Subject,
            exactMatch: false
        );
        Assert.Equal("group", subject.Object.Type.TextValue);
        Assert.Equal("eng", subject.Object.Id.TextValue);
        Assert.Equal("member", subject.Relation.TextValue);
        Assert.Equal("viewer", tuple.Relation.TextValue);
        Assert.Equal("document", tuple.Object.Type.TextValue);
        Assert.Equal("1", tuple.Object.Id.TextValue);
        Assert.Equal("in_network", tuple.Condition.Name.TextValue);
        Assert.Equal(
            /*lang=json,strict*/
            """{"cidr":"10.0.0.0/8","nested":{"a":[1,2]}}""",
            tuple.Condition.Context.ToJsonString()
        );
    }

    [Fact]
    public void RoundTrip()
    {
        Assert.Equal(
            Json,
            JsonSerializer.Serialize(
                JsonSerializer.Deserialize<IConditionalTuple>(Json, _options),
                _options
            )
        );
    }

    [Theory]
    [InlineData( /*lang=json,strict*/
        """{"user":"user:anne","relation":"viewer","object":"document:1"}"""
    )]
    [InlineData(
        /*lang=json,strict*/
        """{"user":"user:anne","relation":"viewer","object":"document:1","condition":null}"""
    )]
    public void ThrowsWithoutCondition(string json)
    {
        JsonException exception = Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<IConditionalTuple>(json, _options)
        );

        Assert.Equal(
            "The OpenFGA conditional tuple key is missing the required property 'condition'.",
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

    private sealed record ConditionalTupleDouble : IConditionalTuple
    {
        public ConditionalTupleDouble(
            ISubject subject,
            string relation,
            IObject @object,
            string condition,
            JsonNode context
        )
        {
            Subject = subject;
            Relation = new String(relation);
            Object = @object;
            Condition = new RelationshipConditionDouble(condition, context);
        }

        public ISubject Subject { get; }

        public IString Relation { get; }

        public IObject Object { get; }

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
