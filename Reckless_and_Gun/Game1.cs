using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGameLibrary;
using MonoGameLibrary.Graphics;

namespace Reckless_and_Gun;

public class Game1 : Core
{
    // texture region that defines the slime sprite in the atlas.

    private AnimatedSprite _pj;


    public Game1() : base("Reckless and Gun", 1680, 820, false)
    {

    }

    protected override void Initialize()
    {
        // TODO: Add your initialization logic here

        base.Initialize();
    }

    protected override void LoadContent()
    {
        // Create the texture atlas from the XML configuration file
        TextureAtlas atlas = TextureAtlas.FromFile(Content, "Sprites_pj/Pj.xml");

        // retrieve the slime region from the atlas.
               // Create the slime sprite from the atlas.
        _pj = atlas.CreateAnimatedSprite("David-walk");
        _pj.Scale = new Vector2(2.0f, 2.0f);
        _pj.Origin = new Vector2(_pj.Width / 2f, _pj.Height / 2f);


    }

    protected override void Update(GameTime gameTime)
    {
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();

        // TODO: Add your update logic here
        _pj.Update(gameTime);
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        // Clear the back buffer.
        GraphicsDevice.Clear(Color.CornflowerBlue);

        // Begin the sprite batch to prepare for rendering.
        SpriteBatch.Begin(samplerState: SamplerState.PointClamp);

        Vector2 centerOfScreen = new Vector2(
        Window.ClientBounds.Width / 2f, 
        Window.ClientBounds.Height / 2f
    );
        _pj.Draw (SpriteBatch,centerOfScreen);
        // Always end the sprite batch when finished.
        SpriteBatch.End();

        base.Draw(gameTime);
    }
}
