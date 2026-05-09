
using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Godot;

public class Vector2JsonConverter : JsonConverter<Vector2>
{
    public override bool CanConvert(Type typeToConvert)
    {
        return true;
    }

    public override Vector2 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        string[] coords = reader.GetString()!.Split(";");
        return new Vector2(coords[0].ToFloat(), coords[1].ToFloat());
    }

    public override void Write(Utf8JsonWriter writer, Vector2 value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.X + ";" + value.Y); 
    }
}

