using Godot;
using System;

namespace NeonSurvivor;

public enum EnemyType
{
    Chaser,    // Red triangle - follows player
    Drifter,   // Purple square - random drift, bounces
    Sprinter,  // Orange diamond - fast, low HP
    Tank       // Green hexagon - slow, high HP
}

public partial class Enemy : Node2D
{
    public EnemyType Type;
    public float Hp;
    public float MaxHp;
    public float Speed;
    public float ContactDamage;
    public float CollisionRadius;
    public int XpValue;

    private Vector2 _velocity;
    private Vector2 _screenSize;
    private float _time;
    private Node2D _target;
    private float _rotation;
    private float _flashTimer;

    public static Enemy Create(EnemyType type, Vector2 position, Node2D target, float difficultyScale = 1f)
    {
        var e = new Enemy();
        e.Type = type;
        e.Position = position;
        e._target = target;

        switch (type)
        {
            case EnemyType.Chaser:
                e.MaxHp = 3f * difficultyScale;
                e.Speed = 90f + 10f * difficultyScale;
                e.ContactDamage = 10f;
                e.CollisionRadius = 12f;
                e.XpValue = 1;
                break;
            case EnemyType.Drifter:
                e.MaxHp = 6f * difficultyScale;
                e.Speed = 50f + 8f * difficultyScale;
                e.ContactDamage = 15f;
                e.CollisionRadius = 16f;
                e.XpValue = 3;
                var rng = new RandomNumberGenerator();
                rng.Randomize();
                float angle = rng.RandfRange(0, Mathf.Tau);
                e._velocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * e.Speed;
                break;
            case EnemyType.Sprinter:
                e.MaxHp = 1f * difficultyScale;
                e.Speed = 200f + 15f * difficultyScale;
                e.ContactDamage = 5f;
                e.CollisionRadius = 9f;
                e.XpValue = 1;
                break;
            case EnemyType.Tank:
                e.MaxHp = 15f * difficultyScale;
                e.Speed = 40f + 5f * difficultyScale;
                e.ContactDamage = 25f;
                e.CollisionRadius = 22f;
                e.XpValue = 5;
                break;
        }
        e.Hp = e.MaxHp;
        return e;
    }

    public override void _Ready()
    {
        _screenSize = GetViewportRect().Size;
    }

    public override void _Process(double delta)
    {
        float dt = (float)delta;
        _time += dt;

        if (_flashTimer > 0)
            _flashTimer -= dt;

        if (_target == null || !IsInstanceValid(_target)) return;

        Vector2 toTarget = _target.GlobalPosition - GlobalPosition;

        switch (Type)
        {
            case EnemyType.Chaser:
                if (toTarget.LengthSquared() > 1f)
                    _velocity = toTarget.Normalized() * Speed;
                break;

            case EnemyType.Drifter:
                // Bounce off edges
                if (Position.X < 20 || Position.X > _screenSize.X - 20)
                    _velocity.X = -_velocity.X;
                if (Position.Y < 20 || Position.Y > _screenSize.Y - 20)
                    _velocity.Y = -_velocity.Y;
                // Slight attraction to player
                _velocity += toTarget.Normalized() * 15f * dt;
                _velocity = _velocity.Normalized() * Speed;
                _rotation += dt * 2f;
                break;

            case EnemyType.Sprinter:
                if (toTarget.LengthSquared() > 1f)
                    _velocity = toTarget.Normalized() * Speed;
                break;

            case EnemyType.Tank:
                if (toTarget.LengthSquared() > 1f)
                    _velocity = toTarget.Normalized() * Speed;
                _rotation += dt * 0.5f;
                break;
        }

        Position += _velocity * dt;

        // Keep on screen (with some tolerance for spawning)
        float margin = 60f;
        Position = new Vector2(
            Mathf.Clamp(Position.X, -margin, _screenSize.X + margin),
            Mathf.Clamp(Position.Y, -margin, _screenSize.Y + margin)
        );

        QueueRedraw();
    }

    public bool TakeDamage(float damage)
    {
        Hp -= damage;
        _flashTimer = 0.1f;
        return Hp <= 0;
    }

    public override void _Draw()
    {
        bool flash = _flashTimer > 0;

        switch (Type)
        {
            case EnemyType.Chaser:
                DrawChaser(flash);
                break;
            case EnemyType.Drifter:
                DrawDrifter(flash);
                break;
            case EnemyType.Sprinter:
                DrawSprinter(flash);
                break;
            case EnemyType.Tank:
                DrawTank(flash);
                break;
        }

        // Health bar for tanky enemies
        if (MaxHp > 5 && Hp < MaxHp)
        {
            float barWidth = CollisionRadius * 2f;
            float barHeight = 3f;
            float yOff = -CollisionRadius - 8f;
            DrawRect(new Rect2(-barWidth / 2, yOff, barWidth, barHeight), new Color(0.3f, 0, 0));
            DrawRect(new Rect2(-barWidth / 2, yOff, barWidth * (Hp / MaxHp), barHeight), new Color(1, 0.2f, 0.2f));
        }
    }

    private void DrawChaser(bool flash)
    {
        Color col = flash ? new Color(1, 1, 1) : new Color(1, 0.2f, 0.2f);
        // Glow
        DrawCircle(Vector2.Zero, 18f, new Color(1, 0.1f, 0.1f, 0.1f));

        // Triangle pointing toward player
        float angle = 0;
        if (_target != null && IsInstanceValid(_target))
            angle = (_target.GlobalPosition - GlobalPosition).Angle() - Mathf.Pi / 2f;

        Vector2[] tri = new Vector2[3];
        for (int i = 0; i < 3; i++)
        {
            float a = angle + i * Mathf.Tau / 3f;
            tri[i] = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 14f;
        }
        DrawColoredPolygon(tri, col);
        for (int i = 0; i < 3; i++)
            DrawLine(tri[i], tri[(i + 1) % 3], new Color(col.R, col.G, col.B, 0.6f), 1.5f);
    }

    private void DrawDrifter(bool flash)
    {
        Color col = flash ? new Color(1, 1, 1) : new Color(0.8f, 0.2f, 1f);
        DrawCircle(Vector2.Zero, 22f, new Color(0.6f, 0.1f, 0.8f, 0.1f));

        // Rotating square
        Vector2[] sq = new Vector2[4];
        for (int i = 0; i < 4; i++)
        {
            float a = _rotation + i * Mathf.Tau / 4f;
            sq[i] = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 16f;
        }
        DrawColoredPolygon(sq, col);
        for (int i = 0; i < 4; i++)
            DrawLine(sq[i], sq[(i + 1) % 4], new Color(col.R, col.G, col.B, 0.6f), 1.5f);
    }

    private void DrawSprinter(bool flash)
    {
        Color col = flash ? new Color(1, 1, 1) : new Color(1, 0.55f, 0.1f);
        DrawCircle(Vector2.Zero, 14f, new Color(1, 0.4f, 0, 0.1f));

        // Small diamond
        Vector2[] diamond = {
            new Vector2(0, -10),
            new Vector2(7, 0),
            new Vector2(0, 10),
            new Vector2(-7, 0),
        };
        DrawColoredPolygon(diamond, col);

        // Speed trail
        if (_velocity.LengthSquared() > 1f)
        {
            Vector2 trail = -_velocity.Normalized() * 15f;
            DrawLine(Vector2.Zero, trail, new Color(1, 0.5f, 0, 0.4f), 2f);
        }
    }

    private void DrawTank(bool flash)
    {
        Color col = flash ? new Color(1, 1, 1) : new Color(0.2f, 1, 0.4f);
        DrawCircle(Vector2.Zero, 28f, new Color(0.1f, 0.8f, 0.2f, 0.08f));

        // Hexagon
        Vector2[] hex = new Vector2[6];
        for (int i = 0; i < 6; i++)
        {
            float a = _rotation + i * Mathf.Tau / 6f;
            hex[i] = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 22f;
        }
        DrawColoredPolygon(hex, col);
        for (int i = 0; i < 6; i++)
            DrawLine(hex[i], hex[(i + 1) % 6], new Color(col.R, col.G, col.B, 0.6f), 1.5f);

        // Inner hexagon
        Vector2[] innerHex = new Vector2[6];
        for (int i = 0; i < 6; i++)
        {
            float a = _rotation + i * Mathf.Tau / 6f + Mathf.Tau / 12f;
            innerHex[i] = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 12f;
        }
        DrawColoredPolygon(innerHex, new Color(0.1f, 0.6f, 0.2f, 0.5f));
    }
}
