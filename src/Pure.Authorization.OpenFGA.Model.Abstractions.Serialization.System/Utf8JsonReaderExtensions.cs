using System.Text.Json;

namespace Pure.Authorization.OpenFGA.Model.Abstractions.Serialization.System;

/// <summary>
/// Forward-only reading helpers shared by the converters. They follow the protobuf JSON
/// mapping OpenFGA uses: a property whose value is <c>null</c> is treated as absent, and
/// properties the converter does not know are skipped.
/// </summary>
internal static class Utf8JsonReaderExtensions
{
    public static void ExpectObject(this ref Utf8JsonReader reader, string element)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException($"Expected a JSON object for the OpenFGA {element}.");
        }
    }

    public static void ExpectArray(this ref Utf8JsonReader reader, string element)
    {
        if (reader.TokenType != JsonTokenType.StartArray)
        {
            throw new JsonException($"Expected a JSON array for the OpenFGA {element}.");
        }
    }

    public static string ExpectString(this ref Utf8JsonReader reader, string element)
    {
        return reader.TokenType == JsonTokenType.String
            ? reader.GetString()!
            : throw new JsonException(
                $"Expected a JSON string for the OpenFGA {element}."
            );
    }

    /// <summary>
    /// Advances to the value of the next property of the current object whose name is one
    /// of <paramref name="known"/> and whose value is not <c>null</c>; other properties are
    /// skipped. Returns <c>false</c> at the end of the object.
    /// </summary>
    public static bool NextProperty(
        this ref Utf8JsonReader reader,
        out string name,
        params string[] known
    )
    {
        return reader.NextMember(out name, known.Contains);
    }

    /// <summary>
    /// Advances to the value of the next entry of the current object, read as a protobuf
    /// map, whose value is not <c>null</c>. Returns <c>false</c> at the end of the object.
    /// </summary>
    public static bool NextEntry(this ref Utf8JsonReader reader, out string key)
    {
        return reader.NextMember(out key, _ => true);
    }

    /// <summary>
    /// Advances to the next item of the current array. Returns <c>false</c> at its end.
    /// </summary>
    public static bool NextItem(this ref Utf8JsonReader reader)
    {
        _ = reader.Read();

        return reader.TokenType != JsonTokenType.EndArray;
    }

    public static JsonException MissingProperty(string property, string element)
    {
        return new JsonException(
            $"The OpenFGA {element} is missing the required property '{property}'."
        );
    }

    private static bool NextMember(
        this ref Utf8JsonReader reader,
        out string name,
        Func<string, bool> wanted
    )
    {
        _ = reader.Read();

        while (reader.TokenType == JsonTokenType.PropertyName)
        {
            name = reader.GetString()!;
            _ = reader.Read();

            if (reader.TokenType != JsonTokenType.Null && wanted(name))
            {
                return true;
            }

            reader.Skip();
            _ = reader.Read();
        }

        name = null!;
        return false;
    }
}
