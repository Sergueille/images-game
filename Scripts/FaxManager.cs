
using Godot;


public partial class FaxManager : Node
{
    [Export] Node2D paperUpPosition;
    [Export] Node2D paperDownPosition;
    [Export] Node2D paperFallenPosition;
    [Export] Node2D paper;
    [Export] Node2D imagesParent;
    [Export] PackedScene moveableImageScene;
    [Export] Node2D paperMask;

    [Export] float printDuration = 1.0f;
    [Export] float fallDuration = 1.0f;

    public void Print(Image image, string url)
    {
        MoveableImage moveableImage = (MoveableImage)moveableImageScene.Instantiate();
        paperMask.AddChild(moveableImage);
        moveableImage.Init(image);
        
        moveableImage.onFirstMoveCallback = () => {
            Tween t = GetTree().CreateTween().SetEase(Tween.EaseType.In).SetTrans(Tween.TransitionType.Quad);
            t.TweenProperty(paper, "global_position", paperFallenPosition.Position, fallDuration);
            moveableImage.Reparent(imagesParent);
        };

        paper.GlobalPosition = paperUpPosition.Position;
        moveableImage.GlobalPosition = paperUpPosition.Position;

        Tween paperTween = GetTree().CreateTween();
        paperTween.TweenProperty(paper, "global_position", paperDownPosition.Position, printDuration);

        Tween imageTween = GetTree().CreateTween();
        imageTween.TweenProperty(moveableImage, "global_position", paperDownPosition.Position, printDuration);
    }
}

