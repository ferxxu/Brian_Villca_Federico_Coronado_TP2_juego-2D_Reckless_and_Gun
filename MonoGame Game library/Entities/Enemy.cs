using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary.Graphics;
using reckless_and_gun.Entities;

namespace MonoGameLibrary.Entities;

public class Enemy : AnimatedSprite
{
    public enum EnemyState { Patrolling, Attacking_Ranged, Attacking_Melee, Dying }

    public float Health { get; protected set; }
    public float MaxHealth { get; protected set; }
    protected float _velocity;
    public bool IsActive { get; protected set; } = true;
    protected EnemyState _currentState;

    protected Vector2 _direction;
    protected TimeSpan _patrolTimer;
    protected TimeSpan _timeToChangeDirection;
    protected TimeSpan _attackTimer;
    protected TimeSpan _timeToNextAttack;
    protected static Random _random = new Random();

    protected float _meleeRange;
    protected float _rangedRange;

    protected string _projectileTextureName;

    protected float _projectileSpeed;
    protected int _projectileDamage;

    protected string _animIdle, _animWalk, _animShoot, _animMelee, _animDie;

    private bool _isFlashingRed;
    private TimeSpan _damageFlashTimer;

    protected bool _shootFrameTriggered;

    public event Action<Rectangle> OnMeleeAttack;

    public Rectangle Hitbox
{
    get
    {
        if (Region == null) return Rectangle.Empty;

        // 1. Calculamos el tamaño VISUAL completo
        float visualW = this.Region.Width * this.Scale.X;
        float visualH = this.Region.Height * this.Scale.Y;

        // 2. Calculamos la posición X,Y original (esquina superior izquierda del sprite)
        float visualX = this.Position.X - (this.Origin.X * this.Scale.X);
        float visualY = this.Position.Y - (this.Origin.Y * this.Scale.Y);

        // --- AJUSTE DE HITBOX ---
        
        // Este número controla el tamaño de la hitbox (0.7f = 70% del tamaño original)
        // Cámbialo si quieres que sea más grande (0.8f) o más chica (0.5f)
        float factor = 0.7f; 

        float hitW = visualW * factor;
        float hitH = visualH * factor;

        // 3. Centramos la hitbox dentro del sprite visual
        // Sumamos la mitad de la diferencia de tamaño a la posición
        float hitX = visualX + (visualW - hitW) / 2;
        float hitY = visualY + (visualH - hitH) / 2;

        return new Rectangle((int)hitX, (int)hitY, (int)hitW, (int)hitH);
    }
}
    public Enemy(float health, float velocity, string namePrefix)
    {
        Health = health;
        MaxHealth = health;
        _velocity = velocity;
        _currentState = EnemyState.Patrolling;

        _animIdle = $"{namePrefix}_idle";
        _animWalk = $"{namePrefix}_walking";
        _animShoot = $"{namePrefix}_shooting";
        _animMelee = $"{namePrefix}_melee";
        _animDie = $"{namePrefix}_die";

        InitializeAI();
    }

    private void InitializeAI()
    {
        _direction = (_random.Next(0, 2) == 0) ? -Vector2.UnitX : Vector2.UnitX;
        _timeToChangeDirection = TimeSpan.FromSeconds(_random.Next(2, 6));
        _timeToNextAttack = TimeSpan.FromSeconds(_random.Next(2, 5));
    }

    public virtual void LoadContent(TextureAtlas atlas, Vector2 scale)
    {
        AnimatedSprite tempSprite = atlas.CreateAnimatedSprite(_animWalk);
        if (tempSprite.Animations != null)
        {
            foreach (var anim in tempSprite.Animations) this.AddAnimation(anim.Key, anim.Value);
        }
        this.Scale = scale;
        Play(_animWalk);
        if (this.Region != null)
            this.Origin = new Vector2(this.Region.Width / 2f, this.Region.Height / 2f);
    }

    public virtual void Update(GameTime gameTime, Vector2 playerPosition, int mapWidth, List<Rectangle> walls)
    {
        if (!IsActive) return;

        if (_isFlashingRed)
        {
            _damageFlashTimer += gameTime.ElapsedGameTime;
            if (_damageFlashTimer >= TimeSpan.FromMilliseconds(100))
            {
                _isFlashingRed = false;
                _damageFlashTimer = TimeSpan.Zero;
                this.Color = Color.White;
            }
        }

        _attackTimer += gameTime.ElapsedGameTime;
        UpdateAI(gameTime, playerPosition);

        switch (_currentState)
        {
            case EnemyState.Patrolling: HandlePatrolling(gameTime, mapWidth, walls); break;
            case EnemyState.Attacking_Ranged: HandleRangedAttack(gameTime); break;
            case EnemyState.Attacking_Melee: HandleMeleeAttack(gameTime); break;
        }

        base.Update(gameTime);
    }

    protected virtual void UpdateAI(GameTime gameTime, Vector2 playerPosition)
    {
        if (_currentState != EnemyState.Patrolling) return;

        float distance = Vector2.Distance(this.Position, playerPosition);

        if (distance <= _rangedRange)
        {
            Effects = (playerPosition.X < this.Position.X) ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
        }

        if (distance <= _meleeRange)
        {
            _currentState = EnemyState.Attacking_Melee;
            Play(_animMelee);
            if (Animations.ContainsKey(_animMelee)) Animations[_animMelee].IsLooping = false;
        }
        else if (distance <= _rangedRange && _attackTimer >= _timeToNextAttack)
        {
            _currentState = EnemyState.Attacking_Ranged;
            Play(_animShoot);
            if (Animations.ContainsKey(_animShoot)) Animations[_animShoot].IsLooping = false;
        }
        else
        {
            Play(_animWalk);
            if (Animations.ContainsKey(_animWalk)) Animations[_animWalk].IsLooping = true;
        }
    }

    protected void HandlePatrolling(GameTime gameTime, int mapWidth, List<Rectangle> walls)
    {
        if (_direction.X < 0) Effects = SpriteEffects.FlipHorizontally;
        else Effects = SpriteEffects.None;

        if (Position.X <= 0) _direction = Vector2.UnitX;
        else if (Position.X >= mapWidth) _direction = -Vector2.UnitX;

        Vector2 sensorPoint = Position + (_direction * 30);
        sensorPoint.Y -= 15;

        foreach (var wall in walls)
        {
            if (wall.Contains(sensorPoint))
            {
                _direction = -_direction;
                _patrolTimer = TimeSpan.Zero;
                break;
            }
        }

        _patrolTimer += gameTime.ElapsedGameTime;
        if (_patrolTimer >= _timeToChangeDirection)
        {
            _direction = -_direction;
            _patrolTimer = TimeSpan.Zero;
        }
        this.Position += _direction * _velocity * (float)gameTime.ElapsedGameTime.TotalSeconds;
    }

    protected void HandleRangedAttack(GameTime gameTime)
    {
        if (this.IsAnimationFinished)
        {
            _shootFrameTriggered = true;
            _currentState = EnemyState.Patrolling;
            Play(_animWalk);
            if (Animations.ContainsKey(_animWalk)) Animations[_animWalk].IsLooping = true;

            _attackTimer = TimeSpan.Zero;
            _timeToNextAttack = TimeSpan.FromSeconds(_random.Next(2, 5));
        }
    }

    protected void HandleMeleeAttack(GameTime gameTime)
    {
        if (this.IsAnimationFinished)
        {
            Rectangle meleeHit = new Rectangle((int)Position.X - 20, (int)Position.Y - 20, 40, 40);
            OnMeleeAttack?.Invoke(meleeHit);
            _currentState = EnemyState.Patrolling;
            Play(_animWalk);
        }
    }

    public virtual Projectile TryShoot(TextureAtlas atlas, Vector2 targetPos)
    {
        if (_shootFrameTriggered)
        {
            _shootFrameTriggered = false;

            Sprite sprite = null;
            try
            {
                sprite = atlas.CreateSprite(_projectileTextureName);
            }
            catch
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] Textura '{_projectileTextureName}' no encontrada.");
            }

            if (sprite != null)
            {
                sprite.Scale = new Vector2(2f, 2f); 
                sprite.Origin = new Vector2(sprite.Region.Width / 2f, sprite.Region.Height / 2f);
            }

            Vector2 dir = targetPos - this.Position;
            if (dir != Vector2.Zero) dir.Normalize();

            var bullet = new EnemyProjectile(sprite, this.Position, dir, _projectileSpeed, _projectileDamage);

            if (sprite == null)
            {
                bullet.Width = 20;
                bullet.Height = 20;
            }

            return bullet;
        }
        return null;
    }

    public void TakeDamage(int amount)
    {
        if (!IsActive) return;
        Health -= amount;
        _isFlashingRed = true;
        this.Color = Color.Red;
        if (Health <= 0) IsActive = false;
    }

    public override void Draw(SpriteBatch spriteBatch, Vector2 position)
    {
        base.Draw(spriteBatch, position);
    }
}