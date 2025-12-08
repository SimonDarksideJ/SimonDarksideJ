using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace EliteDangerousStarMap.Rendering;

/// <summary>
/// Generates and renders spheres for star representation
/// </summary>
public class SphereRenderer : IDisposable
{
    private readonly GraphicsDevice _graphicsDevice;
    private VertexBuffer? _vertexBuffer;
    private IndexBuffer? _indexBuffer;
    private BasicEffect? _effect;
    private int _indexCount;
    private bool _disposed;

    public SphereRenderer(GraphicsDevice graphicsDevice)
    {
        _graphicsDevice = graphicsDevice;
        CreateSphereGeometry(16, 16); // segments for latitude and longitude
        CreateEffect();
    }

    private void CreateSphereGeometry(int latitudeSegments, int longitudeSegments)
    {
        var vertices = new List<VertexPositionNormalTexture>();
        var indices = new List<short>();

        // Generate vertices
        for (int lat = 0; lat <= latitudeSegments; lat++)
        {
            float theta = lat * MathF.PI / latitudeSegments;
            float sinTheta = MathF.Sin(theta);
            float cosTheta = MathF.Cos(theta);

            for (int lon = 0; lon <= longitudeSegments; lon++)
            {
                float phi = lon * 2 * MathF.PI / longitudeSegments;
                float sinPhi = MathF.Sin(phi);
                float cosPhi = MathF.Cos(phi);

                float x = cosPhi * sinTheta;
                float y = cosTheta;
                float z = sinPhi * sinTheta;

                var position = new Vector3(x, y, z);
                var normal = Vector3.Normalize(position);
                var texCoord = new Vector2((float)lon / longitudeSegments, (float)lat / latitudeSegments);

                vertices.Add(new VertexPositionNormalTexture(position, normal, texCoord));
            }
        }

        // Generate indices
        for (int lat = 0; lat < latitudeSegments; lat++)
        {
            for (int lon = 0; lon < longitudeSegments; lon++)
            {
                int current = lat * (longitudeSegments + 1) + lon;
                int next = current + longitudeSegments + 1;

                indices.Add((short)current);
                indices.Add((short)next);
                indices.Add((short)(current + 1));

                indices.Add((short)next);
                indices.Add((short)(next + 1));
                indices.Add((short)(current + 1));
            }
        }

        _vertexBuffer = new VertexBuffer(_graphicsDevice, 
            VertexPositionNormalTexture.VertexDeclaration, 
            vertices.Count, 
            BufferUsage.WriteOnly);
        _vertexBuffer.SetData(vertices.ToArray());

        _indexBuffer = new IndexBuffer(_graphicsDevice, 
            IndexElementSize.SixteenBits, 
            indices.Count, 
            BufferUsage.WriteOnly);
        _indexBuffer.SetData(indices.ToArray());

        _indexCount = indices.Count;
    }

    private void CreateEffect()
    {
        _effect = new BasicEffect(_graphicsDevice)
        {
            LightingEnabled = true,
            PreferPerPixelLighting = true,
            AmbientLightColor = new Vector3(0.2f, 0.2f, 0.2f)
        };

        _effect.DirectionalLight0.Enabled = true;
        _effect.DirectionalLight0.Direction = Vector3.Normalize(new Vector3(1, -1, -1));
        _effect.DirectionalLight0.DiffuseColor = new Vector3(1.0f, 1.0f, 1.0f);
        _effect.DirectionalLight0.SpecularColor = new Vector3(0.5f, 0.5f, 0.5f);

        _effect.EnableDefaultLighting();
    }

    /// <summary>
    /// Draws a sphere at the specified position with the given color and scale
    /// </summary>
    public void DrawSphere(Vector3 position, float scale, Color color, Matrix viewMatrix, Matrix projectionMatrix)
    {
        if (_effect == null || _vertexBuffer == null || _indexBuffer == null)
            return;

        Matrix worldMatrix = Matrix.CreateScale(scale) * Matrix.CreateTranslation(position);

        _effect.World = worldMatrix;
        _effect.View = viewMatrix;
        _effect.Projection = projectionMatrix;
        _effect.DiffuseColor = color.ToVector3();
        _effect.EmissiveColor = color.ToVector3() * 0.3f;

        _graphicsDevice.SetVertexBuffer(_vertexBuffer);
        _graphicsDevice.Indices = _indexBuffer;

        foreach (var pass in _effect.CurrentTechnique.Passes)
        {
            pass.Apply();
            _graphicsDevice.DrawIndexedPrimitives(
                PrimitiveType.TriangleList,
                0,
                0,
                _indexCount / 3);
        }
    }

    /// <summary>
    /// Draws a star sphere with appropriate color based on star properties
    /// </summary>
    public void DrawStar(Vector3 position, float size, bool isSelected, bool isPlayerLocation, Matrix viewMatrix, Matrix projectionMatrix)
    {
        Color color;
        float scale;

        if (isPlayerLocation)
        {
            color = Color.Cyan;
            scale = size * 1.5f;
        }
        else if (isSelected)
        {
            color = Color.Yellow;
            scale = size * 1.3f;
        }
        else
        {
            color = Color.White;
            scale = size;
        }

        DrawSphere(position, scale, color, viewMatrix, projectionMatrix);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _vertexBuffer?.Dispose();
            _indexBuffer?.Dispose();
            _effect?.Dispose();
            _disposed = true;
        }
    }
}
