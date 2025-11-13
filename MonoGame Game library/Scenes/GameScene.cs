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
        TextureAtlas atlasSpider = TextureAtlas.FromFile(Core.Content, "spider.xml");
        TextureAtlas atlas = TextureAtlas.FromFile(Core.Content, "david1.xml"); // Asegúrate que el XML se llame 'david.xml'
        _davidChest = atlas.CreateAnimatedSprite("idle-torso");
        _davidLegs = atlas.CreateAnimatedSprite("idle-legs");
        _spiderAnimation = atlasSpider.CreateAnimatedSprite("spider_walking");
        _davidLegs.Scale = new Vector2(2.0f, 2.0f);
        _davidChest.Scale = new Vector2(2.0f, 2.0f);
        _spiderAnimation.Scale = new Vector2(3.0f, 3.0f);

        // --- Se define el ORIGEN (Pivote) BASE UNA SOLA VEZ ---
        // (Esto detiene la vibración base)
        _spiderWidth = _spiderAnimation.Region.Width * _spiderAnimation.Scale.X;
        _spiderHeight = _spiderAnimation.Region.Height * _spiderAnimation.Scale.Y;
        _davidLegs.Origin = new Vector2(_davidLegs.Region.Width / 2f, 0f);
        _davidChest.Origin = new Vector2(_davidChest.Region.Width / 2f, _davidChest.Region.Height);

        // --- Calculamos constantes ---
        _constLegsHeight = _davidLegs.Region.Height * _davidLegs.Scale.Y;
        _constTorsoHeight = _davidChest.Region.Height * _davidChest.Scale.Y;
        _constHitboxWidth = _davidLegs.Region.Width * _davidLegs.Scale.X;
        _constHitboxHeight = _constLegsHeight + _constTorsoHeight;

        _torsoFrameOffsets = new Dictionary<string, Vector2>()
        {
            { "torso_idle_0", new Vector2(6f, 0f) }, // <- Tu base
            { "torso_run_0", new Vector2(6f, 0f) },
            { "torso_run_1", new Vector2(6f, 0f) },
            { "torso_jump_0", new Vector2(0f, 0f) },
            { "torso_jump_1", new Vector2(0f, 0f) },
            { "torso_jump_2", new Vector2(0f, 0f) },
            { "torso_jump_3", new Vector2(0f, 0f) },
            { "torso_jump_4", new Vector2(0f, 0f) },
            { "torso_shoot_0", new Vector2(0f, 0f) },
            { "torso_shoot_1", new Vector2(0f, 0f) },
            { "torso_shoot_2", new Vector2(0f, 0f) },
            { "torso_shoot_3", new Vector2(0f, 0f) },
            { "torso_shoot_down_0", new Vector2(0f, 0f) },
            { "torso_shoot_down_1", new Vector2(0f, 0f) },
            { "torso_shoot_down_2", new Vector2(0f, 0f) },
            { "torso_shoot_down_3", new Vector2(0f, 0f) },
            { "torso_shoot_down_4", new Vector2(0f, 0f) },
            { "torso_shoot_up_0", new Vector2(0f, 0f) },
            { "torso_shoot_up_1", new Vector2(0f, 0f) },
            { "torso_shoot_up_2", new Vector2(0f, 0f) },
            { "torso_shoot_up_3", new Vector2(0f, 0f) },
            { "torso_shoot_up_4", new Vector2(0f, 0f) },
            { "torso_shoot_up_5", new Vector2(0f, 0f) },
            { "torso_duck_0", new Vector2(0f, 0f) },
            { "torso_duck_1", new Vector2(0f, 0f) },
            { "torso_duck_shoot_2", new Vector2(0f, 0f) },
            { "torso_duck_shoot_3", new Vector2(0f, 0f) },
            { "torso_duck_shoot_4", new Vector2(0f, 0f) },
            { "torso_duck_shoot_5", new Vector2(0f, 0f) },
            { "torso_duck_walk_0", new Vector2(0f, 0f) },
            { "torso_duck_walk_1", new Vector2(0f, 0f) },
            { "torso_duck_walk_2", new Vector2(0f, 0f) },
            { "torso_duck_walk_3", new Vector2(0f, 0f) },
            { "torso_duck_walk_4", new Vector2(0f, 0f) }
        };

        _legsFrameOffsets = new Dictionary<string, Vector2>()
        {
            { "legs_idle_0", new Vector2(0f, 0f) }, // <- Tu base
            { "legs_run_0", new Vector2(-6f, 0f) },
            { "legs_run_1", new Vector2(-5f, 0f) },
            { "legs_run_2", new Vector2(-3f, 0f) },
            { "legs_run_3", new Vector2(3f, 0f) },
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
        { _collisionRects.Add(new Rectangle((int)obj.X, (int)obj.Y, (int)obj.Width, (int)obj.Height)); }
        _position_pj = new Vector2(400, 10);
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
        _spiderAnimation.Play("spider_walking");
        _davidLegs.Play(_legState);
        _davidChest.Play(_chestState);
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
        isMovingHorizontally = false;
        isDucking = false;
        isShooting = false;
        isAimingUp = false;
        if (keyboard.IsKeyDown(Keys.S) && !_isJumping)
        { isDucking = true; }
        if (keyboard.IsKeyDown(Keys.W) && !_isJumping)
        { isAimingUp = true; }
        if (!isDucking && !isAimingUp)
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
        if (keyboard.IsKeyDown(Keys.H))
        { isShooting = true; }
        if (keyboard.WasKeyJustPressed(Keys.R) && !_isJumping && !isDucking)
        { _velocity_pj.Y = _jumpSpeed; _isJumping = true; }
    }
    private void ApplyPhysics(GameTime gameTime)
    {
        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _velocity_pj.Y += _gravity * deltaTime;
        _velocity_pj.Y = MathHelper.Clamp(_velocity_pj.Y, -_jumpSpeed * 2, _gravity * 2f);
        if (isMovingHorizontally)
        { _velocity_pj.X = (_davidLegs.Effects == SpriteEffects.FlipHorizontally) ? -_speed : _speed; }
        else
        { _velocity_pj.X = 0; }
        _position_pj.X += _velocity_pj.X * deltaTime;
        _position_pj.Y += _velocity_pj.Y * deltaTime;
        UpdateHitbox();
        bool isGrounded = false;
        foreach (Rectangle rect in _collisionRects)
        {
            if (_davidHitbox.Intersects(rect) && _velocity_pj.Y >= 0)
            {
                isGrounded = true;
                _velocity_pj.Y = 0;
                _position_pj.Y = rect.Top - _constLegsHeight + 1;
                UpdateHitbox();
                break;
            }
        }
        _isJumping = !isGrounded;
    }
    private void UpdateHitbox()
    {
        _davidHitbox.Width = (int)_constHitboxWidth;
        if (isDucking)
        { _davidHitbox.Height = (int)(_constTorsoHeight + (_constLegsHeight * 0.6f)); }
        else
        { _davidHitbox.Height = (int)_constHitboxHeight; }
        _davidHitbox.X = (int)(_position_pj.X - (_davidHitbox.Width / 2f));
        float feetY = _position_pj.Y + _constLegsHeight;
        _davidHitbox.Y = (int)(feetY - _davidHitbox.Height);
    }
    public void handleChestAnimation()
    {
        if (_isJumping) { _chestState = "jump-torso"; }
        else if (isDucking)
        {
            if (isShooting) { _chestState = "duck-shoot-torso"; }
            else if (isMovingHorizontally) { _chestState = "duck-walk-torso"; }
            else { _chestState = "duck-torso"; }
        }
        else if (isAimingUp)
        {
            if (isShooting) { _chestState = "shoot-up-torso"; }
            else { _chestState = "idle-torso"; }
        }
        else if (isShooting) { _chestState = "shoot-torso"; }
        else if (isMovingHorizontally) { _chestState = "run-torso"; }
        else { _chestState = "idle-torso"; }
    }
    public void handleLegsAnimation()
    {
        if (_isJumping) { _legState = "jump-legs"; }
        else if (isDucking)
        {
            if (isShooting) { _legState = "duck-shoot-legs"; }
            else if (isMovingHorizontally) { _legState = "duck-walk-legs"; }
            else { _legState = "duck-legs"; }
        }
        else if (isAimingUp) { _legState = "idle-legs"; }
        else if (isMovingHorizontally) { _legState = "run-legs"; }
        else { _legState = "idle-legs"; }
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

        // Ajuste vertical por agacharse (tu lógica original)
        if (isDucking)
        {
            float scaledStandingLegsHeight = _constLegsHeight;
            float scaledDuckingLegsVisualHeight = _constLegsHeight * 0.6f;
            float yOffset = scaledStandingLegsHeight - scaledDuckingLegsVisualHeight;
            finalDrawPosition.Y += yOffset;
        }

        // 1. Obtener los nombres de los fotogramas actuales
        string legFrameName = _davidLegs.Region?.Name ?? "";
        string torsoFrameName = _davidChest.Region?.Name ?? "";

        // 2. Obtener los offsets BASE de los diccionarios por nombre de fotograma
        Vector2 legOffsetFromDict = _legsFrameOffsets.GetValueOrDefault(legFrameName, Vector2.Zero);
        Vector2 torsoOffsetFromDict = _torsoFrameOffsets.GetValueOrDefault(torsoFrameName, Vector2.Zero);

        // 3. Obtener el ancho real de la textura del frame actual
        float currentLegsWidth = _davidLegs.Region.Width;
        float currentTorsoWidth = _davidChest.Region.Width;

        // 4. Obtener el Origin (ancla) que definimos en LoadContent (el 50% del frame IDLE)
        float baseLegsOriginX = _davidLegs.Origin.X;
        float baseTorsoOriginX = _davidChest.Origin.X;

        // 5. Calcular la corrección para que el PIXEL 11 (tu hebilla) sea el centro real
        // Esto es la magia: no importa el ancho de la imagen, siempre pivotamos sobre el PIXEL 11
        float pivotX = 11f; // <--- ¡TU PUNTO DE ANCLA DEFINITIVO (Píxel 11)!

        // Corrección de posición horizontal para las piernas
        float legsCorrectionX = (currentLegsWidth / 2f) - pivotX;
        if (_davidLegs.Effects == SpriteEffects.FlipHorizontally)
        {
            legsCorrectionX *= -1; // Invierte si está volteado
            legsCorrectionX += currentLegsWidth - (2 * pivotX); // Corrección extra para que el flip sea perfecto desde el pixel 11
        }

        // Corrección de posición horizontal para el torso
        float torsoCorrectionX = (currentTorsoWidth / 2f) - pivotX;
        if (_davidChest.Effects == SpriteEffects.FlipHorizontally)
        {
            torsoCorrectionX *= -1; // Invierte si está volteado
            torsoCorrectionX += currentTorsoWidth - (2 * pivotX); // Corrección extra para que el flip sea perfecto desde el pixel 11
        }

        // 6. Combinar el offset manual del diccionario con la corrección automática
        Vector2 legsFinalOffset = legOffsetFromDict + new Vector2(legsCorrectionX, 0);
        Vector2 torsoFinalOffset = torsoOffsetFromDict + new Vector2(torsoCorrectionX, 0);

        // 7. Aplicar offsets a la posición de dibujo (¡ya están escalados!)
        Vector2 legsDrawPos = finalDrawPosition + (legsFinalOffset * _davidLegs.Scale);
        Vector2 chestDrawPos = finalDrawPosition + (torsoFinalOffset * _davidChest.Scale);

        // 8. Dibujar
        _davidLegs.Draw(Core.SpriteBatch, legsDrawPos);
        _davidChest.Draw(Core.SpriteBatch, chestDrawPos);

        // --- DIBUJO DE DEBUG ---
        DibujarBordeRectangulo(_davidHitbox, Color.Cyan, 2);
        DibujarBordeRectangulo(_spiderHitbox, Color.Red, 2);


        Core.SpriteBatch.End();
    }
}