using System;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using MonoGameLibrary.Graphics;

namespace MonoGameLibrary.Entities;

public class BabySpider : AnimatedSprite
{
    public enum SpiderState
    {
        Patrolling,
        Attacking_Ranged,
        Attacking_Melee,
        Dying
    }

    private float _spiderHealth;
    private float _spiderVelocity;
    private SpiderState _currentState;

    public bool IsActive { get; private set; }
    public bool IsMarkedForRemoval { get; private set; }

    private Vector2 _direction;
    private TimeSpan _patrolTimer;
    private TimeSpan _timeToChangeDirection;
    private static Random _random = new Random();

    private TimeSpan _attackTimer;
    private TimeSpan _timeToNextAttack;
    private bool _isTakingDamage;
    private TimeSpan _damageFlashTimer;
    private static readonly TimeSpan _damageFlashDuration = TimeSpan.FromMilliseconds(100);
    private const float _meleeAttackRange = 15f; 
    private const float _rangedAttackRange = 120f; 
    // --------------------------------

    // --- Eventos ---
    public event Action<Rectangle> OnMeleeAttack;
    public event Action<Vector2> OnPoisonSpit;

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

    public BabySpider(float health, float velocity, Vector2 startPosition)
    {
        _spiderHealth = health;       
        _spiderVelocity = velocity;   
        this.Position = startPosition;

        IsActive = true;
        IsMarkedForRemoval = false;

        _isTakingDamage = false;
        _damageFlashTimer = TimeSpan.Zero;

        if (this.Color == default(Color))
        {
            this.Color = Color.White;
        }

        _currentState = SpiderState.Patrolling;

        SetNewPatrolTimer();
        SetNewAttackTimer();
    }

    private void SetNewPatrolTimer()
    {
        _direction = (_random.Next(0, 2) == 0) ? -Vector2.UnitX : Vector2.UnitX;
        _timeToChangeDirection = TimeSpan.FromSeconds(_random.Next(1, 4)); 
        _patrolTimer = TimeSpan.Zero;
    }

    private void SetNewAttackTimer()
    {
        _timeToNextAttack = TimeSpan.FromSeconds(_random.Next(2, 5));
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
        AnimatedSprite tempSprite = atlas.CreateAnimatedSprite("spider_walking"); 

        if (tempSprite.Animations != null)
        {
            foreach (var anim in tempSprite.Animations)
            {
                this.AddAnimation(anim.Key, anim.Value);
            }
        }
        
        this.Scale = scale; 

        _currentState = SpiderState.Patrolling;
        Play("spider_walking");

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
                case SpiderState.Patrolling:
                    HandlePatrolling(gameTime);
                    break;
                case SpiderState.Attacking_Ranged:
                    HandleRangedAttack(gameTime);
                    break;
                case SpiderState.Attacking_Melee:
                    HandleMeleeAttack(gameTime);
                    break;
            }
        }

        base.Update(gameTime);
    }

    private void UpdateAI(GameTime gameTime, Vector2 playerPosition)
    {
        if (_currentState == SpiderState.Attacking_Ranged ||
            _currentState == SpiderState.Attacking_Melee ||
            _currentState == SpiderState.Dying)
        {
            return;
        }

        float distanceToPlayer = Vector2.Distance(this.Position, playerPosition);

        if (distanceToPlayer <= _rangedAttackRange)
        {
            if (playerPosition.X < this.Position.X)
            {
                this.Effects = SpriteEffects.FlipHorizontally; 
            }
            else
            {
                this.Effects = SpriteEffects.None; 
            }
        }

        if (distanceToPlayer <= _meleeAttackRange)
        {
            _currentState = SpiderState.Attacking_Melee;
            Play("spider_upwards"); 
        }
        else if (distanceToPlayer <= _rangedAttackRange)
        {
            if (_attackTimer >= _timeToNextAttack)
            {
                _currentState = SpiderState.Attacking_Ranged;
                Play("spider_vomit");
            }
            else
            {
                _currentState = SpiderState.Patrolling;
                Play("spider_walking");
            }
        }
        else
        {
            _currentState = SpiderState.Patrolling;
            Play("spider_walking");
        }
    }

    private void HandleMeleeAttack(GameTime gameTime)
    {
        if (this.IsAnimationFinished)
        {
            Rectangle meleeHitbox = new Rectangle(
                (int)this.Position.X,
                (int)this.Position.Y - 10, 
                (int)(this.Region.Width * this.Scale.X), 
                (int)(this.Region.Height * this.Scale.Y) + 10
            );

            OnMeleeAttack?.Invoke(meleeHitbox);

            _currentState = SpiderState.Patrolling;
            Play("spider_walking"); 
        }
    }

    private void HandlePatrolling(GameTime gameTime)
    {
        if (_currentState == SpiderState.Patrolling)
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
        this.Position += _direction * _spiderVelocity * deltaTime;
    }

    private void HandleRangedAttack(GameTime gameTime)
    {
        if (this.IsAnimationFinished)
        {
            OnPoisonSpit?.Invoke(this.Position);

            _currentState = SpiderState.Patrolling;
            Play("spider_walking"); 
            SetNewAttackTimer(); 
        }
    }

    public void TakeDamage(int amount)
    {
        if (!_isTakingDamage && IsActive)
        {
            _spiderHealth -= amount;

            if (_spiderHealth <= 0)
            {
                IsActive = false;
                _spiderHealth = 0;
                _currentState = SpiderState.Dying;
                this.Color = Color.White;
                Play("die"); 
            }
            else
            {
                _isTakingDamage = true;
                _damageFlashTimer = TimeSpan.Zero;
                this.Color = Color.Red;
            }
        }
    }

    public void DrawDebug(SpriteBatch spriteBatch, Texture2D debugTexture)
    {
        DibujarBordeRectangulo(spriteBatch, debugTexture, this.Hitbox, Color.Yellow, 1);

        // Visualizar rango Melee reducido
        Rectangle meleeRangeRect = new Rectangle(
            (int)(this.Position.X - _meleeAttackRange),
            (int)(this.Position.Y - _meleeAttackRange),
            (int)(_meleeAttackRange * 2),
            (int)(_meleeAttackRange * 2)
        );
        DibujarBordeRectangulo(spriteBatch, debugTexture, meleeRangeRect, Color.Red, 1);

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