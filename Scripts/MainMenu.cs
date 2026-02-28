using Godot;
using System;

namespace NeonSurvivor;

public partial class MainMenu : Node2D
{
    private float _time;
    private Vector2 _screenSize;
    private Vector2[] _stars;
    private float[] _starBrightness;
    private float[] _starSpeed;
    private bool _started;

    public override void _Ready()
    {
        _screenSize = GetViewportRect().Size;

        // Generate random background stars
        var rng = new RandomNumberGenerator();
        rng.Randomize();
        int starCount = 120;
        _stars = new Vector2[starCount];
        _starBrightness = new float[starCount];
        _starSpeed = new float[starCount];
        for (int i = 0; i < starCount; i++)
        {
            _stars[i] = new Vector2(rng.RandfRange(0, _screenSize.X), rng.RandfRange(0, _screenSize.Y));
            _starBrightness[i] = rng.RandfRange(0.2f, 1.0f);
            _starSpeed[i] = rng.RandfRange(0.5f, 3.0f);
        }
    }

    public override void _Process(double delta)
    {
        _time += (float)delta;
        QueueRedraw();

        if (!_started && (Input.IsKeyPressed(Key.Enter) || Input.IsKeyPressed(Key.Space)))
        {
            _started = true;
            GetTree().ChangeSceneToFile("res://Scenes/Game.tscn");
        }
    }

    public override void _Draw()
    {
        // Background
        DrawRect(new Rect2(0, 0, _screenSize.X, _screenSize.Y), new Color(0.04f, 0.04f, 0.1f));

        // Stars
        for (int i = 0; i < _stars.Length; i++)
        {
            float twinkle = (Mathf.Sin(_time * _starSpeed[i] + i * 1.7f) + 1f) * 0.5f;
            float alpha = _starBrightness[i] * (0.3f + 0.7f * twinkle);
            float size = 1f + twinkle * 1.5f;
            DrawCircle(_stars[i], size, new Color(0.7f, 0.8f, 1.0f, alpha));
        }

        // Subtle grid
        Color gridColor = new Color(0.1f, 0.1f, 0.25f, 0.15f);
        float gridSpacing = 60f;
        for (float x = 0; x < _screenSize.X; x += gridSpacing)
            DrawLine(new Vector2(x, 0), new Vector2(x, _screenSize.Y), gridColor, 1f);
        for (float y = 0; y < _screenSize.Y; y += gridSpacing)
            DrawLine(new Vector2(0, y), new Vector2(_screenSize.X, y), gridColor, 1f);

        var font = ThemeDB.FallbackFont;

        // Title glow
        string title = "NEON SURVIVOR";
        float titleY = _screenSize.Y * 0.28f;
        float titleX = _screenSize.X / 2f;

        // Glow layers
        for (int g = 3; g >= 0; g--)
        {
            float glowAlpha = 0.08f + g * 0.02f;
            int glowSize = 52 + g * 4;
            Color glowCol = new Color(0, 1, 1, glowAlpha);
            DrawString(font, new Vector2(titleX - MeasureString(font, title, glowSize) / 2f, titleY + g),
                title, HorizontalAlignment.Left, -1, glowSize, glowCol);
        }
        // Main title
        Color titleColor = new Color(0, 1, 1);
        DrawString(font, new Vector2(titleX - MeasureString(font, title, 48) / 2f, titleY),
            title, HorizontalAlignment.Left, -1, 48, titleColor);

        // Subtitle
        string subtitle = "Survive the geometric onslaught";
        DrawString(font, new Vector2(titleX - MeasureString(font, subtitle, 18) / 2f, titleY + 50),
            subtitle, HorizontalAlignment.Left, -1, 18, new Color(0.6f, 0.6f, 0.8f));

        // Animated ship preview
        float shipY = _screenSize.Y * 0.52f;
        float shipBob = Mathf.Sin(_time * 2f) * 8f;
        Vector2 shipPos = new Vector2(titleX, shipY + shipBob);

        // Ship glow
        DrawCircle(shipPos, 30f, new Color(0, 1, 1, 0.1f));
        DrawCircle(shipPos, 20f, new Color(0, 1, 1, 0.15f));

        // Ship body
        Vector2[] diamond = {
            shipPos + new Vector2(0, -20),
            shipPos + new Vector2(16, 0),
            shipPos + new Vector2(0, 20),
            shipPos + new Vector2(-16, 0),
        };
        Color shipCol = new Color(0, 1, 1);
        DrawPolygon(diamond, new Color[] { shipCol, shipCol, shipCol, shipCol });

        Vector2[] inner = {
            shipPos + new Vector2(0, -10),
            shipPos + new Vector2(8, 0),
            shipPos + new Vector2(0, 10),
            shipPos + new Vector2(-8, 0),
        };
        Color innerCol = new Color(0.5f, 1, 1, 0.6f);
        DrawPolygon(inner, new Color[] { innerCol, innerCol, innerCol, innerCol });

        // Orbiting dots
        for (int i = 0; i < 3; i++)
        {
            float angle = _time * 2f + i * Mathf.Tau / 3f;
            Vector2 orbPos = shipPos + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 35f;
            DrawCircle(orbPos, 3f, new Color(1, 1, 0, 0.8f));
        }

        // Start prompt (blinking)
        float blink = (Mathf.Sin(_time * 3f) + 1f) * 0.5f;
        string startText = "Press ENTER or SPACE to Start";
        float startY = _screenSize.Y * 0.72f;
        DrawString(font, new Vector2(titleX - MeasureString(font, startText, 22) / 2f, startY),
            startText, HorizontalAlignment.Left, -1, 22, new Color(1, 1, 1, 0.4f + 0.6f * blink));

        // Controls info
        string[] controls = {
            "WASD - Move",
            "Auto-aim & Auto-fire",
            "Collect XP gems to level up",
            "Choose upgrades to grow stronger",
            "Survive as long as you can!"
        };
        float infoY = _screenSize.Y * 0.82f;
        for (int i = 0; i < controls.Length; i++)
        {
            DrawString(font, new Vector2(titleX - MeasureString(font, controls[i], 14) / 2f, infoY + i * 20),
                controls[i], HorizontalAlignment.Left, -1, 14, new Color(0.5f, 0.5f, 0.7f));
        }
    }

    private float MeasureString(Font font, string text, int size)
    {
        return font.GetStringSize(text, HorizontalAlignment.Left, -1, size).X;
    }
}
