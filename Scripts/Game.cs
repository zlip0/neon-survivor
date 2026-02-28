using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NeonSurvivor;

public partial class Game : Node2D
{
    // Containers
    private Node2D _bulletContainer;
    private Node2D _enemyContainer;
    private Node2D _gemContainer;
    private Node2D _effectContainer;

    // Entities
    private Player _player;
    private Hud _hud;
    private UpgradePanel _upgradePanel;
    private Camera2D _camera;

    // Game state
    private float _gameTime;
    private int _score;
    private int _enemiesKilled;
    private float _xp;
    private int _level = 1;
    private bool _gameOver;
    private float _gameOverTimer;

    // Spawning
    private float _spawnTimer;
    private float _spawnInterval = 2.0f;
    private int _difficultyTier;

    // Background
    private Vector2 _screenSize;
    private Vector2[] _bgStars;
    private float[] _bgStarBrightness;
    private float[] _bgStarSpeed;

    // Screen shake
    private float _shakeAmount;
    private float _shakeDuration;

    // XP thresholds
    private float XpNeeded => 8f + _level * 5f;

    public override void _Ready()
    {
        _screenSize = GetViewportRect().Size;

        // Create background stars
        var rng = new RandomNumberGenerator();
        rng.Randomize();
        int starCount = 80;
        _bgStars = new Vector2[starCount];
        _bgStarBrightness = new float[starCount];
        _bgStarSpeed = new float[starCount];
        for (int i = 0; i < starCount; i++)
        {
            _bgStars[i] = new Vector2(rng.RandfRange(0, _screenSize.X), rng.RandfRange(0, _screenSize.Y));
            _bgStarBrightness[i] = rng.RandfRange(0.15f, 0.6f);
            _bgStarSpeed[i] = rng.RandfRange(0.5f, 2.5f);
        }

        // Create containers
        _bulletContainer = new Node2D { Name = "Bullets" };
        _enemyContainer = new Node2D { Name = "Enemies" };
        _gemContainer = new Node2D { Name = "Gems" };
        _effectContainer = new Node2D { Name = "Effects" };
        AddChild(_bulletContainer);
        AddChild(_enemyContainer);
        AddChild(_gemContainer);
        AddChild(_effectContainer);

        // Create camera
        _camera = new Camera2D();
        _camera.Enabled = true;
        _camera.Position = _screenSize / 2f;
        AddChild(_camera);

        // Create player
        _player = new Player();
        _player.Name = "Player";
        AddChild(_player);

        _player.OnFireBullet = SpawnBullet;
        _player.OnDied = OnPlayerDied;
        _player.GetNearestEnemyPosition = GetNearestEnemyPosition;

        // Create HUD
        _hud = new Hud();
        AddChild(_hud);

        // Create upgrade panel
        _upgradePanel = new UpgradePanel();
        _upgradePanel.OnUpgradeChosen = OnUpgradeChosen;
        AddChild(_upgradePanel);

        UpdateHud();
    }

    public override void _Process(double delta)
    {
        float dt = (float)delta;

        if (_gameOver)
        {
            _gameOverTimer += dt;
            // Allow restart after 1.5 seconds
            if (_gameOverTimer > 1.5f && (Input.IsKeyPressed(Key.Enter) || Input.IsKeyPressed(Key.Space)))
            {
                GetTree().Paused = false;
                GetTree().ChangeSceneToFile("res://Scenes/MainMenu.tscn");
            }
            QueueRedraw();
            return;
        }

        _gameTime += dt;

        // Update difficulty
        _difficultyTier = (int)(_gameTime / 30f);

        // Spawn enemies
        _spawnTimer -= dt;
        if (_spawnTimer <= 0)
        {
            SpawnEnemyWave();
            // Speed up spawning over time
            _spawnInterval = Mathf.Max(0.35f, 2.0f - _difficultyTier * 0.18f);
            _spawnTimer = _spawnInterval;
        }

        // Handle collisions
        CheckBulletEnemyCollisions();
        CheckPlayerEnemyCollisions();
        CheckPlayerGemCollisions();

        // Screen shake update
        if (_shakeDuration > 0)
        {
            _shakeDuration -= dt;
            _camera.Offset = new Vector2(
                (float)GD.RandRange(-_shakeAmount, _shakeAmount),
                (float)GD.RandRange(-_shakeAmount, _shakeAmount)
            );
        }
        else
        {
            _camera.Offset = Vector2.Zero;
        }

        UpdateHud();
        QueueRedraw();
    }

    public override void _Draw()
    {
        // Background grid
        Color gridColor = new Color(0.08f, 0.08f, 0.2f, 0.12f);
        float gridSpacing = 60f;
        for (float x = 0; x < _screenSize.X; x += gridSpacing)
            DrawLine(new Vector2(x, 0), new Vector2(x, _screenSize.Y), gridColor, 1f);
        for (float y = 0; y < _screenSize.Y; y += gridSpacing)
            DrawLine(new Vector2(0, y), new Vector2(_screenSize.X, y), gridColor, 1f);

        // Stars
        for (int i = 0; i < _bgStars.Length; i++)
        {
            float twinkle = (Mathf.Sin(_gameTime * _bgStarSpeed[i] + i * 2.1f) + 1f) * 0.5f;
            float alpha = _bgStarBrightness[i] * (0.3f + 0.7f * twinkle);
            DrawCircle(_bgStars[i], 1f + twinkle, new Color(0.6f, 0.7f, 1f, alpha));
        }

        // Pickup radius indicator (subtle)
        if (_player != null && !_player.IsDead)
        {
            DrawArc(_player.GlobalPosition, _player.PickupRadius, 0, Mathf.Tau, 32,
                new Color(0.3f, 1, 0.3f, 0.06f), 1f);
        }

        // Game over overlay
        if (_gameOver)
        {
            DrawRect(new Rect2(0, 0, _screenSize.X, _screenSize.Y), new Color(0, 0, 0, 0.7f));

            var font = ThemeDB.FallbackFont;

            string goText = "GAME OVER";
            float goW = font.GetStringSize(goText, HorizontalAlignment.Left, -1, 42).X;
            DrawString(font, new Vector2(_screenSize.X / 2f - goW / 2f, _screenSize.Y * 0.3f),
                goText, HorizontalAlignment.Left, -1, 42, new Color(1, 0.2f, 0.2f));

            int mins = (int)(_gameTime / 60f);
            int secs = (int)(_gameTime % 60f);

            string[] stats = {
                $"Survived: {mins:D2}:{secs:D2}",
                $"Score: {_score}",
                $"Level: {_level}",
                $"Enemies Destroyed: {_enemiesKilled}",
            };

            for (int i = 0; i < stats.Length; i++)
            {
                float sw = font.GetStringSize(stats[i], HorizontalAlignment.Left, -1, 20).X;
                DrawString(font, new Vector2(_screenSize.X / 2f - sw / 2f, _screenSize.Y * 0.42f + i * 30),
                    stats[i], HorizontalAlignment.Left, -1, 20, new Color(0.8f, 0.8f, 0.9f));
            }

            if (_gameOverTimer > 1.5f)
            {
                float blink = (Mathf.Sin(_gameOverTimer * 3f) + 1f) * 0.5f;
                string restartText = "Press ENTER or SPACE to return to menu";
                float rw = font.GetStringSize(restartText, HorizontalAlignment.Left, -1, 18).X;
                DrawString(font, new Vector2(_screenSize.X / 2f - rw / 2f, _screenSize.Y * 0.7f),
                    restartText, HorizontalAlignment.Left, -1, 18, new Color(1, 1, 1, 0.3f + 0.7f * blink));
            }
        }
    }

    // --- Spawning ---

    private void SpawnEnemyWave()
    {
        var rng = new RandomNumberGenerator();
        rng.Randomize();

        float diffScale = 1f + _difficultyTier * 0.25f;

        // Determine what to spawn based on difficulty
        int count = 1 + (int)(_difficultyTier * 0.6f);
        count = Mathf.Min(count, 8);

        for (int i = 0; i < count; i++)
        {
            EnemyType type = PickEnemyType(rng);
            Vector2 pos = GetSpawnPosition(rng);
            var enemy = Enemy.Create(type, pos, _player, diffScale);
            _enemyContainer.AddChild(enemy);
        }
    }

    private EnemyType PickEnemyType(RandomNumberGenerator rng)
    {
        float roll = rng.Randf();

        if (_difficultyTier < 1)
        {
            // Only chasers at start
            return EnemyType.Chaser;
        }
        else if (_difficultyTier < 2)
        {
            // Chasers and sprinters
            return roll < 0.65f ? EnemyType.Chaser : EnemyType.Sprinter;
        }
        else if (_difficultyTier < 4)
        {
            // Mix of 3
            if (roll < 0.45f) return EnemyType.Chaser;
            if (roll < 0.7f) return EnemyType.Sprinter;
            return EnemyType.Drifter;
        }
        else
        {
            // All types
            if (roll < 0.3f) return EnemyType.Chaser;
            if (roll < 0.5f) return EnemyType.Sprinter;
            if (roll < 0.75f) return EnemyType.Drifter;
            return EnemyType.Tank;
        }
    }

    private Vector2 GetSpawnPosition(RandomNumberGenerator rng)
    {
        float margin = 40f;
        int edge = (int)(rng.Randi() % 4);
        return edge switch
        {
            0 => new Vector2(rng.RandfRange(0, _screenSize.X), -margin),            // top
            1 => new Vector2(rng.RandfRange(0, _screenSize.X), _screenSize.Y + margin), // bottom
            2 => new Vector2(-margin, rng.RandfRange(0, _screenSize.Y)),              // left
            _ => new Vector2(_screenSize.X + margin, rng.RandfRange(0, _screenSize.Y)), // right
        };
    }

    private void SpawnBullet(Vector2 pos, Vector2 dir, float damage, float scale, float speed, int pierce)
    {
        var bullet = new Bullet
        {
            Position = pos,
            Direction = dir,
            Damage = damage,
            Scale_ = scale,
            Speed = speed,
            PierceLeft = pierce
        };
        _bulletContainer.AddChild(bullet);
    }

    // --- Collisions ---

    private void CheckBulletEnemyCollisions()
    {
        var bullets = _bulletContainer.GetChildren();
        var enemies = _enemyContainer.GetChildren();

        foreach (var bNode in bullets)
        {
            if (bNode is not Bullet bullet) continue;
            if (!IsInstanceValid(bullet)) continue;

            foreach (var eNode in enemies)
            {
                if (eNode is not Enemy enemy) continue;
                if (!IsInstanceValid(enemy)) continue;

                float dist = bullet.GlobalPosition.DistanceTo(enemy.GlobalPosition);
                if (dist < bullet.CollisionRadius + enemy.CollisionRadius)
                {
                    bool killed = enemy.TakeDamage(bullet.Damage);
                    bool destroyBullet = bullet.OnHitEnemy();

                    if (killed)
                    {
                        OnEnemyKilled(enemy);
                    }

                    if (destroyBullet)
                    {
                        bullet.QueueFree();
                        break;
                    }
                }
            }
        }
    }

    private void CheckPlayerEnemyCollisions()
    {
        if (_player.IsDead) return;

        var enemies = _enemyContainer.GetChildren();
        foreach (var eNode in enemies)
        {
            if (eNode is not Enemy enemy) continue;
            if (!IsInstanceValid(enemy)) continue;

            float dist = _player.GlobalPosition.DistanceTo(enemy.GlobalPosition);
            if (dist < _player.CollisionRadius + enemy.CollisionRadius)
            {
                _player.TakeDamage(enemy.ContactDamage);
                ScreenShake(4f, 0.2f);

                // Knockback enemy
                Vector2 knockDir = (enemy.GlobalPosition - _player.GlobalPosition).Normalized();
                enemy.Position += knockDir * 30f;
            }
        }
    }

    private void CheckPlayerGemCollisions()
    {
        if (_player.IsDead) return;

        var gems = _gemContainer.GetChildren();
        foreach (var gNode in gems)
        {
            if (gNode is not XpGem gem) continue;
            if (!IsInstanceValid(gem)) continue;

            float dist = _player.GlobalPosition.DistanceTo(gem.GlobalPosition);

            // Magnet pull
            if (dist < _player.PickupRadius)
            {
                float pullSpeed = 350f + (1f - dist / _player.PickupRadius) * 300f;
                gem.MoveToward(_player.GlobalPosition, pullSpeed, (float)GetProcessDeltaTime());
            }

            // Collect
            if (dist < 18f)
            {
                _xp += gem.Value;
                _score += gem.Value * 10;
                gem.QueueFree();

                // Check level up
                if (_xp >= XpNeeded && !_upgradePanel.IsOpen)
                {
                    LevelUp();
                }
            }
        }
    }

    // --- Events ---

    private void OnEnemyKilled(Enemy enemy)
    {
        _enemiesKilled++;
        _score += 50 + enemy.XpValue * 20;

        // Spawn XP gems
        var gem = new XpGem();
        gem.Position = enemy.GlobalPosition;
        gem.Value = enemy.XpValue;
        _gemContainer.AddChild(gem);

        // Spawn explosion
        Color explosionColor = enemy.Type switch
        {
            EnemyType.Chaser => new Color(1, 0.3f, 0.2f),
            EnemyType.Drifter => new Color(0.8f, 0.3f, 1f),
            EnemyType.Sprinter => new Color(1, 0.6f, 0.1f),
            EnemyType.Tank => new Color(0.3f, 1, 0.5f),
            _ => Colors.White
        };

        int particleCount = enemy.Type == EnemyType.Tank ? 20 : 10;
        float particleSpeed = enemy.Type == EnemyType.Tank ? 160f : 100f;

        var explosion = ExplosionEffect.Create(enemy.GlobalPosition, explosionColor, particleCount, particleSpeed);
        _effectContainer.AddChild(explosion);

        ScreenShake(2f, 0.1f);

        enemy.QueueFree();
    }

    private void OnPlayerDied()
    {
        _gameOver = true;
        _gameOverTimer = 0f;

        // Big explosion at player position
        var explosion = ExplosionEffect.Create(_player.GlobalPosition, new Color(0, 1, 1), 30, 200f, 5f);
        _effectContainer.AddChild(explosion);
        ScreenShake(8f, 0.5f);
    }

    private void LevelUp()
    {
        _xp -= XpNeeded;
        _level++;
        GetTree().Paused = true;
        _upgradePanel.ShowUpgrades();
    }

    private void OnUpgradeChosen(UpgradeType type)
    {
        // Apply upgrade to player stats
        switch (type)
        {
            case UpgradeType.Multishot:
                _player.BulletCount++;
                break;
            case UpgradeType.FireRate:
                _player.FireInterval *= 0.85f;
                break;
            case UpgradeType.BulletSize:
                _player.BulletScale *= 1.3f;
                break;
            case UpgradeType.Damage:
                _player.BulletDamage *= 1.5f;
                break;
            case UpgradeType.MoveSpeed:
                _player.MoveSpeed *= 1.15f;
                break;
            case UpgradeType.MaxHp:
                _player.MaxHealth += 25f;
                _player.Heal(25f);
                break;
            case UpgradeType.Regen:
                _player.RegenPerSecond += 1f;
                break;
            case UpgradeType.Magnet:
                _player.PickupRadius *= 1.4f;
                break;
            case UpgradeType.Pierce:
                _player.PierceCount++;
                break;
        }

        GetTree().Paused = false;
        UpdateHud();
    }

    // --- Helpers ---

    private Vector2 GetNearestEnemyPosition()
    {
        var enemies = _enemyContainer.GetChildren();
        float bestDist = float.MaxValue;
        Vector2 bestPos = Vector2.Inf;

        foreach (var eNode in enemies)
        {
            if (eNode is not Enemy enemy) continue;
            if (!IsInstanceValid(enemy)) continue;

            float dist = _player.GlobalPosition.DistanceSquaredTo(enemy.GlobalPosition);
            if (dist < bestDist)
            {
                bestDist = dist;
                bestPos = enemy.GlobalPosition;
            }
        }

        return bestPos;
    }

    private void UpdateHud()
    {
        _hud.UpdateStats(
            _player.Health, _player.MaxHealth,
            _xp, XpNeeded,
            _level, _score,
            _gameTime, _enemiesKilled
        );
    }

    private void ScreenShake(float amount, float duration)
    {
        if (amount > _shakeAmount)
        {
            _shakeAmount = amount;
            _shakeDuration = duration;
        }
    }
}
