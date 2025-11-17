using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary.Graphics;

namespace reckless_and_gun.Entities
{
    public abstract class Projectile
    {
        protected Sprite _sprite;
        public Vector2 Position { get; protected set; }
        public Vector2 Velocity { get; protected set; }
        public Rectangle Hitbox { get; protected set; }
        public bool IsActive { get; set; }
        
        public int Damage { get; protected set; }
        public bool IsFromPlayer { get; protected set; }

        public Projectile(Sprite sprite, Vector2 startPosition, Vector2 velocity, int damage, bool isFromPlayer)
        {
            _sprite = sprite;
            Position = startPosition;
            Velocity = velocity;
            Damage = damage;
            IsFromPlayer = isFromPlayer;
            IsActive = true;

            // Centrar la hitbox en el sprite
            UpdateHitbox();
        }

        protected void UpdateHitbox()
        {
            float w = _sprite.Region.Width * _sprite.Scale.X;
            float h = _sprite.Region.Height * _sprite.Scale.Y;
            float x = Position.X - (_sprite.Origin.X * _sprite.Scale.X);
            float y = Position.Y - (_sprite.Origin.Y * _sprite.Scale.Y);
            Hitbox = new Rectangle((int)x, (int)y, (int)w, (int)h);
        }

        public virtual void Update(GameTime gameTime, List<Rectangle> collisionRects)
        {
            if (!IsActive) return;

            // 1. Mover
            Position += Velocity * (float)gameTime.ElapsedGameTime.TotalSeconds;
            UpdateHitbox();

            // 2. Comprobar colisión con el escenario
            foreach (var rect in collisionRects)
            {
                if (Hitbox.Intersects(rect))
                {
                    OnHitWorld(); // Chocó con una pared
                    return;
                }
            }
        }
        
        // 3. Lógica de impacto (la definen las subclases)
        public abstract void OnHitWorld(); // Qué hacer al chocar con una pared
        public abstract void OnHitTarget(); // Qué hacer al chocar con un enemigo/jugador
        
        public virtual void Draw(SpriteBatch spriteBatch)
        {
            if (IsActive)
            {
                _sprite.Draw(spriteBatch, Position);
            }
        }
    }
}