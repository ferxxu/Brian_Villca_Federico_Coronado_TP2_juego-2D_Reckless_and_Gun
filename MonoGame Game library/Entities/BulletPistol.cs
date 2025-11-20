using Microsoft.Xna.Framework;
using MonoGameLibrary.Graphics;

namespace reckless_and_gun.Entities
{
    public class PistolBullet : Projectile
    {
        private const float SPEED = 800f; // Velocidad de la bala
        private const int BULLET_DAMAGE = 10; // Daño

        public PistolBullet(Sprite sprite, Vector2 startPosition, Vector2 direction)
            : base(
                  sprite,
                  startPosition,
                  direction * SPEED,
                  BULLET_DAMAGE,
                  true
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