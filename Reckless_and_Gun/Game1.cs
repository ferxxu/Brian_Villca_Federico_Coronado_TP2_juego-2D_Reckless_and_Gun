using Reckless_and_Gun.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGameLibrary.Graphics; // Usamos tu librería

namespace Reckless_and_Gun;

// Un "enum" para saber qué está haciendo el jugador
public enum PlayerState
{
    Idle,
    Walking,
    Jumping
}

public class Game1 : Core
{
    // AHORA TENEMOS DOS SPRITES:
    private AnimatedSprite _pj_chest; // Torso
    private AnimatedSprite _pj_legs;  // Piernas

    // --- VARIABLES PARA GUARDAR LAS ANIMACIONES ---
    // (Tu AnimatedSprite no puede cambiar de animaciones por nombre,
    // así que le asignamos el objeto de animación completo)
    private Animation _animStandChest;
    private Animation _animStandLegs;
    private Animation _animWalkChest;
    private Animation _animWalkLegs;
    // (Faltarían las de salto, pero empecemos con esto)

    private TextureRegion _bg_beach;
    private Texture2D _beach_texture;
    
    private Vector2 _velocity_david;
    private Vector2 _position_pj = new Vector2(500, 390); // Posición de las PIERNAS
    private float _jumpSpeed = -600f;
    private float _gravity = 1000f;
    private bool _isJumping = false;
    private float _floor = 390f;

    // Variables de estado
    private PlayerState _currentState = PlayerState.Idle;
    private SpriteEffects _direction = SpriteEffects.None; // Para saber si mira izq/der

    public Game1() : base("Reckless and Gun", 1280, 590, false)
    {

    }

    protected override void Initialize()
    {
        base.Initialize();
    }

    protected override void LoadContent()
    {
        // 1. Cargamos el atlas (asumiendo que se llama david_censure.xml)
        //    (Asegúrate de que este XML esté corregido como te pasé antes)
        TextureAtlas atlas = TextureAtlas.FromFile(Content, "Sprites_pj/david_censure.xml");
        _beach_texture = Content.Load<Texture2D>("Spritesheet_map/bg_beach");
        _bg_beach = new TextureRegion(_beach_texture, 0, 0, 3000, 400);

        
        _animStandChest = atlas.CreateAnimatedSprite("David-stand-chest").Animation;
        _animStandLegs  = atlas.CreateAnimatedSprite("David-stand-legs").Animation;
        _animWalkChest  = atlas.CreateAnimatedSprite("David-walk-chest").Animation;
        _animWalkLegs   = atlas.CreateAnimatedSprite("David-walk-legs").Animation;

        // 3. CREAMOS NUESTROS SPRITES DE VERDAD
        
        // Empezamos con la animación "stand"
        _pj_legs = new AnimatedSprite(_animStandLegs); 
        _pj_legs.Scale = new Vector2(2.0f, 2.0f);
        _pj_legs.Origin = Vector2.Zero;

        // Empezamos con la animación "stand"
        _pj_chest = new AnimatedSprite(_animStandChest);
        _pj_chest.Scale = new Vector2(2.0f, 2.0f);
        _pj_chest.Origin = Vector2.Zero;

        base.LoadContent();
    }

    protected override void Update(GameTime gameTime)
    {
        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();

        // 1. Revisamos el input del teclado
        CheckKeyboardInput(deltaTime);

        // 2. Actualizamos las animaciones según el estado
        UpdateAnimations();

        // 3. Actualizamos CADA parte del sprite
        // (Tu método .Update() hace que el sprite avance de frame)
        _pj_chest.Update(gameTime);
        _pj_legs.Update(gameTime);
        
        base.Update(gameTime);
    }

    private void UpdateAnimations()
    {
        // Esta es la lógica clave.
        // En lugar de .Play(), asignamos la animación que queremos.
        
        // Lógica de salto (tiene prioridad)
        if (_isJumping)
        {
            // (Aquí deberías tener animaciones de salto, ej. "David-jump-chest")
            // Si la animación actual NO ES la de saltar, la cambiamos.
            if (_pj_chest.Animation != _animStandChest) // Usamos Stand como "salto" por ahora
                _pj_chest.Animation = _animStandChest;
                
            if (_pj_legs.Animation != _animStandLegs)
                _pj_legs.Animation = _animStandLegs;
        }
        // Lógica en el suelo
        else
        {
            switch (_currentState)
            {
                case PlayerState.Idle:
                    // Si no estamos en "Idle", cambiamos a "Idle"
                    if (_pj_chest.Animation != _animStandChest)
                        _pj_chest.Animation = _animStandChest;
                    if (_pj_legs.Animation != _animStandLegs)
                        _pj_legs.Animation = _animStandLegs;
                    break;
                
                case PlayerState.Walking:
                    // Si no estamos en "Walking", cambiamos a "Walking"
                    if (_pj_chest.Animation != _animWalkChest)
                        _pj_chest.Animation = _animWalkChest;
                    if (_pj_legs.Animation != _animWalkLegs)
                        _pj_legs.Animation = _animWalkLegs;
                    break;
            }
        }
        
        // Aplicamos la dirección (Flip) a ambas partes
        _pj_chest.Effects = _direction;
        _pj_legs.Effects = _direction;
    }

    private void CheckKeyboardInput(float _deltaTime)
    {
        // Asumimos que está quieto, si no se pulsa nada
        // FIX: Tu código original decía _isJoping, lo corregí a _isJumping
        if (!_isJumping) 
            _currentState = PlayerState.Idle;

        if (Input.Keyboard.IsKeyDown(Keys.A))
        {
            _position_pj.X -= 6.0f;
            _currentState = PlayerState.Walking; // Está caminando
            _direction = SpriteEffects.FlipHorizontally; // Mira a la izquierda
        }
        if (Input.Keyboard.IsKeyDown(Keys.D))
        {
            _position_pj.X += 6.0f;
            _currentState = PlayerState.Walking; // Está caminando
            _direction = SpriteEffects.None; // Mira a la derecha
        }

        // Lógica de Salto (igual que antes)
        if (Input.Keyboard.WasKeyJustPressed(Keys.J) && !_isJumping)
        {
            _isJumping = true;
            _velocity_david = new Vector2(_velocity_david.X, _jumpSpeed);
        }
        
        if (_isJumping)
        {
            _currentState = PlayerState.Jumping; // Está saltando
            _velocity_david = new Vector2(_velocity_david.X, _velocity_david.Y + _gravity * _deltaTime);
            _position_pj = _position_pj + (_velocity_david * _deltaTime);

            if (_position_pj.Y >= _floor)
            {
                _position_pj = new Vector2(_position_pj.X, _floor);
                _isJumping = false;
                _velocity_david = Vector2.Zero;
                _currentState = PlayerState.Idle; // Aterrizó
            }
        }
    }
    
    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Black);

        SpriteBatch.Begin(samplerState: SamplerState.PointClamp);
        _bg_beach.Draw(SpriteBatch, Vector2.Zero, Color.White, 0f, Vector2.Zero, 1.5f, SpriteEffects.None, 1.0f);

        // --- Lógica de Dibujo por Partes ---

        // 1. DIBUJAMOS LAS PIERNAS
        // Las dibujamos en la posición base (_position_pj)
        // (Tu AnimatedSprite hereda de Sprite, así que tiene el método .Draw())
        _pj_legs.Draw(SpriteBatch, _position_pj);

        // 2. CALCULAMOS DÓNDE VA EL TORSO
        // El torso va *encima* de las piernas.
        float chestHeight = _pj_chest.Region.Height * _pj_chest.Scale.Y;
        
        // NOTA: Es posible que necesites ajustar el X y Y con valores fijos
        // para que calcen perfectamente. Ej: "chestPos.X = _position_pj.X + 10;"
        // También, tu sprite puede tener un origen (Origin) diferente a (0,0)
        Vector2 chestPos = new Vector2(
            _position_pj.X, // <- Ajusta esto si el torso está desalineado
            _position_pj.Y - chestHeight
        );

        // 3. DIBUJAMOS EL TORSO
        _pj_chest.Draw(SpriteBatch, chestPos);

        SpriteBatch.End();

        base.Draw(gameTime);
    }
}