
using System.Collections.Generic;
using System.IO;
using Godot;
using Microsoft.VisualBasic.FileIO;

public struct Painting
{
    public string id;
    public string name;
    public string author;
    public string wikipediaUrl;
}

[Tool]
public static class PaintingFinder
{
    public static Dictionary<string, Painting> paintings;
    public static Dictionary<string, Texture2D> textures;

    public static void InitPaintings()
    {
        paintings = new Dictionary<string, Painting>();
        textures = new Dictionary<string, Texture2D>();

        Godot.FileAccess listFile = Godot.FileAccess.Open("res://new_painting_list.txt", Godot.FileAccess.ModeFlags.Read);
        string list = listFile.GetAsText();

        Stream stream = new MemoryStream();
        StreamWriter writer = new StreamWriter(stream);
        writer.Write(list);
        writer.Flush();
        stream.Position = 0;

        TextFieldParser parser = new TextFieldParser(stream);
        parser.TextFieldType = FieldType.Delimited;
        parser.Delimiters = [","];
        parser.HasFieldsEnclosedInQuotes = true;
        parser.ReadFields();

        while (!parser.EndOfData)
        {
            string[] fields = parser.ReadFields();

            if (fields.Length != 4)
            {
                throw new System.Exception("Expected 4 items on line " + parser.LineNumber);
            }

            string id = fields[0];

            paintings.Add(id, new Painting {
                id = id,
                wikipediaUrl = fields[1],
                name = fields[2],
                author = fields[3],
            });
        }
    }

    public static Painting? GetPainting(string id)
    {
        if (paintings == null) { InitPaintings(); }

        if (paintings.ContainsKey(id))
        {
            return paintings[id];
        }
        else
        {
            return null;
        }
    }

    public static Texture2D GetPaintingTexture(string id)
    {
        if (textures == null) { textures = new Dictionary<string, Texture2D>(); }
        
        if (!textures.ContainsKey(id) || textures[id] == null)
        {
            textures[id] = GD.Load<Texture2D>($"res://paintings/{id}.jpg");
        }
        
        return textures[id];
    }
}
