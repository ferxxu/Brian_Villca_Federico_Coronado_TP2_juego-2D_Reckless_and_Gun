using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary.Graphics;
using reckless_and_gun.Entities; // Para HealthBar y EnemyProjectile

namespace MonoGameLibrary.Entities;

public class Enemy : AnimatedSprite
{
    public enum EnemyState { Patrolling, Attacking_Ranged, Attacking_Melee, Dying }

    // --- Estadísticas ---
    public float Health { get; protected set; }
    public float MaxHealth { get; protected set; }
    protected float _velocity;
    
    // --- Estado ---
    public bool IsActive { get; protected set; } = true;
    protected EnemyState _currentState;
    
    // --- IA y Timers ---
    protected Vector2 _direction;
    protected TimeSpan _patrolTimer;
    protected TimeSpan _timeToChangeDirection;
    protected TimeSpan _attackTimer;
    protected TimeSpan _timeToNextAttack;
    protected static Random _random = new Random();

    // --- Configuración de Combate (Configurable por hijo) ---
    protected float _meleeRange;
    protected float _rangedRange;
    protected string _projectileTextureName;
    protected float _projectileSpeed;
    protected int _projectileDamage;
    
    // --- Nombres de Animaciones (Configurables) ---
    protected string _animIdle, _animWalk, _animShoot, _animMelee, _animDie;

    // --- Visuales ---
    private bool _isFlashingRed;
    private TimeSpan _damageFlashTimer;
    private bool _shootFrameTriggered; // Para disparar en el frame exacto

    // --- Eventos ---
    public event Action<Rectangle> OnMeleeAttack; // Para colisiones cuerpo a cuerpo

    // Hitbox Dinámica
    public Rectangle Hitbox
    {
        get
        {
            if (Region == null) return Rectangle.Empty;
            float w = this.Region.Width * this.Scale.X;
            float h = this.Region.Height * this.Scale.Y;
            float x = this.Position.X - (this.Origin.X * this.Scale.X);
            float y = this.Position.Y - (this.Origin.Y * this.Scale.Y);
            return new Rectangle((int)x, (int)y, (int)w, (int)h);
        }
    }

    // CONSTRUCTOR GENÉRICO
    public Enemy(float health, float velocity, string namePrefix)
    {
        Health = health;
        MaxHealth = health;
        _velocity = velocity;
        _currentState = EnemyState.Patrolling;

        // Nombres de animación por defecto (Ej: "spider_walking")
        // Los hijos pueden sobrescribir esto si sus XML son raros
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
        // Cargamos la animación de caminar por defecto
        AnimatedSprite tempSprite = atlas.CreateAnimatedSprite(_animWalk);
        if (tempSprite.Animations != null)
        {
            foreach (var anim in tempSprite.Animations) this.AddAnimation(anim.Key, anim.Value);
        }
        
        this.Scale = scale;
        Play(_animWalk);
        
        // Centramos el origen para que las rotaciones y escalas funcionen bien
        if (this.Region != null) 
            this.Origin = new Vector2(this.Region.Width / 2f, this.Region.Height / 2f);
    }

    public virtual void Update(GameTime gameTime, Vector2 playerPosition)
    {
        if (!IsActive) return;

        // Flash de daño
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
        UpdateAI(gameTime, playerPosition); // Lógica de cerebro

        // Ejecutar estado actual
        switch (_currentState)
        {
            case EnemyState.Patrolling: HandlePatrolling(gameTime); break;
            case EnemyState.Attacking_Ranged: HandleRangedAttack(gameTime); break;
            case EnemyState.Attacking_Melee: HandleMeleeAttack(gameTime); break;
        }

        base.Update(gameTime);
    }

    protected virtual void UpdateAI(GameTime gameTime, Vector2 playerPosition)
    {
        // Si estamos atacando o muriendo, no cambiamos de opinión
        if (_currentState != EnemyState.Patrolling) return;

        float distance = Vector2.Distance(this.Position, playerPosition);

        // 1. Mirar al jugador
        if (distance <= _rangedRange)
        {
            Effects = (playerPosition.X < this.Position.X) ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
        }

        // 2. Decidir Ataque
        if (distance <= _meleeRange)
        {
            _currentState = EnemyState.Attacking_Melee;
            Play(_animMelee);
        }
        else if (distance <= _rangedRange && _attackTimer >= _timeToNextAttack)
        {
            _currentState = EnemyState.Attacking_Ranged;
            Play(_animShoot);
        }
        else
        {
            // Si no ataca, camina
            Play(_animWalk);
        }
    }

    protected void HandlePatrolling(GameTime gameTime)
    {
        // Girar sprite según dirección de movimiento
        if (_direction.X < 0) Effects = SpriteEffects.FlipHorizontally;
        else Effects = SpriteEffects.None;

        _patrolTimer += gameTime.ElapsedGameTime;
        if (_patrolTimer >= _timeToChangeDirection)
        {
            _direction = -_direction; // Invertir dirección
            _patrolTimer = TimeSpan.Zero;
        }
        this.Position += _direction * _velocity * (float)gameTime.ElapsedGameTime.TotalSeconds;
    }

    protected void HandleRangedAttack(GameTime gameTime)
    {
        if (this.IsAnimationFinished)
        {
            _shootFrameTriggered = true; // ¡Preparamos la bala!
            
            _currentState = EnemyState.Patrolling;
            Play(_animWalk);
            _attackTimer = TimeSpan.Zero; // Reset cooldown
            _timeToNextAttack = TimeSpan.FromSeconds(_random.Next(2, 5));
        }
    }

    protected void HandleMeleeAttack(GameTime gameTime)
    {
        if (this.IsAnimationFinished)
        {
            // Hitbox Melee Simple
            Rectangle meleeHit = new Rectangle((int)Position.X - 20, (int)Position.Y - 20, 40, 40);
            OnMeleeAttack?.Invoke(meleeHit);

            _currentState = EnemyState.Patrolling;
            Play(_animWalk);
        }
    }

    // --- MÉTODO UNIFICADO DE DISPARO (TRYSHOOT) ---
    public Projectile TryShoot(TextureAtlas atlas, Vector2 targetPos)
    {
        if (_shootFrameTriggered)
        {
            _shootFrameTriggered = false;

            // 1. Crear Sprite desde el Atlas usando el nombre configurado
            Sprite sprite = atlas.CreateSprite(_projectileTextureName);
            sprite.Scale = new Vector2(2f, 2f);

            // 2. Calcular Dirección
            Vector2 dir = targetPos - this.Position;
            if (dir != Vector2.Zero) dir.Normalize();

            // 3. Devolver Proyectil Genérico Enemigo
            return new EnemyProjectile(sprite, this.Position, dir, _projectileSpeed, _projectileDamage);
        }
        return null;
    }

    public void TakeDamage(int amount)
    {
        if (!IsActive) return;

        Health -= amount;
        _isFlashingRed = true;
        this.Color = Color.Red;

        if (Health <= 0)
        {
            IsActive = false;
            _currentState = EnemyState.Dying;
            // Play(_animDie); // Si existe
        }
    }

    public override void Draw(SpriteBatch spriteBatch, Vector2 position)
    {
        base.Draw(spriteBatch, position);
    }
}