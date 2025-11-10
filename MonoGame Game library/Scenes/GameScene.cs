// GameScene.cs (Refactorizado)

using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGameLibrary;
using MonoGameLibrary.Graphics;
using MonoGameLibrary.Scenes;
using MonoGameLibrary.Input;
using MonoGameLibrary.Camera;
using TiledSharp;

namespace reckless_and_gun.Scenes;

public class GameScene : Scene
{
    private Camera2D _camera;
    private List<Rectangle> _collisionRects;
    private Texture2D _background;
    private Rectangle _roomBounds;
    private Rectangle _davidHitbox;
    private AnimatedSprite _davidLegs;
    private AnimatedSprite _davidChest;
    private Vector2 _position_pj;
    private Vector2 _velocity_pj;

    // --- Variables de Estado (FSM) ---
    private string _legState = "idle-legs";
    private string _chestState = "idle-torso";
    private bool _isJumping; // True si está en el aire (cayendo o subiendo)
    private bool isDucking;
    private bool isShooting;
    private bool isMovingHorizontally;
    private bool isAimingUp; // <-- ¡NUEVO ESTADO!

    // --- Constantes de Física ---
    private const float _speed = 150f;
    private const float _jumpSpeed = -500f;
    private const float _gravity = 1500f;

    public override void Initialize()
    {
        base.Initialize();
        Core.ExitOnEscape = false;
        _velocity_pj = Vector2.Zero;
        _camera = new Camera2D();
        _camera.Zoom = 1.0f;
    }

    public override void LoadContent()
    {
        // ... (Tu código de carga de fondo, atlas, sprites y orígenes es PERFECTO) ...
        _background = Content.Load<Texture2D>("beach_map");
        TextureAtlas atlas = TextureAtlas.FromFile(Core.Content, "atlas-definition.xml");
        _davidChest = atlas.CreateAnimatedSprite("idle-torso");
        _davidLegs = atlas.CreateAnimatedSprite("idle-legs");
        _davidLegs.Scale = new Vector2(2.0f, 2.0f);
        _davidChest.Scale = new Vector2(2.0f, 2.0f);
        float legsWidth = _davidLegs.Region.Width;
        float legsHeight = _davidLegs.Region.Height;
        float torsoWidth = _davidChest.Region.Width;
        float torsoHeight = _davidChest.Region.Height;
        _davidLegs.Origin = new Vector2(legsWidth / 2f, 0f); // Arriba-Centro
        _davidChest.Origin = new Vector2(torsoWidth / 2f, torsoHeight); // Abajo-Centro

        // Carga de Mapa Tiled
        _collisionRects = new List<Rectangle>();
        string mapFilePath = Path.Combine(Content.RootDirectory, "beach_map.tmx");
        var map = new TmxMap(mapFilePath);
        _roomBounds = new Rectangle(0, 0, map.Width * map.TileWidth, map.Height * map.TileHeight);
        var collisionLayer = map.ObjectGroups["collisions"];
        foreach (var obj in collisionLayer.Objects)
        {
            _collisionRects.Add(new Rectangle((int)obj.X, (int)obj.Y, (int)obj.Width, (int)obj.Height));
        }

        // Posición Inicial (Encima del suelo)
        _position_pj = new Vector2(400, 10); // Empezar en el aire para que caiga

        // Inicializa la Hitbox
        UpdateHitbox(); // Llama al método de actualización
    }


    public override void Update(GameTime gameTime)
    {
        HandleInput();
        ApplyPhysics(gameTime);
        handleLegsAnimation();
        handleChestAnimation(); // <-- CORREGIDO

        _davidLegs.Play(_legState);
        _davidChest.Play(_chestState);

        _davidLegs.Update(gameTime);
        _davidChest.Update(gameTime);

        _camera.Follow(_position_pj, _roomBounds, Core.GraphicsDevice.Viewport);
        base.Update(gameTime);
    }


    private void UpdateHitbox()
    {
        // (Ajusta la hitbox si está agachado)
        if (isDucking)
        {
            _davidHitbox.Height = (int)(_davidLegs.Height * 0.6f); // 60% de la altura
        }
        else
        {
            _davidHitbox.Height = (int)_davidLegs.Height;
        }
        _davidHitbox.Width = (int)_davidLegs.Width;

        // --- CÁLCULO DE HITBOX CORREGIDO ---
        // Anclaje de piernas (Origen) es Arriba-Centro
        // _position_pj es la cintura
        
        // Esquina X = Posición.X - (Mitad del Ancho del Sprite)
        _davidHitbox.X = (int)(_position_pj.X - _davidLegs.Origin.X * _davidLegs.Scale.X);
        
        // Esquina Y = Posición.Y (ya que Origen.Y es 0)
        _davidHitbox.Y = (int)(_position_pj.Y - _davidLegs.Origin.Y * _davidLegs.Scale.Y);
    }
    
    private void HandleInput()
    {
        KeyboardInfo keyboard = Core.Input.Keyboard;

        if (keyboard.WasKeyJustPressed(Keys.Escape))
        {
            Core.ChangeScene(new TitleScene());
        }

        // Resetea bools de acción
        isMovingHorizontally = false;
        isDucking = false;
        isShooting = false;
        isAimingUp = false; // <-- ¡NUEVO!

        // Agacharse (S)
        if (keyboard.IsKeyDown(Keys.S) && !_isJumping)
        {
            isDucking = true;
        }

        // Apuntar Arriba (W)
        if (keyboard.IsKeyDown(Keys.W) && !_isJumping) // <-- ¡NUEVO!
        {
            isAimingUp = true;
        }

        // Movimiento Horizontal (A/D)
        if (!isDucking && !isAimingUp) // No moverse si está agachado O apuntando arriba
        {
            if (keyboard.IsKeyDown(Keys.A))
            {
                isMovingHorizontally = true;
                _davidLegs.Effects = SpriteEffects.FlipHorizontally;
                _davidChest.Effects = SpriteEffects.FlipHorizontally;
            }
            else if (keyboard.IsKeyDown(Keys.D))
            {
                isMovingHorizontally = true;
                _davidLegs.Effects = SpriteEffects.None;
                _davidChest.Effects = SpriteEffects.None;
            }
        }

        // Disparar (H)
        if (keyboard.IsKeyDown(Keys.H))
        {
            isShooting = true;
        }

        // Salto (J)
        if (keyboard.WasKeyJustPressed(Keys.J) && !_isJumping && !isDucking)
        {
            _isJumping = true; 
            _velocity_pj.Y = _jumpSpeed;
        }
    }

    private void ApplyPhysics(GameTime gameTime)
    {
        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

        // --- 1. ACTUALIZAR VELOCIDAD X ---
        if (isMovingHorizontally)
        {
            _velocity_pj.X = (_davidLegs.Effects == SpriteEffects.FlipHorizontally) ? -_speed : _speed;
        }
        else
        {
            _velocity_pj.X = 0; // Detenerse
        }

        // --- 2. APLICAR GRAVEDAD (Y) ---
        _velocity_pj.Y += _gravity * deltaTime;
        _velocity_pj.Y = MathHelper.Clamp(_velocity_pj.Y, -_jumpSpeed, _gravity * 2f);

        // --- 3. SEPARACIÓN DE EJES: (X) ---
        _position_pj.X += _velocity_pj.X * deltaTime;
        UpdateHitbox(); 

        foreach (Rectangle rect in _collisionRects)
        {
            if (_davidHitbox.Intersects(rect))
            {
                // Colisión X (Pared)
                if (_velocity_pj.X > 0) 
                {
                    float anchorX = _davidLegs.Origin.X * _davidLegs.Scale.X;
                    _position_pj.X = rect.Left - (_davidHitbox.Width - anchorX);
                }
                else if (_velocity_pj.X < 0) 
                {
                    float anchorX = _davidLegs.Origin.X * _davidLegs.Scale.X;
                    _position_pj.X = rect.Right + anchorX;
                }
                _velocity_pj.X = 0;
                UpdateHitbox(); 
            }
        }

        // --- 4. SEPARACIÓN DE EJES: (Y) ---
        _position_pj.Y += _velocity_pj.Y * deltaTime;
        UpdateHitbox(); 

        bool isGrounded = false; 

        foreach (Rectangle rect in _collisionRects)
        {
            // --- CORRECCIÓN CRÍTICA ---
            // ¡Quitamos el '_velocity_pj.Y != 0'! 
            // Siempre debe comprobar si está intersectando, incluso si la velocidad es 0.
            if (_davidHitbox.Intersects(rect))
            {
                if (_velocity_pj.Y > 0) // Si estaba cayendo (colisión con SUELO)
                {
                    isGrounded = true;
                    _velocity_pj.Y = 0;
                    
                    // --- CORRECCIÓN CRÍTICA ---
                    // Pegar los PIES del jugador (hitbox bottom) al TOPE del suelo (rect top)
                    // _position_pj.Y (cintura) = rect.Top (suelo) - AlturaDeLaHitbox
                    _position_pj.Y = rect.Top - _davidHitbox.Height; 
                }
                else if (_velocity_pj.Y < 0) // Si estaba subiendo (golpe de CABEZA)
                {
                    _velocity_pj.Y = 0;
                    // Pega la CABEZA del jugador (hitbox top) al FONDO del techo (rect bottom)
                    _position_pj.Y = rect.Bottom; // (Como Origin.Y es 0, la Hitbox.Y = Position.Y)
                }
                UpdateHitbox(); 
            }
        }

        // --- 5. ACTUALIZAR ESTADO DE SALTO ---
        _isJumping = !isGrounded;
    }


    public void handleChestAnimation()
    {
        // --- LÓGICA DE APUNTAR ARRIBA AÑADIDA ---
        if (_isJumping)
        {
            // (Opcional: puedes tener "jump-shoot-torso" si disparas en el aire)
            _chestState = "jump-torso";
        }
        else if (isDucking)
        {
            if (isShooting)
            {
                _chestState = "duck-shoot-torso";
            }
            else if (isMovingHorizontally) 
            {
                _chestState = "duck-walk-torso";
            }
            else
            {
                _chestState = "duck-torso"; // (Quieto agachado)
            }
        }
        // ¡NUEVA PRIORIDAD!
        else if (isAimingUp) 
        {
            if (isShooting)
            {
                _chestState = "shoot-up-torso";
            }
            else
            {
                _chestState = "idle-torso"; // (O una anim de "apuntar arriba" si la tienes)
            }
        }
        else if (isShooting)
        {
            // (Aquí puedes añadir shoot-down)
            _chestState = "shoot-torso";
        }
        else if (isMovingHorizontally)
        {
            _chestState = "run-torso";
        }
        else
        {
            _chestState = "idle-torso";
        }
    }

    public void handleLegsAnimation()
    {
        if (_isJumping)
        {
            _legState = "jump-legs";
        }
        else if (isDucking)
        {
            if (isMovingHorizontally)
            {
                _legState = "duck-walk-legs";
            }
            else
            {
                _legState = "duck-legs"; // (Quieto agachado)
            }
        }
        // ¡NUEVA CONDICIÓN!
        else if (isAimingUp) // Si apuntas arriba, las piernas se quedan quietas
        {
            _legState = "idle-legs";
        }
        else if (isMovingHorizontally)
        {
            _legState = "run-legs";
        }
        else
        {
            _legState = "idle-legs";
        }
    }

    public override void Draw(GameTime gameTime)
    {
        Core.GraphicsDevice.Clear(Color.Black);

        Core.SpriteBatch.Begin(
            samplerState: SamplerState.PointClamp,
            transformMatrix: _camera.GetViewMatrix(Core.GraphicsDevice.Viewport)
        );

        Core.SpriteBatch.Draw(_background, Vector2.Zero, Color.White);

        _davidLegs.Draw(Core.SpriteBatch, _position_pj);
        _davidChest.Draw(Core.SpriteBatch, _position_pj);

        Core.SpriteBatch.End();
    }
}