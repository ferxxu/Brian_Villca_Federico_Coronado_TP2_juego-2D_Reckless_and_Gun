using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace reckless_and_gun.Entities
{
    public class HealthBar
    {
        private Texture2D _texture;
        private Vector2 _offset; // Ajuste de posición respecto al personaje
        private int _width;
        private int _height;

        public HealthBar(Texture2D texture, int width, int height, Vector2 offset)
        {
            _texture = texture;
            _width = width;
            _height = height;
            _offset = offset;
        }

        public void Draw(SpriteBatch spriteBatch, Vector2 parentPosition, float currentHealth, float maxHealth)
        {
            if (_texture == null) return;

            // 1. Calcular porcentaje (0.0 a 1.0)
            float percentage = MathHelper.Clamp(currentHealth / maxHealth, 0f, 1f);

            // 2. Calcular posición en pantalla (Posición del personaje + Offset)
            // Usamos parentPosition directamente para no depender de hitboxes rotas
            Vector2 finalPos = parentPosition + _offset;

            // 3. Dibujar Fondo (Negro)
            // Lo hacemos un poco más grande que la barra para que haga de borde
            Rectangle bgRect = new Rectangle((int)finalPos.X - 2, (int)finalPos.Y - 2, _width + 4, _height + 4);
            spriteBatch.Draw(_texture, bgRect, Color.Black);

            // 4. Dibujar Barra de Vida (Color dinámico)
            // Verde si está sana, Roja si está muriendo
            Color hpColor = percentage > 0.5f ? Color.LimeGreen : Color.Red;
            
            Rectangle hpRect = new Rectangle((int)finalPos.X, (int)finalPos.Y, (int)(_width * percentage), _height);
            spriteBatch.Draw(_texture, hpRect, hpColor);
        }
    }
}