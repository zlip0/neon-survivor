using Godot;
using System;
using System.Collections.Generic;

namespace NeonSurvivor;

public enum UpgradeType
{
    Multishot,
    FireRate,
    BulletSize,
    Damage,
    MoveSpeed,
    MaxHp,
    Regen,
    Magnet,
    Pierce
}

public class UpgradeInfo
{
    public UpgradeType Type;
    public string Name;
    public string Description;
    public Color IconColor;
    public int CurrentLevel;
    public int MaxLevel;

    public static List<UpgradeInfo> CreateAll()
    {
        return new List<UpgradeInfo>
        {
            new() { Type = UpgradeType.Multishot, Name = "Multishot", Description = "+1 projectile per volley", IconColor = new Color(0, 1, 1), CurrentLevel = 0, MaxLevel = 4 },
            new() { Type = UpgradeType.FireRate, Name = "Fire Rate", Description = "Shoot 15% faster", IconColor = new Color(1, 1, 0), CurrentLevel = 0, MaxLevel = 5 },
            new() { Type = UpgradeType.BulletSize, Name = "Bullet Size", Description = "Bullets 30% larger", IconColor = new Color(1, 0.6f, 0), CurrentLevel = 0, MaxLevel = 5 },
            new() { Type = UpgradeType.Damage, Name = "Damage Up", Description = "+50% bullet damage", IconColor = new Color(1, 0.2f, 0.2f), CurrentLevel = 0, MaxLevel = 5 },
            new() { Type = UpgradeType.MoveSpeed, Name = "Swift", Description = "+15% movement speed", IconColor = new Color(0.3f, 0.8f, 1), CurrentLevel = 0, MaxLevel = 5 },
            new() { Type = UpgradeType.MaxHp, Name = "Vitality", Description = "+25 max HP (heals too)", IconColor = new Color(0, 1, 0.4f), CurrentLevel = 0, MaxLevel = 5 },
            new() { Type = UpgradeType.Regen, Name = "Regen", Description = "+1 HP/sec regeneration", IconColor = new Color(0.2f, 1, 0.6f), CurrentLevel = 0, MaxLevel = 3 },
            new() { Type = UpgradeType.Magnet, Name = "Magnet", Description = "+40% XP pickup radius", IconColor = new Color(0.8f, 0.4f, 1), CurrentLevel = 0, MaxLevel = 5 },
            new() { Type = UpgradeType.Pierce, Name = "Piercing", Description = "Bullets pierce +1 enemy", IconColor = new Color(1, 0.8f, 0.2f), CurrentLevel = 0, MaxLevel = 3 },
        };
    }
}

public partial class UpgradePanel : CanvasLayer
{
    public bool IsOpen { get; private set; }

    private UpgradePanelDrawer _drawer;
    private List<UpgradeInfo> _allUpgrades;
    private UpgradeInfo[] _choices = new UpgradeInfo[3];
    private Rect2[] _cardRects = new Rect2[3];
    private int _hoveredCard = -1;

    public Action<UpgradeType> OnUpgradeChosen;

    public override void _Ready()
    {
        Layer = 20;
        ProcessMode = ProcessModeEnum.Always;
        _allUpgrades = UpgradeInfo.CreateAll();
        _drawer = new UpgradePanelDrawer(this);
        AddChild(_drawer);
        _drawer.Visible = false;
    }

    public void ShowUpgrades()
    {
        // Pick 3 random upgrades that aren't maxed
        var available = _allUpgrades.FindAll(u => u.CurrentLevel < u.MaxLevel);
        if (available.Count == 0) return;

        var rng = new RandomNumberGenerator();
        rng.Randomize();

        // Shuffle and pick up to 3
        for (int i = available.Count - 1; i > 0; i--)
        {
            int j = (int)(rng.Randi() % (i + 1));
            (available[i], available[j]) = (available[j], available[i]);
        }

        int count = Mathf.Min(3, available.Count);
        for (int i = 0; i < 3; i++)
            _choices[i] = i < count ? available[i] : null;

        IsOpen = true;
        _drawer.Visible = true;
        _drawer.QueueRedraw();
    }

    public void ApplyUpgrade(UpgradeType type)
    {
        var upgrade = _allUpgrades.Find(u => u.Type == type);
        if (upgrade != null && upgrade.CurrentLevel < upgrade.MaxLevel)
            upgrade.CurrentLevel++;
    }

    public UpgradeInfo GetUpgrade(UpgradeType type)
    {
        return _allUpgrades.Find(u => u.Type == type);
    }

    public override void _Input(InputEvent @event)
    {
        if (!IsOpen) return;

        if (@event is InputEventMouseMotion mm)
        {
            _hoveredCard = -1;
            for (int i = 0; i < 3; i++)
            {
                if (_choices[i] != null && _cardRects[i].HasPoint(mm.Position))
                {
                    _hoveredCard = i;
                    break;
                }
            }
            _drawer.QueueRedraw();
        }

        if (@event is InputEventMouseButton mb && mb.Pressed && mb.ButtonIndex == MouseButton.Left)
        {
            for (int i = 0; i < 3; i++)
            {
                if (_choices[i] != null && _cardRects[i].HasPoint(mb.Position))
                {
                    SelectUpgrade(i);
                    break;
                }
            }
        }

        // Keyboard shortcuts: 1, 2, 3
        if (@event is InputEventKey key && key.Pressed)
        {
            if (key.Keycode == Key.Key1 && _choices[0] != null) SelectUpgrade(0);
            else if (key.Keycode == Key.Key2 && _choices[1] != null) SelectUpgrade(1);
            else if (key.Keycode == Key.Key3 && _choices[2] != null) SelectUpgrade(2);
        }
    }

    private void SelectUpgrade(int index)
    {
        if (_choices[index] == null) return;
        var chosen = _choices[index];
        ApplyUpgrade(chosen.Type);
        OnUpgradeChosen?.Invoke(chosen.Type);
        Close();
    }

    private void Close()
    {
        IsOpen = false;
        _drawer.Visible = false;
        _hoveredCard = -1;
    }

    // Inner class that draws the upgrade UI
    public partial class UpgradePanelDrawer : Node2D
    {
        private UpgradePanel _panel;

        public UpgradePanelDrawer(UpgradePanel panel)
        {
            _panel = panel;
        }

        public override void _Draw()
        {
            if (!_panel.IsOpen) return;

            var screenSize = GetViewportRect().Size;
            var font = ThemeDB.FallbackFont;

            // Dim overlay
            DrawRect(new Rect2(0, 0, screenSize.X, screenSize.Y), new Color(0, 0, 0, 0.75f));

            // Title
            string title = "LEVEL UP!";
            float titleW = font.GetStringSize(title, HorizontalAlignment.Left, -1, 36).X;
            DrawString(font, new Vector2(screenSize.X / 2f - titleW / 2f, screenSize.Y * 0.2f),
                title, HorizontalAlignment.Left, -1, 36, new Color(1, 1, 0));

            string subtitle = "Choose an upgrade (click or press 1/2/3):";
            float subW = font.GetStringSize(subtitle, HorizontalAlignment.Left, -1, 16).X;
            DrawString(font, new Vector2(screenSize.X / 2f - subW / 2f, screenSize.Y * 0.2f + 35),
                subtitle, HorizontalAlignment.Left, -1, 16, new Color(0.7f, 0.7f, 0.8f));

            // Cards
            float cardW = 220f;
            float cardH = 200f;
            float cardGap = 30f;
            float totalW = cardW * 3 + cardGap * 2;
            float startX = (screenSize.X - totalW) / 2f;
            float cardY = screenSize.Y * 0.35f;

            for (int i = 0; i < 3; i++)
            {
                if (_panel._choices[i] == null) continue;

                var upgrade = _panel._choices[i];
                float cx = startX + i * (cardW + cardGap);
                Rect2 rect = new Rect2(cx, cardY, cardW, cardH);
                _panel._cardRects[i] = rect;

                bool hovered = _panel._hoveredCard == i;

                // Card background
                Color bgColor = hovered ? new Color(0.15f, 0.15f, 0.25f) : new Color(0.08f, 0.08f, 0.15f);
                DrawRect(rect, bgColor);

                // Border
                Color borderCol = hovered ? upgrade.IconColor : new Color(upgrade.IconColor.R, upgrade.IconColor.G, upgrade.IconColor.B, 0.5f);
                float borderW = hovered ? 2.5f : 1.5f;
                DrawRect(rect, borderCol, false, borderW);

                // Hover glow
                if (hovered)
                {
                    DrawRect(new Rect2(cx - 2, cardY - 2, cardW + 4, cardH + 4),
                        new Color(upgrade.IconColor.R, upgrade.IconColor.G, upgrade.IconColor.B, 0.1f));
                }

                // Icon (simple shape)
                Vector2 iconCenter = new Vector2(cx + cardW / 2f, cardY + 50f);
                DrawCircle(iconCenter, 20f, new Color(upgrade.IconColor.R, upgrade.IconColor.G, upgrade.IconColor.B, 0.2f));
                DrawCircle(iconCenter, 12f, upgrade.IconColor);

                // Number
                string num = (i + 1).ToString();
                DrawString(font, new Vector2(cx + 8, cardY + 22), num, HorizontalAlignment.Left, -1, 14, new Color(0.5f, 0.5f, 0.6f));

                // Name
                float nameW = font.GetStringSize(upgrade.Name, HorizontalAlignment.Left, -1, 20).X;
                DrawString(font, new Vector2(cx + cardW / 2f - nameW / 2f, cardY + 95),
                    upgrade.Name, HorizontalAlignment.Left, -1, 20, Colors.White);

                // Description
                float descW = font.GetStringSize(upgrade.Description, HorizontalAlignment.Left, -1, 13).X;
                DrawString(font, new Vector2(cx + cardW / 2f - descW / 2f, cardY + 120),
                    upgrade.Description, HorizontalAlignment.Left, -1, 13, new Color(0.7f, 0.7f, 0.8f));

                // Level pips
                float pipY = cardY + 155f;
                float pipTotal = upgrade.MaxLevel;
                float pipW_each = 14f;
                float pipGap2 = 4f;
                float pipsWidth = pipTotal * pipW_each + (pipTotal - 1) * pipGap2;
                float pipStartX = cx + cardW / 2f - pipsWidth / 2f;
                for (int p = 0; p < upgrade.MaxLevel; p++)
                {
                    float px = pipStartX + p * (pipW_each + pipGap2);
                    bool filled = p < upgrade.CurrentLevel;
                    Color pipCol = filled ? upgrade.IconColor : new Color(0.2f, 0.2f, 0.3f);
                    DrawRect(new Rect2(px, pipY, pipW_each, 8f), pipCol);
                }

                // Level text
                string lvlText = $"Lv {upgrade.CurrentLevel}/{upgrade.MaxLevel}";
                float lvlW = font.GetStringSize(lvlText, HorizontalAlignment.Left, -1, 11).X;
                DrawString(font, new Vector2(cx + cardW / 2f - lvlW / 2f, cardY + 185),
                    lvlText, HorizontalAlignment.Left, -1, 11, new Color(0.5f, 0.5f, 0.6f));
            }
        }
    }
}
