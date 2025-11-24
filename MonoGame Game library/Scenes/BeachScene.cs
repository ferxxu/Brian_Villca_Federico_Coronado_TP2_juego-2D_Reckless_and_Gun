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

namespace reckless_and_gun.Scenes;

public class BeachScene : Scene
{
    private David _david;
    private Spider _spiderBoss;
    private List<BabySpider> _babySpiders;
    private ProjectileManager _bulletManager;

    private List<Rectangle> _collisionRects;
    private TextureAtlas _projectilesAtlas;
    private Texture2D _background;
    private Texture2D _texturaDebug;
    private Rectangle _roomBounds;
    private Camera2D _camera;
    private Rectangle _exitZone;
    private SpriteFont _uiFont;

    public override void Initialize()
    {
        _david = new David();
        _spiderBoss = new Spider(1500, 500, new Vector2(2000, 290));

        _babySpiders = new List<BabySpider>();
        for (int i = 0; i < 5; i++)
        {
            Vector2 pos = new Vector2(1000 + (i * 200), 350);
            _babySpiders.Add(new BabySpider(20, 150, pos));
        }

        _texturaDebug = new Texture2D(Core.GraphicsDevice, 1, 1);
        _texturaDebug.SetData(new[] { Color.White });
        _david.DebugTexture = _texturaDebug;

        _bulletManager = new ProjectileManager(_texturaDebug);

        base.Initialize();
        Core.ExitOnEscape = false;
        _camera = new Camera2D();
        _camera.Zoom = 1.0f;
        _exitZone = new Rectangle(2990, 0, 100, 400);
    }

    public override void LoadContent()
    {
        _uiFont = Content.Load<SpriteFont>("font");

        _background = Content.Load<Texture2D>("beach_map");
        _projectilesAtlas = TextureAtlas.FromFile(Core.Content, "projectiles.xml");

        TextureAtlas atlasDavid = TextureAtlas.FromFile(Core.Content, "david1.xml");
        _david.LoadContent(atlasDavid, new Vector2(600, 10));

        TextureAtlas atlasSpider = TextureAtlas.FromFile(Core.Content, "spider.xml");
        _spiderBoss.LoadContent(atlasSpider, new Vector2(3.0f, 3.0f));

        foreach (var baby in _babySpiders)
        {
            baby.LoadContent(atlasSpider, new Vector2(1f, 1f));
        }

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

    public override void Update(GameTime gameTime)
    {
        if (Core.Input.Keyboard.WasKeyJustPressed(Keys.Escape)) Core.ChangeScene(new TitleScene());

        _david.Update(gameTime, Core.Input.Keyboard, _collisionRects);

        if (_david.IsDead)
        {
            Core.ChangeScene(new GameOverScene());
            return;
        }

        _spiderBoss.Update(gameTime, _david.Position, _roomBounds.Width, _collisionRects);

        foreach (var baby in _babySpiders)
        {
            baby.Update(gameTime, _david.Position, _roomBounds.Width, _collisionRects);
        }

        Projectile pDavid = _david.TryShoot(_projectilesAtlas);
        if (pDavid != null) _bulletManager.AddBullet(pDavid);

        Projectile pBoss = _spiderBoss.TryShoot(_projectilesAtlas, _david.Position);
        if (pBoss != null) _bulletManager.AddBullet(pBoss);

        _bulletManager.Update(gameTime, _collisionRects, _spiderBoss, _david);
        _bulletManager.CheckEnemyListCollisions(_babySpiders);

        _camera.Follow(_david.Position, _roomBounds, Core.GraphicsDevice.Viewport);

        if (_david.Hitbox.Intersects(_exitZone) && !_spiderBoss.IsActive)
        {
            Core.ChangeScene(new JungleScene());
        }

        if (!_spiderBoss.IsActive)
        {
            Core.ChangeScene(new JungleScene());
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

        Matrix centerMatrix = Matrix.CreateTranslation(0, offsetY, 0);
        Matrix finalTransform = _camera.GetViewMatrix(Core.GraphicsDevice.Viewport) * centerMatrix;

        // --- DIBUJAR EL MUNDO DEL JUEGO ---
        Core.SpriteBatch.Begin(samplerState: SamplerState.PointClamp, transformMatrix: finalTransform);

        if (_background != null) Core.SpriteBatch.Draw(_background, Vector2.Zero, Color.White);

        if (_texturaDebug != null)
        {
            Color exitColor = !_spiderBoss.IsActive ? Color.LimeGreen : Color.Red;
            Core.SpriteBatch.Draw(_texturaDebug, _exitZone, exitColor * 0.5f);
        }

        if (_spiderBoss.IsActive) _spiderBoss.Draw(Core.SpriteBatch, _spiderBoss.Position);

        foreach (var baby in _babySpiders) if (baby.IsActive) baby.Draw(Core.SpriteBatch, baby.Position);

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

            Core.SpriteBatch.DrawString(_uiFont, davidName, new Vector2(130, yPosition - 50), Color.Orange, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);

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

            if (_spiderBoss.IsActive)
            {
                barW = 300;
                barH = 20;

                int rightSideX = screenWidth - 20;

                barX = rightSideX - barW;
                barY = yPosition + 5;

                string bossName = "SPIDER";
                scale = 0.3f;

                Vector2 bossNameSize = _uiFont.MeasureString(bossName);

                float realTextWidth = bossNameSize.X * scale;
                float realTextHeight = bossNameSize.Y * scale;

                Vector2 bossNamePos = new Vector2(rightSideX - realTextWidth, barY - realTextHeight - 5);

                Core.SpriteBatch.DrawString(_uiFont, bossName, bossNamePos, Color.Red, 0, Vector2.Zero, scale, SpriteEffects.None, 0f);

                Core.SpriteBatch.Draw(_texturaDebug, new Rectangle(barX - 2, barY - 2, barW + 4, barH + 4), Color.Black);
                Core.SpriteBatch.Draw(_texturaDebug, new Rectangle(barX, barY, barW, barH), Color.Gray);

                float pct = _spiderBoss.Health / _spiderBoss.MaxHealth;
                if (pct < 0) pct = 0;
                Core.SpriteBatch.Draw(_texturaDebug, new Rectangle(barX, barY, (int)(barW * pct), barH), Color.Red);
            }
        }

        Core.SpriteBatch.End();
    }
}