using Microsoft.Xna.Framework;
using MonoGameLibrary.Graphics;

namespace reckless_and_gun.Entities
{
    public class GorillaProjectile : Projectile
    {
        private const float SPEED = 400f;  
        private const int DAMAGE = 20;    

        public GorillaProjectile(Sprite sprite, Vector2 startPosition, Vector2 direction)
            : base(
                  sprite, 
                  startPosition, 
                  direction * SPEED, 
                  DAMAGE, 
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