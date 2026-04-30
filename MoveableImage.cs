using Godot;
using System;
using System.Collections.Generic;

public partial class MoveableImage : Node2D
{
    private float imageDefaultSize = 200.0f; // Should match the size of the rectangle collider!!

    [Export] float squishAmount = 0.1f;
    [Export] float squishDuration = 0.2f;
    
    public static MoveableImage selectedImage = null;
    private static List<MoveableImage> hoveredImages = new List<MoveableImage>();

    public static List<MoveableImage> allImages = new List<MoveableImage>();
    public static bool mouseOverHandles = false;

    [Export] Sprite2D sprite;
    [Export] Node2D rotateHandle;
    [Export] Node2D scaleHandle;
    [Export] Node2D scalingNode;
    [Export] CollisionShape2D selectionShape;
    [Export] ColorControllable colorControllable;

    Vector2 handlePlacementMultiplier;
    
    bool hovered = false;
    bool mousePressedLastFrame = false;
    Vector2 lastMousePosition;
    bool isRotating = false;
    bool isScaling = false;

    Vector2 scale = Vector2.One;
    int layer = 0;

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
                Vector2 scaleDelta = new Vector2(delta.Dot(Transform.X), delta.Dot(Transform.Y)) * 2 / imageDefaultSize / handlePlacementMultiplier;
                scalingNode.Scale += scaleDelta;
                scale += scaleDelta;
            }
            else
            {
                Position += delta;
            }
        }

        if (!mousePressed)
        {
            isRotating = false;
            isScaling = false;
        }

        mousePressedLastFrame = mousePressed;
        lastMousePosition = mousePosition;

        rotateHandle.Visible = selectedImage == this;
        rotateHandle.Position = handlePlacementMultiplier * new Vector2(-1, -1) * scale * imageDefaultSize * 0.5f;
        scaleHandle.Visible = selectedImage == this;
        scaleHandle.Position = handlePlacementMultiplier * new Vector2(1, 1) * scale * imageDefaultSize * 0.5f;

        ZIndex = allImages.IndexOf(this);
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
}
