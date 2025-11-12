
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

 	public override void Initialize()
 	{
 		base.Initialize();
 		Core.ExitOnEscape = false;
 		_velocity_pj = Vector2.Zero;
 		_camera = new Camera2D();
 		_camera.Zoom = 1.0f;
 		_isJumping = true; // Empieza en el aire
 	}

 	public override void LoadContent()
 	{
 		_background = Content.Load<Texture2D>("beach_map");
 		TextureAtlas atlas = TextureAtlas.FromFile(Core.Content, "david.xml");
 		_davidChest = atlas.CreateAnimatedSprite("idle-torso");
 		_davidLegs = atlas.CreateAnimatedSprite("idle-legs");
 		_davidLegs.Scale = new Vector2(2.0f, 2.0f);
 		_davidChest.Scale = new Vector2(2.0f, 2.0f);

 		_constLegsHeight = _davidLegs.Region.Height * _davidLegs.Scale.Y;
 		_constTorsoHeight = _davidChest.Region.Height * _davidChest.Scale.Y;
 		_constHitboxWidth = _davidLegs.Region.Width * _davidLegs.Scale.X;
 		_constHitboxHeight = _constLegsHeight + _constTorsoHeight;

 		_collisionRects = new List<Rectangle>();
 		string mapFilePath = Path.Combine(Content.RootDirectory, "beach_map.tmx");
 		var map = new TmxMap(mapFilePath);
 		_roomBounds = new Rectangle(0, 0, map.Width * map.TileWidth, map.Height * map.TileHeight);
 		var collisionLayer = map.ObjectGroups["collisions"];
 		foreach (var obj in collisionLayer.Objects)
 		{
 			_collisionRects.Add(new Rectangle((int)obj.X, (int)obj.Y, (int)obj.Width, (int)obj.Height));
 		}

 		_position_pj = new Vector2(400, 10); 

 		_texturaDebug = new Texture2D(Core.GraphicsDevice, 1, 1);
 		_texturaDebug.SetData(new[] { Color.White });

 		_debugSuelo = new Rectangle(0, 450, _roomBounds.Width, 50);
 		
 		UpdateHitbox(); 
 	}


 	public override void Update(GameTime gameTime)
 	{
 		HandleInput();
 		ApplyPhysics(gameTime); 
 		handleLegsAnimation();
 		handleChestAnimation(); 

 		_davidLegs.Play(_legState);
 		_davidChest.Play(_chestState);

 		_davidLegs.Update(gameTime);
 		_davidChest.Update(gameTime);// --- AÑADE ESTO AQUÍ O ASEGÚRATE DE QUE ESTÉ ASÍ ---
        // Estas líneas ajustan el punto de pivote (Origen) de cada sprite
        // para que su "cintura" esté en la posición _position_pj.
        // Se llaman en cada Update() para adaptarse al tamaño actual de la animación.
        _davidLegs.Origin = new Vector2(_davidLegs.Region.Width / 2f, 0f); 
        _davidChest.Origin = new Vector2(_davidChest.Region.Width / 2f, _davidChest.Region.Height); 
        // --- FIN DEL BLOQUE DE ORIGEN ---
 		_camera.Follow(_position_pj, _roomBounds, Core.GraphicsDevice.Viewport);
 		base.Update(gameTime);
 	}

 	// --- EL CEREBRO (INPUT) ---
 	private void HandleInput()
 	{
 		KeyboardInfo keyboard = Core.Input.Keyboard;

 		if (keyboard.WasKeyJustPressed(Keys.Escape))
 		{
 			Core.ChangeScene(new TitleScene());
 		}

 		isMovingHorizontally = false;
 		isDucking = false;
 		isShooting = false;
 		isAimingUp = false; 

 		if (keyboard.IsKeyDown(Keys.S) && !_isJumping)
 		{
 			isDucking = true;
 		}

 		if (keyboard.IsKeyDown(Keys.W) && !_isJumping) 
 		{
 			isAimingUp = true;
 		}

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
 		{
 			isShooting = true;
 		}

 		// --- LÓGICA DE SALTO "DESDE CERO" ---
 		// Si el jugador presiona J, y NO está en el aire, y NO está agachado...
 		if (keyboard.WasKeyJustPressed(Keys.R) && !_isJumping && !isDucking)
 		{
 			// 1. Le damos velocidad de salto
 			_velocity_pj.Y = _jumpSpeed; 
 			
 			// 2. Le decimos al sistema que AHORA SÍ está en el aire
 			_isJumping = true; 
 		}
 	}

 	// --- LOS MÚSCULOS (FÍSICA) ---
private void ApplyPhysics(GameTime gameTime)
    {
        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

        // --- 1. APLICAR GRAVEDAD ---
        _velocity_pj.Y += _gravity * deltaTime;
        _velocity_pj.Y = MathHelper.Clamp(_velocity_pj.Y, -_jumpSpeed * 2, _gravity * 2f);

        // --- 2. APLICAR MOVIMIENTO X (CAMINAR) ---
        if (isMovingHorizontally)
        {
            _velocity_pj.X = (_davidLegs.Effects == SpriteEffects.FlipHorizontally) ? -_speed : _speed;
        }
        else
        {
            _velocity_pj.X = 0;
        }

        // --- 3. APLICAR MOVIMIENTO (AMBOS EJES) ---
        _position_pj.X += _velocity_pj.X * deltaTime;
        _position_pj.Y += _velocity_pj.Y * deltaTime;

        UpdateHitbox();

        // --- 4. LÓGICA DE ATERRIZAJE ---

        bool isGrounded = false;

        // ¿Estamos chocando con el suelo Y veníamos cayendo?
        // (Usamos _collisionRects en lugar de _debugSuelo)
        foreach (Rectangle rect in _collisionRects)
        {
            if (_davidHitbox.Intersects(rect) && _velocity_pj.Y >= 0)
            {
                // --- ¡SÍ, HEMOS ATERRIZADO! ---
                isGrounded = true;
                _velocity_pj.Y = 0;

                // CORRECCIÓN 3 (Pegado): 
                // Pegamos los PIES de la hitbox al TOPE del suelo
                _position_pj.Y = rect.Top - _constLegsHeight + 1;

                UpdateHitbox();
                break; // Salimos del bucle, ya encontramos suelo
            }
        }

        // Finalmente, actualizamos el estado de salto
        _isJumping = !isGrounded;
    }

 	// --- HITBOX (Usa constantes para evitar vibraciones) ---
 	// --- HITBOX (Usa constantes para evitar vibraciones) ---
    // --- HITBOX (Corregido para anclar los pies al suelo) ---
   // --- HITBOX (Corregido para ser 100% dinámico) ---
    // --- HITBOX (Con ancho de idle-legs, se corregirá más tarde) ---
    private void UpdateHitbox()
    {
        // Usamos el ancho constante de idle-legs, como pediste
        _davidHitbox.Width = (int)_constHitboxWidth;

        // --- 1. Calcular la ALTURA TOTAL primero ---
        if (isDucking)
        {
            // Cuando se agacha, la altura total es el torso + un % de las piernas.
            // ¡Es posible que tengas que ajustar este 0.6f!
            _davidHitbox.Height = (int)(_constTorsoHeight + (_constLegsHeight * 0.6f));
        }
        else
        {
            // Altura normal (parado o saltando)
            _davidHitbox.Height = (int)_constHitboxHeight;
        }

        // --- 2. Calcular X (centrado en la cintura) ---
        _davidHitbox.X = (int)(_position_pj.X - (_davidHitbox.Width / 2f));
        
        // --- 3. Calcular Y (basado en los pies) ---
        // La posición de los pies (la base) SIEMPRE se calcula con la
        // altura de las piernas DE PIE (_constLegsHeight),
        // porque tu 'snap' en ApplyPhysics usa esa constante para fijar la cintura.
        float feetY = _position_pj.Y + _constLegsHeight; 
        
        // El "techo" de la hitbox es la posición de los pies MENOS la altura total (que sí cambia).
        // Si Height es más chico (agachado), Y bajará (se acerca al suelo).
        _davidHitbox.Y = (int)(feetY - _davidHitbox.Height);
    }
 	public void handleChestAnimation()
 	{
 		if (_isJumping) { _chestState = "jump-torso"; }
 		else if (isDucking) {
 			if (isShooting) { _chestState = "duck-shoot-torso"; }
 			else if (isMovingHorizontally) { _chestState = "duck-walk-torso"; }
 			else { _chestState = "duck-torso"; }
 		}
 		else if (isAimingUp) {
 			if (isShooting) { _chestState = "shoot-up-torso"; }
 			else { _chestState = "idle-torso"; }
 		}
 		else if (isShooting) { _chestState = "shoot-torso"; }
 		else if (isMovingHorizontally) { _chestState = "run-torso"; }
 		else { _chestState = "idle-torso"; }
 	}

 	public void handleLegsAnimation()
 	{
 		// --- ¡AQUÍ ESTABA LA ERRATA, AHORA CORREGIDA! ---
 		if (_isJumping) { _legState = "jump-legs"; }
 		else if (isDucking) {
 			if (isMovingHorizontally) { _legState = "duck-walk-legs"; }
 			else { _legState = "duck-legs"; }
 		}
 		else if (isAimingUp) { _legState = "idle-legs"; }
 		else if (isMovingHorizontally) { _legState = "run-legs"; }
 		else { _legState = "idle-legs"; }
 	}

 	// --- DEBUG DRAW ---
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

        // La posición base es siempre la cintura del personaje.
        Vector2 finalDrawPosition = _position_pj;

        // Si está agachado, necesitamos "mover" el punto de dibujo hacia abajo
        // para compensar que el sprite de piernas agachadas es visualmente más corto.
        if (isDucking)
        {
            float scaledStandingLegsHeight = _constLegsHeight;
            float scaledDuckingLegsVisualHeight = _constLegsHeight * 0.6f; // Asegúrate que este 0.6f sea el mismo que en UpdateHitbox
            float yOffset = scaledStandingLegsHeight - scaledDuckingLegsVisualHeight;
            finalDrawPosition.Y += yOffset;
        }

        // --- DIBUJO DE LAS PIERNAS (no necesita ajuste horizontal) ---
        _davidLegs.Draw(Core.SpriteBatch, finalDrawPosition);
        
        // --- DIBUJO DEL TORSO CON AJUSTE HORIZONTAL ---
        // Aquí ajustaremos la posición X del torso.
        // Prueba con diferentes valores para `xOffsetAdjustment`
        // hasta que el torso se vea perfectamente alineado con las piernas.
        // Si el torso necesita moverse a la DERECHA, el valor es POSITIVO.
        // Si el torso necesita moverse a la IZQUIERDA, el valor es NEGATIVO.
        
        float xOffsetAdjustment = 25.0f; // <-- PRUEBA A CAMBIAR ESTE VALOR (ej: 0.0f, 2.0f, -3.0f, etc.)
        
        Vector2 chestDrawPosition = new Vector2(finalDrawPosition.X + xOffsetAdjustment, finalDrawPosition.Y);
        
        _davidChest.Draw(Core.SpriteBatch, chestDrawPosition);
        
        // --- FIN DEL AJUSTE ---

        // --- DIBUJO DE DEBUG (para la hitbox) ---
        DibujarBordeRectangulo(_davidHitbox, Color.Cyan, 2); 
        DibujarBordeRectangulo(_debugSuelo, Color.LimeGreen, 3);
        // --- FIN CÓDIGO DEBUG ---

        Core.SpriteBatch.End();
    }
}