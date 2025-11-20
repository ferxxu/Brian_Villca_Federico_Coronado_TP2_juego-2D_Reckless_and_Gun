using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGameLibrary;
using MonoGameLibrary.Scenes;
using Gum.Wireframe;          
using MonoGameGum;           
using RenderingLibrary;       
using System;
using MonoGameGum.GueDeriving;
using MonoGameLibrary.Graphics;

namespace reckless_and_gun.Scenes
{
    public class TitleScene : Scene
    {
        // Textos
        private const string RECKLESS_TEXT = "Reckless";
        private const string AND_TEXT = "And";
        private const string GUN_TEXT = "Gun";
        private const string START_TEXT = "Start";
        private const string SETTINGS_TEXT = "Settings";
        private const string EXIT_TEXT = "Exit";

        // Variables visuales
        private SpriteFont _font;
        private Texture2D _background;

        // Posiciones de textos (Títulos)
        private Vector2 _recklessTextPos, _recklessTextOrigin;
        private Vector2 _andTextPos, _andTextOrigin;
        private Vector2 _gunTextPos, _gunTextOrigin;

        // Posiciones de botones (Centros)
        private Vector2 _startPos, _startOrigin;
        private Vector2 _settingsPos, _settingsOrigin;
        private Vector2 _exitPos, _exitOrigin;

        // --- ELEMENTOS DE GUM (Code-Only) ---
        private ColoredRectangleRuntime _btnStart;
        private ColoredRectangleRuntime _btnSettings;
        private ColoredRectangleRuntime _btnExit;

        // Dimensiones de los botones
        private int _btnWidth = 200;
        private int _btnHeight = 40;

        public override void Initialize()
        {
            base.Initialize();

            // Validación Singleton para evitar crashes al volver al menú
            if (SystemManagers.Default == null)
            {
                SystemManagers.Default = new SystemManagers();
                SystemManagers.Default.Initialize(Core.GraphicsDevice, fullInstantiation: true);
            }
            Core.ExitOnEscape = true;
        }

        public override void LoadContent()
        {
            // --- 1. LIMPIEZA PREVENTIVA ---
            if (SystemManagers.Default != null)
            {
                var mainLayer = SystemManagers.Default.Renderer.MainLayer;
                // Copiamos la lista para poder borrar sin romper el iterador
                var objetosParaBorrar = new System.Collections.Generic.List<RenderingLibrary.Graphics.IRenderableIpso>(mainLayer.Renderables);
                foreach (var item in objetosParaBorrar)
                {
                    mainLayer.Remove(item);
                }
            }

            // --- 2. CARGA DE RECURSOS ---
            _font = Core.Content.Load<SpriteFont>("font");
            _background = Core.Content.Load<Texture2D>("jungle");

            // Calcular Posiciones
            Vector2 size = _font.MeasureString(RECKLESS_TEXT);
            _recklessTextPos = new Vector2(640, 100);
            _recklessTextOrigin = size * 0.5f;

            size = _font.MeasureString(AND_TEXT);
            _andTextPos = new Vector2(640, 207);
            _andTextOrigin = size * 0.5f;

            size = _font.MeasureString(GUN_TEXT);
            _gunTextPos = new Vector2(640, 307);
            _gunTextOrigin = size * 0.5f;

            size = _font.MeasureString(START_TEXT);
            _startPos = new Vector2(640, 450);
            _startOrigin = size * 0.5f;

            size = _font.MeasureString(SETTINGS_TEXT);
            _settingsPos = new Vector2(640, 500);
            _settingsOrigin = size * 0.5f;

            size = _font.MeasureString(EXIT_TEXT);
            _exitPos = new Vector2(640, 550);
            _exitOrigin = size * 0.5f;

            // --- 3. CREAR BOTONES NUEVOS ---
            _btnStart = CrearBotonGum(_startPos);
            _btnSettings = CrearBotonGum(_settingsPos);
            _btnExit = CrearBotonGum(_exitPos);
        }

        public override void UnloadContent()
        {
            // Limpieza al salir de la escena
            if (_btnStart != null) _btnStart.RemoveFromManagers();
            if (_btnSettings != null) _btnSettings.RemoveFromManagers();
            if (_btnExit != null) _btnExit.RemoveFromManagers();
        }

        // Función auxiliar para crear botones con estilo DORADO
        private ColoredRectangleRuntime CrearBotonGum(Vector2 posicionCentro)
        {
            int grosorBorde = 4;

            // 1. Crear el BORDE (Fondo externo)
            var borde = new ColoredRectangleRuntime();
            borde.Width = _btnWidth + (grosorBorde * 2);
            borde.Height = _btnHeight + (grosorBorde * 2);
            borde.X = posicionCentro.X - (borde.Width / 2);
            borde.Y = posicionCentro.Y - (borde.Height / 2);
            
            // CAMBIO: Color del borde a Dorado
            borde.Color = Color.Gold; 
            
            borde.AddToManagers(SystemManagers.Default, null);

            // 2. Crear el INTERIOR (Botón interactivo)
            var btn = new ColoredRectangleRuntime();
            btn.Width = _btnWidth;
            btn.Height = _btnHeight;
            btn.X = borde.X + grosorBorde;
            btn.Y = borde.Y + grosorBorde;

            // Color inicial: Negro transparente para que resalte el borde dorado
            btn.Color = Color.Black * 0.8f;

            btn.AddToManagers(SystemManagers.Default, null);

            return btn;
        }

        public override void Update(GameTime gameTime)
        {
            SystemManagers.Default.Activity(gameTime.ElapsedGameTime.TotalSeconds);

            MouseState mouse = Mouse.GetState();
            Point mousePoint = new Point(mouse.X, mouse.Y);

            // --- Lógica de botones ---

            // Start
            ManejarBoton(_btnStart, mousePoint, mouse, () =>
            {
                Core.ChangeScene(new GameScene());
            });

            // Settings (Fusionado en uno solo)
            ManejarBoton(_btnSettings, mousePoint, mouse, () =>
            {
                Core.ChangeScene(new SettingsScene());
            });

            // Exit
            ManejarBoton(_btnExit, mousePoint, mouse, () =>
            {
                Environment.Exit(0);
            });

            // Input de teclado (Enter para iniciar rápido)
            if (Core.Input.Keyboard.WasKeyJustPressed(Keys.Enter))
            {
                Core.ChangeScene(new GameScene());
            }
        }

        private void ManejarBoton(ColoredRectangleRuntime btn, Point mousePos, MouseState mouseState, Action onClick)
        {
            Rectangle rectBoton = new Rectangle((int)btn.X, (int)btn.Y, (int)btn.Width, (int)btn.Height);

            if (rectBoton.Contains(mousePos))
            {
                // CAMBIO: Hover -> Dorado semitransparente
                btn.Color = Color.Gold * 0.6f;

                if (mouseState.LeftButton == ButtonState.Pressed)
                {
                    // CAMBIO: Click -> Naranja Oscuro Intenso
                    btn.Color = Color.DarkOrange;
                    onClick?.Invoke();
                }
            }
            else
            {
                // Normal -> Negro semitransparente
                btn.Color = Color.Black * 0.8f;
            }
        }

        public override void Draw(GameTime gameTime)
        {
            Core.GraphicsDevice.Clear(Color.Black);

            // 1. DIBUJAR FONDO
            Core.SpriteBatch.Begin(samplerState: SamplerState.PointClamp);
            Rectangle destinationRectangle = new Rectangle(0, 0, Core.GraphicsDevice.Viewport.Width, Core.GraphicsDevice.Viewport.Height);
            Core.SpriteBatch.Draw(_background, destinationRectangle, Color.White);
            DrawTitleText();
            Core.SpriteBatch.End();

            // 2. DIBUJAR GUM (Botones)
            SystemManagers.Default.Draw();

            // 3. DIBUJAR TEXTOS SOBRE BOTONES
            Core.SpriteBatch.Begin(samplerState: SamplerState.PointClamp);
            
            // Texto Blanco para contrastar con hover dorado/naranja
            Core.SpriteBatch.DrawString(_font, START_TEXT, _startPos, Color.Orange, 0.0f, _startOrigin, 0.45f, SpriteEffects.None, 0.0f);
            Core.SpriteBatch.DrawString(_font, SETTINGS_TEXT, _settingsPos, Color.Orange, 0.0f, _settingsOrigin, 0.45f, SpriteEffects.None, 0.0f);
            Core.SpriteBatch.DrawString(_font, EXIT_TEXT, _exitPos, Color.Orange, 0.0f, _exitOrigin, 0.45f, SpriteEffects.None, 0.0f);

            Core.SpriteBatch.End();
        }

        private void DrawTitleText()
        {
            Color dropShadowColor = Color.Black * 0.5f;

            Core.SpriteBatch.DrawString(_font, RECKLESS_TEXT, _recklessTextPos + new Vector2(10, 10), dropShadowColor, 0.0f, _recklessTextOrigin, 1.0f, SpriteEffects.None, 1.0f);
            Core.SpriteBatch.DrawString(_font, RECKLESS_TEXT, _recklessTextPos, Color.Orange, 0.0f, _recklessTextOrigin, 1.0f, SpriteEffects.None, 1.0f);

            Core.SpriteBatch.DrawString(_font, AND_TEXT, _andTextPos + new Vector2(10, 10), dropShadowColor, 0.0f, _andTextOrigin, 0.65f, SpriteEffects.None, 1.0f);
            Core.SpriteBatch.DrawString(_font, AND_TEXT, _andTextPos, Color.Orange, 0.0f, _andTextOrigin, 0.65f, SpriteEffects.None, 1.0f);

            Core.SpriteBatch.DrawString(_font, GUN_TEXT, _gunTextPos + new Vector2(10, 10), dropShadowColor, 0.0f, _gunTextOrigin, 1.0f, SpriteEffects.None, 1.0f);
            Core.SpriteBatch.DrawString(_font, GUN_TEXT, _gunTextPos, Color.Orange, 0.0f, _gunTextOrigin, 1.0f, SpriteEffects.None, 1.0f);
        }
    }
}