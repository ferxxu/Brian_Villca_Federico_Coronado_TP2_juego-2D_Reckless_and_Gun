using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary.Entities; 
using reckless_and_gun.Entities;

namespace reckless_and_gun.Managers
{
    public class ProjectileManager
    {
        private List<Projectile> _projectiles;
        private Texture2D _debugTexture;

        public ProjectileManager(Texture2D debugTexture)
        {
            _projectiles = new List<Projectile>();
            _debugTexture = debugTexture;
        }

        public void AddBullet(Projectile bullet)
        {
            bullet.DebugTexture = _debugTexture;
            _projectiles.Add(bullet);
        }

        // --- MÉTODO 1: UPDATE PARA LA ARAÑA (BEACH) ---
        public void Update(GameTime gameTime, List<Rectangle> mapCollisions, Spider boss)
        {
            for (int i = _projectiles.Count - 1; i >= 0; i--)
            {
                Projectile p = _projectiles[i];
                p.Update(gameTime, mapCollisions); // Mover y chocar paredes

                if (p.IsActive)
                {
                    // PROTECCIÓN CONTRA NULL (Por si acaso pasas null)
                    if (boss != null && p.IsFromPlayer && boss.IsActive)
                    {
                        if (p.Hitbox.Intersects(boss.Hitbox))
                        {
                            boss.TakeDamage(p.Damage);
                            p.OnHitTarget();
                        }
                    }
                }
                else
                {
                    _projectiles.RemoveAt(i);
                }
            }
        }

        // --- MÉTODO 2: UPDATE PARA EL GORILA (JUNGLE) ---
        // Copiamos y pegamos, pero cambiamos 'Spider' por 'Gorilla'
        public void Update(GameTime gameTime, List<Rectangle> mapCollisions, Gorilla boss)
        {
            for (int i = _projectiles.Count - 1; i >= 0; i--)
            {
                Projectile p = _projectiles[i];
                p.Update(gameTime, mapCollisions);

                if (p.IsActive)
                {
                    if (boss != null && p.IsFromPlayer && boss.IsActive)
                    {
                        if (p.Hitbox.Intersects(boss.Hitbox))
                        {
                            boss.TakeDamage(p.Damage);
                            p.OnHitTarget();
                            System.Diagnostics.Debug.WriteLine("¡IMPACTO EN GORILA!");
                        }
                    }
                }
                else
                {
                    _projectiles.RemoveAt(i);
                }
            }
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            foreach (var p in _projectiles)
            {
                p.Draw(spriteBatch);
            }
        }
    }
}