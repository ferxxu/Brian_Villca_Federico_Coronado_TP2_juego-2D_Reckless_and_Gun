using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGameLibrary;
using MonoGameLibrary.Graphics;
using MonoGameLibrary.Scenes;
using MonoGameLibrary.Input;
using MonoGameLibrary.Camera;
using TiledSharp;

using reckless_and_gun.Entities;
using MonoGameLibrary.Entities;

namespace reckless_and_gun.Scenes;

public class GameScene : Scene
{
    private List<Projectile> _projectiles;
    private TextureAtlas _projectilesAtlas;
    private Camera2D _camera;
    private List<Rectangle> _collisionRects;
    private Texture2D _background;
    private Rectangle _roomBounds;
    private Texture2D _texturaDebug;
    private David _david;
    private Spider _spiderBoss;


    public override void Initialize()
    {
        _david = new David();
        _spiderBoss = new Spider(2000, 50, new Vector2(2000, 290));
        _projectiles = new List<Projectile>();
        _projectiles = new List<Projectile>();
        base.Initialize();

        Core.ExitOnEscape = false;
        _camera = new Camera2D();
        _camera.Zoom = 1.0f;
    }

    public override void LoadContent()
    {
        _background = Content.Load<Texture2D>("beach_map");
        _texturaDebug = new Texture2D(Core.GraphicsDevice, 1, 1);
        _texturaDebug.SetData(new[] { Color.White });
        _projectilesAtlas = TextureAtlas.FromFile(Core.Content, "projectiles.xml");
        TextureAtlas atlasDavid = TextureAtlas.FromFile(Core.Content, "david1.xml");
        _david.LoadContent(atlasDavid, new Vector2(600, 10));
        _david.DebugTexture = _texturaDebug;

        TextureAtlas atlasSpider = TextureAtlas.FromFile(Core.Content, "spider.xml");
        _spiderBoss.LoadContent(atlasSpider, new Vector2(3.0f, 3.0f));

        _collisionRects = new List<Rectangle>();
        string mapFilePath = Path.Combine(Content.RootDirectory, "beach_map.tmx");
        var map = new TmxMap(mapFilePath);
        _roomBounds = new Rectangle(0, 0, map.Width * map.TileWidth, map.Height * map.TileHeight);
        var collisionLayer = map.ObjectGroups["collisions"];

        foreach (var obj in collisionLayer.Objects)
        {
            _collisionRects.Add(new Rectangle((int)obj.X, (int)obj.Y, (int)obj.Width, (int)obj.Height));
        }

    }
    public override void Update(GameTime gameTime)
    {
        if (Core.Input.Keyboard.WasKeyJustPressed(Keys.Escape))
        {
            Core.ChangeScene(new TitleScene());
        }

        _david.Update(gameTime, Core.Input.Keyboard, _collisionRects);
        _spiderBoss.Update(gameTime, _david.Position);
        Projectile newBullet = _david.TryShoot(_projectilesAtlas);
        if (newBullet != null)
        {
            _projectiles.Add(newBullet);
        }

        for (int i = _projectiles.Count - 1; i >= 0; i--)
        {
            Projectile bullet = _projectiles[i];
            bullet.Update(gameTime, _collisionRects);

            if (bullet.IsActive)
            {
                if (bullet.IsFromPlayer && _spiderBoss.IsActive)
                {
                    if (bullet.Hitbox.Intersects(_spiderBoss.Hitbox))
                    {
                        _spiderBoss.TakeDamage(bullet.Damage);

                        bullet.OnHitTarget();
                    }
                }

            }

            if (!bullet.IsActive)
            {
                _projectiles.RemoveAt(i); 
            }
        }

        _camera.Follow(_david.Position, _roomBounds, Core.GraphicsDevice.Viewport);

        base.Update(gameTime);
    }


    public override void Draw(GameTime gameTime)
    {
        Core.GraphicsDevice.Clear(Color.Black);

        Core.SpriteBatch.Begin(
            samplerState: SamplerState.PointClamp,
            transformMatrix: _camera.GetViewMatrix(Core.GraphicsDevice.Viewport)
        );

        Core.SpriteBatch.Draw(_background, Vector2.Zero, Color.White);
        _spiderBoss.Draw(Core.SpriteBatch, _spiderBoss.Position);
        foreach (var bullet in _projectiles)
        {
            bullet.Draw(Core.SpriteBatch);
        }
        _spiderBoss.DrawDebug(Core.SpriteBatch, _texturaDebug);

        _david.Draw(Core.SpriteBatch);
        Core.SpriteBatch.End();
    }
}