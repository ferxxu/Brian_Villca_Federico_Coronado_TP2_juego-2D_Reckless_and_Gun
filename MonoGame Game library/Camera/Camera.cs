using System;
using System.Drawing;
using System.Numerics;
public class FollowCamera
{
    public Vector2 _position;

    public FollowCamera(Vector2 position)
    {   
        this._position = position;
    }

    public void Following(Rectangle target, Vector2 ScreenSize)
    {
        _position = new Vector2(-target.X + (ScreenSize.X / 2 - target.Width / 2), -target.Y + (ScreenSize.Y / 2 - target.Height / 2));        
    }

    public static implicit operator FollowCamera(Microsoft.Xna.Framework.Vector2 v)
    {
        throw new NotImplementedException();
    }
}