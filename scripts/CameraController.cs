
using Godot;
public partial class CameraController : Node2D
{
    bool animating = false;

    [Export] int currentScreen = 0;
    [Export] int screenCount;
    [Export] float screenSize;
    [Export] float transitionDuration;

    public static CameraController i;

    public override void _Ready()
    {
        i = this;
    }

    public override void _Process(double deltaTime)
    {
        Vector2 mousePos = DisplayServer.MouseGetPosition();
        Vector2 screenSize = DisplayServer.WindowGetSize();

        if (mousePos.X >= screenSize.X - 1)
        {
            GoRight();
        }

        if (mousePos.X <= 1)
        {
            GoLeft();
        }
    }

    public void GoRight()
    {
        if (animating) { return; }
        if (currentScreen == screenCount - 1) { return; }

        currentScreen += 1;
        MoveToRightPosition();
    }

    public void GoLeft()
    {
        if (animating) { return; }
        if (currentScreen == 0) { return; }
        
        currentScreen -= 1;
        MoveToRightPosition();
    }

    public void GoToScreen(int targetScreen)
    {
        currentScreen = targetScreen;
        MoveToRightPosition();
    }

    private void MoveToRightPosition()
    {
        animating = true;
        float targetX = currentScreen * screenSize;

        Tween tween = GetTree().CreateTween().SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.InOut);
        tween.TweenProperty(this, "position", new Vector2(targetX, 0.0f), transitionDuration);
        tween.Finished += () => {
            animating = false;
        };
    }
}


