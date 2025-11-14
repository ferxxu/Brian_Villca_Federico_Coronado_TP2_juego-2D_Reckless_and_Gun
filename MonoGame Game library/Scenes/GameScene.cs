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
    // --- Variables de Escena ---
    private Camera2D _camera;
    private List<Rectangle> _collisionRects;
    private Texture2D _background;
    private Rectangle _roomBounds;

    // --- Variables de Personaje ---
    private Rectangle _davidHitbox;
    private AnimatedSprite _davidLegs;
    private AnimatedSprite _davidChest;
    private Vector2 _position_pj; // La CINTURA del personaje
    private Vector2 _velocity_pj;

    // --- Variables de Física Constante (¡Clave!) ---
    private float _constLegsHeight;
    private float _constTorsoHeight;
    private float _constHitboxWidth;
    private float _constHitboxHeight;

    // --- ¡CAMBIO! ---
    // Diccionarios por nombre de FOTOGRAMA
    private Dictionary<string, Vector2> _torsoFrameOffsets;
    private Dictionary<string, Vector2> _legsFrameOffsets;

    // --- Variables de Debug ---
    private Texture2D _texturaDebug;
    private Rectangle _debugSuelo;

    // --- Variables de Estado (FSM) ---
    private string _legState = "idle-legs";
    private string _chestState = "idle-torso";
    private bool _isJumping; // True = está en el aire
    private bool isDucking;
    private bool isShooting;
    private bool isMovingHorizontally;
    private bool isAimingUp;
    private bool isAimingDown; // <-- ¡AÑADE ESTA LÍNEA!

    // --- Constantes de Física ---
    private const float _speed = 150f;
    private const float _jumpSpeed = -500f; // Negativo (hacia arriba)
    private const float _gravity = 1500f;
    // Enemigo 1:

    private Rectangle _spiderHitbox;
    private AnimatedSprite _spiderAnimation;
    private Vector2 _spiderPosition;
    private Vector2 _spiderVelocity;
    private float _spiderWidth;
    private float _spiderHeight;
    private string _spiderState = "";


    public override void Initialize()
    {
        base.Initialize();
        Core.ExitOnEscape = false;
        _velocity_pj = Vector2.Zero;
        _camera = new Camera2D();
        _camera.Zoom = 1.0f;
        _isJumping = true; // Empieza en el aire
        _spiderVelocity = Vector2.Zero;
    }

    public override void LoadContent()
    {
        _background = Content.Load<Texture2D>("beach_map");

        TextureAtlas atlas = TextureAtlas.FromFile(Core.Content, "david1.xml"); // Asegúrate que el XML se llame 'david.xml'

        TextureAtlas atlasSpider = TextureAtlas.FromFile(Core.Content, "spider.xml");

        _davidChest = atlas.CreateAnimatedSprite("idle-torso");
        _davidLegs = atlas.CreateAnimatedSprite("idle-legs");
        _spiderAnimation = atlasSpider.CreateAnimatedSprite("spider_walking");
        _davidLegs.Scale = new Vector2(2.0f, 2.0f);
        _davidChest.Scale = new Vector2(2.0f, 2.0f);
        _spiderAnimation.Scale = new Vector2(3.0f, 3.0f);

        // --- Se define el ORIGEN (Pivote) BASE UNA SOLA VEZ ---
        // (Esto detiene la vibración base)
        float pivotX = 11f;

        _spiderWidth = _spiderAnimation.Region.Width * _spiderAnimation.Scale.X;
        _spiderHeight = _spiderAnimation.Region.Height * _spiderAnimation.Scale.Y;
        _davidLegs.Origin = new Vector2(pivotX, 0f);
        _davidChest.Origin = new Vector2(pivotX, _davidChest.Region.Height);

        // --- Calculamos constantes ---
        _constLegsHeight = _davidLegs.Region.Height * _davidLegs.Scale.Y;
        _constTorsoHeight = _davidChest.Region.Height * _davidChest.Scale.Y;
        _constHitboxWidth = _davidLegs.Region.Width * _davidLegs.Scale.X;
        _constHitboxHeight = _constLegsHeight + _constTorsoHeight;

        _torsoFrameOffsets = new Dictionary<string, Vector2>()
        {
            { "torso_idle_0", new Vector2(0f, 0f) }, // <- Tu base
            { "torso_run_0", new Vector2(0f, 0f) },
            { "torso_run_1", new Vector2(0f, 0f) },
            { "torso_jump_0", new Vector2(0f, 0f) },
            { "torso_jump_1", new Vector2(0f, 0f) },
            { "torso_jump_2", new Vector2(0f, 0f) },
            { "torso_jump_3", new Vector2(0f, 0f) },
            { "torso_jump_4", new Vector2(0f, 0f) },
            { "torso_shoot_0", new Vector2(0f, 0f) },
            { "torso_shoot_1", new Vector2(0f, 0f) },
            { "torso_shoot_2", new Vector2(0f, 0f) },
            { "torso_shoot_3", new Vector2(0f, 0f) },
            { "torso_down_0", new Vector2(0f, 10f) },
            { "torso_shoot_down_0", new Vector2(0f, 10f) },
            { "torso_shoot_down_1", new Vector2(0f, 10f) },
            { "torso_shoot_down_2", new Vector2(0f, 10f) },
            { "torso_shoot_down_3", new Vector2(0f, 10f) },
            { "torso_up_0", new Vector2(0f, -3f) },
            { "torso_shoot_up_0", new Vector2(0f, -30f) },
            { "torso_shoot_up_1", new Vector2(0f, -30f) },
            { "torso_shoot_up_2", new Vector2(0f, -30f) },
            { "torso_shoot_up_3", new Vector2(0f, -30f) },
            { "torso_shoot_up_4", new Vector2(0f, -30f) },
            { "torso_duck_Animated_0", new Vector2(0f, 0f) },
            { "torso_duck_Animated_1", new Vector2(0f, 0f) },
            { "torso_duck_0", new Vector2(0f, 0f) },
            { "torso_duck_shoot_0", new Vector2(0f, 0f) },
            { "torso_duck_shoot_1", new Vector2(0f, 0f) },
            { "torso_duck_shoot_2", new Vector2(0f, 0f) },
            { "torso_duck_walk_0", new Vector2(0f, 0f) },
            { "torso_duck_walk_1", new Vector2(0f, 0f) },
            { "torso_duck_walk_2", new Vector2(0f, 0f) },
            { "torso_duck_walk_3", new Vector2(0f, 0f) },
            { "torso_duck_walk_4", new Vector2(0f, 0f) }
        };

        _legsFrameOffsets = new Dictionary<string, Vector2>()
        {
            { "legs_idle_0", new Vector2(0f, 0f) }, // <- Tu base
            { "legs_run_0", new Vector2(0f, 0f) },
            { "legs_run_1", new Vector2(0f, 0f) },
            { "legs_run_2", new Vector2(0f, 0f) },
            { "legs_run_3", new Vector2(0f, 0f) },
            { "legs_run_4", new Vector2(0f, 0f) },
            { "legs_run_5", new Vector2(0f, 0f) },
            { "legs_run_6", new Vector2(0f, 0f) },
            { "legs_run_7", new Vector2(0f, 0f) },
            { "legs_run_8", new Vector2(0f, 0f) },
            { "legs_run_9", new Vector2(0f, 0f) },
            { "legs_run_10", new Vector2(0f, 0f) },
            { "legs_run_11", new Vector2(0f, 0f) },
            { "legs_run_12", new Vector2(0f, 0f) },
            { "legs_duck_0", new Vector2(0f, 0f) },
            { "legs_duck_1", new Vector2(0f, 0f) },
            { "legs_duck_shoot_2", new Vector2(0f, 0f) },
            { "legs_duck_shoot_3", new Vector2(0f, 0f) },
            { "legs_duck_shoot_4", new Vector2(0f, 0f) },
            { "legs_duck_shoot_5", new Vector2(0f, 0f) },
            { "legs_duck_walk_0", new Vector2(0f, 0f) },
            { "legs_duck_walk_1", new Vector2(0f, 0f) },
            { "legs_duck_walk_2", new Vector2(0f, 0f) },
            { "legs_duck_walk_3", new Vector2(0f, 0f) },
            { "legs_duck_walk_4", new Vector2(0f, 0f) },
            { "legs_jump_0", new Vector2(0f, 0f) },
            { "legs_jump_1", new Vector2(0f, 0f) },
            { "legs_jump_2", new Vector2(0f, 0f) },
            { "legs_jump_3", new Vector2(0f, 0f) },
            { "legs_jump_4", new Vector2(0f, 0f) },
            { "legs_jump_5", new Vector2(0f, 0f) }
        };

        // ... (El resto de tu LoadContent para colisiones, debug, etc. no cambia) ...
        _collisionRects = new List<Rectangle>();
        string mapFilePath = Path.Combine(Content.RootDirectory, "beach_map.tmx");
        var map = new TmxMap(mapFilePath);
        _roomBounds = new Rectangle(0, 0, map.Width * map.TileWidth, map.Height * map.TileHeight);
        var collisionLayer = map.ObjectGroups["collisions"];

        foreach (var obj in collisionLayer.Objects)
        {
            _collisionRects.Add(new Rectangle((int)obj.X, (int)obj.Y, (int)obj.Width, (int)obj.Height));
        }
        _position_pj = new Vector2(600, 10);
        _texturaDebug = new Texture2D(Core.GraphicsDevice, 1, 1);
        _texturaDebug.SetData(new[] { Color.White });
        _debugSuelo = new Rectangle(0, 450, _roomBounds.Width, 50);
        _spiderPosition = new Vector2(2000, 290);
        _spiderHitbox = new Rectangle(
        (int)(_spiderPosition.X - (_spiderWidth / 2f)),
        (int)(_spiderPosition.Y - (_spiderHeight / 2f)),
        (int)_spiderWidth,
        (int)_spiderHeight
    );
        UpdateHitbox();
    }


    public override void Update(GameTime gameTime)
    {
        // ... (Tu Update() no cambia) ...
        HandleInput();
        ApplyPhysics(gameTime);
        handleLegsAnimation();
        handleChestAnimation();
        handleSpiderAnimation();

        _davidLegs.Update(gameTime);
        _davidChest.Update(gameTime);
        _spiderAnimation.Update(gameTime);

        _spiderAnimation.Effects = SpriteEffects.FlipHorizontally;
        _spiderHitbox.Width = (int)_spiderWidth;
        _spiderHitbox.Height = (int)_spiderHeight;

        _spiderAnimation.Origin = new Vector2(_spiderAnimation.Region.Width / 2f, _spiderAnimation.Region.Height / 2f);

        float hitboxOriginX = _spiderHitbox.Width / 2f;
        float hitboxOriginY = _spiderHitbox.Height / 2f;

        _spiderHitbox.X = (int)(_spiderPosition.X - hitboxOriginX);
        _spiderHitbox.Y = (int)(_spiderPosition.Y - hitboxOriginY);

        _camera.Follow(_position_pj, _roomBounds, Core.GraphicsDevice.Viewport);
        base.Update(gameTime);
    }
    private void HandleInput()
    {
        KeyboardInfo keyboard = Core.Input.Keyboard;
        if (keyboard.WasKeyJustPressed(Keys.Escape))
        { Core.ChangeScene(new TitleScene()); }
        
        // Reset states
        isMovingHorizontally = false;
        isDucking = false;
        isShooting = false;
        isAimingUp = false;
        isAimingDown = false; // <-- NEW

        // 1. Check Aiming/Ducking states
        if (keyboard.IsKeyDown(Keys.W)) 
        { isAimingUp = true; }

        // --- ¡CAMBIO! ---
        // S ahora tiene doble función
        if (keyboard.IsKeyDown(Keys.S))
        {
            if (!_isJumping)
            {
                isDucking = true; // En el suelo, S = Agacharse
            }
            else
            {
                isAimingDown = true; // En el aire, S = Apuntar Abajo
            }
        }
        
        // --- ¡CAMBIO! ---
        // El bloque de movimiento AHORA permite moverse mientras se apunta.
        // Solo se bloquea si estás AGRACHADO (isDucking).
        if (!isDucking) 
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
        // --- FIN DEL CAMBIO ---

        // 3. Check Actions
        if (keyboard.IsKeyDown(Keys.H))
        { isShooting = true; }
        
        // El 'if' original ya previene saltar mientras estás agachado (isDucking)
        if (keyboard.IsKeyDown(Keys.J) && !_isJumping && !isDucking)
        { _velocity_pj.Y = _jumpSpeed; _isJumping = true; }
    }
    private void ApplyPhysics(GameTime gameTime)
    {
        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

        // --- 1. ACTUALIZAR VELOCIDAD X (basado en HandleInput) ---
        if (isMovingHorizontally)
        {
            _velocity_pj.X = (_davidLegs.Effects == SpriteEffects.FlipHorizontally) ? -_speed : _speed;
        }
        else
        {
            _velocity_pj.X = 0; // Detenerse
        }

        // --- 2. APLICAR GRAVEDAD (Y) ---
        // La gravedad SIEMPRE se aplica si no estamos en el suelo
        // Opcional: si _isJumping es true, aplica gravedad.
        _velocity_pj.Y += _gravity * deltaTime;

        // Limita la velocidad de caída y de subida
        _velocity_pj.Y = MathHelper.Clamp(_velocity_pj.Y, _jumpSpeed, _gravity * 2f);

        // --- 3. MANEJAR COLISIONES POR EJE ---
        // (Llamamos a los nuevos métodos)
        HandleHorizontalCollisions(deltaTime);

        float velocityY_beforeCollision = _velocity_pj.Y;

        HandleVerticalCollisions(deltaTime);

        if (!_isJumping && velocityY_beforeCollision > 0)
        {
            _velocity_pj.Y = 0f; // <-- Esta es la corrección final
        }
    }
    // 2. Nuevo método para colisiones HORIZONTALES
    private void HandleHorizontalCollisions(float deltaTime)
    {
        // Mover en X
        _position_pj.X += _velocity_pj.X * deltaTime;
        UpdateHitbox(); // Actualiza la hitbox a la nueva posición X

        // Comprobar colisiones en X
        foreach (Rectangle rect in _collisionRects)
        {
            if (_davidHitbox.Intersects(rect))
            {
                // ¡Colisión X!
                if (_velocity_pj.X > 0) // Si se movía a la derecha
                {
                    float anchorX = _davidLegs.Origin.X * _davidLegs.Scale.X;
                    _position_pj.X = rect.Left - (_davidHitbox.Width - anchorX);
                }
                else if (_velocity_pj.X < 0) // Si se movía a la izquierda
                {
                    float anchorX = _davidLegs.Origin.X * _davidLegs.Scale.X;
                    _position_pj.X = rect.Right + anchorX;
                }
                _velocity_pj.X = 0; // Detiene el movimiento horizontal
                UpdateHitbox(); // Actualiza la hitbox a la posición X corregida
            }
        }
    }

    // 3. Nuevo método para colisiones VERTICALES
    private void HandleVerticalCollisions(float deltaTime)
    {
        // Mover en Y
        _position_pj.Y += _velocity_pj.Y * deltaTime;
        UpdateHitbox(); // Actualiza la hitbox a la nueva posición Y

        bool isGrounded = false; // Asumir que estamos en el aire

        // Comprobar colisiones en Y
        foreach (Rectangle rect in _collisionRects)
        {
            // Comprueba la intersección con CUALQUIER objeto.
            if (_davidHitbox.Intersects(rect))
            {
                if (_velocity_pj.Y > 0) // Si estaba cayendo (colisión con SUELO)
                {
                    isGrounded = true;
                    _velocity_pj.Y = 0;

                    // Pegado al suelo (Corregido de antes)
                    _position_pj.Y = rect.Top - _constLegsHeight;
                }
                else if (_velocity_pj.Y < 0) // Si estaba subiendo (golpe de CABEZA)
                {
                    _velocity_pj.Y = 0;

                    // Pegado al techo (Corregido de antes)
                    _position_pj.Y = rect.Bottom + _constTorsoHeight;
                }
                UpdateHitbox(); // Actualiza la hitbox a la posición Y corregida
            }
        }

        // --- ACTUALIZAR ESTADO DE SALTO ---
        _isJumping = !isGrounded;
    }
    private void UpdateHitbox()
    {
        // 1. Ajustar el tamaño de la hitbox si está agachado
        if (isDucking)
        {
            // (Asumimos que la hitbox de agachado es 60% de la altura de las piernas)
            _davidHitbox.Height = (int)(_constTorsoHeight + (_constLegsHeight * 0.6f));
        }
        else
        {
            // Altura normal (parado o saltando)
            _davidHitbox.Height = (int)_constHitboxHeight;
        }
        _davidHitbox.Width = (int)_constHitboxWidth;

        // --- 2. Calcular X (Centrado en la Cintura) ---
        // (Posición X) - (Origen X de las Piernas * Escala)
        _davidHitbox.X = (int)(_position_pj.X - (_davidLegs.Origin.X * _davidLegs.Scale.X));

        // --- 3. Calcular Y (Anclado a la Cintura) ---
        // (Posición Y) - (Origen Y del Torso * Escala)
        _davidHitbox.Y = (int)(_position_pj.Y - (_davidChest.Origin.Y * _davidChest.Scale.Y));
    }
  public void handleChestAnimation()
    {
        // 1. Determina cuál debería ser el estado
        string newState;

        if (_isJumping)
        {
            if (isAimingUp) // Prioridad 1: Apuntar arriba (aire)
            {
                if (isShooting) { newState = "shoot-up-torso"; }
                else { newState = "up-torso"; }
            }
            else if (isAimingDown) // Prioridad 2: Apuntar abajo (aire)
            {
                if (isShooting) { newState = "shoot-down-torso"; } 
                else 
                { 
                    // CORRECCIÓN: Tu XML no define "down-torso", así que volvemos
                    // a "jump-torso" si no estás disparando.
                    newState = "jump-torso"; 
                }
            }
            // --- ¡NUEVA LÓGICA AÑADIDA! ---
            else if (isShooting) // Prioridad 3: Disparar horizontal (aire)
            {
                // Usa la misma animación de disparar que en el suelo
                newState = "shoot-torso"; 
            }
            // --- FIN DE LA LÓGICA AÑADIDA ---
            else // Prioridad 4: Salto normal (sin disparar)
            {
                newState = "jump-torso";
            }
        }
        else if (isDucking) // En el suelo y agachado
        {
            if (isShooting)
            {
                if (isMovingHorizontally)
                {
                    newState = "shoot-down-torso"; 
                }
                else
                {
                    newState = "duck-shoot-torso"; 
                }
            }
            else if (isMovingHorizontally)
            {
                newState = "duck-walk-torso";
            }
            else
            {
                newState = "duck-torso";
            }
        }
        else if (isAimingUp) // En el suelo y apuntando arriba
        {
            if (isShooting) { newState = "shoot-up-torso"; }
            else { newState = "up-torso"; }
        }
        else if (isShooting) // En el suelo, disparando de frente
        {
            newState = "shoot-torso";
        }
        else if (isMovingHorizontally) // En el suelo, corriendo
        {
            newState = "run-torso";
        }
        else // En el suelo, quieto
        {
            newState = "idle-torso";
        }


        // 2. ¡LA MAGIA! Solo actualiza si el estado cambió.
        if (newState != _chestState)
        {
            _chestState = newState;

            // --- Lógica de Loop (No necesita cambios) ---
            
            bool shouldLoop = (newState == "idle-torso" ||
                               newState == "run-torso" ||
                               newState == "jump-torso" ||
                               newState == "duck-walk-torso" || 
                               newState == "duck-torso" ||      
                               newState == "up-torso"||
                               newState == "shoot-torso"||
                               newState == "shoot-up-torso"||
                               newState == "shoot-down-torso"
                               );      

            if (_davidChest.Animations.ContainsKey(_chestState))
            {
                _davidChest.Animations[_chestState].IsLooping = shouldLoop;
            }
            
            _davidChest.Play(_chestState);
        }
    }

    public void handleLegsAnimation()
    {
        // 1. Determina cuál debería ser el estado
        string newState;

        if (_isJumping) // <-- Prioridad 1: Saltando
        {
            newState = "jump-legs";
        }
        else if (isDucking) // <-- Prioridad 2: Agachado (maneja duck-walk)
        {
            if (isShooting) { newState = "duck-shoot-legs"; }
            else if (isMovingHorizontally) { newState = "duck-walk-legs"; }
            else { newState = "duck-legs"; }
        }
        else if (isMovingHorizontally) // <-- ¡CAMBIO! Prioridad 3: Correr
        {

            newState = "run-legs"; 
        }
        else if (isAimingUp) // <-- Prioridad 4: Apuntar arriba (quieto)
        {
            newState = "idle-legs";
        }
        else // <-- Prioridad 5: Quieto
        {
            newState = "idle-legs";
        }


        // 2. ¡LA MAGIA! Solo actualiza si el estado cambió.
        if (newState != _legState)
        {
            _legState = newState;

            // --- Lógica de Loop (ya estaba bien) ---
            bool shouldLoop = (newState == "idle-legs" ||
                               newState == "run-legs" ||
                               newState == "jump-legs" ||
                               newState == "duck-walk-legs" ||
                               newState == "duck-legs");

            if (_davidLegs.Animations.ContainsKey(_legState))
            {
                _davidLegs.Animations[_legState].IsLooping = shouldLoop;
            }

            _davidLegs.Play(_legState);
        }
    }
    private void handleSpiderAnimation()
    {
        // Por ahora, la araña siempre está en este estado
        string newState = "spider_vomit";

        // Solo llamamos a .Play() si el estado ha cambiado
        if (newState != _spiderState)
        {
            _spiderState = newState;
            _spiderAnimation.Play(_spiderState);
        }
    }
    private void DibujarBordeRectangulo(Rectangle rect, Color color, int grosor)
    {
        Core.SpriteBatch.Draw(_texturaDebug, new Rectangle(rect.Left, rect.Top, rect.Width, grosor), color);
        Core.SpriteBatch.Draw(_texturaDebug, new Rectangle(rect.Left, rect.Bottom - grosor, rect.Width, grosor), color);
        Core.SpriteBatch.Draw(_texturaDebug, new Rectangle(rect.Left, rect.Top, grosor, rect.Height), color);
        Core.SpriteBatch.Draw(_texturaDebug, new Rectangle(rect.Right - grosor, rect.Top, grosor, rect.Height), color);
    }



    public override void Draw(GameTime gameTime)
    {
        Core.GraphicsDevice.Clear(Color.Black);

        Core.SpriteBatch.Begin(
          samplerState: SamplerState.PointClamp,
          transformMatrix: _camera.GetViewMatrix(Core.GraphicsDevice.Viewport)
        );

        Core.SpriteBatch.Draw(_background, Vector2.Zero, Color.White);
        _spiderAnimation.Draw(Core.SpriteBatch, _spiderPosition);

        Vector2 finalDrawPosition = _position_pj;

        // Ajuste vertical por agacharse (Esta lógica está bien)
        if (isDucking)
        {
            float scaledStandingLegsHeight = _constLegsHeight;
            float scaledDuckingLegsVisualHeight = _constLegsHeight * 0.6f;
            float yOffset = scaledStandingLegsHeight - scaledDuckingLegsVisualHeight;
            finalDrawPosition.Y += yOffset;
        }

        // --- ¡¡LÓGICA DE DIBUJO SIMPLIFICADA!! ---

        // 1. Obtener los nombres de los fotogramas (¡Con protección anti-crash!)
        string legFrameName = _davidLegs.Region?.Name ?? "";
        string torsoFrameName = _davidChest.Region?.Name ?? "";

        // 2. Obtener los offsets BASE (para ajustes manuales finos)
        Vector2 legOffsetFromDict = _legsFrameOffsets.GetValueOrDefault(legFrameName, Vector2.Zero);
        Vector2 torsoOffsetFromDict = _torsoFrameOffsets.GetValueOrDefault(torsoFrameName, Vector2.Zero);

        // 3. ¡LISTO! El 'Origin' (Píxel 11) se alineará con 'finalDrawPosition'
        // Solo necesitamos sumar el offset del diccionario

        Vector2 legsDrawPos = finalDrawPosition + (legOffsetFromDict * _davidLegs.Scale);
        Vector2 chestDrawPos = finalDrawPosition + (torsoOffsetFromDict * _davidChest.Scale);

        // 4. Dibujar
        // (MonoGame alinea _davidLegs.Origin con legsDrawPos automáticamente)
        _davidLegs.Draw(Core.SpriteBatch, legsDrawPos);
        _davidChest.Draw(Core.SpriteBatch, chestDrawPos);


        // --- DIBUJO DE DEBUG ---
        if (_isJumping)
            DibujarBordeRectangulo(_davidHitbox, Color.Red, 2);
        else
            DibujarBordeRectangulo(_davidHitbox, Color.LimeGreen, 2);

        DibujarBordeRectangulo(_spiderHitbox, Color.Red, 2);
        Core.SpriteBatch.End();
    }
}