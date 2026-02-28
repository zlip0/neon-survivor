using Godot;
using System;

namespace NeonSurvivor;

public partial class Hud : CanvasLayer
{
    private Node2D _drawer;
    public float Health = 100f;
    public float MaxHealth = 100f;
    public float Xp = 0f;
    public float XpNeeded = 10f;
    public int Level = 1;
    public int Score = 0;
    public float GameTime = 0f;
    public int EnemiesKilled = 0;

    public override void _Ready()
    {
        Layer = 10;
        _drawer = new HudDrawer(this);
        AddChild(_drawer);
    }

    public void UpdateStats(float health, float maxHealth, float xp, float xpNeeded, int level, int score, float gameTime, int enemiesKilled)
    {
        Health = health;
        MaxHealth = maxHealth;
        Xp = xp;
        XpNeeded = xpNeeded;
        Level = level;
        Score = score;
        GameTime = gameTime;
        EnemiesKilled = enemiesKilled;
        _drawer?.QueueRedraw();
    }
}

public partial class HudDrawer : Node2D
{
    private Hud _hud;

    public HudDrawer(Hud hud)
    {
        _hud = hud;
    }

    public override void _Draw()
    {
        var font = ThemeDB.FallbackFont;
        var screenSize = GetViewportRect().Size;

        // Health bar (top-left)
        float hbX = 20f, hbY = 20f, hbW = 200f, hbH = 16f;
        DrawRect(new Rect2(hbX - 1, hbY - 1, hbW + 2, hbH + 2), new Color(0.3f, 0.3f, 0.4f, 0.5f));
        DrawRect(new Rect2(hbX, hbY, hbW, hbH), new Color(0.15f, 0, 0));
        float healthPct = _hud.MaxHealth > 0 ? _hud.Health / _hud.MaxHealth : 0;
        Color healthCol = healthPct > 0.5f ? new Color(0, 1, 0.4f) :
                          healthPct > 0.25f ? new Color(1, 0.8f, 0) :
                                               new Color(1, 0.2f, 0.2f);
        DrawRect(new Rect2(hbX, hbY, hbW * healthPct, hbH), healthCol);
        string hpText = $"HP: {(int)_hud.Health}/{(int)_hud.MaxHealth}";
        DrawString(font, new Vector2(hbX + 4, hbY + 13), hpText, HorizontalAlignment.Left, -1, 12, Colors.White);

        // XP bar (bottom)
        float xpY = screenSize.Y - 30f;
        float xpW = screenSize.X - 40f;
        float xpH = 12f;
        DrawRect(new Rect2(19, xpY - 1, xpW + 2, xpH + 2), new Color(0.3f, 0.3f, 0.4f, 0.4f));
        DrawRect(new Rect2(20, xpY, xpW, xpH), new Color(0, 0.05f, 0.15f));
        float xpPct = _hud.XpNeeded > 0 ? Mathf.Min(_hud.Xp / _hud.XpNeeded, 1f) : 0;
        DrawRect(new Rect2(20, xpY, xpW * xpPct, xpH), new Color(0.3f, 0.6f, 1f));
        string xpText = $"XP: {(int)_hud.Xp}/{(int)_hud.XpNeeded}  (Lv {_hud.Level})";
        DrawString(font, new Vector2(24, xpY + 10), xpText, HorizontalAlignment.Left, -1, 10, Colors.White);

        // Score (top-right)
        string scoreText = $"Score: {_hud.Score}";
        float scoreW = font.GetStringSize(scoreText, HorizontalAlignment.Left, -1, 20).X;
        DrawString(font, new Vector2(screenSize.X - scoreW - 20, 35), scoreText, HorizontalAlignment.Left, -1, 20, new Color(1, 1, 0.5f));

        // Timer (top-center)
        int mins = (int)(_hud.GameTime / 60f);
        int secs = (int)(_hud.GameTime % 60f);
        string timeText = $"{mins:D2}:{secs:D2}";
        float timeW = font.GetStringSize(timeText, HorizontalAlignment.Left, -1, 22).X;
        DrawString(font, new Vector2(screenSize.X / 2f - timeW / 2f, 35), timeText, HorizontalAlignment.Left, -1, 22, new Color(0.8f, 0.8f, 0.9f));

        // Kills (below score)
        string killText = $"Kills: {_hud.EnemiesKilled}";
        float killW = font.GetStringSize(killText, HorizontalAlignment.Left, -1, 14).X;
        DrawString(font, new Vector2(screenSize.X - killW - 20, 55), killText, HorizontalAlignment.Left, -1, 14, new Color(0.7f, 0.7f, 0.8f));
    }
}
