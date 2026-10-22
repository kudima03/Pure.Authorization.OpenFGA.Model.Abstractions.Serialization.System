using System.Text.Json;
using System.Text.Json.Serialization;
using Pure.Authorization.OpenFGA.Model.Abstractions.SubjectForm;
using Pure.Primitives.Abstractions.String;
using String = Pure.Primitives.String.String;

namespace Pure.Authorization.OpenFGA.Model.Abstractions.Serialization.System.Tests;

public sealed record SubjectFormConverterTests
{
    private readonly JsonSerializerOptions _options;

    public SubjectFormConverterTests()
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
    public void WritesTypeForm()
    {
        Assert.Equal(
            /*lang=json,strict*/
            """{"type":"user"}""",
            JsonSerializer.Serialize<ISubjectForm>(new TypeFormDouble("user"), _options)
        );
    }

    [Fact]
    public void WritesSubjectSetForm()
    {
        Assert.Equal(
            /*lang=json,strict*/
            """{"type":"group","relation":"member"}""",
            JsonSerializer.Serialize<ISubjectForm>(
                new SubjectSetFormDouble("group", "member"),
                _options
            )
        );
    }

    [Fact]
    public void WritesWildcardForm()
    {
        Assert.Equal(
            /*lang=json,strict*/
            """{"type":"user","wildcard":{}}""",
            JsonSerializer.Serialize<ISubjectForm>(
                new WildcardFormDouble("user"),
                _options
            )
        );
    }

    [Fact]
    public void WritesConditionalTypeForm()
    {
        Assert.Equal(
            /*lang=json,strict*/
            """{"type":"user","condition":"non_expired"}""",
            JsonSerializer.Serialize<ISubjectForm>(
                new ConditionalFormDouble(new TypeFormDouble("user"), "non_expired"),
                _options
            )
        );
    }

    [Fact]
    public void WritesConditionalSubjectSetForm()
    {
        Assert.Equal(
            /*lang=json,strict*/
            """{"type":"group","relation":"member","condition":"in_region"}""",
            JsonSerializer.Serialize<ISubjectForm>(
                new ConditionalFormDouble(
                    new SubjectSetFormDouble("group", "member"),
                    "in_region"
                ),
                _options
            )
        );
    }

    [Fact]
    public void WritesConditionalWildcardForm()
    {
        Assert.Equal(
            /*lang=json,strict*/
            """{"type":"user","wildcard":{},"condition":"in_region"}""",
            JsonSerializer.Serialize<ISubjectForm>(
                new ConditionalFormDouble(new WildcardFormDouble("user"), "in_region"),
                _options
            )
        );
    }

    [Fact]
    public void ThrowsOnNestedConditionalFormWrite()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            JsonSerializer.Serialize<ISubjectForm>(
                new ConditionalFormDouble(
                    new ConditionalFormDouble(new TypeFormDouble("user"), "a"),
                    "b"
                ),
                _options
            )
        );

        Assert.Equal(
            "An IConditionalForm cannot wrap another IConditionalForm: an OpenFGA "
                + "relation reference carries at most one condition.",
            exception.Message
        );
    }

    [Fact]
    public void ThrowsOnUnknownFormWrite()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            JsonSerializer.Serialize<ISubjectForm>(new UnknownForm(), _options)
        );

        Assert.Equal(
            "Cannot write UnknownForm as an OpenFGA relation reference: it implements "
                + "none of ITypeForm, ISubjectSetForm, IWildcardForm and IConditionalForm.",
            exception.Message
        );
    }

    [Fact]
    public void ThrowsOnConditionalUnknownFormWrite()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            JsonSerializer.Serialize<ISubjectForm>(
                new ConditionalFormDouble(new UnknownForm(), "a"),
                _options
            )
        );

        Assert.StartsWith("Cannot write UnknownForm", exception.Message);
    }

    [Theory]
    [InlineData( /*lang=json,strict*/
        """{"type":"user"}"""
    )]
    [InlineData( /*lang=json,strict*/
        """{"type":"user","relation":"","condition":""}"""
    )]
    [InlineData( /*lang=json,strict*/
        """{"type":"user","wildcard":null,"condition":null,"relation":null}"""
    )]
    [InlineData( /*lang=json,strict*/
        """{"module":"m","type":"user","source_info":{"file":"f.fga"}}"""
    )]
    public void ReadsTypeForm(string json)
    {
        ISubjectForm form = JsonSerializer.Deserialize<ISubjectForm>(json, _options)!;

        ITypeForm type = Assert.IsType<ITypeForm>(form, exactMatch: false);
        Assert.Equal("user", type.Type.TextValue);
    }

    [Fact]
    public void ReadsSubjectSetForm()
    {
        ISubjectForm form = JsonSerializer.Deserialize<ISubjectForm>(
            """{"relation":"member","type":"group"}""",
            _options
        )!;

        ISubjectSetForm set = Assert.IsType<ISubjectSetForm>(form, exactMatch: false);
        Assert.Equal("group", set.Type.TextValue);
        Assert.Equal("member", set.Relation.TextValue);
    }

    [Fact]
    public void ReadsWildcardForm()
    {
        ISubjectForm form = JsonSerializer.Deserialize<ISubjectForm>(
            """{"type":"user","wildcard":{}}""",
            _options
        )!;

        IWildcardForm wildcard = Assert.IsType<IWildcardForm>(form, exactMatch: false);
        Assert.Equal("user", wildcard.Type.TextValue);
    }

    [Fact]
    public void ReadsConditionalTypeForm()
    {
        ISubjectForm form = JsonSerializer.Deserialize<ISubjectForm>(
            """{"type":"user","condition":"non_expired"}""",
            _options
        )!;

        IConditionalForm conditional = Assert.IsType<IConditionalForm>(
            form,
            exactMatch: false
        );
        Assert.Equal("non_expired", conditional.Condition.TextValue);
        Assert.Equal(
            "user",
            Assert.IsType<ITypeForm>(conditional.Form, exactMatch: false).Type.TextValue
        );
    }

    [Fact]
    public void ReadsConditionalSubjectSetForm()
    {
        ISubjectForm form = JsonSerializer.Deserialize<ISubjectForm>(
            """{"type":"group","relation":"member","condition":"in_region"}""",
            _options
        )!;

        IConditionalForm conditional = Assert.IsType<IConditionalForm>(
            form,
            exactMatch: false
        );
        ISubjectSetForm set = Assert.IsType<ISubjectSetForm>(
            conditional.Form,
            exactMatch: false
        );
        Assert.Equal("group", set.Type.TextValue);
        Assert.Equal("member", set.Relation.TextValue);
        Assert.Equal("in_region", conditional.Condition.TextValue);
    }

    [Fact]
    public void ReadsConditionalWildcardForm()
    {
        ISubjectForm form = JsonSerializer.Deserialize<ISubjectForm>(
            """{"type":"user","wildcard":{},"condition":"in_region"}""",
            _options
        )!;

        IConditionalForm conditional = Assert.IsType<IConditionalForm>(
            form,
            exactMatch: false
        );
        Assert.Equal(
            "user",
            Assert
                .IsType<IWildcardForm>(conditional.Form, exactMatch: false)
                .Type.TextValue
        );
        Assert.Equal("in_region", conditional.Condition.TextValue);
    }

    [Theory]
    [InlineData( /*lang=json,strict*/
        """{"type":"user"}"""
    )]
    [InlineData( /*lang=json,strict*/
        """{"type":"group","relation":"member"}"""
    )]
    [InlineData( /*lang=json,strict*/
        """{"type":"user","wildcard":{}}"""
    )]
    [InlineData( /*lang=json,strict*/
        """{"type":"user","condition":"non_expired"}"""
    )]
    [InlineData( /*lang=json,strict*/
        """{"type":"group","relation":"member","condition":"c"}"""
    )]
    [InlineData( /*lang=json,strict*/
        """{"type":"user","wildcard":{},"condition":"c"}"""
    )]
    public void RoundTrip(string json)
    {
        Assert.Equal(
            json,
            JsonSerializer.Serialize(
                JsonSerializer.Deserialize<ISubjectForm>(json, _options),
                _options
            )
        );
    }

    [Theory]
    [InlineData("\"user\"", "Expected a JSON object for the OpenFGA relation reference.")]
    [InlineData(
        /*lang=json,strict*/
        """{"relation":"member"}""",
        "The OpenFGA relation reference is missing the required property 'type'."
    )]
    [InlineData(
        /*lang=json,strict*/
        """{"type":"group","relation":"member","wildcard":{}}""",
        "An OpenFGA relation reference cannot have both 'relation' and 'wildcard'."
    )]
    [InlineData(
        /*lang=json,strict*/
        """{"type":1}""",
        "Expected a JSON string for the OpenFGA property 'type'."
    )]
    [InlineData(
        /*lang=json,strict*/
        """{"type":"group","relation":1}""",
        "Expected a JSON string for the OpenFGA property 'relation'."
    )]
    [InlineData(
        /*lang=json,strict*/
        """{"type":"user","condition":{}}""",
        "Expected a JSON string for the OpenFGA property 'condition'."
    )]
    [InlineData(
        /*lang=json,strict*/
        """{"type":"user","wildcard":true}""",
        "Expected a JSON object for the OpenFGA wildcard."
    )]
    public void ThrowsOnInvalidRelationReference(string json, string message)
    {
        JsonException exception = Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<ISubjectForm>(json, _options)
        );

        Assert.Equal(message, exception.Message);
    }

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
