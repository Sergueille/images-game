
using Godot;
using System;

[Tool]
public partial class PaintingSelector : Node2D
{
    [Export] string paintingID;
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
        float maxSize = Mathf.Max(sizeVector.X, sizeVector.Y);

        sprite.Scale = Vector2.One * imageSize / maxSize;

        Vector2 frameSize = sizeVector.X > sizeVector.Y 
            ? new Vector2(imageSize, imageSize * sizeVector.Y / sizeVector.X)
            : new Vector2(imageSize * sizeVector.X / sizeVector.Y, imageSize);
        frameControl.Size = frameSize;
        frameControl.Position = -0.5f * frameSize;

        clickArea.Scale = frameSize;
    }

    public void OnClick()
    {
        ManagementManager.i.ShowPaintingView(painting.Value);
    }
}
