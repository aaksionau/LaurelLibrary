using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LaurelLibrary.Services.Abstractions.Helpers;

public class IsbnDateTimeConverter : JsonConverter<DateTime>
{
    public override DateTime Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            string? dateString = reader.GetString();

            if (string.IsNullOrEmpty(dateString))
            {
                throw new JsonException("Date string cannot be null or empty");
            }

            // Try the default JSON date parsing first
            if (reader.TryGetDateTime(out var dateTime))
            {
                return dateTime;
            }

            // Try parsing just a year (e.g., "2022")
            if (int.TryParse(dateString, out int year) && year >= 1 && year <= 9999)
            {
                return new DateTime(year, 1, 1);
            }

            // Try parsing common date formats
            string[] formats =
            {
                "MM/dd/yyyy",
                "M/d/yyyy",
                "MM/dd/yy",
                "M/d/yy",
                "dd/MM/yyyy",
                "d/M/yyyy",
                "yyyy-MM-dd",
                "yyyy/MM/dd",
            };

            foreach (string format in formats)
            {
                if (
                    DateTime.TryParseExact(
                        dateString,
                        format,
                        null,
                        System.Globalization.DateTimeStyles.None,
                        out dateTime
                    )
                )
                {
                    return dateTime;
                }
            }

            // Try general parsing as fallback
            if (DateTime.TryParse(dateString, out dateTime))
            {
                return dateTime;
            }

            throw new JsonException($"Invalid date format: {dateString}");
        }

        throw new JsonException("Expected string token for date conversion");
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString("yyyy-MM-dd"));
    }
}
