using Microsoft.Xna.Framework;
using MonoGameLibrary.Entities;

namespace reckless_and_gun.Entities;

public class Gorilla : Enemy
{
    public Gorilla(float health, float velocity, Vector2 startPosition) 
        : base(health, velocity, "gorilla")
    {
        this.Position = startPosition;

        // --- GORILA ES UN TANQUE ---
        _meleeRange = 80f;
        _rangedRange = 400f;

        // Proyectil de Cohete
        _projectileTextureName = "Bazooka_Rocket";
        _projectileSpeed = 400f; // Rápido
        _projectileDamage = 25;  // Duele

        // AJUSTE DE ANIMACIONES:
        // Tu XML no tiene "gorilla_melee". Usaremos "gorilla_shooting" o "idle"
        // para que no crashee si se acerca mucho.
        _animMelee = "gorilla_idle"; 
    }
}