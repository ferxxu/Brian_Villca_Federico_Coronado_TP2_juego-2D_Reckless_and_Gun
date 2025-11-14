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

    // --- ¡CAMBIO 1! ---
    // Hecho público (con "public") para que GameScene.cs pueda acceder a él
    public Dictionary<string, Animation> Animations;

    public string CurrentAnimationName => _currentAnimation?.Name;

    public AnimatedSprite()
    {
        // --- ¡CAMBIO 2! ---
        Animations = new Dictionary<string, Animation>(); // Usar la propiedad pública
    }

    public void AddAnimation(string name, Animation animation)
    {
        // --- ¡CAMBIO 3! ---
        Animations[name] = animation; // Usar la propiedad pública

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

        // --- ¡CAMBIO 4! ---
        if (!Animations.ContainsKey(name)) // Usar la propiedad pública
        {
            Console.WriteLine($"Error: No se encontró la animación '{name}'");
            return;
        }

        _currentAnimation = Animations[name]; // Usar la propiedad pública
        _currentFrame = 0;
        _elapsed = TimeSpan.Zero;

        if (_currentAnimation.Frames.Count > 0)
        {
            Region = _currentAnimation.Frames[0];
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
                // --- ¡¡CAMBIO 5: LA LÓGICA DE LOOP!! ---
                // (Esto asume que tu clase 'Animation' tiene una propiedad 'IsLooping')
                if (_currentAnimation.IsLooping)
                {
                    _currentFrame = 0; // Si loopea, vuelve al inicio
                }
                else
                {
                    // Si no, se queda en el último fotograma
                    _currentFrame = _currentAnimation.Frames.Count - 1;
                }
            }

            Region = _currentAnimation.Frames[_currentFrame];
        }
    }
}