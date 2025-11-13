using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using MonoGameLibrary.Graphics;

namespace MonoGameLibrary.Graphics;

public class AnimatedSprite : Sprite
{
    private int _currentFrame;
    private TimeSpan _elapsed;
    private Animation _currentAnimation;
    private Dictionary<string, Animation> _animations;
    public string CurrentAnimationName => _currentAnimation?.Name;

    public AnimatedSprite()
    {
        _animations = new Dictionary<string, Animation>();
    }

    public void AddAnimation(string name, Animation animation)
    {
        _animations[name] = animation;

        if (_currentAnimation == null)
        {
            Play(name);
        }
    }

    public void Play(string name)
    {
        if (CurrentAnimationName == name)
        {
            return; 
        }

        if (!_animations.ContainsKey(name))
        {
            Console.WriteLine($"Error: No se encontró la animación '{name}'");
            return; 
        }

        _currentAnimation = _animations[name];
        _currentFrame = 0;
        _elapsed = TimeSpan.Zero;

        if (_currentAnimation.Frames.Count > 0)
        {
            Region = _currentAnimation.Frames[0];
            // NOTA: El 'Origin' base se establece en GameScene.cs
        }
    }
    public void Update(GameTime gameTime)
    {
        if (_currentAnimation == null) return;

        _elapsed += gameTime.ElapsedGameTime;

        if (_elapsed >= _currentAnimation.Delay)
        {
            _elapsed -= _currentAnimation.Delay;
            _currentFrame++;

            if (_currentFrame >= _currentAnimation.Frames.Count)
            {
                _currentFrame = 0;
            }

            Region = _currentAnimation.Frames[_currentFrame];
            // NOTA: El 'Origin' base se establece en GameScene.cs
        }
    }
}