using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Godot;

public partial class ManagementManager : Node
{
    public const string paintingImagesSaveFolder = SaveManager.saveFileFolder + "/paintings/";

    [Export] FaxManager faxManager;
    [Export] InternetMachine internetMachine;
    [Export] PackedScene moveableImageScene;

    [Export] TextureRect paintingSprite;
    [Export] Node2D imagesParent;

    [Export] Control canvas;
    [Export] ColorControllable canvasColorControllable;
    [Export] float canvasMaxSize;
    [Export] Vector2 canvasCenter;

    [Export] Node2D paintingView;
    [Export] PaintingSelector paintingViewPaintingSelector;
    [Export] Label paintingViewTitle;
    [Export] Label paintingViewAuthor;

    [Export] float paintingViewShowAnimationAmount = 0.2f;
    [Export] float paintingViewShowAnimationDuration = 0.3f;

    [Export] ColorControllable canvasSprite;
    [Export] public CameraController cameraController;
    [Export] Control titleScreen;
    [Export] DialogueManager dialogueManager;
    [Export] ColorRect fadeInColorRect;
    [Export] SubViewport paintingSaveViewport;

    [Export] float saveInterval = 5.0f;
    double lastSaveTime = 0.0f;

    public SaveManager.SaveData saveData;

    Painting paintingViewPainting;
    public bool isShowingPaintingView;

    private TaskCompletionSource paintingImageSaveInternalTask;

    public List<MoveableImage> currentMoveableImages;

    public static ManagementManager i;

    public override void _Ready()
    {
        i = this;

        GetTree().AutoAcceptQuit = false;

        paintingView.Visible = false;
        paintingSprite.Visible = false;
        currentMoveableImages = new List<MoveableImage>();

        saveData = SaveManager.Load();
        if (saveData.currentPaintingId != null && saveData.currentPaintingId != "")
        {
            saveData.currentPaintingId = null; // Temporary fix: do not load current painting
            /*
            SetCurrentPainting(saveData.currentPaintingId);

            if (saveData.paintings[saveData.currentPaintingId].images != null)
            {
                foreach (MoveableImage.MoveableImageState state in saveData.paintings[saveData.currentPaintingId].images)
                {
                    MoveableImage img = (MoveableImage)moveableImageScene.Instantiate();
                    imagesParent.AddChild(img);
                    currentMoveableImages.Add(img);
                    img.InitFromState(state);
                }
            }
            */
        }

        cameraController.InitPosition();
        if (saveData.state == SaveManager.GameState.TitleScreen)
        {
            cameraController.EnableTitleScreenZoom();
        }
        titleScreen.Visible = saveData.state == SaveManager.GameState.TitleScreen;

        fadeInColorRect.Visible = true;
        Tween t = GetTree().CreateTween();
        t.TweenProperty(fadeInColorRect, "color", new Color(0.0f, 0.0f, 0.0f, 0.0f), 2.0f);
    }

    public override void _Process(double delta)
    {
        if (Time.GetUnixTimeFromSystem() - lastSaveTime > saveInterval)
        {
            lastSaveTime = Time.GetUnixTimeFromSystem();
            SaveCurrentPainting();
        }
    }
    
    public void SetCurrentPainting(string paintingId)
    {
        Texture2D tex = PaintingFinder.GetPaintingTexture(paintingId);
        paintingSprite.Texture = tex;
        paintingSprite.Visible = true;

        Vector2 sizeVector = tex.GetSize();
        float maxSize = Mathf.Max(sizeVector.X, sizeVector.Y);
        float paintingRefScale = Mathf.Min(540.0f / sizeVector.X, 500.0f / sizeVector.Y);

        paintingSprite.Size = sizeVector * paintingRefScale;

        canvas.Size = sizeVector / maxSize * canvasMaxSize;
        canvas.Position = canvasCenter + canvas.Size * -0.5f;

        saveData.currentPaintingId = paintingId;
        
        if (!saveData.paintings.ContainsKey(paintingId))
        {
            saveData.paintings[paintingId] = new SaveManager.PaintingState();
        }
    }

    public void LayerUp()
    {
        MoveableImage.selectedImage?.LayerUp();
    }

    public void LayerDown()
    {
        MoveableImage.selectedImage?.LayerDown();
    }

    public void MouseEntersPalette()
    {
        MoveableImage.mouseOverHandles = true;
    }

    public void MouseLeavesPalette()
    {
        MoveableImage.mouseOverHandles = false;
    }

    public void HuePlus()
    {
        (MoveableImage.selectedImage?.GetColorControllable() ?? canvasSprite).SetMaterialProperty(ColorControllable.MaterialProperty.Hue, true);
    }

    public void HueLess()
    {
        (MoveableImage.selectedImage?.GetColorControllable() ?? canvasSprite).SetMaterialProperty(ColorControllable.MaterialProperty.Hue, false);
    }

    public void SaturationPlus()
    {
        (MoveableImage.selectedImage?.GetColorControllable() ?? canvasSprite).SetMaterialProperty(ColorControllable.MaterialProperty.Saturation, true);
    }

    public void SaturationLess()
    {
        (MoveableImage.selectedImage?.GetColorControllable() ?? canvasSprite).SetMaterialProperty(ColorControllable.MaterialProperty.Saturation, false);
    }

    public void BrightnessPlus()
    {
        (MoveableImage.selectedImage?.GetColorControllable() ?? canvasSprite).SetMaterialProperty(ColorControllable.MaterialProperty.Brightness, true);
    }

    public void BrightnessLess()
    {
        (MoveableImage.selectedImage?.GetColorControllable() ?? canvasSprite).SetMaterialProperty(ColorControllable.MaterialProperty.Brightness, false);
    }

    public void ShowPaintingView(Painting painting)
    {
        paintingViewPainting = painting;
        paintingViewTitle.Text = painting.name;
        paintingViewAuthor.Text = painting.author;
        paintingViewPaintingSelector.paintingID = painting.id;
        paintingViewPaintingSelector.SetPaintingImage();
        paintingView.Scale = Vector2.One * (1.0f - paintingViewShowAnimationAmount);
        paintingView.Visible = true;
        isShowingPaintingView = true;

        Tween t = GetTree().CreateTween().SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Quad);
        t.TweenProperty(paintingView, "scale", Vector2.One, paintingViewShowAnimationDuration);
        
        Utils.PlaySound(this, "Whoosh", 0.2f);
    }

    public void HidePaintingView()
    {
        paintingView.Visible = false;
        isShowingPaintingView = false;
        Utils.PlaySound(this, "Whoosh", 0.2f);
    }

    public async void OnPainingViewConfirm()
    {
        if (saveData.currentPaintingId != null)
        {
            await SaveCurrentPaintingImage();
        }

        internetMachine.ResetVisitedUrls();
        SetNoCurrentPainting();
        SetCurrentPainting(paintingViewPainting.id);
        SaveManager.Save(saveData);
        HidePaintingView();
        cameraController.GoToScreen(0);
    }

    public void SaveCurrentPainting()
    {
        if (saveData.currentPaintingId == null) return;
        
        saveData.paintings[saveData.currentPaintingId].images = currentMoveableImages.Select(img => img.state).ToArray();
        SaveManager.Save(saveData);
    }

    private void SetNoCurrentPainting()
    {
        SaveCurrentPainting();

        canvasColorControllable.ResetMaterialPropertyToWhite(); 

        foreach (MoveableImage img in currentMoveableImages)
        {
           img.QueueFree(); 
        }

        currentMoveableImages.Clear();
        saveData.currentPaintingId = "";
        paintingSprite.Visible = false;
    }

    public void OnTitleScreenStart()
    {
        Tween t = GetTree().CreateTween().SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Expo);
        t.TweenProperty(titleScreen, "modulate", new Color(1.0f, 1.0f, 1.0f, 0.0f), 3.0f);
        t.Finished += () => {
            titleScreen.Visible = false;
            cameraController.EnableAwkwardZoom();
            dialogueManager.DoDialogue([
                new DialogueManager.DialogueText { text = "Hey! Are you still paying attention?" },
                new DialogueManager.DialogueText { text = "I was telling you about my little advert I put in the journal." },
                new DialogueManager.DialogueText { text = "If I remind correctly, it was because I needed to hire someone to help me move my art collection in my new house, right?" },
                new DialogueManager.DialogueText { text = "But, actually, I changed my mind." },
                new DialogueManager.DialogueText { text = "Those paintings are way too old and begin to deteriorate." },
                new DialogueManager.DialogueText { text = "I'm afraid I will have to throw them away." },
                new DialogueManager.DialogueText { text = "So, now I need someone to make reproductions of my paintings for my collection." },
                new DialogueManager.DialogueText { text = "Nothing exceptional of course! As you can see..." },
                new DialogueManager.DialogueText { text = "... it's just a couple of small paintings.", unZoom = true },
                new DialogueManager.DialogueText { text = "I know you didn't came here for this specific job, but, you know, I didn't want to bother putting another ad in the journal." },
                new DialogueManager.DialogueText { text = "They're getting really expensive these days!" },
                new DialogueManager.DialogueText { text = "Anyway! Are you ready to do some painting?" },
                new DialogueManager.DialogueText { text = "..." },
                new DialogueManager.DialogueText { text = "What do you mean, \"you can't draw\"?" },
                new DialogueManager.DialogueText { text = "Of course, I won't let you on your own." },
                new DialogueManager.DialogueText { text = "I have build a special device to help you with this job!" },
                new DialogueManager.DialogueText { text = "It's still a prototype, but it's fully functional." },
                new DialogueManager.DialogueText { text = "Just type in the thing you want to draw, and the machine will do the rest!" },
                new DialogueManager.DialogueText { text = "But keep in mind that the machine can only handle simple and generic objects." },
                new DialogueManager.DialogueText { text = "Just give it a word or two, and it should work just fine!" },
                new DialogueManager.DialogueText { text = "Anyway! You should be able to figure it out on your own!" },
                new DialogueManager.DialogueText { text = "Just take the painting you want to start with, and go to the room on your left, everything is already set up!" },
                new DialogueManager.DialogueText { text = "Good luck! Don't hesitate to ask for advice." },
            ]);

            saveData.state = SaveManager.GameState.Beginning;
        };
    }

    public Task SaveCurrentPaintingImage()
    {
        paintingImageSaveInternalTask = new TaskCompletionSource();

        if (saveData.currentPaintingId == null || currentMoveableImages.Count == 0)
        { 
            paintingImageSaveInternalTask.SetResult(); 
            return paintingImageSaveInternalTask.Task;
        }

        SaveCurrentPainting();
        paintingSaveViewport.Size = new Vector2I((int)canvas.Size.X, (int)canvas.Size.Y);

        List<MoveableImage> imagesListBackup = MoveableImage.allImages.ToList();

        for (int i = 0; i < currentMoveableImages.Count; i++)
        {
            MoveableImage mi = currentMoveableImages[i];
            Vector2 pos = mi.Position - canvas.Position;
            mi.Reparent(paintingSaveViewport);
            mi.Position = pos;
        }

        MoveableImage.allImages = imagesListBackup;

        DirAccess.MakeDirAbsolute(ProjectSettings.GlobalizePath(paintingImagesSaveFolder));
        
        paintingSaveViewport.RenderTargetUpdateMode = SubViewport.UpdateMode.Once;

        RenderingServer.FramePostDraw += SavePaintingImageInternalCallback;

        return paintingImageSaveInternalTask.Task;
    }

    private async void SavePaintingImageInternalCallback() 
    {
        string userPath = paintingImagesSaveFolder + saveData.currentPaintingId + ".png";
        RenderingServer.FramePostDraw -= SavePaintingImageInternalCallback;
        Texture2D tex = paintingSaveViewport.GetTexture();
        Image image = tex.GetImage();
        image.SavePng(userPath);
        saveData.paintings[saveData.currentPaintingId].imageSaved = true;

        List<MoveableImage> imagesListBackup = MoveableImage.allImages.ToList();

        for (int i = 0; i < currentMoveableImages.Count; i++)
        {
            MoveableImage mi = currentMoveableImages[i];
            Vector2 pos = mi.Position + canvas.Position;
            mi.Reparent(imagesParent);
            mi.Position = pos;
            mi.ZIndex = imagesListBackup.IndexOf(mi);
        }
        
        MoveableImage.allImages = imagesListBackup;
        
        await ToSignal(GetTree(), "process_frame");

        paintingImageSaveInternalTask.SetResult();
    }

    public override async void _Notification(int what)
    {
        if (what == NotificationWMCloseRequest)
        {
            SaveCurrentPainting();
            await SaveCurrentPaintingImage();
            SaveManager.Save(saveData);
            GetTree().Quit();
        }
    }
}
