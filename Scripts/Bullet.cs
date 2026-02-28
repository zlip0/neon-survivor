using Godot;
using System;

namespace NeonSurvivor;

public partial class Bullet : Node2D
{
    public Vector2 Direction;
    public float Speed = 500f;
    public float Damage = 1f;
    public float Scale_ = 1f;
    public int PierceLeft = 0;
    public float CollisionRadius => 5f * Scale_;

    private Vector2 _screenSize;
    private float _life;

    public override void _Ready()
    {
        _screenSize = GetViewportRect().Size;
    }

    public override void _Process(double delta)
    {
        float dt = (float)delta;
        _life += dt;
        Position += Direction * Speed * dt;

        // Destroy if off-screen with margin
        float margin = 40f;
        if (Position.X < -margin || Position.X > _screenSize.X + margin ||
            Position.Y < -margin || Position.Y > _screenSize.Y + margin)
        {
            QueueFree();
            return;
        }

        // Max lifetime safety
        if (_life > 5f)
        {
            QueueFree();
            return;
        }

        QueueRedraw();
    }

    public bool OnHitEnemy()
    {
        // Returns true if bullet should be destroyed
        if (PierceLeft > 0)
        {
            PierceLeft--;
            return false;
        }
        return true;
    }

    public override void _Draw()
    {
        float radius = 4f * Scale_;
        // Glow
        DrawCircle(Vector2.Zero, radius + 6f, new Color(1, 1, 0, 0.1f));
        DrawCircle(Vector2.Zero, radius + 3f, new Color(1, 1, 0, 0.2f));
        // Core
        DrawCircle(Vector2.Zero, radius, new Color(1, 1, 0.3f, 0.9f));
        DrawCircle(Vector2.Zero, radius * 0.5f, new Color(1, 1, 0.8f, 1f));

        // Trail
        Vector2 trail = -Direction * (12f * Scale_);
        DrawLine(Vector2.Zero, trail, new Color(1, 0.8f, 0, 0.4f), 2f * Scale_);
    }
}
