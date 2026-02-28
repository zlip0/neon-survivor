using Godot;
using System;

namespace NeonSurvivor;

public partial class Player : Node2D
{
    // Base stats (modified by upgrades)
    public float MoveSpeed = 220f;
    public float MaxHealth = 100f;
    public float Health;
    public float FireInterval = 0.30f;
    public int BulletCount = 1;
    public float BulletDamage = 1f;
    public float BulletScale = 1f;
    public float BulletSpeed = 500f;
    public float PickupRadius = 70f;
    public float CollisionRadius = 12f;
    public int PierceCount = 0;
    public float RegenPerSecond = 0f;

    // Internal state
    private float _fireTimer;
    private float _invincTimer;
    private float _regenAccum;
    private bool _invincible;
    private Vector2 _screenSize;
    private float _time;
    public bool IsDead { get; private set; }

    // Delegates for communication with Game
    public Action<Vector2, Vector2, float, float, float, int> OnFireBullet;
    public Action OnDied;
    public Func<Vector2> GetNearestEnemyPosition;

    public override void _Ready()
    {
        Health = MaxHealth;
        _screenSize = GetViewportRect().Size;
        Position = _screenSize / 2f;
    }

    public override void _Process(double delta)
    {
        if (IsDead) return;
        float dt = (float)delta;
        _time += dt;

        // Movement
        Vector2 input = Vector2.Zero;
        if (Input.IsKeyPressed(Key.A) || Input.IsKeyPressed(Key.Left)) input.X -= 1;
        if (Input.IsKeyPressed(Key.D) || Input.IsKeyPressed(Key.Right)) input.X += 1;
        if (Input.IsKeyPressed(Key.W) || Input.IsKeyPressed(Key.Up)) input.Y -= 1;
        if (Input.IsKeyPressed(Key.S) || Input.IsKeyPressed(Key.Down)) input.Y += 1;
        if (input.LengthSquared() > 0)
            input = input.Normalized();

        Position += input * MoveSpeed * dt;
        Position = new Vector2(
            Mathf.Clamp(Position.X, 20, _screenSize.X - 20),
            Mathf.Clamp(Position.Y, 20, _screenSize.Y - 20)
        );

        // Invincibility
        if (_invincible)
        {
            _invincTimer -= dt;
            if (_invincTimer <= 0)
                _invincible = false;
        }

        // Regeneration
        if (RegenPerSecond > 0)
        {
            _regenAccum += RegenPerSecond * dt;
            if (_regenAccum >= 1f)
            {
                float heal = Mathf.Floor(_regenAccum);
                _regenAccum -= heal;
                Health = Mathf.Min(Health + heal, MaxHealth);
            }
        }

        // Auto-fire
        _fireTimer -= dt;
        if (_fireTimer <= 0 && GetNearestEnemyPosition != null)
        {
            Vector2 target = GetNearestEnemyPosition.Invoke();
            if (target != Vector2.Inf)
            {
                Fire(target);
            }
            _fireTimer = FireInterval;
        }

        QueueRedraw();
    }

    private void Fire(Vector2 targetPos)
    {
        Vector2 dir = (targetPos - GlobalPosition).Normalized();
        if (BulletCount == 1)
        {
            OnFireBullet?.Invoke(GlobalPosition, dir, BulletDamage, BulletScale, BulletSpeed, PierceCount);
        }
        else
        {
            float spreadAngle = Mathf.DegToRad(8f);
            float totalSpread = spreadAngle * (BulletCount - 1);
            float startAngle = -totalSpread / 2f;
            float baseAngle = dir.Angle();
            for (int i = 0; i < BulletCount; i++)
            {
                float angle = baseAngle + startAngle + spreadAngle * i;
                Vector2 bulletDir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                OnFireBullet?.Invoke(GlobalPosition, bulletDir, BulletDamage, BulletScale, BulletSpeed, PierceCount);
            }
        }
    }

    public void TakeDamage(float damage)
    {
        if (_invincible || IsDead) return;
        Health -= damage;
        _invincible = true;
        _invincTimer = 0.8f;

        if (Health <= 0)
        {
            Health = 0;
            IsDead = true;
            OnDied?.Invoke();
        }
    }

    public void Heal(float amount)
    {
        Health = Mathf.Min(Health + amount, MaxHealth);
    }

    public override void _Draw()
    {
        if (IsDead) return;

        // Blinking during invincibility
        if (_invincible && ((int)(_invincTimer * 10f) % 2 == 0))
        {
            DrawGlowDiamond(new Color(1, 1, 1, 0.4f), 0.7f);
            return;
        }

        DrawGlowDiamond(new Color(0, 1, 1), 1f);
    }

    private void DrawGlowDiamond(Color baseColor, float alpha)
    {
        // Outer glow
        DrawCircle(Vector2.Zero, 28f, new Color(baseColor.R, baseColor.G, baseColor.B, 0.08f * alpha));
        DrawCircle(Vector2.Zero, 20f, new Color(baseColor.R, baseColor.G, baseColor.B, 0.12f * alpha));

        // Main body - diamond
        Color bodyColor = new Color(baseColor.R, baseColor.G, baseColor.B, alpha);
        Vector2[] diamond = {
            new Vector2(0, -16),
            new Vector2(13, 0),
            new Vector2(0, 16),
            new Vector2(-13, 0),
        };
        DrawColoredPolygon(diamond, bodyColor);

        // Edge lines for sharpness
        Color edgeColor = new Color(baseColor.R, baseColor.G, baseColor.B, alpha * 0.8f);
        for (int i = 0; i < diamond.Length; i++)
            DrawLine(diamond[i], diamond[(i + 1) % diamond.Length], edgeColor, 1.5f);

        // Inner highlight
        Vector2[] inner = {
            new Vector2(0, -8),
            new Vector2(6, 0),
            new Vector2(0, 8),
            new Vector2(-6, 0),
        };
        DrawColoredPolygon(inner, new Color(0.6f, 1, 1, 0.4f * alpha));

        // Engine trail based on movement
        Vector2 input = Vector2.Zero;
        if (Input.IsKeyPressed(Key.A) || Input.IsKeyPressed(Key.Left)) input.X -= 1;
        if (Input.IsKeyPressed(Key.D) || Input.IsKeyPressed(Key.Right)) input.X += 1;
        if (Input.IsKeyPressed(Key.W) || Input.IsKeyPressed(Key.Up)) input.Y -= 1;
        if (Input.IsKeyPressed(Key.S) || Input.IsKeyPressed(Key.Down)) input.Y += 1;
        if (input.LengthSquared() > 0.01f)
        {
            Vector2 trail = -input.Normalized() * 22f;
            float flicker = 0.6f + 0.4f * Mathf.Sin(_time * 20f);
            DrawLine(new Vector2(0, 0), trail, new Color(0, 0.8f, 1, 0.5f * flicker * alpha), 3f);
            DrawLine(new Vector2(0, 0), trail * 0.6f, new Color(0.5f, 1, 1, 0.8f * flicker * alpha), 2f);
        }
    }
}
