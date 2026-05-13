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

            SetThingVisible(continueButton, true);
            await ToSignal(continueButton, "pressed");
            SetThingVisible(continueButton, false);

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
            
            foreach (string word in words)
            {
                textLabel.Text += word + " ";
                await Wait(wordDelay);
            }
            StopProfessorAnimation();
        }
        else if (item is CallFunction callFunctionItem)
        {
            callFunctionItem.action();
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
           new DialogueText { text = "Dialogue coming soon™!" },
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

