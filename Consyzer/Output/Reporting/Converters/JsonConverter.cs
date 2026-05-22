using System.Text.Json;
using System.Text.Json.Serialization;

namespace Consyzer.Output.Reporting.Converters;

internal class JsonFileInfoConverter : JsonConverter<FileInfo>
{
    public override FileInfo? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var path = reader.GetString();
        return path is null ? null : new FileInfo(path);
    }

    public override void Write(Utf8JsonWriter writer, FileInfo value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.FullName);
    }
}

internal sealed class JsonEnumConverter<T> : JsonConverter<T> where T : struct, Enum
{
    public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var enumText = reader.GetString();

        if (Enum.TryParse<T>(enumText, ignoreCase: true, out var value))
        {
            return value;
        }

        return default;
    }

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}
