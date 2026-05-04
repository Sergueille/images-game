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

    [Export] float professorAnimationAmount;
    [Export] float professorAnimationDuration;

    [Export] float wordDelay;
    [Export] float sizeChangeDuration;

    int lastLineCount = -1;
    bool textVisible = false;

    Vector2 textParentBottomPosition;

    Tween professorAnimationTween;
    bool shouldContinueProfessorAnimation = false;

    public abstract class DialogueItem { };
    
    public class DialogueText : DialogueItem
    {
        public string text;
    };

    public override async void _Ready()
    {
        textParent.Scale = Vector2.Zero;
        continueButton.Scale = Vector2.Zero;

        textParentBottomPosition = textParent.Position + textParent.Size.Y * Vector2.Down;

        // TEST
        await Wait(1.0f);

        DoDialogue([
            new DialogueText{ text = "Bonjour, je suis un dialogue de test." },
            new DialogueText{ text = "Je vais parler super longtemps pour être sure que le retour à la ligne marche, fonctionne!" },
            new DialogueText{ text = "Je n'ai aucune idée de ce que je raconte..." }
        ]);
    }

    public override void _Process(double delta)
    {
        UpdateParentSize();
    }

    public async void DoDialogue(IEnumerable<DialogueItem> dialogue)
    {
        CameraController.i.EnableAwkwardZoom();
        
        await Wait(sizeChangeDuration);

        foreach (DialogueItem item in dialogue)
        {
            await HandleDialogueItem(item);

            SetThingVisible(continueButton, true);
            await ToSignal(continueButton, "pressed");
            SetThingVisible(continueButton, false);
        }

        SetThingVisible(textParent, false);
        CameraController.i.DisableAwkwardZoom();
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
}

