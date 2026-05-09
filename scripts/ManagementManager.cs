using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class ManagementManager : Node
{
    [Export] FaxManager faxManager;
    [Export] InternetMachine internetMachine;
    [Export] PackedScene moveableImageScene;

    [Export] Sprite2D paintingSprite;

    [Export] Control canvas;
    [Export] float canvasMaxSize;
    [Export] Vector2 canvasCenter;

    [Export] Node2D paintingView;
    [Export] PaintingSelector paintingViewPaintingSelector;
    [Export] Label paintingViewTitle;
    [Export] Label paintingViewAuthor;

    [Export] float paintingViewShowAnimationAmount = 0.2f;
    [Export] float paintingViewShowAnimationDuration = 0.3f;

    [Export] ColorControllable canvasSprite;

    SaveManager.SaveData saveData;

    Painting paintingViewPainting;

    public List<MoveableImage> currentMoveableImages;

    public static ManagementManager i;

    public override void _Ready()
    {
        i = this;
        paintingView.Visible = false;
        currentMoveableImages = new List<MoveableImage>();

        saveData = SaveManager.Load();
        if (saveData.currentPaintingId != null && saveData.currentPaintingId != "")
        {
            SetCurrentPainting(saveData.currentPaintingId);
            foreach (MoveableImage.MoveableImageState state in saveData.paintings[saveData.currentPaintingId].images)
            {
                MoveableImage img = (MoveableImage)moveableImageScene.Instantiate();
                img.InitFromState(state);
            }
        }
    }
    
    public void SetCurrentPainting(string paintingId)
    {
        SetNoCurrentPainting();

        Texture2D tex = PaintingFinder.GetPaintingTexture(paintingId);
        paintingSprite.Texture = tex;

        Vector2 sizeVector = tex.GetSize();
        float maxSize = Mathf.Max(sizeVector.X, sizeVector.Y);

        paintingSprite.Scale = Vector2.One * 300.0f / maxSize;

        canvas.Size = sizeVector / maxSize * canvasMaxSize;
        canvas.Position = canvasCenter + canvas.Size * -0.5f;

        saveData.currentPaintingId = paintingId;
        SaveManager.Save(saveData);
    }

    public void LayerUp()
    {
        MoveableImage.selectedImage?.LayerUp();
    }

    public void LayerDown()
    {
        MoveableImage.selectedImage?.LayerDown();
    }

    public void MouseEntersPalette()
    {
        MoveableImage.mouseOverHandles = true;
    }

    public void MouseLeavesPalette()
    {
        MoveableImage.mouseOverHandles = false;
    }

    public void HuePlus()
    {
        (MoveableImage.selectedImage?.GetColorControllable() ?? canvasSprite).SetMaterialProperty(ColorControllable.MaterialProperty.Hue, true);
    }

    public void HueLess()
    {
        (MoveableImage.selectedImage?.GetColorControllable() ?? canvasSprite).SetMaterialProperty(ColorControllable.MaterialProperty.Hue, false);
    }

    public void SaturationPlus()
    {
        (MoveableImage.selectedImage?.GetColorControllable() ?? canvasSprite).SetMaterialProperty(ColorControllable.MaterialProperty.Saturation, true);
    }

    public void SaturationLess()
    {
        (MoveableImage.selectedImage?.GetColorControllable() ?? canvasSprite).SetMaterialProperty(ColorControllable.MaterialProperty.Saturation, false);
    }

    public void BrightnessPlus()
    {
        (MoveableImage.selectedImage?.GetColorControllable() ?? canvasSprite).SetMaterialProperty(ColorControllable.MaterialProperty.Brightness, true);
    }

    public void BrightnessLess()
    {
        (MoveableImage.selectedImage?.GetColorControllable() ?? canvasSprite).SetMaterialProperty(ColorControllable.MaterialProperty.Brightness, false);
    }

    public void ShowPaintingView(Painting painting)
    {
        paintingViewPainting = painting;
        paintingViewTitle.Text = painting.name;
        paintingViewAuthor.Text = painting.author;
        paintingViewPaintingSelector.paintingID = painting.id;
        paintingViewPaintingSelector.SetPaintingImage();
        paintingView.Scale = Vector2.One * (1.0f - paintingViewShowAnimationAmount);
        paintingView.Visible = true;

        Tween t = GetTree().CreateTween().SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Quad);
        t.TweenProperty(paintingView, "scale", Vector2.One, paintingViewShowAnimationDuration);
    }

    public void HidePaintingView()
    {
        paintingView.Visible = false;
    }

    public void OnPainingViewConfirm()
    {
        internetMachine.ResetVisitedUrls();
        SetCurrentPainting(paintingViewPainting.id);
        HidePaintingView();
        CameraController.i.GoToScreen(0);
    }

    public void SaveCurrentPainting()
    {
        saveData.paintings[saveData.currentPaintingId].images = currentMoveableImages.Select(img => img.state).ToArray();
        SaveManager.Save(saveData);
    }

    private void SetNoCurrentPainting()
    {
        foreach (MoveableImage img in currentMoveableImages)
        {
           img.QueueFree(); 
        }

        currentMoveableImages.Clear();
        saveData.currentPaintingId = "";
    }
}
