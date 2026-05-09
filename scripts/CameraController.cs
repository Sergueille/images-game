
using Godot;
public partial class CameraController : Camera2D
{
    bool animating = false;

    [Export] int currentScreen = 0;
    [Export] int screenCount;
    [Export] float screenSize;
    [Export] public float transitionDuration;

    [Export] Node2D zoomPosition;
    [Export] Node2D titleScreenZoomPosition;
    [Export] float zoomAmount;
    [Export] float titleScreenZoomAmount;
    [Export] float zoomTransitionDuration;

    Vector2 positionBeforeZoom;
    public bool isZooming = false;

    bool titleScreenZoom = false;

    public void InitPosition()
    {
        Position = new Vector2(currentScreen * screenSize, 0.0f);
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
        if (animating || isZooming || ManagementManager.i.isShowingPaintingView) { return; }
        if (currentScreen == screenCount - 1) { return; }

        currentScreen += 1;
        MoveToRightPosition();
    }

    public void GoLeft()
    {
        if (animating || isZooming || ManagementManager.i.isShowingPaintingView) { return; }
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

    public void EnableTitleScreenZoom()
    {
        positionBeforeZoom = Position;
        titleScreenZoom = true;
        Zoom = titleScreenZoomAmount * Vector2.One;
        Position = titleScreenZoomPosition.Position;
        isZooming = true;
    }

    public void EnableAwkwardZoom()
    {
        if (!isZooming && !titleScreenZoom) { positionBeforeZoom = Position; }
        MoveCameraToPoint(zoomPosition.Position, zoomAmount);
        isZooming = true;
    }

    public void DisableAwkwardZoom()
    {
        MoveCameraToPoint(positionBeforeZoom, 1.0f);
        isZooming = false;
    }

    public void MoveCameraToPoint(Vector2 position, float zoom)
    {
        Tween tPos = GetTree().CreateTween().SetEase(Tween.EaseType.InOut).SetTrans(Tween.TransitionType.Quad);
        Tween tZoom = GetTree().CreateTween().SetEase(Tween.EaseType.InOut).SetTrans(Tween.TransitionType.Quad);
        tPos.TweenProperty(this, "zoom", zoom * Vector2.One, zoomTransitionDuration);
        tZoom.TweenProperty(this, "position", position, zoomTransitionDuration);
    }
}


