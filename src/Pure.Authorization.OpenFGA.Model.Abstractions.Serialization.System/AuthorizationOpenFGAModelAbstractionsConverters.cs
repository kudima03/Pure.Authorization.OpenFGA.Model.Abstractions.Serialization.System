using System.Collections;
using System.Text.Json.Serialization;

namespace Pure.Authorization.OpenFGA.Model.Abstractions.Serialization.System;

public sealed record AuthorizationOpenFGAModelAbstractionsConverters
    : IEnumerable<JsonConverter>
{
    public IEnumerator<JsonConverter> GetEnumerator()
    {
        yield return new AuthorizationModelConverter();
        yield return new TypeDefinitionConverter();
        yield return new SubjectFormConverter();
        yield return new ConditionConverter();
        yield return new ConditionParameterTypeConverter();
        yield return new TupleConverter();
        yield return new ConditionalTupleConverter();
        yield return new RelationshipConditionConverter();
        yield return new SubjectConverter();
        yield return new ObjectConverter();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
