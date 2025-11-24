using Microsoft.Xna.Framework;
using MonoGameLibrary.Graphics;
using MonoGameLibrary.Entities;

namespace reckless_and_gun.Entities;

public class BabySpider : Enemy
{
    public BabySpider(float health, float velocity, Vector2 startPosition) 
        : base(health, velocity, "spider") 
    {
        this.Position = startPosition;

        _meleeRange = 20f; 
        _rangedRange = 0f; 
        
        _animMelee = "spider_upwards"; 
    }

    public override void LoadContent(TextureAtlas atlas, Vector2 scale)
    {
        base.LoadContent(atlas, scale * 0.9f);
    }
}