using Microsoft.Xna.Framework;
using MonoGameLibrary.Graphics;

namespace reckless_and_gun.Entities
{
    public class EnemyProjectile : Projectile
    {
        public EnemyProjectile(Sprite sprite, Vector2 startPosition, Vector2 direction, float speed, int damage)
            : base(
                  sprite, 
                  startPosition, 
                  direction * speed,
                  damage, 
                  false 
              )
        {
        }

        public override void OnHitWorld() 
        { 
            IsActive = false; 
        }

        public override void OnHitTarget() 
        { 
            IsActive = false; 
        }
    }
}