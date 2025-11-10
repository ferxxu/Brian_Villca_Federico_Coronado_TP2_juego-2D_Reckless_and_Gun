
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
 		TextureAtlas atlas = TextureAtlas.FromFile(Core.Content, "atlas-definition.xml");
 		_davidChest = atlas.CreateAnimatedSprite("idle-torso");
 		_davidLegs = atlas.CreateAnimatedSprite("idle-legs");
 		_davidLegs.Scale = new Vector2(2.0f, 2.0f);
 		_davidChest.Scale = new Vector2(2.0f, 2.0f);
 		
 		_davidLegs.Origin = new Vector2(_davidLegs.Region.Width / 2f, 0f); 
 		_davidChest.Origin = new Vector2(_davidChest.Region.Width / 2f, _davidChest.Region.Height); 

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
 		_davidChest.Update(gameTime);

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
 		
 		// Asumimos que estamos en el aire...
 		_isJumping = true; 

 		// ... a menos que demostremos lo contrario.
 		// ¿Estamos chocando con el suelo Y veníamos cayendo?
 		if (_davidHitbox.Intersects(_debugSuelo) && _velocity_pj.Y >= 0)
 		{
 			// --- ¡SÍ, HEMOS ATERRIZADO! ---
 			
 			// 1. Detenemos la caída
 			_velocity_pj.Y = 0; 
 			
 			// 2. Corregimos la posición (LA LÍNEA MÁGICA)
 			// Posicionamos la CINTURA para que los PIES estén 1 PÍXEL DENTRO del suelo.
 			_position_pj.Y = (_debugSuelo.Top - _constLegsHeight) + 1; 
 			
 			// 3. Informamos al sistema que estamos en el suelo
 			_isJumping = false; 
 			
 			// 4. Actualizamos la hitbox a la posición final corregida
 			UpdateHitbox();
 		}
 	}

 	// --- HITBOX (Usa constantes para evitar vibraciones) ---
 	private void UpdateHitbox()
 	{
 		_davidHitbox.Width = (int)_constHitboxWidth;
 		
 		if (isDucking)
 		{
 			_davidHitbox.Height = (int)(_constTorsoHeight + (_constLegsHeight * 0.6f));
 		}
 		else
 		{
 			_davidHitbox.Height = (int)_constHitboxHeight;
 		}

 		_davidHitbox.X = (int)(_position_pj.X - (_davidHitbox.Width / 2));
 		_davidHitbox.Y = (int)(_position_pj.Y - _constTorsoHeight);
 	}

 	// --- ANIMACIONES (Dependen de _isJumping) ---
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

 		_davidLegs.Draw(Core.SpriteBatch, _position_pj);
 		_davidChest.Draw(Core.SpriteBatch, _position_pj);

 		// --- DIBUJO DE DEBUG ---
 		DibujarBordeRectangulo(_davidHitbox, Color.Cyan, 2); 
 		DibujarBordeRectangulo(_debugSuelo, Color.LimeGreen, 3);
 		// --- FIN CÓDIGO DEBUG ---

 		Core.SpriteBatch.End();
 	}
}