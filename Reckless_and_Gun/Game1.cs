using Reckless_and_Gun.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGameLibrary.Graphics;

namespace Reckless_and_Gun;

public class Game1 : Core
{
    // texture region that defines the slime sprite in the atlas.

    private AnimatedSprite _pj;
    private TextureRegion _bg_beach;
    private Texture2D _beach_texture;
    private Vector2 _velocity_david;
    private Vector2 _position_pj = new Vector2(500, 390);
    private float _jumpSpeed = -500f;
    private float _gravity = 1500f;
    private bool _isJumping = false;
    private float _floor = 390f;

    public Game1() : base("Reckless and Gun", 1280, 590, false)
    {

    }

    protected override void Initialize()
    {
        // TODO: Add your initialization logic here

        base.Initialize();
    }

    protected override void LoadContent()
    {
        TextureAtlas atlas = TextureAtlas.FromFile(Content, "Sprites_pj/Pj.xml");
        _beach_texture = Content.Load<Texture2D>("Spritesheet_map/bg_beach");
        _bg_beach = new TextureRegion(_beach_texture, 0, 0, 3000, 400);

        _pj = atlas.CreateAnimatedSprite("David-walk");
        _pj.Scale = new Vector2(2.0f, 2.0f);
        _pj.Origin = Vector2.Zero;

        base.LoadContent();
    }

    protected override void Update(GameTime gameTime)
    {
        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();

        // TODO: Add your update logic here
        _pj.Update(gameTime);
        base.Update(gameTime);
        CheckKeyboardInput(deltaTime);
    }

    private void CheckKeyboardInput(float _deltaTime)
    {
        if (Input.Keyboard.IsKeyDown(Keys.A)) _position_pj.X -= 1.5f;
        if (Input.Keyboard.IsKeyDown(Keys.D)) _position_pj.X += 1.5f;

        if (Input.Keyboard.WasKeyJustPressed(Keys.J) && !_isJumping)
        {
            _isJumping = true;
            _velocity_david = new Vector2(_velocity_david.X, _jumpSpeed);
        }
        if (_isJumping)
        {
            _velocity_david = new Vector2(_velocity_david.X, _velocity_david.Y + _gravity * _deltaTime);

            _position_pj = _position_pj + (_velocity_david * _deltaTime);

            if (_position_pj.Y >= _floor)
            {
                _position_pj = new Vector2(_position_pj.X, _floor);
                _isJumping = false;
                _velocity_david = Vector2.Zero;
            }
        }
    }
    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Black);

        SpriteBatch.Begin(samplerState: SamplerState.PointClamp);
        _bg_beach.Draw(SpriteBatch, Vector2.Zero, Color.White, 0f, Vector2.Zero, 1.5f, SpriteEffects.None, 1.0f);

        _pj.Draw(SpriteBatch, _position_pj);
        SpriteBatch.End();

        base.Draw(gameTime);
    }
}
