using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGameLibrary.Graphics;
using MonoGameLibrary.Input;
using Microsoft.Xna.Framework.Audio; // <--- NECESARIO PARA SOUNDEFFECT
using MonoGameLibrary;               // <--- NECESARIO PARA CORE
namespace reckless_and_gun.Entities;

public class David
{
    private AnimatedSprite _davidLegs;
    private AnimatedSprite _davidChest;

    private Vector2 _position;
    private Vector2 _velocity;

    public int Health { get; private set; }
    public int MaxHealth { get; private set; } = 20;
    public int Lives { get; private set; } = 3;
    public bool IsDead { get; private set; } = false;

    private bool _isInvulnerable = false;
    private float _invulnerabilityTimer = 0f;
    private const float INVULNERABILITY_DURATION = 2.0f;

    public Vector2 Position => _position;
    public Rectangle Hitbox { get; private set; }

    // Constantes de movimiento
    private const float _speed = 300f;
    private const float _jumpSpeed = -700f;
    private const float _gravity = 1500f;

    private float _constLegsHeight;
    private float _constTorsoHeight;
    private float _constHitboxWidth;
    private float _constHitboxHeight;

    private float _hitboxCenterXOffset;

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

    private TimeSpan _fireRate = TimeSpan.FromMilliseconds(200);
    private TimeSpan _fireCooldownTimer = TimeSpan.Zero;

    public Texture2D DebugTexture { get; set; }

    private Vector2 _offsetRight_Normal = new Vector2(80f, -20f);
    private Vector2 _offsetRight_Duck = new Vector2(76f, -15f);
    private Vector2 _offsetRight_Up = new Vector2(46f, -25f);
    private Vector2 _offsetRight_Down = new Vector2(52f, 0f);
    private Vector2 _offsetLeft_Normal = new Vector2(20f, -20f);
    private Vector2 _offsetLeft_Duck = new Vector2(24f, -15f);
    private Vector2 _offsetLeft_Up = new Vector2(49f, -25f);
    private Vector2 _offsetLeft_Down = new Vector2(47f, 0f);

    private SoundEffect _sfxShoot;
    public David()
    {
        _velocity = Vector2.Zero;
        _isJumping = true;
        Health = MaxHealth;
    }
    public void SetSoundEffects( SoundEffect shoot)
    {

        _sfxShoot = shoot;
    }
    public void LoadContent(TextureAtlas atlas, Vector2 startPosition)
    {
        _davidChest = atlas.CreateAnimatedSprite("idle-torso");
        _davidLegs = atlas.CreateAnimatedSprite("idle-legs");

        _davidLegs.Scale = new Vector2(2.0f, 2.0f);
        _davidChest.Scale = new Vector2(2.0f, 2.0f);

        float pivotX = 11f;
        _davidLegs.Origin = new Vector2(pivotX, 0f);
        _davidChest.Origin = new Vector2(pivotX, _davidChest.Region.Height);

        _constLegsHeight = _davidLegs.Region.Height * _davidLegs.Scale.Y;
        _constTorsoHeight = _davidChest.Region.Height * _davidChest.Scale.Y;

        float totalVisualWidth = _davidLegs.Region.Width * _davidLegs.Scale.X;

        _constHitboxWidth = totalVisualWidth * 0.4f;

        float anchorX = _davidLegs.Origin.X * _davidLegs.Scale.X;
        _hitboxCenterXOffset = (totalVisualWidth / 2.0f) - anchorX;

        _constHitboxHeight = _constLegsHeight + _constTorsoHeight;

        _position = startPosition;

        CargarOffsetsDeAnimacion();
        UpdateHitbox();
    }

    public void Update(GameTime gameTime, KeyboardInfo keyboard, List<Rectangle> collisionRects)
    {
        if (IsDead) return;

        HandleInput(keyboard);
        ApplyPhysics(gameTime, collisionRects);

        if (_fireCooldownTimer > TimeSpan.Zero)
            _fireCooldownTimer -= gameTime.ElapsedGameTime;

        if (_isInvulnerable)
        {
            _invulnerabilityTimer -= (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (_invulnerabilityTimer <= 0)
            {
                _isInvulnerable = false;
                _davidChest.Color = Color.White;
                _davidLegs.Color = Color.White;
            }
            else
            {
                float flicker = _invulnerabilityTimer * 20;
                Color flashColor = ((int)flicker % 2 == 0) ? Color.Red : Color.White * 0.5f;
                _davidChest.Color = flashColor;
                _davidLegs.Color = flashColor;
            }
        }

        handleLegsAnimation();
        handleChestAnimation();

        _davidLegs.Update(gameTime);
        _davidChest.Update(gameTime);
    }

    public void TakeDamage(int amount)
    {
        if (_isInvulnerable || IsDead) return;
        Health -= amount;
        System.Diagnostics.Debug.WriteLine($"Vida: {Health}/{MaxHealth}");
        _isInvulnerable = true;
        _invulnerabilityTimer = INVULNERABILITY_DURATION;
        if (Health <= 0) LoseLife();
    }

    private void LoseLife()
    {
        Lives--;
        if (Lives > 0) Health = MaxHealth;
        else { IsDead = true; Health = 0; }
    }

    public Projectile TryShoot(TextureAtlas projectilesAtlas)
    {
        if (IsDead) return null;
        if (isShooting && _fireCooldownTimer <= TimeSpan.Zero)
        {
            _fireCooldownTimer = _fireRate;
            if (_sfxShoot != null)
            {
                float pitch = (float)(new Random().NextDouble() * 0.2 - 0.1); 
                Core.Audio.PlaySoundEffect(_sfxShoot, 0.2f, pitch, 0.0f, false);
            }
            var facingEffect = _davidLegs.Effects;
            bool mirandoDerecha = facingEffect != SpriteEffects.FlipHorizontally;
            float scale = _davidLegs.Scale.X;
            string torsoFrameName = _davidChest.Region?.Name ?? "torso_idle_0";
            Vector2 torsoAnimOffset = _torsoFrameOffsets.GetValueOrDefault(torsoFrameName, Vector2.Zero);
            Vector2 direction;
            if (isAimingUp && !isDucking) direction = -Vector2.UnitY;
            else if (isAimingDown) direction = Vector2.UnitY;
            else direction = new Vector2(mirandoDerecha ? 1f : -1f, 0);
            direction.Normalize();
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
            Vector2 spawnPosition = this.Position + (muzzleOffset * scale) + (torsoAnimOffset * scale);
            Sprite bulletSprite = projectilesAtlas.CreateSprite("Pistol_Bullet");
            bulletSprite.Origin = new Vector2(bulletSprite.Region.Width / 2f, bulletSprite.Region.Height / 2f);
            bulletSprite.Scale = new Vector2(2f, 2f);
            var newBullet = new PistolBullet(bulletSprite, spawnPosition, direction);
            newBullet.DebugTexture = this.DebugTexture;
            return newBullet;
        }
        return null;
    }

   private void HandleInput(KeyboardInfo keyboard)
{
    // 1. Reseteamos los estados por defecto para este frame
    isMovingHorizontally = false;
    isDucking = false;
    isShooting = false;
    isAimingUp = false;
    isAimingDown = false;
    
    // Resetear transición de agachado si soltamos la tecla S
    if (!keyboard.IsKeyDown(Keys.S)) _isDuckingTransitionDone = false;

    // 2. Leemos teclas (Intenciones)
    bool keyUp = keyboard.IsKeyDown(Keys.W);
    bool keyDown = keyboard.IsKeyDown(Keys.S);
    bool keyLeft = keyboard.IsKeyDown(Keys.A);
    bool keyRight = keyboard.IsKeyDown(Keys.D);
    bool keyShoot = keyboard.IsKeyDown(Keys.H);
    bool keyJump = keyboard.IsKeyDown(Keys.J);

    // 3. Configurar Estados de Combate
    isShooting = keyShoot;
    if (keyUp) isAimingUp = true;

    // Lógica Agacharse vs Apuntar Abajo
    if (keyDown)
    {
        if (_isJumping) isAimingDown = true; // En el aire apunta abajo
        else isDucking = true;               // En el suelo se agacha
    }

    // 4. Lógica de Movimiento (AQUÍ ESTÁ LA SOLUCIÓN)
    // Detectamos si el jugador QUIERE moverse
    bool intentToMove = (keyLeft || keyRight);

    if (intentToMove)
    {
        // Dirección
        if (keyLeft)
        {
            _davidLegs.Effects = SpriteEffects.FlipHorizontally;
            _davidChest.Effects = SpriteEffects.FlipHorizontally;
        }
        else
        {
            _davidLegs.Effects = SpriteEffects.None;
            _davidChest.Effects = SpriteEffects.None;
        }
        
        if (isDucking && isShooting)
        {
            isMovingHorizontally = false; // Bloqueado (Metal Slug style: agachado quieto)
        }
        else
        {
            isMovingHorizontally = true;  // Permitido (Correr y disparar)
        }
    }

    // 5. Salto
    if (keyJump && !_isJumping && !isDucking)
    {
        _velocity.Y = _jumpSpeed;
        _isJumping = true;
        _isStandingJump = !isMovingHorizontally;
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

        float halfHitbox = Hitbox.Width / 2f;

        foreach (Rectangle rect in collisionRects)
        {
            if (rect.Height > rect.Width && Hitbox.Intersects(rect))
            {
                if (_velocity.X > 0)
                {
                    _position.X = rect.Left - _hitboxCenterXOffset - halfHitbox;
                }
                else if (_velocity.X < 0)
                {
                    _position.X = rect.Right - _hitboxCenterXOffset + halfHitbox;
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
        float torsoAnchorY = _davidChest.Origin.Y * _davidChest.Scale.Y;

        Hitbox = new Rectangle(
            (int)(_position.X + _hitboxCenterXOffset - (_constHitboxWidth / 2)),
            (int)((_position.Y - torsoAnchorY) + yOffset),
            (int)_constHitboxWidth,
            (int)currentHitboxHeight
        );
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        if (IsDead) return;

        string legFrameName = _davidLegs.Region?.Name ?? "";
        string torsoFrameName = _davidChest.Region?.Name ?? "";
        Vector2 legOffset = _legsFrameOffsets.GetValueOrDefault(legFrameName, Vector2.Zero);
        Vector2 torsoOffset = _torsoFrameOffsets.GetValueOrDefault(torsoFrameName, Vector2.Zero);

        _davidLegs.Draw(spriteBatch, _position + (legOffset * _davidLegs.Scale));
        _davidChest.Draw(spriteBatch, _position + (torsoOffset * _davidChest.Scale));

    }

   public void handleChestAnimation()
{
    string newState = "idle-torso";

    // --- PRIORIDAD 1: AIRE (Salto) ---
    if (_isJumping)
    {
        // En el aire, apuntar tiene prioridad sobre la animación de salto normal
        if (isAimingUp) 
            newState = isShooting ? "shoot-up-torso" : "up-torso";
        else if (isAimingDown) 
            newState = isShooting ? "shoot-down-torso" : "down-torso";
        else if (isShooting) 
            newState = "shoot-torso";
        else 
            newState = "jump-torso"; 
    }
    // --- PRIORIDAD 2: AGACHADO (Suelo) ---
    else if (isDucking)
    {
        // NOTA: HandleInput ya garantiza que si disparas, isMovingHorizontally es false.
        
        if (isShooting)
        {
            newState = "duck-shoot-torso";
            _isDuckingTransitionDone = true; // Saltamos la transición si dispara
        }
        else if (isMovingHorizontally)
        {
            newState = "duck-walk-torso";
            _isDuckingTransitionDone = true; // Saltamos la transición si camina agachado
        }
        // Lógica de transición (bajar el cuerpo)
        else if (!_isDuckingTransitionDone)
        {
            newState = "duck-torso-animated";
            // Si la animación de bajar terminó, pasamos a estado estático
            if (_chestState == "duck-torso-animated" && _davidChest.IsAnimationFinished)
            {
                _isDuckingTransitionDone = true;
            }
        }
        else
        {
            newState = "duck-torso"; // Agachado estático (Idle duck)
        }
    }
    // --- PRIORIDAD 3: DE PIE (Suelo) ---
    else
    {
        // Aquí ocurre la magia del Run & Gun
        
        // 1. ¿Está mirando arriba? (Gana a todo lo demás)
        if (isAimingUp)
        {
            newState = isShooting ? "shoot-up-torso" : "up-torso";
        }
        // 2. ¿Está disparando recto?
        else if (isShooting)
        {
            newState = "shoot-torso";
        }
        // 3. ¿Está corriendo? (Solo si no apunta arriba ni dispara, aunque si dispara recto 
        // a veces se prefiere 'run-shoot-torso' si tienes ese sprite, si no, 'shoot-torso' está bien)
        else if (isMovingHorizontally)
        {
            newState = "run-torso";
        }
        // 4. Quieto
        else
        {
            newState = "idle-torso";
        }
    }

    // Aplicar cambios
    if (newState != _chestState)
    {
        _chestState = newState;
        // Evitamos loop en la animación de transición de agacharse
        bool loop = newState != "duck-torso-animated"; 
        
        if (_davidChest.Animations.ContainsKey(_chestState)) 
            _davidChest.Animations[_chestState].IsLooping = loop;
            
        _davidChest.Play(_chestState);
    }
}
    public void handleLegsAnimation()
{
    string newState = "idle-legs";

    // --- SALTO ---
    if (_isJumping)
    {
        newState = _isStandingJump ? "jump-legs" : "jump-legs_run";
    }
    // --- AGACHADO ---
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
    else 
    {
        if (isMovingHorizontally)
        {
            newState = "run-legs";
        }
        else
        {
            newState = "idle-legs";
        }
    }
    if (newState != _legState)
    {
        _legState = newState;
        
        bool loop = newState != "duck-legs-animated";

        if (_davidLegs.Animations.ContainsKey(_legState)) 
             _davidLegs.Animations[_legState].IsLooping = loop;

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