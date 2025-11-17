using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGameLibrary.Graphics;
using MonoGameLibrary.Input;

// Puedes poner esta clase en un nuevo namespace, ej. reckless_and_gun.EntitieCgh
namespace reckless_and_gun.Entities;

public class David
{
    // --- Sprites (Composición) ---
    // David "tiene" dos sprites, no "es" un sprite.
    private AnimatedSprite _davidLegs;
    private AnimatedSprite _davidChest;

    // --- Atributos de David ---
    // (Reemplazan a _position_pj y _velocity_pj)
    private Vector2 _position;
    private Vector2 _velocity;
    public float Health; // (Ejemplo, aún no lo usas)

    // --- Propiedades Públicas (para la Cámara y Colisiones) ---
    public Vector2 Position => _position; // La CINTURA del personaje
    public Rectangle Hitbox { get; private set; }

    // --- Constantes de Física (Internas) ---
    private const float _speed = 150f;
    private const float _jumpSpeed = -500f; // Negativo (hacia arriba)
    private const float _gravity = 1500f;

    // --- Variables de Física Constante (Calculadas) ---
    private float _constLegsHeight;
    private float _constTorsoHeight;
    private float _constHitboxWidth;
    private float _constHitboxHeight;

    // --- Offsets de Animación ---
    private Dictionary<string, Vector2> _torsoFrameOffsets;
    private Dictionary<string, Vector2> _legsFrameOffsets;

    // --- Variables de Estado (FSM) ---
    private string _legState = "idle-legs";
    private string _chestState = "idle-torso";
    private bool _isJumping;
    private bool isDucking;
    private bool isShooting;
    private bool isMovingHorizontally;
    private bool isAimingUp;
    private bool isAimingDown;
    private bool _isDuckingTransitionDone = false;
    private bool _isStandingJump = false;

    // --- Debug ---
    public Texture2D DebugTexture { get; set; } // La GameScene nos pasa esto

    // --- Constructor e Inicialización ---
    public David()
    {
        _velocity = Vector2.Zero;
        _isJumping = true; // Empieza en el aire
    }

    public void LoadContent(TextureAtlas atlas, Vector2 startPosition)
    {
        _davidChest = atlas.CreateAnimatedSprite("idle-torso");
        _davidLegs = atlas.CreateAnimatedSprite("idle-legs");

        _davidLegs.Scale = new Vector2(2.0f, 2.0f);
        _davidChest.Scale = new Vector2(2.0f, 2.0f);

        // --- Se define el ORIGEN (Pivote) BASE UNA SOLA VEZ ---
        float pivotX = 11f;
        _davidLegs.Origin = new Vector2(pivotX, 0f);
        _davidChest.Origin = new Vector2(pivotX, _davidChest.Region.Height);

        // --- Calculamos constantes ---
        _constLegsHeight = _davidLegs.Region.Height * _davidLegs.Scale.Y;
        _constTorsoHeight = _davidChest.Region.Height * _davidChest.Scale.Y;
        _constHitboxWidth = _davidLegs.Region.Width * _davidLegs.Scale.X;
        _constHitboxHeight = _constLegsHeight + _constTorsoHeight;

        // --- Posición Inicial ---
        _position = startPosition;

        // --- Offsets de Torso ---
        _torsoFrameOffsets = new Dictionary<string, Vector2>()
            {
                { "torso_idle_0", new Vector2(0f, 0f) },
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
                { "torso_duck_Animated_0", new Vector2(0f, 2f) },
                { "torso_duck_Animated_1", new Vector2(0f, 13f) },
                { "torso_duck_0", new Vector2(0f, 13f) },
                { "torso_duck_shoot_0", new Vector2(0f, 13f) },
                { "torso_duck_shoot_1", new Vector2(0f, 13f) },
                { "torso_duck_shoot_2", new Vector2(0f, 13f) },
                { "torso_duck_walk_0", new Vector2(0f, 13f) },
                { "torso_duck_walk_1", new Vector2(0f, 13f) },
                { "torso_duck_walk_2", new Vector2(0f, 14f) },
                { "torso_duck_walk_3", new Vector2(0f, 13f) },
                { "torso_duck_walk_4", new Vector2(0f, 13f) }
            };

        // --- Offsets de Piernas ---
        _legsFrameOffsets = new Dictionary<string, Vector2>()
            {
                { "legs_idle_0", new Vector2(0f, 0f) },
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
                { "legs_duck_0", new Vector2(0f,4f) },
                { "legs_duck_1", new Vector2(0f, 8f) },
                { "legs_duck_shoot_0", new Vector2(0f, 8f) },
                { "legs_duck_shoot_1", new Vector2(0f, 8f) },
                { "legs_duck_shoot_2", new Vector2(0f, 8f) },
                { "legs_duck_walk_0", new Vector2(0f, 8f) },
                { "legs_duck_walk_1", new Vector2(0f, 8f) },
                { "legs_duck_walk_2", new Vector2(0f, 8f) },
                { "legs_duck_walk_3", new Vector2(0f, 8f) },
                { "legs_duck_walk_4", new Vector2(0f, 8f) },
                { "legs_jump_0", new Vector2(0f, 0f) },
                { "legs_jump_1", new Vector2(0f, 0f) },
                { "legs_jump_2", new Vector2(0f, 0f) },
                { "legs_jump_3", new Vector2(0f, 0f) },
                { "legs_jump_4", new Vector2(0f, 0f) },
                { "legs_jump_5", new Vector2(0f, 0f) },
            };

        UpdateHitbox(); // Calcula la hitbox inicial
    }

    // --- Bucle Principal de David ---

    public void Update(GameTime gameTime, KeyboardInfo keyboard, List<Rectangle> collisionRects)
    {
        HandleInput(keyboard);
        ApplyPhysics(gameTime, collisionRects);
        handleLegsAnimation();
        handleChestAnimation();

        _davidLegs.Update(gameTime);
        _davidChest.Update(gameTime);

        // Lógica de transición de agachado
        if (!_isDuckingTransitionDone &&
            (_davidLegs.CurrentAnimationName == "duck-legs-animated" ||
             _davidChest.CurrentAnimationName == "duck-torso-animated"))
        {
            if (_davidLegs.IsAnimationFinished || _davidChest.IsAnimationFinished)
            {
                _isDuckingTransitionDone = true;
            }
        }
    }

    // --- Manejo de Input ---
    private void HandleInput(KeyboardInfo keyboard)
    {
        // La lógica de 'Escape' para salir la dejamos en GameScene

        // Reset states
        isMovingHorizontally = false;
        isDucking = false;

        if (!keyboard.IsKeyDown(Keys.S))
        {
            _isDuckingTransitionDone = false;
        }

        isShooting = false;
        isAimingUp = false;
        isAimingDown = false;

        // 1. Check Aiming/Ducking states
        if (keyboard.IsKeyDown(Keys.W))
        { isAimingUp = true; }

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

        // 3. Check Actions
        if (keyboard.IsKeyDown(Keys.H))
        { isShooting = true; }

        if (keyboard.IsKeyDown(Keys.J) && !_isJumping && !isDucking)
        {
            _velocity.Y = _jumpSpeed;
            _isJumping = true;

            if (!isMovingHorizontally)
            {
                _isStandingJump = true;
            }
            else
            {
                _isStandingJump = false; // Es un salto corriendo
            }
        }
    }

    // --- Física y Colisiones ---
    private void ApplyPhysics(GameTime gameTime, List<Rectangle> collisionRects)
    {
        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

        // 1. ACTUALIZAR VELOCIDAD X
        if (isMovingHorizontally)
        {
            _velocity.X = (_davidLegs.Effects == SpriteEffects.FlipHorizontally) ? -_speed : _speed;
        }
        else
        {
            _velocity.X = 0; // Detenerse
        }

        // 2. APLICAR GRAVEDAD (Y)
        if (!_isJumping)
        {
            _velocity.Y = 0f;
        }
        else
        {
            _velocity.Y += _gravity * deltaTime;
        }

        _velocity.Y = MathHelper.Clamp(_velocity.Y, _jumpSpeed, _gravity * 2f);

        // 3. MANEJAR COLISIONES POR EJE
        HandleHorizontalCollisions(deltaTime, collisionRects);
        HandleVerticalCollisions(deltaTime, collisionRects);
    }

    private void HandleHorizontalCollisions(float deltaTime, List<Rectangle> collisionRects)
    {
        _position.X += _velocity.X * deltaTime;
        UpdateHitbox();

        foreach (Rectangle rect in collisionRects)
        {
            bool isWall = rect.Height > rect.Width;

            if (isWall && Hitbox.Intersects(rect))
            {
                if (_velocity.X > 0) // Derecha
                {
                    float anchorX = _davidLegs.Origin.X * _davidLegs.Scale.X;
                    _position.X = rect.Left - (Hitbox.Width - anchorX);
                }
                else if (_velocity.X < 0) // Izquierda
                {
                    float anchorX = _davidLegs.Origin.X * _davidLegs.Scale.X;
                    _position.X = rect.Right + anchorX;
                }
                _velocity.X = 0;
                UpdateHitbox();
            }
        }
    }

    private void HandleVerticalCollisions(float deltaTime, List<Rectangle> collisionRects)
    {
        _position.Y += _velocity.Y * deltaTime;
        UpdateHitbox();

        bool isGrounded = false;

        foreach (Rectangle rect in collisionRects)
        {
            bool isGround = rect.Width > rect.Height;

            if (isGround && Hitbox.Intersects(rect))
            {
                if (_velocity.Y >= 0) // Cayendo o en suelo
                {
                    isGrounded = true;
                    _velocity.Y = 0;
                    float desiredBottom = rect.Top + 1;
                    _position.Y = desiredBottom - _constLegsHeight;
                }
                else if (_velocity.Y < 0) // Golpeando techo
                {
                    _velocity.Y = 0;
                    _position.Y = rect.Bottom + _constTorsoHeight;
                }
                UpdateHitbox();
            }
        }

        _isJumping = !isGrounded;
        if (!_isJumping)
        {
            _isStandingJump = false;
        }
    }

    // --- Actualización de Hitbox ---
    private void UpdateHitbox()
    {
        float currentHitboxHeight;
        float yOffset = 0f;

        if (isDucking)
        {
            float duckingLegsHeight = _constLegsHeight * 0.6f;
            currentHitboxHeight = _constTorsoHeight + duckingLegsHeight;
            yOffset = _constHitboxHeight - currentHitboxHeight;
        }
        else
        {
            currentHitboxHeight = _constHitboxHeight;
        }

        int width = (int)_constHitboxWidth;
        int height = (int)currentHitboxHeight;

        float anchorX = _davidLegs.Origin.X * _davidLegs.Scale.X;
        int x = (int)(_position.X - anchorX);

        float torsoAnchorY = _davidChest.Origin.Y * _davidChest.Scale.Y;
        int y = (int)((_position.Y - torsoAnchorY) + yOffset);

        Hitbox = new Rectangle(x, y, width, height);
    }

    // --- Lógica de Animación ---
    public void handleChestAnimation()
    {
        string newState;
        if (_isJumping)
        {
            if (_isStandingJump)
            {
                if (isAimingUp)
                {
                    if (isShooting) { newState = "shoot-up-torso"; }
                    else { newState = "up-torso"; }
                }
                else if (isAimingDown)
                {
                    if (isShooting) { newState = "shoot-down-torso"; }
                    else { newState = "down-torso"; }
                }
                else if (isShooting)
                {
                    newState = "shoot-torso";
                }
                else
                {
                    newState = "jump-torso";
                }
            }
            else
            {
                if (isAimingUp)
                {
                    if (isShooting) { newState = "shoot-up-torso"; }
                    else { newState = "up-torso"; }
                }
                else if (isAimingDown)
                {
                    if (isShooting) { newState = "shoot-down-torso"; }
                    else { newState = "down-torso"; }
                }
                else if (isShooting)
                {
                    newState = "shoot-torso";
                }
                else
                {
                    newState = "run-torso";
                }
            }
        }
        else if (isDucking)
        {
            if (isShooting && !_isDuckingTransitionDone)
            {
                _isDuckingTransitionDone = true;
            }

            if (!_isDuckingTransitionDone)
            {
                newState = "duck-torso-animated";
            }
            else if (isMovingHorizontally)
            {
                newState = "duck-walk-torso";
            }
            else if (isShooting)
            {
                newState = "duck-shoot-torso";
            }
            else
            {
                newState = "duck-torso";
            }
        }
        else if (isAimingUp)
        {
            if (isShooting) { newState = "shoot-up-torso"; }
            else { newState = "up-torso"; }
        }
        else if (isShooting)
        {
            newState = "shoot-torso";
        }
        else if (isMovingHorizontally)
        {
            newState = "run-torso";
        }
        else
        {
            newState = "idle-torso";
        }

        if (newState != _chestState)
        {
            _chestState = newState;
            bool shouldLoop = (newState == "idle-torso" ||
                               newState == "run-torso" ||
                               newState == "jump-torso" ||
                               newState == "duck-walk-torso" ||
                               newState == "up-torso" ||
                               newState == "shoot-torso" ||
                               newState == "shoot-up-torso" ||
                               newState == "shoot-down-torso" ||
                               newState == "duck-shoot-torso"
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
        string newState;
        if (_isJumping)
        {
            if (_isStandingJump)
            {
                newState = "jump-legs";
            }
            else
            {
                newState = "jump-legs_run";
            }
        }
        else if (isDucking)
        {
            if (isShooting && !_isDuckingTransitionDone)
            {
                _isDuckingTransitionDone = true;
            }

            if (!_isDuckingTransitionDone)
            {
                newState = "duck-legs-animated";
            }
            else if (isMovingHorizontally)
            {
                newState = "duck-walk-legs";
            }
            else if (isShooting)
            {
                newState = "duck-shoot-legs";
            }
            else
            {
                newState = "duck-legs";
            }
        }
        else if (isMovingHorizontally)
        {
            newState = "run-legs";
        }
        else if (isAimingUp)
        {
            newState = "idle-legs";
        }
        else
        {
            newState = "idle-legs";
        }

        if (newState != _legState)
        {
            _legState = newState;
            bool shouldLoop = (newState == "idle-legs" ||
                               newState == "run-legs" ||
                               newState == "jump-legs" ||
                               newState == "duck-walk-legs"
            );

            if (_davidLegs.Animations.ContainsKey(_legState))
            {
                _davidLegs.Animations[_legState].IsLooping = shouldLoop;
            }
            _davidLegs.Play(_legState);
        }
    }

    // --- Dibujado ---

    public void Draw(SpriteBatch spriteBatch)
    {
        // 1. Obtener los nombres de los fotogramas
        string legFrameName = _davidLegs.Region?.Name ?? "";
        string torsoFrameName = _davidChest.Region?.Name ?? "";

        // 2. Obtener los offsets BASE
        Vector2 legOffsetFromDict = _legsFrameOffsets.GetValueOrDefault(legFrameName, Vector2.Zero);
        Vector2 torsoOffsetFromDict = _torsoFrameOffsets.GetValueOrDefault(torsoFrameName, Vector2.Zero);

        // 3. Calcular posiciones finales de dibujo
        Vector2 legsDrawPos = _position + (legOffsetFromDict * _davidLegs.Scale);
        Vector2 chestDrawPos = _position + (torsoOffsetFromDict * _davidChest.Scale);

        // 4. Dibujar
        _davidLegs.Draw(spriteBatch, legsDrawPos);
        _davidChest.Draw(spriteBatch, chestDrawPos);

        // 5. Dibujo de Debug (si la textura fue asignada)
        if (DebugTexture != null)
        {
            Color debugColor = _isJumping ? Color.Red : Color.LimeGreen;
            DibujarBordeRectangulo(spriteBatch, DebugTexture, Hitbox, debugColor, 2);
        }
    }

    // Helper de Debug (movido aquí)
    private void DibujarBordeRectangulo(SpriteBatch spriteBatch, Texture2D debugTexture, Rectangle rect, Color color, int grosor)
    {
        spriteBatch.Draw(debugTexture, new Rectangle(rect.Left, rect.Top, rect.Width, grosor), color);
        spriteBatch.Draw(debugTexture, new Rectangle(rect.Left, rect.Bottom - grosor, rect.Width, grosor), color);
        spriteBatch.Draw(debugTexture, new Rectangle(rect.Left, rect.Top, grosor, rect.Height), color);
        spriteBatch.Draw(debugTexture, new Rectangle(rect.Right - grosor, rect.Top, grosor, rect.Height), color);
    }
}