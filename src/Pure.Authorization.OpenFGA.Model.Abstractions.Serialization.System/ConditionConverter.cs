using System.Text.Json;
using System.Text.Json.Serialization;
using Pure.Authorization.OpenFGA.Model.Abstractions.Condition;
using Pure.Primitives.Abstractions.String;
using String = Pure.Primitives.String.String;

namespace Pure.Authorization.OpenFGA.Model.Abstractions.Serialization.System;

internal sealed record ConditionJsonModel : ICondition
{
    public ConditionJsonModel(
        IString name,
        IString expression,
        IEnumerable<IConditionParameter> parameters
    )
    {
        Name = name;
        Expression = expression;
        Parameters = parameters;
    }

    public IString Name { get; }

    public IString Expression { get; }

    public IEnumerable<IConditionParameter> Parameters { get; }
}

internal sealed record ConditionParameterJsonModel : IConditionParameter
{
    public ConditionParameterJsonModel(IString name, IConditionParameterType type)
    {
        Name = name;
        Type = type;
    }

    public IString Name { get; }

    public IConditionParameterType Type { get; }
}

/// <summary>
/// Converts <see cref="ICondition"/> to and from an OpenFGA <c>Condition</c>,
/// <c>{name, expression, parameters: {name: ConditionParamTypeRef}}</c>.
/// </summary>
public sealed class ConditionConverter : JsonConverter<ICondition>
{
    private const string Element = "condition";

    private readonly ConditionParameterTypeConverter _types =
        new ConditionParameterTypeConverter();

    public override ICondition Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        reader.ExpectObject(Element);

        string? name = null;
        string? expression = null;
        List<IConditionParameter> parameters = [];

        while (
            reader.NextProperty(out string property, "name", "expression", "parameters")
        )
        {
            switch (property)
            {
                case "name":
                    name = reader.ExpectString("property 'name'");
                    break;
                case "expression":
                    expression = reader.ExpectString("property 'expression'");
                    break;
                default:
                    reader.ExpectObject("property 'parameters'");

                    while (reader.NextEntry(out string parameter))
                    {
                        parameters.Add(
                            new ConditionParameterJsonModel(
                                new String(parameter),
                                _types.Read(
                                    ref reader,
                                    typeof(IConditionParameterType),
                                    options
                                )
                            )
                        );
                    }

                    break;
            }
        }

        return new ConditionJsonModel(
            new String(
                name ?? throw Utf8JsonReaderExtensions.MissingProperty("name", Element)
            ),
            new String(
                expression
                    ?? throw Utf8JsonReaderExtensions.MissingProperty(
                        "expression",
                        Element
                    )
            ),
            parameters
        );
    }

    public override void Write(
        Utf8JsonWriter writer,
        ICondition value,
        JsonSerializerOptions options
    )
    {
        writer.WriteStartObject();
        writer.WriteString("name", value.Name.TextValue);
        writer.WriteString("expression", value.Expression.TextValue);

        IConditionParameter[] parameters = [.. value.Parameters];

        if (parameters.Length > 0)
        {
            writer.WriteStartObject("parameters");

            foreach (IConditionParameter parameter in parameters)
            {
                writer.WritePropertyName(parameter.Name.TextValue);
                _types.Write(writer, parameter.Type, options);
            }

            writer.WriteEndObject();
        }

        writer.WriteEndObject();
    }
}
