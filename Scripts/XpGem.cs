using Godot;
using System;

namespace NeonSurvivor;

public partial class XpGem : Node2D
{
    public int Value = 1;
    public float CollisionRadius = 8f;

    private float _time;
    private float _magnetSpeed = 0f;
    private Vector2 _driftVelocity;

    public override void _Ready()
    {
        var rng = new RandomNumberGenerator();
        rng.Randomize();
        float angle = rng.RandfRange(0, Mathf.Tau);
        _driftVelocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * rng.RandfRange(20f, 50f);
    }

    public override void _Process(double delta)
    {
        float dt = (float)delta;
        _time += dt;

        // Slow drift down
        _driftVelocity *= 1f - 2f * dt;
        Position += _driftVelocity * dt;

        // Clamp to screen
        var screenSize = GetViewportRect().Size;
        Position = new Vector2(
            Mathf.Clamp(Position.X, 5, screenSize.X - 5),
            Mathf.Clamp(Position.Y, 5, screenSize.Y - 5)
        );

        QueueRedraw();
    }

    public void MoveToward(Vector2 target, float speed, float dt)
    {
        Vector2 dir = (target - GlobalPosition);
        if (dir.LengthSquared() > 1f)
        {
            Position += dir.Normalized() * speed * dt;
        }
    }

    public override void _Draw()
    {
        float pulse = 0.7f + 0.3f * Mathf.Sin(_time * 5f);
        float size = 5f + Value * 1.5f;
        float rot = _time * 3f;

        // Glow
        Color glowColor = Value >= 3 ? new Color(0.2f, 1, 0.4f, 0.12f) : new Color(0.3f, 1, 0.3f, 0.1f);
        DrawCircle(Vector2.Zero, size + 8f, glowColor);

        // Diamond shape (rotated)
        Color gemColor = Value >= 5 ? new Color(0.4f, 0.8f, 1f) :
                         Value >= 3 ? new Color(0.3f, 1, 0.5f) :
                                      new Color(0.4f, 1, 0.4f);
        gemColor = new Color(gemColor.R, gemColor.G, gemColor.B, pulse);

        Vector2[] diamond = new Vector2[4];
        for (int i = 0; i < 4; i++)
        {
            float a = rot + i * Mathf.Tau / 4f;
            diamond[i] = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * size;
        }
        DrawColoredPolygon(diamond, gemColor);

        // Inner sparkle
        Color sparkle = new Color(1, 1, 1, 0.6f * pulse);
        DrawCircle(Vector2.Zero, size * 0.3f, sparkle);
    }
}
