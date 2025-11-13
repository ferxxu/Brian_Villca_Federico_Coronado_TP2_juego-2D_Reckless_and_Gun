using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MonoGameLibrary.Graphics;

public class TextureRegion
{
    // --- ¡NUEVA PROPIEDAD! ---
    // La región ahora "sabe" su propio nombre
    public string Name { get; private set; } 
    
    // --- Tus propiedades existentes ---
    public Texture2D Texture { get; set; }
    public Rectangle SourceRectangle { get; set; }
    public int Width => SourceRectangle.Width;
    public int Height => SourceRectangle.Height;

    // --- CONSTRUCTOR MODIFICADO (para Sprites y TextureAtlas) ---
    // Este es el que usa TextureAtlas.cs
    public TextureRegion(string name, Texture2D texture, int x, int y, int width, int height)
    {
        this.Name = name; // <-- Lo guardamos
        Texture = texture;
        SourceRectangle = new Rectangle(x, y, width, height);
    }

    // --- ¡FIX! CONSTRUCTOR ORIGINAL (para Tileset/Tilemap) ---
    // Añadimos de nuevo el constructor original que tus otras clases necesitan.
    // Llama al constructor nuevo con un 'name' nulo o vacío.
    public TextureRegion(Texture2D texture, int x, int y, int width, int height)
        : this(string.Empty, texture, x, y, width, height)
    {
        // No se necesita nada aquí. El 'name' será vacío, lo cual está bien.
    }

    // ... (El resto de tus métodos Draw no cambian) ...
    public void Draw(SpriteBatch spriteBatch, Vector2 position, Color color)
    {
        Draw(spriteBatch, position, color, 0.0f, Vector2.Zero, Vector2.One, SpriteEffects.None, 0.0f);
    }
    public void Draw(SpriteBatch spriteBatch, Vector2 position, Color color, float rotation, Vector2 origin, float scale, SpriteEffects effects, float layerDepth)
    {
        Draw(
            spriteBatch,
            position,
            color,
            rotation,
            origin,
            new Vector2(scale, scale),
            effects,
            layerDepth
        );
    }
    public void Draw(SpriteBatch spriteBatch, Vector2 position, Color color, float rotation, Vector2 origin, Vector2 scale, SpriteEffects effects, float layerDepth)
    {
        spriteBatch.Draw(
            Texture,
            position,
            SourceRectangle,
            color,
            rotation,
            origin,
            scale,
            effects,
            layerDepth
        );
    }
}