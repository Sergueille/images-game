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

    [Export] ColorControllable canvasSprite;
    

    public override void _Ready()
    {
        PaintingFinder.InitPaintings();
        Painting p = PaintingFinder.GetRandomPainting();

        internetMachine.DoRequest(p.imageUrl, 4, (data) => {
            Image img = internetMachine.ImageFromData(data, p.imageUrl.Split(".")[^1]);
            ImageTexture tex = ImageTexture.CreateFromImage(img);

            paintingSprite.Texture = tex;

            Vector2 sizeVector = tex.GetSize();
            float maxSize = Mathf.Max(sizeVector.X, sizeVector.Y);

            paintingSprite.Scale = Vector2.One * 300.0f / maxSize;

            canvas.Size = sizeVector / maxSize * canvasMaxSize;
            canvas.Position = canvasCenter + canvas.Size * -0.5f;

        }, () => { /* TODO */ }, false);
    }

    public void OnTestButtonPressed()
    {
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
}
