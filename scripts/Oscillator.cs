
using Godot;

public partial class Oscillator: Node2D
{
    [Export] float period = 1.0f;
    [Export] Vector2 direction = 30.0f * Vector2.Left;

    private Vector2 basePosition;

    public override void _Ready()
    {
        basePosition = Position;
    }

    public override void _Process(double delta)
    {
        Position = basePosition + direction * (float)Mathf.Sin(Time.GetUnixTimeFromSystem() * Mathf.Pi * 2.0 / period);
    }
}

