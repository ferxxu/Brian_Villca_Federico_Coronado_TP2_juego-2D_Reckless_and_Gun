using System;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using MonoGameLibrary.Graphics;
using System.Linq;

namespace MonoGameLibrary.Entities;

public class Gorilla : AnimatedSprite
{
    public enum GorillaState
    {
        Patrolling,       // Caminar aleatoriamente
        Attacking_Ranged, // Tirar banana/piedra
        Attacking_Melee,  // Golpe cuerpo a cuerpo
        Dying
    }

    private float _gorillaHealth;
    private float _gorillaVelocity;
    private GorillaState _currentState;

    public bool IsActive { get; private set; }
    public bool IsMarkedForRemoval { get; private set; }

    private Vector2 _direction;
    private TimeSpan _patrolTimer;
    private TimeSpan _timeToChangeDirection;
    private static Random _random = new Random();

    // Timers de ataque
    private TimeSpan _attackTimer;
    private TimeSpan _timeToNextAttack;

    // Daño
    private bool _isTakingDamage;
    private TimeSpan _damageFlashTimer;
    private static readonly TimeSpan _damageFlashDuration = TimeSpan.FromMilliseconds(100);

    // Rangos (El gorila es más grande, aumentamos un poco el melee y el rango de visión)
    private const float _meleeAttackRange = 80f;
    private const float _rangedAttackRange = 400f;

    // --- Eventos ---
    // Evento para golpe físico
    public event Action<Rectangle> OnMeleeAttack;
    // Evento para disparar proyectil (Banana)
    public event Action<Vector2> OnProjectileThrow;

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

    public Gorilla(float health, float velocity, Vector2 startPosition)
    {
        _gorillaHealth = health;
        _gorillaVelocity = velocity;
        this.Position = startPosition;

        IsActive = true;
        IsMarkedForRemoval = false;

        _isTakingDamage = false;
        _damageFlashTimer = TimeSpan.Zero;

        if (this.Color == default(Color))
        {
            this.Color = Color.White;
        }

        _currentState = GorillaState.Patrolling;

        SetNewPatrolTimer();
        SetNewAttackTimer();
    }

    private void SetNewPatrolTimer()
    {
        _direction = (_random.Next(0, 2) == 0) ? -Vector2.UnitX : Vector2.UnitX;
        _timeToChangeDirection = TimeSpan.FromSeconds(_random.Next(2, 5)); // Cambia de dirección cada 2-5 seg
        _patrolTimer = TimeSpan.Zero;
    }

    private void SetNewAttackTimer()
    {
        _timeToNextAttack = TimeSpan.FromSeconds(_random.Next(2, 4)); // Ataca más seguido que la araña
        _attackTimer = TimeSpan.Zero;
    }

    private void HandleDamageFlash(GameTime gameTime)
    {
        if (_isTakingDamage)
        {
            _damageFlashTimer += gameTime.ElapsedGameTime;

            if (_damageFlashTimer >= _damageFlashDuration)
            {
                _isTakingDamage = false;
                _damageFlashTimer = TimeSpan.Zero;
                this.Color = Color.White;
            }
        }
    }

    public void LoadContent(TextureAtlas atlas, Vector2 scale)
    {
        // Usamos "gorilla_idle" como base, asegúrate que exista en tu XML
        AnimatedSprite tempSprite = atlas.CreateAnimatedSprite("gorilla_idle");

        if (tempSprite.Animations != null)
        {
            foreach (var anim in tempSprite.Animations)
            {
                this.AddAnimation(anim.Key, anim.Value);
            }
        }
        this.Scale = scale;

        _currentState = GorillaState.Patrolling;
        Play("gorilla_idle");

        if (this.Region != null)
        {
            this.Origin = new Vector2(this.Region.Width / 2f, this.Region.Height / 2f);
        }
    }

    public void Update(GameTime gameTime, Vector2 playerPosition)
    {
        if (!IsActive)
        {
            if (this.IsAnimationFinished)
            {
                IsMarkedForRemoval = true;
            }
            base.Update(gameTime);
            return;
        }

        HandleDamageFlash(gameTime);

        _attackTimer += gameTime.ElapsedGameTime;

        if (!_isTakingDamage)
        {
            UpdateAI(gameTime, playerPosition);

            switch (_currentState)
            {
                case GorillaState.Patrolling:
                    HandlePatrolling(gameTime);
                    break;
                case GorillaState.Attacking_Ranged:
                    HandleRangedAttack(gameTime);
                    break;
                case GorillaState.Attacking_Melee:
                    HandleMeleeAttack(gameTime);
                    break;
            }
        }

        base.Update(gameTime);
    }

    private void UpdateAI(GameTime gameTime, Vector2 playerPosition)
    {
        if (_currentState == GorillaState.Attacking_Ranged ||
            _currentState == GorillaState.Attacking_Melee ||
            _currentState == GorillaState.Dying)
        {
            return;
        }

        float distanceToPlayer = Vector2.Distance(this.Position, playerPosition);

        // FIX 2 (Parte A): Mirar al jugador si está en rango
        if (distanceToPlayer <= _rangedAttackRange)
        {
            if (playerPosition.X < this.Position.X)
            {
                this.Effects = SpriteEffects.FlipHorizontally; // Mirar Izquierda
            }
            else
            {
                this.Effects = SpriteEffects.None; // Mirar Derecha
            }
        }

        // Lógica de selección de estado
       
        else if (distanceToPlayer <= _rangedAttackRange)
        {
            if (_attackTimer >= _timeToNextAttack)
            {
                _currentState = GorillaState.Attacking_Ranged;
                // Animación de lanzar banana/piedra
                Play("gorilla_shooting");
            }
            else
            {
                // En rango pero en cooldown -> Patrullamos/Esperamos
                _currentState = GorillaState.Patrolling;
                Play("gorilla_idle"); // O "gorilla_run" si quieres que corra
            }
        }
        else
        {
            // Jugador lejos -> Patrullar
            _currentState = GorillaState.Patrolling;
            Play("gorilla_walking"); // Animación de correr/caminar
        }
    }

    private void HandleMeleeAttack(GameTime gameTime)
    {
        if (this.IsAnimationFinished)
        {
            // Hitbox del puñetazo
            Rectangle meleeHitbox = new Rectangle(
                (int)this.Position.X,
                (int)this.Position.Y - 20,
                (int)(this.Region.Width * this.Scale.X),
                (int)(this.Region.Height * this.Scale.Y)
            );

            OnMeleeAttack?.Invoke(meleeHitbox);

            _currentState = GorillaState.Patrolling;
            Play("gorilla_idle"); // Volver a idle tras golpear
        }
    }

    private void HandlePatrolling(GameTime gameTime)
    {
        // FIX 2 (Parte B): Girar según movimiento si patrullamos
        if (_currentState == GorillaState.Patrolling)
        {
            if (_direction.X < 0)
            {
                this.Effects = SpriteEffects.FlipHorizontally;
            }
            else
            {
                this.Effects = SpriteEffects.None;
            }
        }

        _patrolTimer += gameTime.ElapsedGameTime;
        if (_patrolTimer >= _timeToChangeDirection)
        {
            SetNewPatrolTimer();
        }

        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
        this.Position += _direction * _gorillaVelocity * deltaTime;
    }

    private void HandleRangedAttack(GameTime gameTime)
    {
        if (this.IsAnimationFinished)
        {
            // Disparamos el evento. En GameScene nos suscribimos a esto para crear la bala.
            OnProjectileThrow?.Invoke(this.Position);

            _currentState = GorillaState.Patrolling;
            Play("gorilla_idle");
            SetNewAttackTimer(); // Reiniciar cooldown
        }
    }

    public void TakeDamage(int amount)
    {
        if (!_isTakingDamage && IsActive)
        {
            _gorillaHealth -= amount;

            if (_gorillaHealth <= 0)
            {
                IsActive = false;
                _gorillaHealth = 0;
                _currentState = GorillaState.Dying;
                this.Color = Color.White;
                Play("gorilla_die"); // Asegúrate que exista en XML
            }
            else
            {
                _isTakingDamage = true;
                _damageFlashTimer = TimeSpan.Zero;
                this.Color = Color.Red; // Feedback visual
            }
        }
    }

    public void DrawDebug(SpriteBatch spriteBatch, Texture2D debugTexture)
    {
        // Hitbox del cuerpo
        DibujarBordeRectangulo(spriteBatch, debugTexture, this.Hitbox, Color.Yellow, 1);

        // Rango Melee
        Rectangle meleeRangeRect = new Rectangle(
            (int)(this.Position.X - _meleeAttackRange),
            (int)(this.Position.Y - _meleeAttackRange),
            (int)(_meleeAttackRange * 2),
            (int)(_meleeAttackRange * 2)
        );
        DibujarBordeRectangulo(spriteBatch, debugTexture, meleeRangeRect, Color.Red, 1);

        // Rango Disparo
        Rectangle rangedRangeRect = new Rectangle(
            (int)(this.Position.X - _rangedAttackRange),
            (int)(this.Position.Y - _rangedAttackRange),
            (int)(_rangedAttackRange * 2),
            (int)(_rangedAttackRange * 2)
        );
        DibujarBordeRectangulo(spriteBatch, debugTexture, rangedRangeRect, Color.Orange, 1);
    }

    private void DibujarBordeRectangulo(SpriteBatch spriteBatch, Texture2D debugTexture, Rectangle rect, Color color, int grosor)
    {
        spriteBatch.Draw(debugTexture, new Rectangle(rect.Left, rect.Top, rect.Width, grosor), color);
        spriteBatch.Draw(debugTexture, new Rectangle(rect.Left, rect.Bottom - grosor, rect.Width, grosor), color);
        spriteBatch.Draw(debugTexture, new Rectangle(rect.Left, rect.Top, grosor, rect.Height), color);
        spriteBatch.Draw(debugTexture, new Rectangle(rect.Right - grosor, rect.Top, grosor, rect.Height), color);
    }
}