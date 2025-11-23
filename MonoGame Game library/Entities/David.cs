using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGameLibrary;
using MonoGameLibrary.Graphics;
using MonoGameLibrary.Input;
using reckless_and_gun.Entities;
using reckless_and_gun.Scenes;

namespace reckless_and_gun.Entities;

public class David
{
    private AnimatedSprite _davidLegs;
    private AnimatedSprite _davidChest;

    private Vector2 _position;
    private Vector2 _velocity;
    public float Health;
    public Vector2 Position => _position;
    public Rectangle Hitbox { get; private set; }

    // Constantes de movimiento
    private const float _speed = 150f;
    private const float _jumpSpeed = -500f;
    private const float _gravity = 1500f;

    // Constantes de dimensiones
    private float _constLegsHeight;
    private float _constTorsoHeight;
    private float _constHitboxWidth;
    private float _constHitboxHeight;

    private Dictionary<string, Vector2> _torsoFrameOffsets;
    private Dictionary<string, Vector2> _legsFrameOffsets;

    // Estados
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

    // Disparo
    private TimeSpan _fireRate = TimeSpan.FromMilliseconds(200);
    private TimeSpan _fireCooldownTimer = TimeSpan.Zero;

    // DEBUG
    public Texture2D DebugTexture { get; set; }

    // =================================================================
    // 🛠️ ZONA DE CONFIGURACIÓN DE DISPAROS 🛠️
    // =================================================================
    // Ajusta estos valores. El primer número es X (Horizontal), el segundo es Y (Vertical).

    // --- CUANDO MIRA A LA DERECHA --->
    private Vector2 _offsetRight_Normal = new Vector2(80f, -20f);  // Parado/Corriendo
    private Vector2 _offsetRight_Duck = new Vector2(76f, -15f);    // Agachado
    private Vector2 _offsetRight_Up = new Vector2(46f, -25f);  // Apuntando Arriba
    private Vector2 _offsetRight_Down = new Vector2(52f, 0f);   // Apuntando Abajo (aire)

    // --- CUANDO MIRA A LA IZQUIERDA <---
    // (Usa números negativos en X para ir hacia atrás)
    private Vector2 _offsetLeft_Normal = new Vector2(20f, -20f); // Parado/Corriendo
    private Vector2 _offsetLeft_Duck = new Vector2(24f, -15f);   // Agachado
    private Vector2 _offsetLeft_Up = new Vector2(49f, -25f); // Apuntando Arriba
    private Vector2 _offsetLeft_Down = new Vector2(47f, 0f);  // Apuntando Abajo (aire)
    // =================================================================

    public David()
    {
        _velocity = Vector2.Zero;
        _isJumping = true;
    }

    public void LoadContent(TextureAtlas atlas, Vector2 startPosition)
    {
        _davidChest = atlas.CreateAnimatedSprite("idle-torso");
        _davidLegs = atlas.CreateAnimatedSprite("idle-legs");

        _davidLegs.Scale = new Vector2(2.0f, 2.0f);
        _davidChest.Scale = new Vector2(2.0f, 2.0f);

        // PIVOTE (El culpable de que el espejo automático fallara)
        float pivotX = 11f;
        _davidLegs.Origin = new Vector2(pivotX, 0f);
        _davidChest.Origin = new Vector2(pivotX, _davidChest.Region.Height);

        _constLegsHeight = _davidLegs.Region.Height * _davidLegs.Scale.Y;
        _constTorsoHeight = _davidChest.Region.Height * _davidChest.Scale.Y;
        _constHitboxWidth = _davidLegs.Region.Width * _davidLegs.Scale.X;
        _constHitboxHeight = _constLegsHeight + _constTorsoHeight;

        _position = startPosition;

        // Offsets de animación (para que el sprite no vibre)
        CargarOffsetsDeAnimacion();

        UpdateHitbox();
    }

    public void Update(GameTime gameTime, KeyboardInfo keyboard, List<Rectangle> collisionRects)
    {
        HandleInput(keyboard);
        ApplyPhysics(gameTime, collisionRects);
        System.Diagnostics.Debug.WriteLine($"David Pos: {this.Position.X} | Salida en: 2800");


        if (_fireCooldownTimer > TimeSpan.Zero)
        {
            _fireCooldownTimer -= gameTime.ElapsedGameTime;
        }
        handleLegsAnimation();
        handleChestAnimation();

        _davidLegs.Update(gameTime);
        _davidChest.Update(gameTime);
    }

    public Projectile TryShoot(TextureAtlas projectilesAtlas)
    {
        if (isShooting && _fireCooldownTimer <= TimeSpan.Zero)
        {
            _fireCooldownTimer = _fireRate;

            var facingEffect = _davidLegs.Effects;
            bool mirandoDerecha = (facingEffect != SpriteEffects.FlipHorizontally);
            float scale = _davidLegs.Scale.X;

            // Ajuste de animación del torso (para que la bala suba/baje con la animación)
            string torsoFrameName = _davidChest.Region?.Name ?? "torso_idle_0";
            Vector2 torsoAnimOffset = _torsoFrameOffsets.GetValueOrDefault(torsoFrameName, Vector2.Zero);

            // 1. Definir Dirección de la bala
            Vector2 direction;
            if (isAimingUp && !isDucking)
            {
                direction = -Vector2.UnitY;
            }
            else if (isAimingDown)
            {
                direction = Vector2.UnitY;
            }
            else
            {
                // Si está agachado (aunque apriete arriba) o no apunta a nada, sale recta.
                direction = new Vector2(mirandoDerecha ? 1f : -1f, 0);
            }

            direction.Normalize();

            direction.Normalize();

            // 2. Seleccionar la posición EXACTA (Offset)
            Vector2 muzzleOffset = Vector2.Zero;

            if (mirandoDerecha)
            {
                // --- DERECHA ---
                if (isDucking) muzzleOffset = _offsetRight_Duck;
                else if (isAimingUp) muzzleOffset = _offsetRight_Up;
                else if (isAimingDown && _isJumping) muzzleOffset = _offsetRight_Down;
                else muzzleOffset = _offsetRight_Normal; // Normal
            }
            else
            {
                // --- IZQUIERDA ---
                if (isDucking) muzzleOffset = _offsetLeft_Duck;
                else if (isAimingUp) muzzleOffset = _offsetLeft_Up;
                else if (isAimingDown && _isJumping) muzzleOffset = _offsetLeft_Down;
                else muzzleOffset = _offsetLeft_Normal; // Normal
            }

            // 3. Calcular posición final
            Vector2 spawnPosition = this.Position +
                                    (muzzleOffset * scale) +
                                    (torsoAnimOffset * scale);

            // 4. Crear Bala
            Sprite bulletSprite = projectilesAtlas.CreateSprite("Pistol_Bullet");
            bulletSprite.Origin = new Vector2(bulletSprite.Region.Width / 2f, bulletSprite.Region.Height / 2f);
            bulletSprite.Scale = new Vector2(2f, 2f);
            var newBullet = new PistolBullet(bulletSprite, spawnPosition, direction);
            newBullet.DebugTexture = this.DebugTexture;

            return newBullet;
        }
        return null;
    }

    // --- Helper para calcular dónde saldría la bala sin disparar (para Debug) ---
    private Vector2 GetMuzzlePosition()
    {
        var facingEffect = _davidLegs.Effects;
        bool mirandoDerecha = (facingEffect != SpriteEffects.FlipHorizontally);
        float scale = _davidLegs.Scale.X;
        string torsoFrameName = _davidChest.Region?.Name ?? "torso_idle_0";
        Vector2 torsoAnimOffset = _torsoFrameOffsets.GetValueOrDefault(torsoFrameName, Vector2.Zero);

        Vector2 muzzleOffset = Vector2.Zero;
        if (mirandoDerecha)
        {
            if (isDucking) muzzleOffset = _offsetRight_Duck;
            else if (isAimingUp) muzzleOffset = _offsetRight_Up;
            else if (isAimingDown && _isJumping) muzzleOffset = _offsetRight_Down;
            else muzzleOffset = _offsetRight_Normal;
        }
        else
        {
            if (isDucking) muzzleOffset = _offsetLeft_Duck;
            else if (isAimingUp) muzzleOffset = _offsetLeft_Up;
            else if (isAimingDown && _isJumping) muzzleOffset = _offsetLeft_Down;
            else muzzleOffset = _offsetLeft_Normal;
        }

        return this.Position + (muzzleOffset * scale) + (torsoAnimOffset * scale);
    }


    private void HandleInput(KeyboardInfo keyboard)
    {
        // Reset states
        isMovingHorizontally = false;
        isDucking = false;
        
        if (keyboard.WasKeyJustPressed(Keys.P))
        {
            Core.ChangeScene(new JungleScene());
            return;
        }


        if (!keyboard.IsKeyDown(Keys.S))
        {
            _isDuckingTransitionDone = false;
        }

        isShooting = false;
        isAimingUp = false;
        isAimingDown = false;

        // 1. Check Aiming/Ducking states
        if (keyboard.IsKeyDown(Keys.W)) { isAimingUp = true; }

        if (keyboard.IsKeyDown(Keys.S))
        {
            if (!_isJumping) isDucking = true; // En el suelo, S = Agacharse
            else isAimingDown = true; // En el aire, S = Apuntar Abajo
        }

        // 2. Movimiento
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
        if (keyboard.IsKeyDown(Keys.H)) { isShooting = true; }

        if (keyboard.IsKeyDown(Keys.J) && !_isJumping && !isDucking)
        {
            _velocity.Y = _jumpSpeed;
            _isJumping = true;
            _isStandingJump = !isMovingHorizontally;
        }

        if (isDucking && isShooting)
        {
            isMovingHorizontally = false;
        }
    }
    private void ApplyPhysics(GameTime gameTime, List<Rectangle> collisionRects)
    {
        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

        if (isMovingHorizontally)
            _velocity.X = (_davidLegs.Effects == SpriteEffects.FlipHorizontally) ? -_speed : _speed;
        else
            _velocity.X = 0;

        if (!_isJumping) _velocity.Y = 0f;
        else _velocity.Y += _gravity * deltaTime;

        _velocity.Y = MathHelper.Clamp(_velocity.Y, _jumpSpeed, _gravity * 2f);

        HandleHorizontalCollisions(deltaTime, collisionRects);
        HandleVerticalCollisions(deltaTime, collisionRects);
    }

    private void HandleHorizontalCollisions(float deltaTime, List<Rectangle> collisionRects)
    {
        _position.X += _velocity.X * deltaTime;
        UpdateHitbox();
        foreach (Rectangle rect in collisionRects)
        {
            if (rect.Height > rect.Width && Hitbox.Intersects(rect))
            {
                float anchorX = _davidLegs.Origin.X * _davidLegs.Scale.X;
                if (_velocity.X > 0) _position.X = rect.Left - (Hitbox.Width - anchorX);
                else if (_velocity.X < 0) _position.X = rect.Right + anchorX;
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
            if (rect.Width > rect.Height && Hitbox.Intersects(rect))
            {
                if (_velocity.Y >= 0)
                {
                    isGrounded = true;
                    _velocity.Y = 0;
                    _position.Y = rect.Top + 1 - _constLegsHeight;
                }
                else if (_velocity.Y < 0)
                {
                    _velocity.Y = 0;
                    _position.Y = rect.Bottom + _constTorsoHeight;
                }
                UpdateHitbox();
            }
        }
        _isJumping = !isGrounded;
        if (!_isJumping) _isStandingJump = false;
    }

    private void UpdateHitbox()
    {
        float currentHitboxHeight = isDucking
            ? _constTorsoHeight + (_constLegsHeight * 0.6f)
            : _constHitboxHeight;

        float yOffset = _constHitboxHeight - currentHitboxHeight;
        float anchorX = _davidLegs.Origin.X * _davidLegs.Scale.X;
        float torsoAnchorY = _davidChest.Origin.Y * _davidChest.Scale.Y;

        Hitbox = new Rectangle(
            (int)(_position.X - anchorX),
            (int)((_position.Y - torsoAnchorY) + yOffset),
            (int)_constHitboxWidth,
            (int)currentHitboxHeight
        );
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        string legFrameName = _davidLegs.Region?.Name ?? "";
        string torsoFrameName = _davidChest.Region?.Name ?? "";

        Vector2 legOffset = _legsFrameOffsets.GetValueOrDefault(legFrameName, Vector2.Zero);
        Vector2 torsoOffset = _torsoFrameOffsets.GetValueOrDefault(torsoFrameName, Vector2.Zero);

        _davidLegs.Draw(spriteBatch, _position + (legOffset * _davidLegs.Scale));
        _davidChest.Draw(spriteBatch, _position + (torsoOffset * _davidChest.Scale));

        if (DebugTexture != null)
        {
            // Dibujar Hitbox
            Color debugColor = _isJumping ? Color.Red : Color.LimeGreen;
            DibujarBordeRectangulo(spriteBatch, DebugTexture, Hitbox, debugColor, 2);

            // 🔴 DIBUJAR PUNTO DE SALIDA DE BALA (DEBUG)
            // Esto te mostrará un cuadrado rojo pequeño de donde saldrá la bala.
            Vector2 muzzlePos = GetMuzzlePosition();
            spriteBatch.Draw(DebugTexture, new Rectangle((int)muzzlePos.X - 2, (int)muzzlePos.Y - 2, 4, 4), Color.Red);
        }
    }

    private void DibujarBordeRectangulo(SpriteBatch spriteBatch, Texture2D tex, Rectangle rect, Color color, int grosor)
    {
        spriteBatch.Draw(tex, new Rectangle(rect.Left, rect.Top, rect.Width, grosor), color);
        spriteBatch.Draw(tex, new Rectangle(rect.Left, rect.Bottom - grosor, rect.Width, grosor), color);
        spriteBatch.Draw(tex, new Rectangle(rect.Left, rect.Top, grosor, rect.Height), color);
        spriteBatch.Draw(tex, new Rectangle(rect.Right - grosor, rect.Top, grosor, rect.Height), color);
    }

    public void handleChestAnimation()
    {
        string newState = "idle-torso";

        if (_isJumping)
        {
            if (_isStandingJump)
            {
                if (isAimingUp) newState = isShooting ? "shoot-up-torso" : "up-torso";
                else if (isAimingDown) newState = isShooting ? "shoot-down-torso" : "down-torso";
                else newState = isShooting ? "shoot-torso" : "jump-torso";
            }
            else
            {
                if (isAimingUp) newState = isShooting ? "shoot-up-torso" : "up-torso";
                else if (isAimingDown) newState = isShooting ? "shoot-down-torso" : "down-torso";
                else newState = isShooting ? "shoot-torso" : "run-torso";
            }
        }
        else if (isDucking)
        {
            // PRIORIDAD 1: DISPARAR (Quieto)
            // Gracias a , si entra aquí, isMovingHorizontally ya es falso.
            if (isShooting)
            {
                newState = "duck-shoot-torso";
                _isDuckingTransitionDone = true;
            }
            // PRIORIDAD 2: CAMINAR (Solo si no dispara)
            else if (isMovingHorizontally)
            {
                newState = "duck-walk-torso";
                _isDuckingTransitionDone = true;
            }
            // PRIORIDAD 3: TRANSICIÓN DE BAJADA
            else if (!_isDuckingTransitionDone)
            {
                newState = "duck-torso-animated";
                if (_chestState == "duck-torso-animated" && _davidChest.IsAnimationFinished)
                {
                    _isDuckingTransitionDone = true;
                }
            }
            // PRIORIDAD 4: IDLE (Quieto agachado)
            else
            {
                newState = "duck-torso";
            }
        }
        else if (isAimingUp) newState = isShooting ? "shoot-up-torso" : "up-torso";
        else if (isShooting) newState = "shoot-torso";
        else if (isMovingHorizontally) newState = "run-torso";

        if (newState != _chestState)
        {
            _chestState = newState;
            bool loop = (newState != "duck-torso-animated");
            if (_davidChest.Animations.ContainsKey(_chestState)) _davidChest.Animations[_chestState].IsLooping = loop;
            _davidChest.Play(_chestState);
        }
    }

    public void handleLegsAnimation()
    {
        string newState = "idle-legs";
        if (_isJumping) newState = _isStandingJump ? "jump-legs" : "jump-legs_run";
        else if (isDucking)
        {
            if (isShooting)
            {
                newState = "duck-shoot-legs";
                _isDuckingTransitionDone = true;
            }
            else if (isMovingHorizontally)
            {
                newState = "duck-walk-legs";
                _isDuckingTransitionDone = true;
            }
            else if (!_isDuckingTransitionDone)
            {
                newState = "duck-legs-animated";
            }
            else
            {
                newState = "duck-legs";
            }
        }
        else if (isMovingHorizontally) newState = "run-legs";

        if (newState != _legState)
        {
            _legState = newState;
            if (_davidLegs.Animations.ContainsKey(_legState)) _davidLegs.Animations[_legState].IsLooping = true;
            _davidLegs.Play(_legState);
        }
    }
    private void CargarOffsetsDeAnimacion()
    {
        _torsoFrameOffsets = new Dictionary<string, Vector2>() {
            { "torso_idle_0", Vector2.Zero }, { "torso_run_0", Vector2.Zero }, { "torso_run_1", Vector2.Zero },
            { "torso_jump_0", Vector2.Zero }, { "torso_jump_1", Vector2.Zero }, { "torso_jump_2", Vector2.Zero },
            { "torso_jump_3", Vector2.Zero }, { "torso_jump_4", Vector2.Zero }, { "torso_shoot_0", Vector2.Zero },
            { "torso_shoot_1", Vector2.Zero }, { "torso_shoot_2", Vector2.Zero }, { "torso_shoot_3", Vector2.Zero },
            { "torso_down_0", new Vector2(0f, 10f) }, { "torso_shoot_down_0", new Vector2(0f, 10f) },
            { "torso_shoot_down_1", new Vector2(0f, 10f) }, { "torso_shoot_down_2", new Vector2(0f, 10f) },
            { "torso_shoot_down_3", new Vector2(0f, 10f) }, { "torso_up_0", new Vector2(0f, -3f) },
            { "torso_shoot_up_0", new Vector2(0f, -30f) }, { "torso_shoot_up_1", new Vector2(0f, -30f) },
            { "torso_shoot_up_2", new Vector2(0f, -30f) }, { "torso_shoot_up_3", new Vector2(0f, -30f) },
            { "torso_shoot_up_4", new Vector2(0f, -30f) }, { "torso_duck_Animated_0", new Vector2(0f, 2f) },
            { "torso_duck_Animated_1", new Vector2(0f, 13f) }, { "torso_duck_0", new Vector2(0f, 13f) },
            { "torso_duck_shoot_0", new Vector2(0f, 13f) }, { "torso_duck_shoot_1", new Vector2(0f, 13f) },
            { "torso_duck_shoot_2", new Vector2(0f, 13f) }, { "torso_duck_walk_0", new Vector2(0f, 13f) },
            { "torso_duck_walk_1", new Vector2(0f, 13f) }, { "torso_duck_walk_2", new Vector2(0f, 14f) },
            { "torso_duck_walk_3", new Vector2(0f, 13f) }, { "torso_duck_walk_4", new Vector2(0f, 13f) }
        };

        _legsFrameOffsets = new Dictionary<string, Vector2>() {
            { "legs_idle_0", Vector2.Zero }, { "legs_run_0", Vector2.Zero }, { "legs_run_1", Vector2.Zero },
            { "legs_run_2", Vector2.Zero }, { "legs_run_3", Vector2.Zero }, { "legs_run_4", Vector2.Zero },
            { "legs_run_5", Vector2.Zero }, { "legs_run_6", Vector2.Zero }, { "legs_run_7", Vector2.Zero },
            { "legs_run_8", Vector2.Zero }, { "legs_run_9", Vector2.Zero }, { "legs_run_10", Vector2.Zero },
            { "legs_run_11", Vector2.Zero }, { "legs_run_12", Vector2.Zero }, { "legs_duck_0", new Vector2(0f,4f) },
            { "legs_duck_1", new Vector2(0f, 8f) }, { "legs_duck_shoot_0", new Vector2(0f, 8f) },
            { "legs_duck_shoot_1", new Vector2(0f, 8f) }, { "legs_duck_shoot_2", new Vector2(0f, 8f) },
            { "legs_duck_walk_0", new Vector2(0f, 8f) }, { "legs_duck_walk_1", new Vector2(0f, 8f) },
            { "legs_duck_walk_2", new Vector2(0f, 8f) }, { "legs_duck_walk_3", new Vector2(0f, 8f) },
            { "legs_duck_walk_4", new Vector2(0f, 8f) }, { "legs_jump_0", Vector2.Zero },
            { "legs_jump_1", Vector2.Zero }, { "legs_jump_2", Vector2.Zero }, { "legs_jump_3", Vector2.Zero },
            { "legs_jump_4", Vector2.Zero }, { "legs_jump_5", Vector2.Zero }
        };
    }
}