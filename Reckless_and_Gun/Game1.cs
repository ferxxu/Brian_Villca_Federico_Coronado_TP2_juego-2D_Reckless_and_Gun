using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGameLibrary;
using MonoGameLibrary.Graphics;

namespace Reckless_and_Gun;

public class Game1 : Core
{
    // texture region that defines the slime sprite in the atlas.
    private TextureRegion _pj;


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
        _pj = atlas.GetRegion("David");

    }

    protected override void Update(GameTime gameTime)
    {
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();

        // TODO: Add your update logic here

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        // Clear the back buffer.
        GraphicsDevice.Clear(Color.CornflowerBlue);

        // Begin the sprite batch to prepare for rendering.
        SpriteBatch.Begin(samplerState: SamplerState.PointClamp);

        // Draw the slime texture region at a scale of 4.0
        _pj.Draw(SpriteBatch, new Vector2(
        (Window.ClientBounds.Width * 0.5f) - (_pj.Width * 0.5f),
            (Window.ClientBounds.Height * 0.5f) - (_pj.Height * 0.5f)),
             Color.White, 0.0f, Vector2.One, 2.0f, SpriteEffects.None, 0.0f);

        // Always end the sprite batch when finished.
        SpriteBatch.End();

        base.Draw(gameTime);
    }
}
