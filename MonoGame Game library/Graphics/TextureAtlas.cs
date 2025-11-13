using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System.Linq;

namespace MonoGameLibrary.Graphics;

public class TextureAtlas
{
    private Dictionary<string, Animation> _animations;
    private Dictionary<string, TextureRegion> _regions;
    public Texture2D Texture { get; set; }

    public TextureAtlas()
    {
        _animations = new Dictionary<string, Animation>();
        _regions = new Dictionary<string, TextureRegion>();
    }

    public TextureAtlas(Texture2D texture)
    {
        Texture = texture;
        _regions = new Dictionary<string, TextureRegion>();
        _animations = new Dictionary<string, Animation>(); 
    }

    public void AddRegion(string name, int x, int y, int width, int height)
    {
        TextureRegion region = new TextureRegion(name, Texture, x, y, width, height); 
        _regions.Add(name, region);
    }
    
    public void AddAnimation(string animationName, Animation animation) { _animations.Add(animationName, animation); }
    public Animation GetAnimation(string animationName) { return _animations[animationName]; }
    public bool RemoveAnimation(string animationName) { return _animations.Remove(animationName); }
    public TextureRegion GetRegion(string name) { return _regions[name]; }
    public bool RemoveRegion(string name) { return _regions.Remove(name); }
    public void Clear() { _regions.Clear(); }
    public Sprite CreateSprite(string regionName) { TextureRegion region = GetRegion(regionName); return new Sprite(region); }
    public AnimatedSprite CreateAnimatedSprite(string defaultAnimationName)
    {
        AnimatedSprite newSprite = new AnimatedSprite();
        foreach (var animationEntry in _animations)
        { newSprite.AddAnimation(animationEntry.Key, animationEntry.Value); }
        if (!string.IsNullOrEmpty(defaultAnimationName) && _animations.ContainsKey(defaultAnimationName))
        { newSprite.Play(defaultAnimationName); }
        else if (_animations.Count > 0)
        { newSprite.Play(_animations.Keys.First()); }
        return newSprite;
    }

    public static TextureAtlas FromFile(ContentManager content, string fileName)
    {
        TextureAtlas atlas = new TextureAtlas();
        string filePath = Path.Combine(content.RootDirectory, fileName);
        using (Stream stream = TitleContainer.OpenStream(filePath))
        {
            using (XmlReader reader = XmlReader.Create(stream))
            {
                XDocument doc = XDocument.Load(reader);
                XElement root = doc.Root;
                string texturePath = root.Element("Texture")?.Value;
                if (string.IsNullOrEmpty(texturePath))
                { throw new Exception($"La etiqueta <Texture> está vacía o no existe en el XML: {fileName}"); }
                atlas.Texture = content.Load<Texture2D>(texturePath);
                var regions = root.Element("Regions")?.Elements("Region");
                if (regions != null)
                {
                    foreach (var region in regions)
                    {
                        string name = region.Attribute("name")?.Value;
                        int x = int.Parse(region.Attribute("x")?.Value ?? "0");
                        int y = int.Parse(region.Attribute("y")?.Value ?? "0");
                        int width = int.Parse(region.Attribute("width")?.Value ?? "0");
                        int height = int.Parse(region.Attribute("height")?.Value ?? "0");
                        if (!string.IsNullOrEmpty(name))
                        {
                            atlas.AddRegion(name, x, y, width, height); // <-- Llama al método modificado
                        }
                    }
                }
                var animationElements = root.Element("Animations")?.Elements("Animation");
                if (animationElements != null)
                {
                    foreach (var animationElement in animationElements)
                    {
                        string name = animationElement.Attribute("name")?.Value;
                        float delayInMilliseconds = float.Parse(animationElement.Attribute("delay")?.Value ?? "100");
                        TimeSpan delay = TimeSpan.FromMilliseconds(delayInMilliseconds);
                        List<TextureRegion> frames = new List<TextureRegion>();
                        var frameElements = animationElement.Elements("Frame");
                        if (frameElements != null)
                        {
                            foreach (var frameElement in frameElements)
                            {
                                string regionName = frameElement.Attribute("region").Value;
                                TextureRegion region = atlas.GetRegion(regionName);
                                frames.Add(region);
                            }
                        }
                        Animation animation = new Animation(name, frames, delay); 
                        atlas.AddAnimation(name, animation);
                    }
                }
                return atlas;
            }
        }
    }
}