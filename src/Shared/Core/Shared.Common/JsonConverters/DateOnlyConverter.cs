// ----------------------------------------------------------------------------------------------
// <copyright file="DateOnlyConverter.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Shared.Common.JsonConverters;

/// <summary>
/// <see cref="JsonConverter"/> для <see cref="DateOnly"/>.
/// </summary>
public class DateOnlyConverter : JsonConverter<DateOnly>
{
    private const string Format = "dd.MM.yyyy";

    /// <inheritdoc />
    public override DateOnly Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        return DateOnly.ParseExact(reader.GetString(), Format);
    }

    /// <inheritdoc />
    public override void Write(
        Utf8JsonWriter writer,
        DateOnly value,
        JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString(Format));
    }
}

/// <summary>
/// <see cref="JsonConverter"/> для nullable <see cref="DateOnly"/>.
/// </summary>
public class NullableDateOnlyConverter : JsonConverter<DateOnly?>
{
    private const string Format = "dd.MM.yyyy";

    /// <inheritdoc />
    public override DateOnly? Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        return DateOnly.ParseExact(reader.GetString(), Format);
    }

    /// <inheritdoc />
    public override void Write(
        Utf8JsonWriter writer,
        DateOnly? value,
        JsonSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNullValue();
        }
        else
        {
            writer.WriteStringValue(value.Value.ToString(Format));
        }
    }
}
