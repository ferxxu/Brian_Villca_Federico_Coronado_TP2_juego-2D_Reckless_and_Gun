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
                p.Update(gameTime, mapCollisions);

                if (p.IsActive)
                {
                    if (p.IsFromPlayer)
                    {
                        if (activeEnemy != null && activeEnemy.IsActive && p.Hitbox.Intersects(activeEnemy.Hitbox))
                        {
                            activeEnemy.TakeDamage(p.Damage);
                            p.OnHitTarget();
                        }
                    }
                    else
                    {
                    }
                }
                else
                {
                    _projectiles.RemoveAt(i);
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
                            System.Diagnostics.Debug.WriteLine("le pegaste al gorila :(");
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