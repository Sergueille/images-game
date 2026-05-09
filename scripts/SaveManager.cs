
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Godot;

public static class SaveManager
{
    private const string saveFileFolder = "user://save";
    private const string saveFilePath = "user://save/data.json";

    public enum GameState
    {
        TitleScreen,
        Beginning
    };

    public class SaveData
    {
        [JsonInclude] public string currentPaintingId;
        [JsonInclude] public Dictionary<string, PaintingState> paintings;
        [JsonInclude] public GameState state;
    }

    public class PaintingState
    {
        [JsonInclude] public MoveableImage.MoveableImageState[] images;
    }

    public static void Save(SaveData save)
    {
        DirAccess.MakeDirAbsolute(ProjectSettings.GlobalizePath(saveFileFolder));

        string content = JsonSerializer.Serialize(save);
        FileAccess file = FileAccess.Open(saveFilePath, FileAccess.ModeFlags.Write);
        GD.Print($"Saving at {file.GetPathAbsolute()}");
        file.StoreString(content);
        file.Close();

        GD.Print("> Saved");
    }

    public static SaveData Load()
    {        
        if (DirAccess.DirExistsAbsolute(ProjectSettings.GlobalizePath(saveFileFolder)))
        {
            FileAccess file = FileAccess.Open(saveFilePath, FileAccess.ModeFlags.Read);
            string content = file.GetAsText();
            file.Close();

            GD.Print("> Loaded from file");

            return JsonSerializer.Deserialize<SaveData>(content);
        }
        else
        {
            GD.Print("> Created default save");
            return GetStartSaveData();
        }
    }

    private static SaveData GetStartSaveData()
    {
        return new SaveData
        {
            paintings = new Dictionary<string, PaintingState>(),
            state = GameState.TitleScreen,
        };
    }
}

