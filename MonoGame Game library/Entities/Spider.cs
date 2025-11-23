using System;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using MonoGameLibrary.Graphics;
using reckless_and_gun.Entities; // Necesario para HealthBar

namespace MonoGameLibrary.Entities;

public class Spider : AnimatedSprite
{
    // ... (Tus Enums y variables de siempre) ...
    public enum SpiderState { Patrolling, Attacking_Ranged, Attacking_Melee, Dying }
    
    public float Health { get; private set; }
    public float MaxHealth { get; private set; }

    // --- NUEVO: COMPONENTE DE BARRA DE VIDA ---
    private HealthBar _hpBar; 
    // ------------------------------------------

    private float _spiderVelocity;
    private SpiderState _currentState;
    public bool IsActive { get; private set; }
    
    private Vector2 _direction;
    private TimeSpan _patrolTimer;
    private TimeSpan _timeToChangeDirection;
    private static Random _random = new Random();
    private TimeSpan _attackTimer;
    private TimeSpan _timeToNextAttack;
    private bool _isFlashingRed;
    private TimeSpan _damageFlashTimer;
    private const float _meleeAttackRange = 40f;
    private const float _rangedAttackRange = 250f;

    public event Action<Rectangle> OnMeleeAttack;
    public event Action<Vector2> OnPoisonSpit;

    public Rectangle Hitbox {
        get {
            if (Region == null) return Rectangle.Empty;
            float w = this.Region.Width * this.Scale.X;
            float h = this.Region.Height * this.Scale.Y;
            float x = this.Position.X - (this.Origin.X * this.Scale.X);
            float y = this.Position.Y - (this.Origin.Y * this.Scale.Y);
            return new Rectangle((int)x, (int)y, (int)w, (int)h);
        }
    }

    public Spider(float health, float velocity, Vector2 startPosition)
    {
        Health = health;
        _spiderVelocity = velocity;
        Position = startPosition;
        IsActive = true;
        _currentState = SpiderState.Patrolling;
        
        // Inicializamos timers...
        _direction = (_random.Next(0, 2) == 0) ? -Vector2.UnitX : Vector2.UnitX;
        _timeToChangeDirection = TimeSpan.FromSeconds(_random.Next(2, 6));
        _timeToNextAttack = TimeSpan.FromSeconds(_random.Next(3, 8));
    }

    public void InitializeHealthBar(Texture2D texture)
    {
        _hpBar = new HealthBar(texture, 50, 6, new Vector2(-25, -80));
    }

    public void LoadContent(TextureAtlas atlas, Vector2 scale)
    {
        AnimatedSprite tempSprite = atlas.CreateAnimatedSprite("spider_walking"); 
        if (tempSprite.Animations != null)
        {
            foreach (var anim in tempSprite.Animations) this.AddAnimation(anim.Key, anim.Value);
        }
        this.Scale = scale;
        _currentState = SpiderState.Patrolling;
        Play("spider_walking");
        if (this.Region != null) this.Origin = new Vector2(this.Region.Width / 2f, this.Region.Height / 2f);
    }

    public void Update(GameTime gameTime, Vector2 playerPosition)
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
        
        // Updates de estado (Patrol, Attack...) igual que antes
        // ... (Pega aquí tu switch de estados original) ...
        
        // Lógica de movimiento básica para que no se rompa si no pegas el switch
        if (_currentState == SpiderState.Patrolling)
        {
             _patrolTimer += gameTime.ElapsedGameTime;
             if (_patrolTimer >= _timeToChangeDirection) {
                 _direction = (_random.Next(0, 2) == 0) ? -Vector2.UnitX : Vector2.UnitX;
                 _timeToChangeDirection = TimeSpan.FromSeconds(_random.Next(2, 6));
                 _patrolTimer = TimeSpan.Zero;
             }
             this.Position += _direction * _spiderVelocity * (float)gameTime.ElapsedGameTime.TotalSeconds;
        }

        base.Update(gameTime);
    }

    // Necesitas pegar aquí tus métodos UpdateAI, HandlePatrolling, etc.
    // Para simplificar el ejemplo asumo que ya los tienes.
    private void UpdateAI(GameTime gameTime, Vector2 playerPosition) { /* ... Tu IA ... */ }

    public void TakeDamage(int amount)
    {
        if (IsActive)
        {
            Health -= amount;
            _isFlashingRed = true;
            this.Color = Color.Red;

            if (Health <= 0)
            {
                IsActive = false;
                Health = 0;
                _currentState = SpiderState.Dying;
            }
        }
    }

    // --- MODIFICADO: Draw ahora dibuja también la barra ---
    public override void Draw(SpriteBatch spriteBatch, Vector2 position)
    {
        // Dibujamos la araña
        base.Draw(spriteBatch, position);

        // Dibujamos la barra encima
        if (_hpBar != null && IsActive)
        {
            _hpBar.Draw(spriteBatch, this.Position, Health, MaxHealth);
        }
    }
    
    // Método DrawDebug para las hitboxes
    public void DrawDebug(SpriteBatch spriteBatch, Texture2D tex)
    {
        // Dibujar borde amarillo
        Rectangle r = Hitbox;
        spriteBatch.Draw(tex, new Rectangle(r.X, r.Y, r.Width, 2), Color.Yellow);
        spriteBatch.Draw(tex, new Rectangle(r.X, r.Y + r.Height, r.Width, 2), Color.Yellow);
        spriteBatch.Draw(tex, new Rectangle(r.X, r.Y, 2, r.Height), Color.Yellow);
        spriteBatch.Draw(tex, new Rectangle(r.X + r.Width, r.Y, 2, r.Height), Color.Yellow);
    }
}