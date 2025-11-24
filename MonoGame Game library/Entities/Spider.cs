using Microsoft.Xna.Framework;
using MonoGameLibrary.Entities;

namespace reckless_and_gun.Entities;

public class Spider : Enemy
{
    public Spider(float health, float velocity, Vector2 startPosition) 
        : base(health, velocity, "spider")
    {
        this.Position = startPosition;

        _meleeRange = 40f;
        _rangedRange = 700f;
        
        _projectileTextureName = "Vomit_Ball"; 
        _projectileSpeed = 250f;
        _projectileDamage = 10;

        _animMelee = "spider_upwards"; 
    }

    
}