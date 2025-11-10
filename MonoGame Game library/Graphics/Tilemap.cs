using System;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace MonoGameLibrary.Graphics;

public class Tilemap
{
    private readonly Tileset _tileset;
    private readonly int[] _tiles;
    public int Rows { get; }
    public int Columns { get; }
    public int Count { get; }
    public Vector2 Scale { get; set; }
    public float TileWidth => _tileset.TileWidth * Scale.X;
    public float TileHeight => _tileset.TileHeight * Scale.Y;

    public Tilemap(Tileset tileset, int columns, int rows)
    {
        _tileset = tileset;
        Rows = rows;
        Columns = columns;
        Count = Columns * Rows;
        Scale = Vector2.One;
        _tiles = new int[Count];
    }

    public void SetTile(int index, int tilesetID)
    {
        _tiles[index] = tilesetID;
    }
    public void SetTile(int column, int row, int tilesetID)
    {
        int index = row * Columns + column;
        SetTile(index, tilesetID);
    }
    public TextureRegion GetTile(int index)
    {
        return _tileset.GetTile(_tiles[index]);
    }

    public TextureRegion GetTile(int column, int row)
    {
        int index = row * Columns + column;
        return GetTile(index);
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        for (int i = 0; i < Count; i++)
        {
            int tilesetIndex = _tiles[i];
            TextureRegion tile = _tileset.GetTile(tilesetIndex);

            int x = i % Columns;
            int y = i / Columns;

            Vector2 position = new Vector2(x * TileWidth, y * TileHeight);
            tile.Draw(spriteBatch, position, Color.White, 0.0f, Vector2.Zero, Scale, SpriteEffects.None, 1.0f);
        }
    }

    public static Tilemap FromFile(ContentManager content, string filename)
{
    string filePath = Path.Combine(content.RootDirectory, filename);

    using (Stream stream = TitleContainer.OpenStream(filePath))
    {
        using (XmlReader reader = XmlReader.Create(stream))
        {
            XDocument doc = XDocument.Load(reader);
            XElement root = doc.Root;
            XElement tilesetElement = root.Element("Tileset");

            string regionAttribute = tilesetElement.Attribute("region").Value;
            string[] split = regionAttribute.Split(" ", StringSplitOptions.RemoveEmptyEntries);
            int x = int.Parse(split[0]);
            int y = int.Parse(split[1]);
            int width = int.Parse(split[2]);
            int height = int.Parse(split[3]);

            int tileWidth = int.Parse(tilesetElement.Attribute("tileWidth").Value);
            int tileHeight = int.Parse(tilesetElement.Attribute("tileHeight").Value);
            string contentPath = tilesetElement.Value;

            Texture2D texture = content.Load<Texture2D>(contentPath);

            TextureRegion textureRegion = new TextureRegion(texture, x, y, width, height);

            Tileset tileset = new Tileset(textureRegion, tileWidth, tileHeight);

            XElement tilesElement = root.Element("Tiles");

            string[] rows = tilesElement.Value.Trim().Split('\n', StringSplitOptions.RemoveEmptyEntries);

            int columnCount = rows[0].Split(" ", StringSplitOptions.RemoveEmptyEntries).Length;
            Tilemap tilemap = new Tilemap(tileset, columnCount, rows.Length);

            for (int row = 0; row < rows.Length; row++)
            {
                string[] columns = rows[row].Trim().Split(" ", StringSplitOptions.RemoveEmptyEntries);

                for (int column = 0; column < columnCount; column++)
                {
                    int tilesetIndex = int.Parse(columns[column]);

                    TextureRegion region = tileset.GetTile(tilesetIndex);

                    tilemap.SetTile(column, row, tilesetIndex);
                }
            }
            return tilemap;
        }
    }
}
}
