using System;
using System.Collections.Generic;

namespace MonoGameLibrary.Graphics;

public class Animation
{
    public string Name { get; set; }
    public List<TextureRegion> Frames { get; set; }
    public TimeSpan Delay { get; set; }

    public Animation()
    {
        Frames = new List<TextureRegion>();
        Delay = TimeSpan.FromMilliseconds(100);
        Name = string.Empty;
    }

    public Animation(string name, List<TextureRegion> frames, TimeSpan delay)
    {
        Name = name;
        Frames = frames;
        Delay = delay;
    }
}