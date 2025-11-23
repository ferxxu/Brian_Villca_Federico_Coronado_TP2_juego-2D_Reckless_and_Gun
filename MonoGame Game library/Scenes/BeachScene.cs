using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGameLibrary;
using MonoGameLibrary.Graphics;
using MonoGameLibrary.Scenes;
using MonoGameLibrary.Camera;
using TiledSharp;
using reckless_and_gun.Entities;
using reckless_and_gun.Managers;
using MonoGameLibrary.Entities;

namespace reckless_and_gun.Scenes;

public class BeachScene : Scene
{
    private David _david;
    private Spider _spiderBoss;
    private ProjectileManager _bulletManager;

    private List<Rectangle> _collisionRects;
    private TextureAtlas _projectilesAtlas;
    private Texture2D _background;

    private Rectangle _roomBounds;
    private Camera2D _camera;

    public override void Initialize()
    {
        _david = new David();
        _spiderBoss = new Spider(500, 500, new Vector2(2000, 290));

        _bulletManager = new ProjectileManager(null);

        base.Initialize();
        Core.ExitOnEscape = false;
        _camera = new Camera2D();
        _camera.Zoom = 1.0f;
    }

    public override void LoadContent()
    {
        try
        {
            _background = Content.Load<Texture2D>("beach_map");
            _projectilesAtlas = TextureAtlas.FromFile(Core.Content, "projectiles.xml");

            TextureAtlas atlasDavid = TextureAtlas.FromFile(Core.Content, "david1.xml");
            _david.LoadContent(atlasDavid, new Vector2(600, 10));

            TextureAtlas atlasSpider = TextureAtlas.FromFile(Core.Content, "spider.xml");
            _spiderBoss.LoadContent(atlasSpider, new Vector2(3.0f, 3.0f));

            _collisionRects = new List<Rectangle>();
            string mapFilePath = System.IO.Path.Combine(Content.RootDirectory, "beach_map.tmx");
            var map = new TmxMap(mapFilePath);
            _roomBounds = new Rectangle(0, 0, map.Width * map.TileWidth, map.Height * map.TileHeight);

            if (map.ObjectGroups.Contains("collisions"))
            {
                foreach (var obj in map.ObjectGroups["collisions"].Objects)
                {
                    _collisionRects.Add(new Rectangle((int)obj.X, (int)obj.Y, (int)obj.Width, (int)obj.Height));
                }
            }
        }
        catch (System.Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("ERROR: " + ex.Message);
        }
    }

    public override void Update(GameTime gameTime)
    {
        if (Core.Input.Keyboard.WasKeyJustPressed(Keys.Escape)) Core.ChangeScene(new TitleScene());
        
        _david.Update(gameTime, Core.Input.Keyboard, _collisionRects);
        _spiderBoss.Update(gameTime, _david.Position);


        Projectile newBullet = _david.TryShoot(_projectilesAtlas);
        if (newBullet != null) _bulletManager.AddBullet(newBullet);
        _bulletManager.Update(gameTime, _collisionRects, _spiderBoss);

        _camera.Follow(_david.Position, _roomBounds, Core.GraphicsDevice.Viewport);

        // 2. LÓGICA DE SALIDA POR COORDENADAS (INFALIBLE)
        // Si David cruza la línea de 2800 pixeles a la derecha...
        if (_david.Position.X > 2800)
        {
            System.Diagnostics.Debug.WriteLine(">>> LLEGÓ A LA SALIDA >>> CAMBIANDO ESCENA");
            Core.ChangeScene(new JungleScene());
        }

        base.Update(gameTime);
    }

    public override void Draw(GameTime gameTime)
    {
        Core.GraphicsDevice.Clear(Color.CornflowerBlue);
        Core.SpriteBatch.Begin(samplerState: SamplerState.PointClamp, transformMatrix: _camera.GetViewMatrix(Core.GraphicsDevice.Viewport));

        if (_background != null) Core.SpriteBatch.Draw(_background, Vector2.Zero, Color.White);

        if (_spiderBoss.IsActive) _spiderBoss.Draw(Core.SpriteBatch, _spiderBoss.Position);

        _bulletManager.Draw(Core.SpriteBatch);
        _david.Draw(Core.SpriteBatch);

        Core.SpriteBatch.End();
    }
}