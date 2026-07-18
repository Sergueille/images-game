using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

public partial class DialogueManager : Node
{
    [Export] Control textParent;
    [Export] Label textLabel;
    [Export] Button continueButton;
    [Export] Node2D professor;
    [Export] Control buttonsParent;
    [Export] Area2D clickPreventionArea;
    [Export] float professorAnimationAmount;
    [Export] float professorAnimationDuration;

    [Export] float wordDelay;
    [Export] float sizeChangeDuration;

    int lastLineCount = -1;
    bool textVisible = false;

    Vector2 textParentBottomPosition;

    Tween professorAnimationTween;
    bool shouldContinueProfessorAnimation = false;

    public abstract class DialogueItem 
    { 
        public bool unZoom = false;
    };
    
    public class DialogueText : DialogueItem
    {
        public string text;
    };

    public class CallFunction : DialogueItem
    {
        public Action action;
    };

    public class WaitForClick : DialogueItem
    {
        public float delay;
    };

    public override async void _Ready()
    {
        textParent.Scale = Vector2.Zero;
        continueButton.Scale = Vector2.Zero;
        clickPreventionArea.Visible = false;
        buttonsParent.Visible = false;

        textParentBottomPosition = textParent.Position + textParent.Size.Y * Vector2.Down;

        // TEST
        /*
        await Wait(1.0f);

        DoDialogue([
            new DialogueText{ text = "Bonjour, je suis un dialogue de test." },
            new DialogueText{ text = "Je vais parler super longtemps pour être sure que le retour à la ligne marche, fonctionne!" },
            new DialogueText{ text = "Je n'ai aucune idée de ce que je raconte..." }
        ]);
        */
    }

    public override void _Process(double delta)
    {
        UpdateParentSize();
    }

    public async void DoDialogue(IEnumerable<DialogueItem> dialogue)
    {
        clickPreventionArea.Visible = true;
        ManagementManager.i.cameraController.EnableAwkwardZoom();
        
        await Wait(sizeChangeDuration);

        foreach (DialogueItem item in dialogue)
        {
            if (item.unZoom)
            {
                ManagementManager.i.cameraController.DisableAwkwardZoom();   
                ManagementManager.i.cameraController.isZooming = true; // Make the camera controller believe it's zoomed to prevent clicks
                await Wait(ManagementManager.i.cameraController.transitionDuration);
            }

            await HandleDialogueItem(item);

            if (item.unZoom)
            {
                ManagementManager.i.cameraController.EnableAwkwardZoom();
                await Wait(ManagementManager.i.cameraController.transitionDuration);
            }
        }

        SetThingVisible(textParent, false);
        ManagementManager.i.cameraController.DisableAwkwardZoom();
        clickPreventionArea.Visible = false;
    }

    public async Task HandleDialogueItem(DialogueItem item)
    {
        if (item is DialogueText textItem)
        {
            StartProfessorAnimation();
            SetThingVisible(textParent, true);
            string[] words = textItem.text.Split(' ');
            textLabel.Text = "";
            
            int last = -1;
            for (int i = 0; i < words.Length; i++)
            {
                textLabel.Text += words[i] + " ";
                if (i % 3 == 0)
                {   
                    last = Utils.PlayRandomSound(this, "Old", 13, 0.1f, last);
                }
                await Wait(wordDelay);
            }
            StopProfessorAnimation();

            SetThingVisible(continueButton, true);
            await ToSignal(continueButton, "pressed");
            SetThingVisible(continueButton, false);
        }
        else if (item is CallFunction callFunctionItem)
        {
            callFunctionItem.action();
        }
        else if (item is WaitForClick clickItem)
        {
            await Wait(clickItem.delay);
            
            while (!Input.IsActionJustPressed("click"))
            {
                await ToSignal(GetTree(), "process_frame");
            }
        }
        else { throw new NotImplementedException(); }
    }

    private Task Wait(float duration)
    {
        TaskCompletionSource t = new TaskCompletionSource();
        GetTree().CreateTimer(duration).Timeout += t.SetResult;
        return t.Task;
    }

    private void UpdateParentSize()
    {
        int lineCount = textLabel.GetLineCount();
        if (lineCount == lastLineCount)
        {
            return;
        }

        lastLineCount = lineCount;

        float labelHeight = textLabel.GetLineCount() * (textLabel.GetLineHeight() + textLabel.LabelSettings.LineSpacing) + 5.0f;

        Tween t = GetTree().CreateTween().SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Quart);
        t.TweenProperty(textParent, "size", new Vector2(textParent.Size.X, labelHeight), sizeChangeDuration);
        Tween tPos = GetTree().CreateTween().SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Quart);
        tPos.TweenProperty(textParent, "position", textParentBottomPosition + labelHeight * Vector2.Up, sizeChangeDuration);
    }

    public void SetThingVisible(CanvasItem thing, bool visible)
    {
        Tween t = GetTree().CreateTween().SetEase(Tween.EaseType.InOut).SetTrans(Tween.TransitionType.Quart);
        t.TweenProperty(thing, "scale", visible ? Vector2.One : Vector2.Zero, sizeChangeDuration);
    }

    private void StartProfessorAnimation()
    {
        shouldContinueProfessorAnimation = true;
        professorAnimationTween = GetTree().CreateTween().SetTrans(Tween.TransitionType.Quad);
        professorAnimationTween.TweenProperty(professor, "scale", new Vector2(1.0f + professorAnimationAmount, 1.0f - professorAnimationAmount), professorAnimationDuration / 2.0f);
        professorAnimationTween.TweenProperty(professor, "scale", new Vector2(1.0f - professorAnimationAmount, 1.0f + professorAnimationAmount), professorAnimationDuration / 2.0f);

        professorAnimationTween.Finished += () => {
            if (shouldContinueProfessorAnimation)
            {
                StartProfessorAnimation();
            }
            else
            {
                GetTree().CreateTween().SetTrans(Tween.TransitionType.Quad).TweenProperty(professor, "scale", Vector2.One, professorAnimationDuration / 2.0f);
            }
        };
    }

    private void StopProfessorAnimation()
    {
        shouldContinueProfessorAnimation = false;
    }

    public void PaintingDoneButton()
    {
        HideButtons();
        DoDialogue([
           new DialogueText { text = "You're done already? Great!" }, 
           new DialogueText { text = "Feel free to pick another painting." }, 
           new DialogueText { text = "You don't need to tell me each time you finish one, just do them all in the order you like." },
        ]);
    }

    public void MachineBrokenButton()
    {
        HideButtons();
        DoDialogue([
           new DialogueText { text = "Aha! Having trouble with my creation?" },
           new DialogueText { text = "So, you've figured out how to type text into the machine, with your keyboard, right?" },
           new DialogueText { text = "Okay, and the machine refuses to print anything? It can happen sometimes." },
           new DialogueText { text = "The machine needs to communicate with the internet, you know, and sometimes the reception isn't good enough." },
           new DialogueText { text = "Try to *think very hard* about the internet reception, and it might solve the problem." },
           new DialogueText { text = "Also, it could be because the thing you asked is way too far-fetched and the machine doesn't understand." },
           new DialogueText { text = "If you're sure it's not because one of these reasons, then maybe the machine is simply broken." },
           new DialogueText { text = "As the old saying goes, \"The search engine's gift is never true, for its API's old before it's new\"." },
        ]);
    }

    public void MachineBadButton()
    {
        HideButtons();
        DoDialogue([
           new DialogueText { text = "Not getting the images you want?" },
           new DialogueText { text = "Try to stick with simple words and adjectives." },
           new DialogueText { text = "If you have something specific in mind, always try to ask for small image parts." },
           new DialogueText { text = "You can ask for a body part instead of a whole person, or a branch instead of the whole tree." },
           new DialogueText { text = "Try to be creative!" },
        ]);
    }

    public void PaletteHelpButton()
    {
        HideButtons();
        DoDialogue([
           new DialogueText { text = "Trouble using the palette?" },
           new DialogueText { text = "The way it works can indeed be a bit confusing. Here is a recap:" },
           new DialogueText { text = "You can change the color of objects in three ways: you can change the hue, the saturation or the brightness." },
           new DialogueText { text = "Changing the hue will change the global color of the object, a blue image will become red, or green for instance." },
           new DialogueText { text = "Saturation controls how much color there is in the image. At its minimum, the image will be black and white." },
           new DialogueText { text = "And the brightness corresponds to, mmh..., the brightness." },
           new DialogueText { text = "For the background, there are colors ready to be used directly." },
           new DialogueText { text = "But if you didn't select any image, you can also use the rest of the palette to change the color of the background at your will!" },
        ]);
    }

    public void AllDoneButton()
    {
        HideButtons();
        ManagementManager.i.SetNoCurrentPainting();
        DoDialogue([
           new DialogueText { text = "You're finished, really? Wonderful!" }, 
           new DialogueText { text = "I saw you took some liberty while reproducing some of the paintings..." }, 
           new DialogueText { text = "I was skeptical at first but I'm definitely satisfied with how it looks." }, 
           new DialogueText { text = "You made a great job!", unZoom = true }, 
           new DialogueText { text = "I can finally enjoy these new, fresh paintings and throw away the old ones!" }, 
           new DialogueText { text = "A waste, these original copies, you say? Well, maybe you want to take them with you?" }, 
           new DialogueText { text = "They're worthless to me now, but they could make a fine decoration for your appartment!" }, 
           new DialogueText { text = "Great, that gets rid of it for me!" }, 
           new DialogueText { text = "Anyway, I have to say goodbye now because I have golf half an hour." }, 
           new DialogueText { text = "Thanks again!" }, 
           new CallFunction { action = () => { ManagementManager.i.CreditsRoll(); } }
        ]);
    }

    public void ShowButtonsAndZoom()
    {
        clickPreventionArea.Visible = true;
        ManagementManager.i.cameraController.EnableAwkwardZoom();
        buttonsParent.Visible = true;
    }

    public void HideButtons()
    {
        buttonsParent.Visible = false;
    }

    public void CancelButtons()
    {
        HideButtons();
        ManagementManager.i.cameraController.DisableAwkwardZoom();
    }

    public void DialogueAreaMouseEvent(Node _0, InputEvent inputEvent, int _1)
    {
        if (inputEvent is InputEventMouseButton buttonEvent)
        {
            if (buttonEvent.ButtonIndex == MouseButton.Left 
             && buttonEvent.Pressed 
             && !ManagementManager.i.isShowingPaintingView
             && !ManagementManager.i.cameraController.isZooming)
            {
                ShowButtonsAndZoom();
            }
        }
    }
}

