
using Godot;
using System;

[Tool]
public partial class PaintingSelector : Node2D
{
    [Export] public string paintingID;
    [Export] float imageSize;

    [Export] Sprite2D sprite;
    [Export] Control frameControl;
    [Export] Area2D clickArea;

    [ExportToolButton("Force refresh painting list")] Callable ForceRefreshPaintings => Callable.From(() => {
        PaintingFinder.InitPaintings();
    });

    Painting? painting = null;

    string editorLastPaintingId = "";

    public override void _Ready()
    {
        SetPaintingImage();
    }

    public override void _Process(double delta)
    {
        if (Engine.IsEditorHint() && editorLastPaintingId != paintingID)
        {
            SetPaintingImage();
            editorLastPaintingId = paintingID;
        }
    }

    public void SetPaintingImage()
    {
        Painting? p = PaintingFinder.GetPainting(paintingID);
        painting = p;

        if (p == null)
        {
            GD.Print($"Unknown painting name {paintingID}. Consider refreshing painting list.");
            return;
        }

        Texture2D tex = PaintingFinder.GetPaintingTexture(paintingID);
        sprite.Texture = tex;

        Vector2 sizeVector = tex.GetSize();

        sprite.Scale = Vector2.One * imageSize / sizeVector.Y;

        Vector2 frameSize = new Vector2(sizeVector.X / sizeVector.Y, 1.0f) * imageSize;
        frameControl.Size = frameSize;
        frameControl.Position = -0.5f * frameSize;

        clickArea.Scale = frameSize;
    }

    public void OnInputEvent(Node _node, InputEvent inputEvent, int _shapeId)
    {
        if (inputEvent is InputEventMouseButton clickEvent)
        {
            if (clickEvent.ButtonIndex == MouseButton.Left)
            {
                ManagementManager.i.ShowPaintingView(painting.Value);
            }
        }
    }
}
