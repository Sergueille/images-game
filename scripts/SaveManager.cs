
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Godot;

public static class SaveManager
{
    public const string saveFileFolder = "user://save";
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
        [JsonInclude] public bool imageSaved;
        [JsonInclude] public Dictionary<string, float> backgroundColorProperties;
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

            // TODO: test
            try 
            {
                return JsonSerializer.Deserialize<SaveData>(content);
            }
            catch
            {
                GD.PushError("File save corrupted, starting with a fresh save");

                // Try to save at least the painting images
                try 
                {   
                    string backupFolder = saveFileFolder + "/../backup_paintings";
                    DirAccess.MakeDirAbsolute(backupFolder);
                    foreach (string filename in DirAccess.GetFilesAt(ManagementManager.paintingImagesSaveFolder))
                    {
                        DirAccess.CopyAbsolute(
                            ProjectSettings.GlobalizePath(ManagementManager.paintingImagesSaveFolder + filename), 
                            ProjectSettings.GlobalizePath(backupFolder + "/" + filename));
                    }
                } 
                catch (Exception e)
                {
                    GD.PushError("Couldn't even make a backup of paintings :( ", e);
                }

                DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(saveFileFolder)); // FIXME: check if recursive

                return GetStartSaveData();
            }
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

