using Godot;
using System;

public partial class ManagementManager : Node
{
    [Export] InternetMachine internetMachine;
    [Export] Node2D imagesParent;
    [Export] LineEdit testLineEdit;

    [Export] float imageSize;
    

    public override void _Ready()
    {
    }

    private void OnImageResult(Image image, string link)
    {
        Sprite2D sprite = new Sprite2D();
        imagesParent.AddChild(sprite); 

        ImageTexture tex = ImageTexture.CreateFromImage(image);
        sprite.Texture = tex;

        Vector2 sizeVect = tex.GetSize();
        float maxSize = Mathf.Max(sizeVect.X, sizeVect.Y);

        sprite.Scale = Vector2.One * imageSize / maxSize;

        RandomNumberGenerator rand = new RandomNumberGenerator();
        rand.Randomize();
        sprite.Position = new Vector2((rand.Randf() * 2.0f - 1.0f) * 800, (rand.Randf() * 2.0f - 1.0f) * 500);

        sprite.Name = link;
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
}
