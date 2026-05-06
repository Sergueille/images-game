using Godot;
using System;
using System.Collections.Generic;

public partial class MoveableImage : Node2D
{
    public struct MoveableImageState
    {
        public Vector2 position;
        public Vector2 scale;
        public float skew;
        public string savedImageAbsolutePath;
    }

    private const string imagePathPrefix = "user://save/images/";
    private const float imageDefaultSize = 160.0f; // Should match the size of the rectangle collider!!
    private const float skewMax = 80; // Degrees

    [Export] float squishAmount = 0.1f;
    [Export] float squishDuration = 0.2f;
    
    public static MoveableImage selectedImage = null;
    private static List<MoveableImage> hoveredImages = new List<MoveableImage>();

    public static List<MoveableImage> allImages = new List<MoveableImage>();
    public static bool mouseOverHandles = false;

    public Action onFirstMoveCallback = null;

    [Export] Sprite2D sprite;
    [Export] Node2D rotateHandle;
    [Export] Node2D scaleHandle;
    [Export] Node2D skewHandle;
    [Export] Node2D scalingNode;
    [Export] CollisionShape2D selectionShape;
    [Export] ColorControllable colorControllable;

    Vector2 handlePlacementMultiplier;
    
    bool hovered = false;
    bool mousePressedLastFrame = false;
    Vector2 lastMousePosition;
    bool isRotating = false;
    bool isScaling = false;
    bool isSkewing = false;

    bool haveMovedYet = false;

    int layer = 0;

    public MoveableImageState state;

    public override void _EnterTree()
    {
        allImages.Add(this);
    }

    public override void _ExitTree()
    {
        allImages.Remove(this);
    }

    public void Init(Image image)
    {
        InitInternal(image);
        string path = imagePathPrefix + image.GetHashCode(); 
        byte[] buffer = image.SavePngToBuffer();
        FileAccess file = FileAccess.Open(path, FileAccess.ModeFlags.Write);
        file.StoreBuffer(buffer);
        file.Close();
        state.savedImageAbsolutePath = file.GetPathAbsolute();
    }

    public void InitFromState(MoveableImageState state)
    {
        this.state = state;
        Position = state.position;
        InitInternal(Image.LoadFromFile(state.savedImageAbsolutePath));
    }

    private void InitInternal(Image image)
    {
        ImageTexture tex = ImageTexture.CreateFromImage(image);
        sprite.Texture = tex;

        Vector2 sizeVector = tex.GetSize();
        float maxSize = Mathf.Max(sizeVector.X, sizeVector.Y);

        sprite.Scale = Vector2.One * imageDefaultSize / maxSize;

        if (sizeVector.Y > sizeVector.X)
        {
            handlePlacementMultiplier = new Vector2(sizeVector.X / sizeVector.Y, 1.0f);
        }
        else
        {
            handlePlacementMultiplier = new Vector2(1.0f, sizeVector.Y / sizeVector.X);
        }

        selectionShape.Scale = handlePlacementMultiplier;
    }

    public override void _Process(double deltaTime)
    {
        Vector2 mousePosition = GetGlobalMousePosition();

        bool mousePressed = Input.IsMouseButtonPressed(MouseButton.Left);
        if (mousePressed && !mousePressedLastFrame && !mouseOverHandles)
        {
            hoveredImages.Sort((a, b) => {
                return b.ZIndex.CompareTo(a.ZIndex);
            });

            if (hoveredImages.Count == 0)
            {
                if (selectedImage == this)
                {
                    OnDeselected();
                    selectedImage = null;
                }
            }
            else if (this == hoveredImages[0])
            {
                if (selectedImage != this)
                {
                    if (selectedImage != null) { selectedImage.OnDeselected(); }

                    selectedImage = this;
                    OnSelected();
                }
            }
        }

        if (mousePressed && selectedImage == this)
        {
            Vector2 delta = mousePosition - lastMousePosition;
            
            if (isRotating)
            {
                float angleBefore = (lastMousePosition - Position).Angle();
                float angleAfter = (mousePosition - Position).Angle();

                Rotate(angleAfter - angleBefore);
            }
            else if (isScaling)
            {
                Vector2 scaleDelta = new Vector2(delta.Dot(Transform.X), delta.Dot(Transform.Y + Mathf.Tan(state.skew) * Transform.X)) * 2 / imageDefaultSize / handlePlacementMultiplier;
                state.scale += scaleDelta;
            }
            else if (isSkewing)
            {
                float newSkew = Mathf.Atan(2.0f * delta.Dot(Transform.Y) / imageDefaultSize / handlePlacementMultiplier.X / state.scale.X + Mathf.Tan(state.skew));
                if (newSkew > skewMax * Mathf.Pi / 180) { newSkew = skewMax * Mathf.Pi / 180; }
                if (newSkew < -skewMax * Mathf.Pi / 180) { newSkew = -skewMax * Mathf.Pi / 180; }
                state.skew = newSkew;
            }
            else
            {
                Position += delta;

                if (delta != Vector2.Zero)
                {
                    if (!haveMovedYet && onFirstMoveCallback != null) { onFirstMoveCallback(); }
                    haveMovedYet = true;
                }
            }
        }

        state.position = Position;
        scalingNode.Scale = state.scale * new Vector2(1.0f / Mathf.Cos(state.skew), 1.0f);
        scalingNode.Skew = state.skew;
        scalingNode.Rotation = -state.skew;

        if (!mousePressed)
        {
            isRotating = false;
            isScaling = false;
            isSkewing = false;
        }

        mousePressedLastFrame = mousePressed;
        lastMousePosition = mousePosition;

        Vector2 bx = new Vector2(1.0f, -Mathf.Tan(state.skew)) * state.scale.X * handlePlacementMultiplier.X;
        Vector2 by = new Vector2(0.0f, 1.0f) * state.scale.Y * handlePlacementMultiplier.Y;

        rotateHandle.Position = (-bx - by) * imageDefaultSize * 0.5f;
        scaleHandle.Position = (bx + by) * imageDefaultSize * 0.5f;
        skewHandle.Position = (-bx + by) * imageDefaultSize * 0.5f;

        foreach (Node2D handle in new Node2D[] { rotateHandle, scaleHandle, skewHandle })
        {
            handle.Visible = selectedImage == this && haveMovedYet;
            
            handle.Scale = new Vector2(
                state.scale.X < 0 ? -1.0f : 1.0f,
                state.scale.Y < 0 ? -1.0f : 1.0f
            );
        }

        if (haveMovedYet)
        {
            ZIndex = allImages.IndexOf(this);
        }
    }

    private void OnMouseEnter()
    {
        hovered = true;
        hoveredImages.Add(this);
    }

    private void OnMouseLeave()
    {
        hovered = false;
        hoveredImages.Remove(this);
    }

    private void OnSelected()
    {
        Vector2 baseScale = sprite.Scale;
        Tween tween = GetTree().CreateTween();
        tween.TweenProperty(sprite, "scale", new Vector2(1.0f + squishAmount, 1.0f - squishAmount) * baseScale, squishDuration / 3.0f);
        tween.TweenProperty(sprite, "scale", new Vector2(1.0f - squishAmount, 1.0f + squishAmount) * baseScale, squishDuration / 3.0f);
        tween.TweenProperty(sprite, "scale", baseScale, squishDuration / 3.0f);
    }

    private void OnDeselected()
    {
        
    }

    private void OnRotateInput(Node node, InputEvent inputEvent, int id)
    {
        if (inputEvent is InputEventMouseButton mouseEvent && mouseEvent.ButtonIndex == MouseButton.Left && mouseEvent.Pressed)
        {
            isRotating = true;                
        }
    }

    private void OnScaleInput(Node node, InputEvent inputEvent, int id)
    {
        if (inputEvent is InputEventMouseButton mouseEvent && mouseEvent.ButtonIndex == MouseButton.Left && mouseEvent.Pressed)
        {
            isScaling = true;                
        }
    }

    private void OnSkewInput(Node node, InputEvent inputEvent, int id)
    {
        if (inputEvent is InputEventMouseButton mouseEvent && mouseEvent.ButtonIndex == MouseButton.Left && mouseEvent.Pressed)
        {
            isSkewing = true;                
        }
    }

    public void LayerUp()
    {
        int myIndex = allImages.IndexOf(this);
        if (myIndex == allImages.Count - 1) { return; }

        allImages[myIndex] = allImages[myIndex + 1];
        allImages[myIndex + 1] = this;
    }

    public void LayerDown()
    {
        int myIndex = allImages.IndexOf(this);
        if (myIndex == 0) { return; }

        allImages[myIndex] = allImages[myIndex - 1];
        allImages[myIndex - 1] = this;
    }

    public void OnHoverHandle()
    {
        mouseOverHandles = true;
    }

    public void OnLeaveHandle()
    {
        mouseOverHandles = false;
    }

    public ColorControllable GetColorControllable()
    {
        return colorControllable;
    }

    public static void ClearSave(MoveableImageState state)
    {
        DirAccess.RemoveAbsolute(state.savedImageAbsolutePath);
    }
}
