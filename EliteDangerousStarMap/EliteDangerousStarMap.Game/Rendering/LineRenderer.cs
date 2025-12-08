using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace EliteDangerousStarMap.Rendering;

/// <summary>
/// Renders lines between points (for route visualization)
/// </summary>
public class LineRenderer : IDisposable
{
    private readonly GraphicsDevice _graphicsDevice;
    private BasicEffect _effect;
    private bool _disposed;

    public LineRenderer(GraphicsDevice graphicsDevice)
    {
        _graphicsDevice = graphicsDevice;
        _effect = new BasicEffect(graphicsDevice)
        {
            VertexColorEnabled = true,
            LightingEnabled = false
        };
    }

    /// <summary>
    /// Draws a line between two 3D points
    /// </summary>
    public void DrawLine(Vector3 start, Vector3 end, Color color, Matrix viewMatrix, Matrix projectionMatrix)
    {
        var vertices = new VertexPositionColor[]
        {
            new VertexPositionColor(start, color),
            new VertexPositionColor(end, color)
        };

        _effect.World = Matrix.Identity;
        _effect.View = viewMatrix;
        _effect.Projection = projectionMatrix;

        foreach (var pass in _effect.CurrentTechnique.Passes)
        {
            pass.Apply();
            _graphicsDevice.DrawUserPrimitives(PrimitiveType.LineList, vertices, 0, 1);
        }
    }

    /// <summary>
    /// Draws a dashed line between two 3D points
    /// </summary>
    public void DrawDashedLine(Vector3 start, Vector3 end, Color color, float dashLength, Matrix viewMatrix, Matrix projectionMatrix)
    {
        Vector3 direction = end - start;
        float totalLength = direction.Length();
        direction.Normalize();

        float currentLength = 0;
        bool drawDash = true;

        while (currentLength < totalLength)
        {
            float nextLength = Math.Min(currentLength + dashLength, totalLength);
            
            if (drawDash)
            {
                Vector3 dashStart = start + direction * currentLength;
                Vector3 dashEnd = start + direction * nextLength;
                DrawLine(dashStart, dashEnd, color, viewMatrix, projectionMatrix);
            }

            currentLength = nextLength;
            drawDash = !drawDash;
        }
    }

    /// <summary>
    /// Draws a route as connected lines through multiple waypoints
    /// </summary>
    public void DrawRoute(List<Vector3> waypoints, Color color, Matrix viewMatrix, Matrix projectionMatrix)
    {
        if (waypoints.Count < 2)
            return;

        for (int i = 0; i < waypoints.Count - 1; i++)
        {
            DrawLine(waypoints[i], waypoints[i + 1], color, viewMatrix, projectionMatrix);
        }
    }

    /// <summary>
    /// Draws a grid on the XZ plane for reference
    /// </summary>
    public void DrawGrid(float size, int divisions, Color color, Matrix viewMatrix, Matrix projectionMatrix)
    {
        float step = size / divisions;
        float halfSize = size / 2;

        for (int i = 0; i <= divisions; i++)
        {
            float pos = -halfSize + i * step;
            
            // Lines parallel to Z axis
            DrawLine(
                new Vector3(pos, 0, -halfSize),
                new Vector3(pos, 0, halfSize),
                color, viewMatrix, projectionMatrix);

            // Lines parallel to X axis
            DrawLine(
                new Vector3(-halfSize, 0, pos),
                new Vector3(halfSize, 0, pos),
                color, viewMatrix, projectionMatrix);
        }
    }

    /// <summary>
    /// Draws coordinate axes for orientation
    /// </summary>
    public void DrawAxes(float length, Matrix viewMatrix, Matrix projectionMatrix)
    {
        DrawLine(Vector3.Zero, Vector3.Right * length, Color.Red, viewMatrix, projectionMatrix);
        DrawLine(Vector3.Zero, Vector3.Up * length, Color.Green, viewMatrix, projectionMatrix);
        DrawLine(Vector3.Zero, Vector3.Forward * length, Color.Blue, viewMatrix, projectionMatrix);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _effect?.Dispose();
            _disposed = true;
        }
    }
}
