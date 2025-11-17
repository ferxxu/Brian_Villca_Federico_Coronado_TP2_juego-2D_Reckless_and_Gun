using Microsoft.Xna.Framework;
using MonoGameLibrary.Graphics;

namespace reckless_and_gun.Entities
{
    public class BazookaRocket : Projectile
    {
        private const float SPEED = 300f; // Más lento que la bala
        private const int ROCKET_DAMAGE = 50; // Más daño

        public BazookaRocket(Sprite sprite, Vector2 startPosition, Vector2 direction)
            : base(
                  sprite, 
                  startPosition, 
                  direction * SPEED, 
                  ROCKET_DAMAGE, 
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