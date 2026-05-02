
using System.Collections.Generic;
using System.IO;
using Godot;
using Microsoft.VisualBasic.FileIO;

public struct Painting
{
    public string name;
    public string imageUrl;
    public string wikipediaUrl;
}

public static class PaintingFinder
{
    public static List<Painting> paintings;

    public static void InitPaintings()
    {
        paintings = new List<Painting>();

        Godot.FileAccess listFile = Godot.FileAccess.Open("res://painting_list.txt", Godot.FileAccess.ModeFlags.Read);
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

            paintings.Add(new Painting {
                name = fields[3],
                imageUrl = fields[2],
                wikipediaUrl = fields[1],
            });
        }
    }

    public static Painting GetRandomPainting() // TODO: remove duplicates
    {
        int id = GD.RandRange(0, paintings.Count - 1);
        return paintings[id];
    } 
}
