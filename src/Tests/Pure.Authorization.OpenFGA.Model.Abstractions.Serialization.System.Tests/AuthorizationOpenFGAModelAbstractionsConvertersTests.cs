using System.Collections;

namespace Pure.Authorization.OpenFGA.Model.Abstractions.Serialization.System.Tests;

public sealed record AuthorizationOpenFGAModelAbstractionsConvertersTests
{
    [Fact]
    public void EnumeratesAllConverters()
    {
        Assert.Equal(
            [
                typeof(AuthorizationModelConverter),
                typeof(TypeDefinitionConverter),
                typeof(SubjectFormConverter),
                typeof(ConditionConverter),
                typeof(ConditionParameterTypeConverter),
                typeof(TupleConverter),
                typeof(ConditionalTupleConverter),
                typeof(RelationshipConditionConverter),
                typeof(SubjectConverter),
                typeof(ObjectConverter),
            ],
            new AuthorizationOpenFGAModelAbstractionsConverters().Select(converter =>
                converter.GetType()
            )
        );
    }

    [Fact]
    public void NonGenericEnumeratorReturnsAllConverters()
    {
        IEnumerable converters = new AuthorizationOpenFGAModelAbstractionsConverters();

        int count = 0;

        foreach (object _ in converters)
        {
            count++;
        }

        Assert.Equal(10, count);
    }
}
