using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media; // <--- NECESARIO PARA SONG
using MonoGameLibrary;
using MonoGameLibrary.Graphics;
using MonoGameLibrary.Scenes;
using MonoGameLibrary.Camera;
using TiledSharp;
using reckless_and_gun.Entities;
using reckless_and_gun.Managers;
using Microsoft.Xna.Framework.Audio;

namespace reckless_and_gun.Scenes;

public class JungleScene : Scene
{
    private David _david;
    private Gorilla _gorillaBoss;
    private ProjectileManager _bulletManager;

    private List<Rectangle> _collisionRects;
    private TextureAtlas _projectilesAtlas;
    private Texture2D _background;
    private Texture2D _texturaDebug;
    private Rectangle _roomBounds;
    private Camera2D _camera;
    private SpriteFont _uiFont;

    // --- VARIABLE DE AUDIO ---
    private Song _jungleMusic;

    public override void Initialize()
    {
        _david = new David();
        _gorillaBoss = new Gorilla(3000, 120, new Vector2(1100, 250));

        _texturaDebug = new Texture2D(Core.GraphicsDevice, 1, 1);
        _texturaDebug.SetData(new[] { Color.White });
        _david.DebugTexture = _texturaDebug;

        _bulletManager = new ProjectileManager(_texturaDebug);

        base.Initialize();
        Core.ExitOnEscape = false;
        _camera = new Camera2D();
        _camera.Zoom = 1.0f;
    }

    public override void LoadContent()
    {
        // --- CARGAR MÚSICA JUNGLA ---
        // Asegúrate de importar el mp3 "jungle_theme" en el MGCB y ponerlo como "Song"
        _jungleMusic = Content.Load<Song>("Audio/intento 100"); 

        if (Core.Audio != null && _jungleMusic != null)
        {
            Core.Audio.SongVolume = 0.5f; 
            Core.Audio.PlaySong(_jungleMusic, true);
        }
        // ----------------------------
        SoundEffect sfxShoot = Content.Load<SoundEffect>("Audio/Laser_shoot 5");
    _david.SetSoundEffects( sfxShoot);
        _uiFont = Content.Load<SpriteFont>("font");
        _background = Content.Load<Texture2D>("jungle_map");
        _projectilesAtlas = TextureAtlas.FromFile(Core.Content, "projectiles.xml");

        TextureAtlas atlasDavid = TextureAtlas.FromFile(Core.Content, "david1.xml");
        _david.LoadContent(atlasDavid, new Vector2(600, 10));

        TextureAtlas atlasGorilla = TextureAtlas.FromFile(Core.Content, "gorilla.xml");
        _gorillaBoss.LoadContent(atlasGorilla, new Vector2(2.0f, 2.0f));

        _collisionRects = new List<Rectangle>();
        string mapFilePath = System.IO.Path.Combine(Content.RootDirectory, "jungle_map.tmx");
        var map = new TmxMap(mapFilePath);
        _roomBounds = new Rectangle(0, 0, map.Width * map.TileWidth, map.Height * map.TileHeight);

        // Nota: Mantuve "colissions" tal cual estaba en tu código original
        // Si en Tiled se llama "collisions" (con dos s), corrige esta línea.
        if (map.ObjectGroups.Contains("colissions"))
        {
             var collisionLayer = map.ObjectGroups["colissions"];
             foreach (var obj in collisionLayer.Objects)
             {
                 _collisionRects.Add(new Rectangle((int)obj.X, (int)obj.Y, (int)obj.Width, (int)obj.Height));
             }
        }
    }

    public override void Update(GameTime gameTime)
    {
        if (Core.Input.Keyboard.WasKeyJustPressed(Keys.Escape)) Core.ChangeScene(new TitleScene());

        _david.Update(gameTime, Core.Input.Keyboard, _collisionRects);

        if (_david.IsDead)
        {
            Core.ChangeScene(new GameOverScene());
            return;
        }

        _gorillaBoss.Update(gameTime, _david.Position, _roomBounds.Width, _collisionRects);

        Projectile newBullet = _david.TryShoot(_projectilesAtlas);
        if (newBullet != null) _bulletManager.AddBullet(newBullet);

        Projectile gorillaRocket = _gorillaBoss.TryShoot(_projectilesAtlas, _david.Position);
        if (gorillaRocket != null) _bulletManager.AddBullet(gorillaRocket);

        _bulletManager.Update(gameTime, _collisionRects, _gorillaBoss, _david);

        _camera.Follow(_david.Position, _roomBounds, Core.GraphicsDevice.Viewport);

        if (!_gorillaBoss.IsActive)
        {
            Core.ChangeScene(new SuccessScene());
        }

        base.Update(gameTime);
    }

    public override void Draw(GameTime gameTime)
    {
        Core.GraphicsDevice.Clear(Color.Black);

        float offsetY = 0;
        int screenHeight = Core.GraphicsDevice.Viewport.Height;
        int screenWidth = Core.GraphicsDevice.Viewport.Width;

        if (_roomBounds.Height < screenHeight)
        {
            offsetY = (screenHeight - _roomBounds.Height) / 2f;
        }

        Matrix finalTransform = _camera.GetViewMatrix(Core.GraphicsDevice.Viewport) * Matrix.CreateTranslation(0, offsetY, 0);

        Core.SpriteBatch.Begin(samplerState: SamplerState.PointClamp, transformMatrix: finalTransform);

        if (_background != null) Core.SpriteBatch.Draw(_background, Vector2.Zero, Color.White);

        if (_gorillaBoss.IsActive) _gorillaBoss.Draw(Core.SpriteBatch, _gorillaBoss.Position);

        _bulletManager.Draw(Core.SpriteBatch);
        _david.Draw(Core.SpriteBatch);

        Core.SpriteBatch.End();

        Core.SpriteBatch.Begin(samplerState: SamplerState.PointClamp);

        if (_texturaDebug != null && _uiFont != null)
        {
            int paddingBottom = 50;
            int yPosition = screenHeight - paddingBottom;

            string davidName = "DAVID";
            float scale = 0.3f;
            int barW = 200;
            int barH = 15;
            int maxHealth = 20;

            int currentLives = _david.Lives;

            Vector2 nameSize = _uiFont.MeasureString(davidName);
            float realNameWidth = nameSize.X * scale;

            Core.SpriteBatch.DrawString(_uiFont, davidName, new Vector2(20, yPosition), Color.Orange, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);

            int barX = 20 + (int)realNameWidth + 10;
            int barY = yPosition;

            Core.SpriteBatch.Draw(_texturaDebug, new Rectangle(barX - 2, barY - 2, barW + 4, barH + 4), Color.Black);
            Core.SpriteBatch.Draw(_texturaDebug, new Rectangle(barX, barY, barW, barH), Color.Gray);

            float davidPct = (float)_david.Health / maxHealth;
            if (davidPct < 0) davidPct = 0;
            Core.SpriteBatch.Draw(_texturaDebug, new Rectangle(barX, barY, (int)(barW * davidPct), barH), Color.LimeGreen);

            int heartsY = barY + barH + 5;
            int heartSize = 10;

            for (int i = 0; i < currentLives; i++)
            {
                Rectangle lifeRect = new Rectangle(barX + (i * (heartSize + 5)), heartsY, heartSize, heartSize);
                Core.SpriteBatch.Draw(_texturaDebug, lifeRect, Color.Red);
            }

            if (_gorillaBoss.IsActive)
            {
                barW = 300;
                barH = 20;

                int rightSideX = screenWidth - 20;

                barX = rightSideX - barW;
                barY = yPosition + 5;

                string bossName = "GORILLA";
                scale = 0.3f;

                Vector2 bossNameSize = _uiFont.MeasureString(bossName);
                float realTextWidth = bossNameSize.X * scale;
                float realTextHeight = bossNameSize.Y * scale;

                Vector2 bossNamePos = new Vector2(rightSideX - realTextWidth, barY - realTextHeight - 5);

                Core.SpriteBatch.DrawString(_uiFont, bossName, bossNamePos, Color.Red, 0, Vector2.Zero, scale, SpriteEffects.None, 0f);

                Core.SpriteBatch.Draw(_texturaDebug, new Rectangle(barX - 2, barY - 2, barW + 4, barH + 4), Color.Black);
                Core.SpriteBatch.Draw(_texturaDebug, new Rectangle(barX, barY, barW, barH), Color.Gray);

                float pct = _gorillaBoss.Health / _gorillaBoss.MaxHealth;
                if (pct < 0) pct = 0;
                Core.SpriteBatch.Draw(_texturaDebug, new Rectangle(barX, barY, (int)(barW * pct), barH), Color.Red);
            }
        }

        Core.SpriteBatch.End();
    }
}