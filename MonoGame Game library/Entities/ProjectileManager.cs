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

        public void Update(GameTime gameTime, List<Rectangle> mapCollisions, Enemy activeEnemy, David player)
        {
            for (int i = _projectiles.Count - 1; i >= 0; i--)
            {
                Projectile p = _projectiles[i];

                // 1. Mover la bala y chequear paredes
                p.Update(gameTime, mapCollisions);

                if (p.IsActive)
                {
                    // Bala del jugador 
                    if (p.IsFromPlayer)
                    {
                        // Verificamos que el enemigo exista, esté vivo y haya colisión
                        if (activeEnemy != null && activeEnemy.IsActive && p.Hitbox.Intersects(activeEnemy.Hitbox))
                        {
                            activeEnemy.TakeDamage(p.Damage);
                            p.OnHitTarget(); // Destruir bala
                        }
                    }
                    // Bala del enemigo
                    else
                    {
                        if (player != null && !player.IsDead && p.Hitbox.Intersects(player.Hitbox))
                        {
                            player.TakeDamage(p.Damage);
                            p.OnHitTarget(); // Destruir bala
                        }
                    }
                }
                else
                {
                    _projectiles.RemoveAt(i); // Eliminar balas muertas o que chocaron pared
                }
            }
        }

        public void CheckEnemyListCollisions(List<BabySpider> enemies)
        {
            for (int i = _projectiles.Count - 1; i >= 0; i--)
            {
                Projectile p = _projectiles[i];

                if (p.IsActive && p.IsFromPlayer)
                {
                    foreach (var enemy in enemies)
                    {
                        if (enemy.IsActive && p.Hitbox.Intersects(enemy.Hitbox))
                        {
                            enemy.TakeDamage(p.Damage);
                            p.OnHitTarget();
                            break;
                        }
                    }
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