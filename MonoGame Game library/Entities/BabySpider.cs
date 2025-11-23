using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary.Graphics;
using MonoGameLibrary.Entities;

namespace reckless_and_gun.Entities;

public class BabySpider : AnimatedSprite
{
    // Estadísticas
    public float Health { get; private set; }
    public float MaxHealth { get; private set; }
    public bool IsActive { get; private set; } = true;

    // Movimiento
    private float _speed;
    private Vector2 _direction;
    private float _changeDirectionTimer; 
    private static Random _random = new Random();

    // Visuals
    private bool _isFlashingRed;
    private float _flashTimer;

    // Hitbox
    public Rectangle Hitbox
    {
        get
        {
            if (Region == null) return Rectangle.Empty;
            return new Rectangle(
                (int)Position.X - (int)(Region.Width * Scale.X) / 2,
                (int)Position.Y - (int)(Region.Height * Scale.Y), 
                (int)(Region.Width * Scale.X),
                (int)(Region.Height * Scale.Y)
            );
        }
    }

    public BabySpider(float health, float speed, Vector2 startPosition)
    {
        Health = health;
        MaxHealth = health;
        _speed = speed;
        Position = startPosition;

        // Iniciar con dirección aleatoria
        PickRandomDirection();
    }

    private void PickRandomDirection()
    {
        // Elige -1 (izquierda) o 1 (derecha)
        int dirX = _random.Next(0, 2) == 0 ? -1 : 1;
        _direction = new Vector2(dirX, 0);

        // Tiempo aleatorio entre 1 y 3 segundos antes de volver a cambiar
        _changeDirectionTimer = (float)_random.NextDouble() * 2.0f + 1.0f;
    }

    public void LoadContent(TextureAtlas atlas, Vector2 scale)
    {
        // Usamos la animación de la madre
        AnimatedSprite temp = atlas.CreateAnimatedSprite("spider_walking");
        if (temp.Animations != null)
            foreach (var anim in temp.Animations) this.AddAnimation(anim.Key, anim.Value);

        this.Scale = scale;

        // ORIGEN: Centro horizontal, Abajo vertical (Para que pisen bien el suelo)
        if (this.Region != null)
            this.Origin = new Vector2(this.Region.Width / 2f, this.Region.Height);

        Play("spider_walking");
    }

    public new void Update(GameTime gameTime)
    {
        if (!IsActive) return;

        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

        _changeDirectionTimer -= deltaTime;

        if (_changeDirectionTimer <= 0)
        {
            PickRandomDirection();
        }

        Position += _direction * _speed * deltaTime;

        if (_direction.X < 0) Effects = SpriteEffects.FlipHorizontally;
        else Effects = SpriteEffects.None;

        if (_isFlashingRed)
        {
            _flashTimer -= deltaTime;
            if (_flashTimer <= 0)
            {
                _isFlashingRed = false;
                this.Color = Color.White;
            }
        }

        base.Update(gameTime);
    }

    public void TakeDamage(int amount)
    {
        if (!IsActive) return;

        Health -= amount;

        _isFlashingRed = true;
        _flashTimer = 0.1f; 
        this.Color = Color.Red;

        if (Health <= 0)
        {
            IsActive = false;
        }
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        if (IsActive)
        {
            base.Draw(spriteBatch, Position);
        }
    }
}