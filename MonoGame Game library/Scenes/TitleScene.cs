using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGameLibrary;
using MonoGameLibrary.Scenes;


namespace reckless_and_gun.Scenes;

public class TitleScene : Scene
{
    private const string RECKLESS_TEXT = "Reckless";
    private const string AND_TEXT = "And";
    private const string GUN_TEXT = "Gun";
    private const string START_TEXT = "Start";
    private const string EXIT_TEXT = "Exit";
    private const string SETTINGS_TEXT = "Settings";

    private SpriteFont _font;
    private Vector2 _recklessTextPos;
    private Vector2 _recklessTextOrigin;
    private Vector2 _andTextPos;
    private Vector2 _andTextOrigin;
    private Vector2 _gunTextPos;
    private Vector2 _gunTextOrigin;
    private Vector2 _startPos;
    private Vector2 _startOrigin;
    private Vector2 _exitPos;
    private Vector2 _exitOrigin;
    private Vector2 _settingsPos;
    private Vector2 _settingsOrigin;
    private Texture2D _background;

    public override void Initialize()
    {
        base.Initialize();

        Core.ExitOnEscape = true;
        Vector2 size = _font.MeasureString(RECKLESS_TEXT);
        _recklessTextPos = new Vector2(640, 100);
        _recklessTextOrigin = size * 0.5f;

        size = _font.MeasureString(AND_TEXT);
        _andTextPos = new Vector2(640, 207);
        _andTextOrigin = size * 0.5f;

        size = _font.MeasureString(GUN_TEXT);
        _gunTextPos = new Vector2(640, 307);
        _gunTextOrigin = size * 0.5f;

        size = _font.MeasureString(START_TEXT);
        _startPos = new Vector2(640, 450);
        _startOrigin = size * 0.5f;

        size = _font.MeasureString(SETTINGS_TEXT);
        _settingsPos = new Vector2(640, 500);
        _settingsOrigin = size * 0.5f;

        size = _font.MeasureString(EXIT_TEXT);
        _exitPos = new Vector2(640, 550);
        _exitOrigin = size * 0.5f;
    }
    public override void LoadContent()
    {
        // Load the font for the title text.    
        _font = Core.Content.Load<SpriteFont>("font");
        _background = Core.Content.Load<Texture2D>("jungle");
    }
    public override void Update(GameTime gameTime)
    {
        // If the user presses enter, switch to the game scene.
        if (Core.Input.Keyboard.WasKeyJustPressed(Keys.Enter))
        {
            Core.ChangeScene(new GameScene());
        }
    }
    public override void Draw(GameTime gameTime)
    {
        Core.GraphicsDevice.Clear(Color.Black);

        // Begin the sprite batch to prepare for rendering.
        Core.SpriteBatch.Begin(samplerState: SamplerState.PointClamp);

        // The color to use for the drop shadow text.
        Color dropShadowColor = Color.Black * 0.5f;
        // Centering the background
        Rectangle destinationRectangle = new Rectangle(
        0,
        0,
        Core.GraphicsDevice.Viewport.Width,
        Core.GraphicsDevice.Viewport.Height
    );
        Core.SpriteBatch.Draw(_background,destinationRectangle, Color.White );
        Core.SpriteBatch.DrawString(_font, RECKLESS_TEXT, _recklessTextPos + new Vector2(10, 10), dropShadowColor, 0.0f, _recklessTextOrigin, 1.0f, SpriteEffects.None, 1.0f);
        Core.SpriteBatch.DrawString(_font, RECKLESS_TEXT, _recklessTextPos, Color.Orange, 0.0f, _recklessTextOrigin, 1.0f, SpriteEffects.None, 1.0f);

        Core.SpriteBatch.DrawString(_font, AND_TEXT, _andTextPos + new Vector2(10, 10), dropShadowColor, 0.0f, _andTextOrigin, 0.65f, SpriteEffects.None, 1.0f);
        Core.SpriteBatch.DrawString(_font, AND_TEXT, _andTextPos, Color.Orange, 0.0f, _andTextOrigin, 0.65f, SpriteEffects.None, 1.0f);

        Core.SpriteBatch.DrawString(_font, GUN_TEXT, _gunTextPos + new Vector2(10, 10), dropShadowColor, 0.0f, _gunTextOrigin, 1.0f, SpriteEffects.None, 1.0f);
        Core.SpriteBatch.DrawString(_font, GUN_TEXT, _gunTextPos, Color.Orange, 0.0f, _gunTextOrigin, 1.0f, SpriteEffects.None, 1.0f);

        Core.SpriteBatch.DrawString(_font, START_TEXT, _startPos, Color.Orange, 0.0f, _startOrigin, 0.45f, SpriteEffects.None, 0.0f);
        Core.SpriteBatch.DrawString(_font, SETTINGS_TEXT, _settingsPos, Color.Orange, 0.0f, _settingsOrigin, 0.45f, SpriteEffects.None, 0.0f);
        Core.SpriteBatch.DrawString(_font, EXIT_TEXT, _exitPos, Color.Orange, 0.0f, _exitOrigin, 0.45f, SpriteEffects.None, 0.0f);

        Core.SpriteBatch.End();
    }

}
