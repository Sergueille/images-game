using System.Collections.Generic;
using Godot;


public partial class MusicManager : Node
{
    [Export] AudioStreamPlayer player;
    [Export] AudioStream[] songs;
    [Export] CameraController camera;
    [Export] Node2D phonograph;


    int currentSong = 0;
    bool preventPhonographClick = false;

    public bool doNotStartNewTracks = false;
    public float volumeOverride = 1.0f;

    float baseVolume;


    public override void _Ready()
    {
        player.Finished += OnPlayerFinished;
        baseVolume = player.VolumeLinear;
    }

    public override void _Process(double deltaTime)
    {
        if (camera.currentScreen == 1)
        {
            player.Bus = "Music hall";
        }
        else
        {
            player.Bus = "Music room";
        }

        player.VolumeLinear = baseVolume * volumeOverride;
    }

    public void StartPlaying()
    {
        currentSong = -1;
        PlayNextSong();
    }

    public void PlayNextSong()
    {
        if (doNotStartNewTracks) return;

        currentSong += 1;
        currentSong %= songs.Length;

        player.Stream = songs[currentSong];
        player.Play();
    }

    private void OnPlayerFinished()
    {
        PlayNextSong();
    }

    public void OnPhonographInputEvent(Node viewport, InputEvent e, int shapeId)
    {
        if (e is InputEventMouseButton clickEvent)
        {
            if (clickEvent.ButtonIndex == MouseButton.Left && !preventPhonographClick)
            {
                preventPhonographClick = true;
                player.Stop();
                Utils.PlaySound(this, "scratch", 0.0f);

                Vector2 initialScale = phonograph.Scale;
                Tween t = GetTree().CreateTween();
                t.TweenProperty(phonograph, "scale", new Vector2(0.8f, 1.1f) * initialScale, 0.1).SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Expo);
                t.TweenProperty(phonograph, "scale", initialScale, 0.2).SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Expo);

                GetTree().CreateTimer(1.0).Timeout += () =>
                {
                    preventPhonographClick = false;
                    PlayNextSong();
                };
            }
        }
    }
}

