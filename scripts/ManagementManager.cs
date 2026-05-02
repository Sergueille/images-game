using Godot;

public partial class ManagementManager : Node
{
    [Export] FaxManager faxManager;
    [Export] InternetMachine internetMachine;
    [Export] LineEdit testLineEdit;

    [Export] Sprite2D paintingSprite;

    [Export] Control canvas;
    [Export] float canvasMaxSize;
    [Export] Vector2 canvasCenter;

    [Export] Node2D paintingView;
    [Export] Sprite2D paintingViewSprite;
    [Export] Label paintingViewTitle;
    [Export] Label paintingViewAuthor;

    [Export] ColorControllable canvasSprite;

    public static ManagementManager i;

    public override void _Ready()
    {
        i = this;
        paintingView.Visible = false;
    }
    
    public void SetReferencePainting(string paintingId)
    {
        Texture2D tex = PaintingFinder.GetPaintingTexture(paintingId);
        paintingSprite.Texture = tex;

        Vector2 sizeVector = tex.GetSize();
        float maxSize = Mathf.Max(sizeVector.X, sizeVector.Y);

        paintingSprite.Scale = Vector2.One * 300.0f / maxSize;

        canvas.Size = sizeVector / maxSize * canvasMaxSize;
        canvas.Position = canvasCenter + canvas.Size * -0.5f;
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
        paintingViewTitle.Text = painting.name;
        paintingViewAuthor.Text = painting.author;
        paintingSprite.Texture = PaintingFinder.GetPaintingTexture(painting.id);
        paintingView.Visible = true;
    }

    public void HidePaintingView(Painting painting)
    {
        paintingView.Visible = false;
    }
}
