using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary.Graphics;
using System.Collections.Generic;

namespace reckless_and_gun.Entities
{
    public class Projectile
    {
        protected Sprite _sprite;
        public Vector2 Position { get; set; }
        public Vector2 Velocity { get; protected set; }

        public int Width { get; set; } = 20;
        public int Height { get; set; } = 20;

        public Texture2D DebugTexture { get; set; }

        public Rectangle Hitbox
        {
            get
            {
                // --- CAMBIO AQUÍ: Reducción de hitbox ---
                // 0.5f significa el 50% del tamaño original.
                // Súbelo a 0.7f si quieres que sea más grande, o bájalo a 0.3f para que sea más pequeño.
                float hitboxScale = 0.5f;

                int reducedWidth = (int)(Width * hitboxScale);
                int reducedHeight = (int)(Height * hitboxScale);

                // Calculamos la posición centrada usando el nuevo tamaño reducido
                return new Rectangle(
                    (int)Position.X - reducedWidth / 2,
                    (int)Position.Y - reducedHeight / 2,
                    reducedWidth,
                    reducedHeight
                );
            }
        }

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

            if (_sprite != null && _sprite.Region.Width > 0)
            {
                float sX = _sprite.Scale.X == 0 ? 1f : _sprite.Scale.X;
                float sY = _sprite.Scale.Y == 0 ? 1f : _sprite.Scale.Y;
                Width = (int)(_sprite.Region.Width * sX);
                Height = (int)(_sprite.Region.Height * sY);
            }
        }

        public virtual void Update(GameTime gameTime, List<Rectangle> collisionRects)
        {
            if (!IsActive) return;
            Position += Velocity * (float)gameTime.ElapsedGameTime.TotalSeconds;

            foreach (var rect in collisionRects)
            {
                if (Hitbox.Intersects(rect))
                {
                    OnHitWorld();
                    return;
                }
            }
        }

        public virtual void OnHitWorld() { IsActive = false; }
        public virtual void OnHitTarget() { IsActive = false; }

        protected void DibujarBordeRectangulo(SpriteBatch spriteBatch, Texture2D tex, Rectangle rect, Color color, int grosor)
        {
            spriteBatch.Draw(tex, new Rectangle(rect.Left, rect.Top, rect.Width, grosor), color);
            spriteBatch.Draw(tex, new Rectangle(rect.Left, rect.Bottom - grosor, rect.Width, grosor), color);
            spriteBatch.Draw(tex, new Rectangle(rect.Left, rect.Top, grosor, rect.Height), color);
            spriteBatch.Draw(tex, new Rectangle(rect.Right - grosor, rect.Top, grosor, rect.Height), color);
        }
        public virtual void Draw(SpriteBatch spriteBatch)
        {
            if (IsActive)
            {
                if (_sprite != null)
                {
                    _sprite.Draw(spriteBatch, Position);

                    if (DebugTexture != null)
                        DibujarBordeRectangulo(spriteBatch, DebugTexture, Hitbox, Color.Cyan, 2);
                }
                else if (DebugTexture != null)
                {
                    Color colorBala = IsFromPlayer ? Color.Yellow : Color.LimeGreen;

                    spriteBatch.Draw(DebugTexture, Hitbox, colorBala);
                }
            }
        }
    }
}