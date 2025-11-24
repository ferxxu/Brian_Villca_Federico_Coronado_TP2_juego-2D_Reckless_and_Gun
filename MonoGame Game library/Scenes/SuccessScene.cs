using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGameLibrary;
using MonoGameLibrary.Scenes;

namespace reckless_and_gun.Scenes;

public class SuccessScene : Scene
{
    private SpriteFont _font;
    private Texture2D _pixelTexture;
    private Rectangle _buttonRect;

    private string _successText = "YOU WON!";
    private string _buttonText = "Back to Main Menu";

    private bool _isHovering = false;

    private float _textScale = 0.3f; 

    public override void Initialize()
    {
        base.Initialize();
        Core.ExitOnEscape = false;

        _pixelTexture = new Texture2D(Core.GraphicsDevice, 1, 1);
        _pixelTexture.SetData(new[] { Color.White });
    }

    public override void LoadContent()
    {
        _font = Content.Load<SpriteFont>("font");
    }

    public override void Update(GameTime gameTime)
    {
        if (Core.Input.Keyboard.WasKeyJustPressed(Keys.Enter))
        {
            Core.ChangeScene(new TitleScene());
        }

        MouseState mouseState = Mouse.GetState();
        Point mousePos = mouseState.Position;

        SetupButtonPosition();

        if (_buttonRect.Contains(mousePos))
        {
            _isHovering = true;
            if (mouseState.LeftButton == ButtonState.Pressed)
            {
                Core.ChangeScene(new TitleScene());
            }
        }
        else
        {
            _isHovering = false;
        }

        base.Update(gameTime);
    }

    private void SetupButtonPosition()
    {
        int screenW = Core.GraphicsDevice.Viewport.Width;
        int screenH = Core.GraphicsDevice.Viewport.Height;

        int btnW = 220;
        int btnH = 40;

        int btnX = (screenW - btnW) / 2;
        int btnY = (screenH / 2) + 60;

        _buttonRect = new Rectangle(btnX, btnY, btnW, btnH);
    }

    public override void Draw(GameTime gameTime)
    {
        Core.GraphicsDevice.Clear(Color.Black);

        Core.SpriteBatch.Begin();

        if (_font != null && _pixelTexture != null)
        {
            int screenW = Core.GraphicsDevice.Viewport.Width;
            int screenH = Core.GraphicsDevice.Viewport.Height;

            // --- TEXTO DE VICTORIA ---
            Vector2 sizeSuccess = _font.MeasureString(_successText);
            Vector2 posSuccess = new Vector2(
                (screenW - sizeSuccess.X) / 2,
                (screenH / 2) - 80
            );

            Core.SpriteBatch.DrawString(_font, _successText, posSuccess, Color.LimeGreen);

            Color btnColor = _isHovering ? Color.LimeGreen : Color.ForestGreen;

            Core.SpriteBatch.Draw(_pixelTexture, _buttonRect, btnColor);

            Vector2 sizeBtnText = _font.MeasureString(_buttonText) * _textScale;
            Vector2 posBtnText = new Vector2(
                _buttonRect.X + (_buttonRect.Width - sizeBtnText.X) / 2,
                _buttonRect.Y + (_buttonRect.Height - sizeBtnText.Y) / 2
            );

            Core.SpriteBatch.DrawString(
                _font,
                _buttonText,
                posBtnText,
                Color.White,
                0f,
                Vector2.Zero,
                _textScale,
                SpriteEffects.None,
                0f
            );
        }

        Core.SpriteBatch.End();
    }
}