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

public class JungleScene : Scene
{
    private David _david;
    private Gorilla _gorillaBoss;
    private ProjectileManager _bulletManager;

    private List<Rectangle> _collisionRects;
    private TextureAtlas _projectilesAtlas;
    private Texture2D _background;
    private Texture2D _texturaDebug; // Para ver las colisiones del piso
    private Rectangle _roomBounds;
    private Camera2D _camera;

    public override void Initialize()
    {
        _david = new David();
        // Gorilla: Vida 3000, Velocidad 120
        _gorillaBoss = new Gorilla(3000, 120, new Vector2(1500, 300));

        // 1. Crear Textura Debug (Blanca)
        _texturaDebug = new Texture2D(Core.GraphicsDevice, 1, 1);
        _texturaDebug.SetData(new[] { Color.White });
        _david.DebugTexture = _texturaDebug;

        // 2. Inicializar Manager
        _bulletManager = new ProjectileManager(_texturaDebug);

        base.Initialize();
        Core.ExitOnEscape = false;
        _camera = new Camera2D();
        _camera.Zoom = 1.0f;
    }

    public override void LoadContent()
    {
        try
        {
            _background = Content.Load<Texture2D>("jungle_map"); // Asegúrate de que la imagen se llame así
            _projectilesAtlas = TextureAtlas.FromFile(Core.Content, "projectiles.xml");

            TextureAtlas atlasDavid = TextureAtlas.FromFile(Core.Content, "david1.xml");
            _david.LoadContent(atlasDavid, new Vector2(600, 10));

            TextureAtlas atlasGorilla = TextureAtlas.FromFile(Core.Content, "gorilla.xml");
            _gorillaBoss.LoadContent(atlasGorilla, new Vector2(2.0f, 2.0f));

            // --- CARGA DE COLISIONES DEL MAPA (ADAPTADO) ---
            _collisionRects = new List<Rectangle>();
            string mapFilePath = System.IO.Path.Combine(Content.RootDirectory, "jungle_map.tmx");
            var map = new TmxMap(mapFilePath);
            _roomBounds = new Rectangle(0, 0, map.Width * map.TileWidth, map.Height * map.TileHeight);

            if (map.ObjectGroups.Contains("colissions"))
            {
                foreach (var obj in map.ObjectGroups["colissions"].Objects)
                {
                    // Agregamos los rectángulos del piso a la lista
                    _collisionRects.Add(new Rectangle((int)obj.X, (int)obj.Y, (int)obj.Width, (int)obj.Height));
                }
            }
            // ------------------------------------------------
        }
        catch (System.Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("ERROR JUNGLE LOAD: " + ex.Message);
        }
    }

    public override void Update(GameTime gameTime)
    {
        if (Core.Input.Keyboard.WasKeyJustPressed(Keys.Escape)) Core.ChangeScene(new TitleScene());

        // 1. Actualizar David (Pasándole las colisiones para que pise el suelo)
        _david.Update(gameTime, Core.Input.Keyboard, _collisionRects);

        _gorillaBoss.Update(gameTime, _david.Position);

        // 2. Crear y gestionar balas
        Projectile newBullet = _david.TryShoot(_projectilesAtlas);
        if (newBullet != null)
        {
            _bulletManager.AddBullet(newBullet);
        }

        // Actualizamos el manager para mover las balas y que choquen con PAREDES
        // (Pasamos 'null' como boss temporalmente porque el Manager espera una Spider, 
        //  luego haremos una sobrecarga para Gorilla).
        _bulletManager.Update(gameTime, _collisionRects, _gorillaBoss);

        _camera.Follow(_david.Position, _roomBounds, Core.GraphicsDevice.Viewport);

        // --- LÓGICA DE MUERTE POR CAÍDA ELIMINADA ---
        // Ya no hay chequeo de posición Y aquí.

        base.Update(gameTime);
    }

    public override void Draw(GameTime gameTime)
    {
        Core.GraphicsDevice.Clear(Color.Black); 

        Core.SpriteBatch.Begin(samplerState: SamplerState.PointClamp, transformMatrix: _camera.GetViewMatrix(Core.GraphicsDevice.Viewport));

        if (_background != null) Core.SpriteBatch.Draw(_background, Vector2.Zero, Color.White);

        // Dibujamos los pisos para verificar (Debug Visual)
        if (_texturaDebug != null)
        {
            foreach (var rect in _collisionRects)
            {
                // Dibujamos el suelo en verde semitransparente
                Core.SpriteBatch.Draw(_texturaDebug, rect, Color.LimeGreen * 0.5f);
            }
        }

        _gorillaBoss.Draw(Core.SpriteBatch, _gorillaBoss.Position);

        _bulletManager.Draw(Core.SpriteBatch);

        _david.Draw(Core.SpriteBatch);

        Core.SpriteBatch.End();
    }
}