// Camera2D.cs
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MonoGameLibrary.Camera;

public class Camera2D
{
    private Matrix _transform;
    public Vector2 Position { get; set; }
    public float Zoom { get; set; }
    public float Rotation { get; set; }

    public Camera2D()
    {
        Zoom = 1.0f;
        Rotation = 0.0f;
        Position = Vector2.Zero;
    }

    // Genera la Matriz de Vista (View Matrix)
    public Matrix GetViewMatrix(Viewport viewport)
    {
        _transform =
            // 1. Mueve el "mundo" en la dirección opuesta a la cámara
            Matrix.CreateTranslation(new Vector3(-Position.X, -Position.Y, 0)) *

            // 2. Rota alrededor del centro de la pantalla
            Matrix.CreateRotationZ(Rotation) *

            // 3. Aplica el Zoom
            Matrix.CreateScale(Zoom) *

            // 4. Centra la vista en la pantalla
            Matrix.CreateTranslation(new Vector3(viewport.Width * 0.5f, viewport.Height * 0.5f, 0));

        return _transform;
    }

    public void Follow(Vector2 target, Rectangle worldBounds, Viewport viewport)
    {
        Position = target;
        float minX = viewport.Width / 2f;
        float maxX = worldBounds.Width - (viewport.Width / 2f);
        float minY = viewport.Height / 2f;
        float maxY = worldBounds.Height - (viewport.Height / 2f);

        Position = new Vector2(
            MathHelper.Clamp(Position.X, minX, maxX),
            MathHelper.Clamp(Position.Y, minY, maxY)
        );
    }
}