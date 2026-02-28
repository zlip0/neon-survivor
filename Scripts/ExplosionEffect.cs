using Godot;
using System;
using System.Collections.Generic;

namespace NeonSurvivor;

public partial class ExplosionEffect : Node2D
{
    private struct Particle
    {
        public Vector2 Position;
        public Vector2 Velocity;
        public float Life;
        public float MaxLife;
        public float Size;
        public Color Color;
    }

    private List<Particle> _particles = new();
    private float _elapsed;

    public static ExplosionEffect Create(Vector2 position, Color color, int count = 12, float speed = 120f, float size = 3f)
    {
        var fx = new ExplosionEffect();
        fx.Position = position;

        var rng = new RandomNumberGenerator();
        rng.Randomize();

        for (int i = 0; i < count; i++)
        {
            float angle = rng.RandfRange(0, Mathf.Tau);
            float spd = rng.RandfRange(speed * 0.4f, speed);
            float life = rng.RandfRange(0.3f, 0.7f);
            float sz = rng.RandfRange(size * 0.5f, size);

            fx._particles.Add(new Particle
            {
                Position = Vector2.Zero,
                Velocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * spd,
                Life = life,
                MaxLife = life,
                Size = sz,
                Color = new Color(
                    color.R + rng.RandfRange(-0.1f, 0.1f),
                    color.G + rng.RandfRange(-0.1f, 0.1f),
                    color.B + rng.RandfRange(-0.1f, 0.1f),
                    1f)
            });
        }

        return fx;
    }

    public override void _Process(double delta)
    {
        float dt = (float)delta;
        _elapsed += dt;

        bool anyAlive = false;
        for (int i = 0; i < _particles.Count; i++)
        {
            var p = _particles[i];
            p.Life -= dt;
            if (p.Life > 0)
            {
                p.Position += p.Velocity * dt;
                p.Velocity *= 1f - 3f * dt; // drag
                _particles[i] = p;
                anyAlive = true;
            }
            else
            {
                _particles[i] = p;
            }
        }

        if (!anyAlive)
        {
            QueueFree();
            return;
        }

        QueueRedraw();
    }

    public override void _Draw()
    {
        foreach (var p in _particles)
        {
            if (p.Life <= 0) continue;
            float t = p.Life / p.MaxLife;
            float alpha = t;
            float size = p.Size * (0.3f + 0.7f * t);
            Color col = new Color(p.Color.R, p.Color.G, p.Color.B, alpha);
            DrawCircle(p.Position, size + 2f, new Color(col.R, col.G, col.B, alpha * 0.3f));
            DrawCircle(p.Position, size, col);
        }
    }
}
