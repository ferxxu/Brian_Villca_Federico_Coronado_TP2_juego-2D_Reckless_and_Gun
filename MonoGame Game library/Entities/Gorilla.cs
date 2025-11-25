using Microsoft.Xna.Framework;
using MonoGameLibrary.Entities;

namespace reckless_and_gun.Entities;

public class Gorilla : Enemy
{
    public Gorilla(float health, float velocity, Vector2 startPosition) 
        : base(health, velocity, "gorilla")
    {
        this.Position = startPosition;

        _meleeRange = 80f;
        _rangedRange = 1000f;

        _projectileTextureName = "Bazooka_Rocket";
        _projectileSpeed = 400f; 
        _projectileDamage = 20;  

        _animMelee = "gorilla_idle"; 
    }
}