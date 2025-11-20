using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Audio;
using MonoGameLibrary;
using MonoGameLibrary.Scenes;
using Gum.Wireframe;
using MonoGameGum;
using RenderingLibrary;
using System.Collections.Generic;
using System;
using MonoGameGum.GueDeriving;

namespace reckless_and_gun.Scenes
{
    public class SettingsScene : Scene
    {
        // --- CONSTANTES ---
        private const string TITLE_TEXT = "SETTINGS";

        // --- ESTADOS LÓGICOS ---
        private Point[] _resolutions = {
            new Point(1280, 600), new Point(1280, 720),
            new Point(1366, 768), new Point(1920, 1080)
        };
        private int _resIndex = 0;
        private float _volume = 1.0f;
        private MouseState _lastMouse;

        // --- RECURSOS ---
        private SpriteFont _font;
        private Texture2D _background;

        // --- UI ---
        private UiButton _btnRes, _btnBack;
        // private UiButton _btnVolMinus, _btnVolPlus; // Descomentar si se usan

        public override void Initialize()
        {
            base.Initialize();
            InitGum();

            Core.ExitOnEscape = false;
            _volume = SoundEffect.MasterVolume;

            SyncCurrentResolutionIndex();
        }

        public override void LoadContent()
        {
            InitGum(); // Asegurar inicialización
            ClearOldUi(); // Limpiar basura anterior para evitar duplicados

            _font = Core.Content.Load<SpriteFont>("font");
            _background = Core.Content.Load<Texture2D>("jungle");

            CreateLayout();
        }

        public override void UnloadContent()
        {
            // 1. Limpiar botones manuales
            _btnRes?.Dispose();
            _btnBack?.Dispose();
            
            // 2. Limpiar la capa de Gum para evitar el crash al volver al Title
            ClearOldUi();
        }

        public override void Update(GameTime gameTime)
        {
            SystemManagers.Default.Activity(gameTime.ElapsedGameTime.TotalSeconds);

            var mouse = Mouse.GetState();

            // Actualizar lógica de botones
            _btnRes.Update(mouse, _lastMouse);
            _btnBack.Update(mouse, _lastMouse);

            if (Core.Input.Keyboard.WasKeyJustPressed(Keys.Escape)) GoBack();

            _lastMouse = mouse;
        }

        public override void Draw(GameTime gameTime)
        {
            Core.GraphicsDevice.Clear(Color.Black);

            // 1. Fondo y Textos fijos
            Core.SpriteBatch.Begin();
            DrawBackground();
            DrawStaticLabels();
            Core.SpriteBatch.End();

            // 2. Botones (Gum)
            SystemManagers.Default.Draw();

            // 3. Textos sobre botones (Dinámicos)
            Core.SpriteBatch.Begin();

            // Usamos escala 0.4f para que entre en los botones chicos
            _btnRes.DrawText(Core.SpriteBatch, _font, $"{_resolutions[_resIndex].X} x {_resolutions[_resIndex].Y}", 0.4f);
            _btnBack.DrawText(Core.SpriteBatch, _font, "Back", 0.4f);

            // Porcentaje de volumen
            DrawCenteredText($"{(int)(_volume * 100)}%", 310, Color.Yellow, 0.4f);

            Core.SpriteBatch.End();
        }

        // ---------------------------------------------------------
        // MÉTODOS PRIVADOS (LÓGICA INTERNA)
        // ---------------------------------------------------------

        private void InitGum()
        {
            if (SystemManagers.Default == null)
            {
                SystemManagers.Default = new SystemManagers();
                SystemManagers.Default.Initialize(Core.GraphicsDevice, fullInstantiation: true);
            }
        }

        private void SyncCurrentResolutionIndex()
        {
            for (int i = 0; i < _resolutions.Length; i++)
            {
                if (Core.Graphics.PreferredBackBufferWidth == _resolutions[i].X &&
                    Core.Graphics.PreferredBackBufferHeight == _resolutions[i].Y)
                {
                    _resIndex = i;
                    break;
                }
            }
        }

        // Aquí corregimos el error de ReadOnlyCollection
        private void ClearOldUi()
        {
            if (SystemManagers.Default == null) return;
            
            var layer = SystemManagers.Default.Renderer.MainLayer;
            
            // Creamos una COPIA de la lista para poder iterar y borrar
            var itemsToRemove = new List<RenderingLibrary.Graphics.IRenderableIpso>(layer.Renderables);
            
            foreach (var item in itemsToRemove)
            {
                layer.Remove(item);
            }
        }

        private void CreateLayout()
        {
            int cx = Core.GraphicsDevice.Viewport.Width / 2;
            int y = 240;

            // BOTONES MÁS CHICOS:
            // Ancho reducido y Alto bajado a 30px
            
            _btnRes = new UiButton(cx + 80, y, 180, 30, () => {
                _resIndex = (_resIndex + 1) % _resolutions.Length;
                ApplyResolution();
            });

            // Botón Back más compacto
            _btnBack = new UiButton(cx, y + 250, 150, 30, GoBack);
        }

        private void ApplyResolution()
        {
            Core.Graphics.PreferredBackBufferWidth = _resolutions[_resIndex].X;
            Core.Graphics.PreferredBackBufferHeight = _resolutions[_resIndex].Y;
            Core.Graphics.ApplyChanges();
            
            // Importante: Resetear zoom de cámara si Gum se confunde al cambiar res
            if (SystemManagers.Default != null)
            {
                 SystemManagers.Default.Renderer.Camera.Zoom = 1;
            }
        }

        private void GoBack()
        {
            Core.ExitOnEscape = true;
            Core.ChangeScene(new TitleScene());
        }

        private void DrawBackground()
        {
            if (_background != null)
            {
                Core.SpriteBatch.Draw(_background, new Rectangle(0, 0, Core.GraphicsDevice.Viewport.Width, Core.GraphicsDevice.Viewport.Height), Color.White);
            }
        }

        private void DrawStaticLabels()
        {
            int cx = Core.GraphicsDevice.Viewport.Width / 2;

            // Título
            DrawCenteredText(TITLE_TEXT, 50, Color.Gold, 1.2f);

            // Etiquetas (Resolution / Volume) más chicas y ajustadas
            DrawLabel("Resolution:", cx - 320, 205, 0.4f);
            DrawLabel("Volume:", cx - 320, 305, 0.4f);
        }

        // Helper para dibujar etiquetas en posición específica con escala
        private void DrawLabel(string text, float x, float y, float scale)
        {
            Core.SpriteBatch.DrawString(_font, text, new Vector2(x, y), Color.White, 0, Vector2.Zero, scale, SpriteEffects.None, 0);
        }

        // Helper para centrar texto
        private void DrawCenteredText(string text, float y, Color color, float scale = 0.5f)
        {
            Vector2 size = _font.MeasureString(text) * scale;
            Core.SpriteBatch.DrawString(_font, text, new Vector2((Core.GraphicsDevice.Viewport.Width - size.X) / 2, y), color, 0, Vector2.Zero, scale, SpriteEffects.None, 0);
        }

        // ---------------------------------------------------------
        // CLASE UI BUTTON (WRAPPER)
        // ---------------------------------------------------------
        private class UiButton
        {
            private ColoredRectangleRuntime _border;
            private ColoredRectangleRuntime _inner;
            private Action _onClick;
            private Rectangle _hitbox;

            public UiButton(float centerX, float centerY, float w, float h, Action onClick)
            {
                _onClick = onClick;

                // Bordes finos para botones chicos
                _border = new ColoredRectangleRuntime { X = centerX - w / 2 - 2, Y = centerY - h / 2 - 2, Width = w + 4, Height = h + 4, Color = Color.White };
                _border.AddToManagers(SystemManagers.Default, null);

                _inner = new ColoredRectangleRuntime { X = centerX - w / 2, Y = centerY - h / 2, Width = w, Height = h, Color = Color.Black * 0.8f };
                _inner.AddToManagers(SystemManagers.Default, null);

                _hitbox = new Rectangle((int)(centerX - w / 2), (int)(centerY - h / 2), (int)w, (int)h);
            }

            public void Update(MouseState current, MouseState last)
            {
                if (_hitbox.Contains(current.Position))
                {
                    _inner.Color = Color.DarkOrange * 0.8f; // Hover
                    if (current.LeftButton == ButtonState.Pressed && last.LeftButton == ButtonState.Released)
                    {
                        _inner.Color = Color.Orange; // Click
                        _onClick?.Invoke();
                    }
                }
                else
                {
                    _inner.Color = Color.Black * 0.8f; // Normal
                }
            }

            public void DrawText(SpriteBatch sb, SpriteFont font, string text, float scale = 1.0f)
            {
                Vector2 size = font.MeasureString(text) * scale;
                Vector2 pos = new Vector2(
                    _inner.X + (_inner.Width - size.X) / 2,
                    _inner.Y + (_inner.Height - size.Y) / 2
                );
                sb.DrawString(font, text, pos, Color.White, 0, Vector2.Zero, scale, SpriteEffects.None, 0);
            }

            public void Dispose()
            {
                _border.RemoveFromManagers();
                _inner.RemoveFromManagers();
            }
        }
    }
}