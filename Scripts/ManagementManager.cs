using Godot;

public partial class ManagementManager : Node
{
    [Export] InternetMachine internetMachine;
    [Export] Node2D imagesParent;
    [Export] LineEdit testLineEdit;

    [Export] Sprite2D paintingSprite;

    [Export] Control canvas;
    [Export] float canvasMaxSize;



    [Export] PackedScene moveableImageScene;
    

    public override void _Ready()
    {
        PaintingFinder.InitPaintings();
        Painting p = PaintingFinder.GetRandomPainting();

        internetMachine.DoRequest(p.imageUrl, 3, (data) => {
            Image img = internetMachine.ImageFromData(data, p.imageUrl.Split(".")[^1]);
            ImageTexture tex = ImageTexture.CreateFromImage(img);

            paintingSprite.Texture = tex;

            Vector2 sizeVector = tex.GetSize();
            float maxSize = Mathf.Max(sizeVector.X, sizeVector.Y);

            paintingSprite.Scale = Vector2.One * 300.0f / maxSize;

            canvas.Size = sizeVector / maxSize * canvasMaxSize;
            canvas.Position = canvas.Size * -0.5f;

        }, () => { /* TODO */ });
    }

    private void OnImageResult(Image image, string link)
    {
        MoveableImage moveableImage = (MoveableImage)moveableImageScene.Instantiate();
        imagesParent.AddChild(moveableImage);
        moveableImage.Init(image);
    }

    public void OnTestButtonPressed()
    {
        internetMachine.RequestImage(
            testLineEdit.Text, 
            OnImageResult, 
            () => { GD.PrintErr("Pas d'image = pas de burger"); },
            [new TransparencyFilter(), new PhotoFilter()]
        );
    }

    public void LayerUp()
    {
        if (MoveableImage.selectedImage != null)
        {
            MoveableImage.selectedImage.LayerUp();
        }
    }

    public void LayerDown()
    {
        if (MoveableImage.selectedImage != null)
        {
            MoveableImage.selectedImage.LayerDown();
        }
    }

    public void MouseEntersPalette()
    {
        MoveableImage.shouldNotDeselect = true;
    }

    public void MouseLeavesPalette()
    {
        MoveableImage.shouldNotDeselect = false;
    }
}
