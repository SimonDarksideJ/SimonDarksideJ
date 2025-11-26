using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace EliteDangerousStarMap.Rendering;

/// <summary>
/// Renders a simple cube primitive for the player ship
/// </summary>
public class CubeRenderer : IDisposable
{
    private readonly GraphicsDevice _graphicsDevice;
    private readonly BasicEffect _effect;
    private VertexPositionColor[] _vertices = null!;
    private short[] _indices = null!;
    private bool _disposed;

    public CubeRenderer(GraphicsDevice graphicsDevice)
    {
        _graphicsDevice = graphicsDevice;
        _effect = new BasicEffect(graphicsDevice)
        {
            VertexColorEnabled = true,
            LightingEnabled = false
        };
        
        CreateCubeGeometry();
    }

    private void CreateCubeGeometry()
    {
        // Create cube vertices
        var cubeVertices = new Vector3[]
        {
            new(-0.5f, -0.5f, -0.5f), // 0: back-bottom-left
            new( 0.5f, -0.5f, -0.5f), // 1: back-bottom-right
            new( 0.5f,  0.5f, -0.5f), // 2: back-top-right
            new(-0.5f,  0.5f, -0.5f), // 3: back-top-left
            new(-0.5f, -0.5f,  0.5f), // 4: front-bottom-left
            new( 0.5f, -0.5f,  0.5f), // 5: front-bottom-right
            new( 0.5f,  0.5f,  0.5f), // 6: front-top-right
            new(-0.5f,  0.5f,  0.5f), // 7: front-top-left
        };

        // Different colors for each face
        var colors = new Color[]
        {
            new Color(255, 150, 50),  // Orange front
            new Color(200, 100, 50),  // Dark orange back
            new Color(255, 200, 100), // Light orange top
            new Color(150, 75, 25),   // Brown bottom
            new Color(255, 175, 75),  // Side 1
            new Color(200, 125, 50),  // Side 2
        };

        _vertices = new VertexPositionColor[24]; // 6 faces * 4 vertices
        
        // Front face
        _vertices[0] = new VertexPositionColor(cubeVertices[4], colors[0]);
        _vertices[1] = new VertexPositionColor(cubeVertices[5], colors[0]);
        _vertices[2] = new VertexPositionColor(cubeVertices[6], colors[0]);
        _vertices[3] = new VertexPositionColor(cubeVertices[7], colors[0]);
        
        // Back face
        _vertices[4] = new VertexPositionColor(cubeVertices[1], colors[1]);
        _vertices[5] = new VertexPositionColor(cubeVertices[0], colors[1]);
        _vertices[6] = new VertexPositionColor(cubeVertices[3], colors[1]);
        _vertices[7] = new VertexPositionColor(cubeVertices[2], colors[1]);
        
        // Top face
        _vertices[8] = new VertexPositionColor(cubeVertices[7], colors[2]);
        _vertices[9] = new VertexPositionColor(cubeVertices[6], colors[2]);
        _vertices[10] = new VertexPositionColor(cubeVertices[2], colors[2]);
        _vertices[11] = new VertexPositionColor(cubeVertices[3], colors[2]);
        
        // Bottom face
        _vertices[12] = new VertexPositionColor(cubeVertices[0], colors[3]);
        _vertices[13] = new VertexPositionColor(cubeVertices[1], colors[3]);
        _vertices[14] = new VertexPositionColor(cubeVertices[5], colors[3]);
        _vertices[15] = new VertexPositionColor(cubeVertices[4], colors[3]);
        
        // Right face
        _vertices[16] = new VertexPositionColor(cubeVertices[5], colors[4]);
        _vertices[17] = new VertexPositionColor(cubeVertices[1], colors[4]);
        _vertices[18] = new VertexPositionColor(cubeVertices[2], colors[4]);
        _vertices[19] = new VertexPositionColor(cubeVertices[6], colors[4]);
        
        // Left face
        _vertices[20] = new VertexPositionColor(cubeVertices[0], colors[5]);
        _vertices[21] = new VertexPositionColor(cubeVertices[4], colors[5]);
        _vertices[22] = new VertexPositionColor(cubeVertices[7], colors[5]);
        _vertices[23] = new VertexPositionColor(cubeVertices[3], colors[5]);

        // Create indices for 6 faces (2 triangles per face)
        _indices = new short[36];
        for (int face = 0; face < 6; face++)
        {
            int baseVertex = face * 4;
            int baseIndex = face * 6;
            
            _indices[baseIndex + 0] = (short)(baseVertex + 0);
            _indices[baseIndex + 1] = (short)(baseVertex + 1);
            _indices[baseIndex + 2] = (short)(baseVertex + 2);
            _indices[baseIndex + 3] = (short)(baseVertex + 0);
            _indices[baseIndex + 4] = (short)(baseVertex + 2);
            _indices[baseIndex + 5] = (short)(baseVertex + 3);
        }
    }

    /// <summary>
    /// Draws a ship cube at the specified position with scale
    /// </summary>
    public void DrawShip(Vector3 position, float scale, Vector3 velocity, Matrix viewMatrix, Matrix projectionMatrix)
    {
        if (scale <= 0.001f) return; // Don't draw if too small
        
        // Calculate rotation based on velocity direction
        Matrix rotation = Matrix.Identity;
        if (velocity.LengthSquared() > 0.01f)
        {
            Vector3 forward = Vector3.Normalize(velocity);
            Vector3 up = Vector3.Up;
            Vector3 right = Vector3.Cross(up, forward);
            if (right.LengthSquared() > 0.001f)
            {
                right = Vector3.Normalize(right);
                up = Vector3.Cross(forward, right);
                rotation = new Matrix(
                    right.X, right.Y, right.Z, 0,
                    up.X, up.Y, up.Z, 0,
                    forward.X, forward.Y, forward.Z, 0,
                    0, 0, 0, 1
                );
            }
        }
        
        // Create world matrix with elongated shape (ship-like)
        Matrix scaleMatrix = Matrix.CreateScale(scale * 1.5f, scale * 0.5f, scale * 3f);
        Matrix world = scaleMatrix * rotation * Matrix.CreateTranslation(position);
        
        _effect.World = world;
        _effect.View = viewMatrix;
        _effect.Projection = projectionMatrix;
        
        foreach (var pass in _effect.CurrentTechnique.Passes)
        {
            pass.Apply();
            _graphicsDevice.DrawUserIndexedPrimitives(
                PrimitiveType.TriangleList,
                _vertices,
                0,
                _vertices.Length,
                _indices,
                0,
                _indices.Length / 3);
        }
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
