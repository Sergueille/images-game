
using System.Collections.Generic;
using System.Linq;
using Godot;


public partial class FaxManager : Node
{
    [Export] InternetMachine internetMachine;

    [Export] Node2D paperUpPosition;
    [Export] Node2D paperDownPosition;
    [Export] Node2D paperFallenPosition;
    [Export] Node2D paper;
    [Export] Node2D imagesParent;
    [Export] PackedScene moveableImageScene;
    [Export] Node2D paperMask;
    [Export] Label screenTextLabel;
    [Export] float blinkDuration;

    [Export] string queryPrefix = ">/ ";
    [Export] string cursor = "_";

    [Export] float printDuration = 1.0f;
    [Export] float fallDuration = 1.0f;

    string screenText = "";

    bool acceptInput = true;
    string inputText = "";
    Key keyPressedLastFrame;
    

    public override void _Ready()
    {
        SetReady();
    }


    public override void _Process(double delta)
    {
        Key keyPressed = Key.None;

        bool shouldShowCursor = (Time.GetUnixTimeFromSystem() % blinkDuration) > blinkDuration * 0.5f;
        string cursorString = shouldShowCursor ? cursor : "";

        int aKey = 65;
        int zKey = 90;
        for (int i = aKey; i <= zKey; i++)
        {
            if (keyPressed == Key.None && Input.IsKeyPressed((Key)i))
            {
                keyPressed = (Key)i;

                if (keyPressedLastFrame != (Key)i)
                {
                    inputText += (char)i;
                    Utils.PlayRandomSound(this, "Key", 5, 0.2f);
                }
            }
        }

        if (keyPressed == Key.None && Input.IsKeyPressed(Key.Backspace))
        {
            keyPressed = Key.Backspace;
            
            if (keyPressedLastFrame != Key.Backspace && inputText.Length > 0)
            {
                inputText = inputText[0..^1];
                Utils.PlayRandomSound(this, "Key", 5, 0.2f);
            }
        }

        if (keyPressed == Key.None && Input.IsKeyPressed(Key.Space))
        {
            keyPressed = Key.Space;
            
            if (keyPressedLastFrame != Key.Space)
            {
                inputText += ' ';
                Utils.PlayRandomSound(this, "Key", 5, 0.2f);
            }
        }

        if (Input.IsKeyPressed(Key.Enter))
        {
            if (acceptInput && inputText.Length >= 2)
            {
                Utils.PlayRandomSound(this, "Key", 5, 0.2f);
                acceptInput = false;

                ClearScreen();

                GetNode<AudioStreamPlayer>("Fan").Play();
                GetNode<AudioStreamPlayer>("Memory").Play();
                GetNode<AudioStreamPlayer>("Beeps").Play();
                
                internetMachine.RequestImage(
                    inputText.ToLower(), 
                    (img, url) => { 
                        GetNode<AudioStreamPlayer>("Fan").Stop();
                        GetNode<AudioStreamPlayer>("Memory").Stop();
                        GetNode<AudioStreamPlayer>("Beeps").Stop();
                        GetNode<AudioStreamPlayer>("MemoryEnd").Play();

                        Print(img, url); 
                        ClearScreen();
                        AddLine("Done. Please take sheet");
                    }, 
                    () => {
                        GetNode<AudioStreamPlayer>("Fan").Stop();
                        GetNode<AudioStreamPlayer>("Memory").Stop();
                        GetNode<AudioStreamPlayer>("Beeps").Stop();
                        GetNode<AudioStreamPlayer>("MemoryEnd").Play();
                        GetNode<AudioStreamPlayer>("Error").Play();
                        ClearScreen();
                        AddLine("Failed. Check your internet.");
                        SceneTreeTimer t = GetTree().CreateTimer(3.0f); // TODO: blink
                        t.Timeout += () =>
                        {
                            SetReady();
                        };
                    },
                    [new TransparencyFilter(), new PhotoFilter()],
                    (logMessage) => {
                        GetNode<AudioStreamPlayer>("Logs").Play();
                        AddLine(logMessage);
                    }
                );
            }
        }
        
        keyPressedLastFrame = keyPressed;

        if (acceptInput)
        {
            UpdateLastLine(queryPrefix + inputText.ToUpper() + cursorString);
        }
    }

    public void Print(Image image, string url)
    {
        GetNode<AudioStreamPlayer>("Print").Play();
        GetNode<AudioStreamPlayer>("Ding").Play();

        MoveableImage moveableImage = (MoveableImage)moveableImageScene.Instantiate();
        paperMask.AddChild(moveableImage);
        moveableImage.Init(image);
        
        Tween paperTween = GetTree().CreateTween();
        Tween imageTween = GetTree().CreateTween();

        moveableImage.onFirstMoveCallback = () => {
            paperTween.Stop();
            imageTween.Stop();

            Utils.PlaySound(this, "Paper", 0.2f);

            Tween t = GetTree().CreateTween().SetEase(Tween.EaseType.In).SetTrans(Tween.TransitionType.Quad);
            t.TweenProperty(paper, "global_position", paperFallenPosition.Position, fallDuration);
            moveableImage.Reparent(imagesParent);
            SetReady();
        };

        paper.GlobalPosition = paperUpPosition.Position;
        moveableImage.GlobalPosition = paperUpPosition.Position;

        paperTween.TweenProperty(paper, "global_position", paperDownPosition.Position, printDuration);
        imageTween.TweenProperty(moveableImage, "global_position", paperDownPosition.Position, printDuration);

        ManagementManager.i.currentMoveableImages.Add(moveableImage);
    }

    public void UpdateLastLine(string newValue)
    {
        string[] lines = screenText.Split("\n");
        lines[^1] = newValue;
        screenText = lines.Join("\n");
        UpdateScreen();
    }

    public void AddLine(string line)
    {
        string[] lines = screenText.Split("\n");

        if (lines.Length >= 3)
        {
            screenText = lines[^2] + "\n" + lines[^1];
        }

        if (screenText.Length > 0)
        {
            screenText += "\n";
        }

        screenText += line;
        UpdateScreen();
    }

    public void ClearScreen()
    {
        screenText = "";
        UpdateScreen();
    }

    private void UpdateScreen()
    {
        screenTextLabel.Text = screenText;
    }

    private void SetReady()
    {
        inputText = "";
        acceptInput = true;
        ClearScreen();
        AddLine("Ready.");
        AddLine(queryPrefix);
    }

}

