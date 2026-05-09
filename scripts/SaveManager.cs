
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Godot;

public static class SaveManager
{
    private const string saveFileFolder = "user://save";
    private const string saveFilePath = "user://save/data.json";

    public class SaveData
    {
        public string currentPaintingId;
        public Dictionary<string, PaintingState> paintings;
    }

    public class PaintingState
    {
        public MoveableImage.MoveableImageState[] images;
    }

    public static void Save(SaveData save)
    {
        DirAccess.MakeDirAbsolute(ProjectSettings.GlobalizePath(saveFileFolder));

        string content = JsonSerializer.Serialize(save);
        FileAccess file = FileAccess.Open(saveFilePath, FileAccess.ModeFlags.Write);
        GD.Print($"Saving at {file.GetPathAbsolute()}");
        file.StoreString(content);
        file.Close();
    }

    public static SaveData Load()
    {
        string absolutePath = ProjectSettings.GlobalizePath(saveFilePath);
        
        if (DirAccess.DirExistsAbsolute(absolutePath))
        {
            FileAccess file = FileAccess.Open(saveFilePath, FileAccess.ModeFlags.Read);
            string content = file.GetAsText();
            file.Close();

            return JsonSerializer.Deserialize<SaveData>(content);
        }
        else
        {
            return GetStartSaveData();
        }
    }

    public static void SaveMoveableImages(SaveData save, string paintingId, IEnumerable<MoveableImage> images)
    {
        save.paintings[paintingId].images = images.Select(img => img.state).ToArray();
    }

    private static SaveData GetStartSaveData()
    {
        return new SaveData
        {
            paintings = new Dictionary<string, PaintingState>()   
        };
    }
}

