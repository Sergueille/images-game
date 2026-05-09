
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
    float editorLastImageSize = -1.0f;
    string lastSelectedPaintingID;

    public override void _Ready()
    {
        SetPaintingImage();
    }

    public override void _Process(double delta)
    {
        if (Engine.IsEditorHint() && (editorLastPaintingId != paintingID || imageSize != editorLastImageSize))
        {
            SetPaintingImage();
            editorLastPaintingId = paintingID;
            editorLastImageSize = imageSize;
        }

        if (!Engine.IsEditorHint())
        {
            sprite.Visible = ManagementManager.i.saveData.currentPaintingId != paintingID;

            if (ManagementManager.i.saveData.paintings != null && ManagementManager.i.saveData.paintings.ContainsKey(paintingID))
            {
                string currentPaintingId = ManagementManager.i.saveData.currentPaintingId;
                if (lastSelectedPaintingID != ManagementManager.i.saveData.currentPaintingId
                    && (currentPaintingId == paintingID || lastSelectedPaintingID == paintingID))
                {
                    SetPaintingImage();
                }

                lastSelectedPaintingID = currentPaintingId;
            }
        } 
    }

    public void SetPaintingImage()
    {
        Texture2D tex;

        Painting? p = PaintingFinder.GetPainting(paintingID);
        painting = p;

        bool imageSaved =
            ManagementManager.i.saveData.paintings != null 
         && ManagementManager.i.saveData.paintings.ContainsKey(paintingID)
         && ManagementManager.i.saveData.paintings[paintingID].imageSaved;

        if (imageSaved)
        {
            Image img = Image.LoadFromFile(ManagementManager.paintingImagesSaveFolder + paintingID + ".png");
            tex = ImageTexture.CreateFromImage(img);
        }
        else
        {

            if (p == null)
            {
                GD.Print($"Unknown painting name {paintingID}. Consider refreshing painting list.");
                return;
            }

            tex = PaintingFinder.GetPaintingTexture(paintingID);
        }

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
            if (clickEvent.ButtonIndex == MouseButton.Left 
             && clickEvent.Pressed 
             && !ManagementManager.i.cameraController.isZooming
             && !ManagementManager.i.isShowingPaintingView)
            {
                ManagementManager.i.ShowPaintingView(painting.Value);
            }
        }
    }
}
