using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using CameraAnimation.Interfaces;

namespace EliteDangerousStarMap.Input;

/// <summary>
/// FPS-style camera controller with WASD movement and mouse look.
/// Implements IAnimatedCamera to support the animation system.
/// </summary>
public class FpsCameraController : IAnimatedCamera
{
    // Camera properties - now with public setters for animation support
    private Vector3 _position;
    private float _yaw;
    private float _pitch;
    private float _zoom = 1.0f;

    /// <summary>
    /// Gets or sets the camera position.
    /// </summary>
    public Vector3 Position
    {
        get => _position;
        set => _position = value;
    }

    /// <summary>
    /// Gets or sets the camera yaw rotation.
    /// </summary>
    public float Yaw
    {
        get => _yaw;
        set => _yaw = value;
    }

    /// <summary>
    /// Gets or sets the camera pitch rotation.
    /// </summary>
    public float Pitch
    {
        get => _pitch;
        set => _pitch = MathHelper.Clamp(value, MinPitch, MaxPitch);
    }

    /// <summary>
    /// Gets or sets the camera zoom level.
    /// </summary>
    public float Zoom
    {
        get => _zoom;
        set
        {
            _zoom = MathHelper.Clamp(value, MinZoom, MaxZoom);
            UpdateProjectionMatrix(_centerPosition.X * 2, _centerPosition.Y * 2);
        }
    }

    // Movement settings
    public float MoveSpeed { get; set; } = 100f;
    public float MouseSensitivity { get; set; } = 0.005f;
    public float ZoomSpeed { get; set; } = 10f;
    public float MinZoom { get; set; } = 0.1f;
    public float MaxZoom { get; set; } = 10f;

    // Pitch limits (prevent flipping)
    private const float MaxPitch = MathHelper.PiOver2 - 0.01f;
    private const float MinPitch = -MathHelper.PiOver2 + 0.01f;

    // Mouse state tracking
    private MouseState _previousMouseState;
    private bool _isMouseLookEnabled;
    private Point _centerPosition;

    // View and projection matrices
    public Matrix ViewMatrix { get; private set; }
    public Matrix ProjectionMatrix { get; private set; }

    /// <summary>
    /// Tracks whether user input was detected in the last update.
    /// </summary>
    public bool HasUserInput { get; private set; }

    public Vector3 Forward => Vector3.Normalize(new Vector3(
        (float)(Math.Cos(_pitch) * Math.Sin(_yaw)),
        (float)Math.Sin(_pitch),
        (float)(Math.Cos(_pitch) * Math.Cos(_yaw))
    ));

    public Vector3 Right => Vector3.Normalize(Vector3.Cross(Forward, Vector3.Up));

    public Vector3 Up => Vector3.Normalize(Vector3.Cross(Right, Forward));

    public FpsCameraController(Vector3 startPosition, int viewportWidth, int viewportHeight)
    {
        _position = startPosition;
        _yaw = 0;
        _pitch = 0;
        _previousMouseState = Mouse.GetState();
        _centerPosition = new Point(viewportWidth / 2, viewportHeight / 2);

        UpdateProjectionMatrix(viewportWidth, viewportHeight);
        UpdateViewMatrix();
    }

    /// <summary>
    /// Sets the camera position to center on a world position.
    /// </summary>
    public void SetPosition(Vector3 position)
    {
        Position = position;
        UpdateViewMatrix();
    }

    /// <summary>
    /// Updates the projection matrix when viewport changes.
    /// </summary>
    public void UpdateProjectionMatrix(int viewportWidth, int viewportHeight)
    {
        _centerPosition = new Point(viewportWidth / 2, viewportHeight / 2);
        float aspectRatio = (float)viewportWidth / viewportHeight;
        ProjectionMatrix = Matrix.CreatePerspectiveFieldOfView(
            MathHelper.ToRadians(60f / _zoom),
            aspectRatio,
            0.1f,
            100000f
        );
    }

    /// <summary>
    /// Updates camera matrices (implements IAnimatedCamera).
    /// </summary>
    public void UpdateMatrices()
    {
        UpdateViewMatrix();
    }

    /// <summary>
    /// Updates camera based on input.
    /// </summary>
    /// <param name="gameTime">The game time.</param>
    /// <param name="keyboardState">Current keyboard state.</param>
    /// <param name="mouseState">Current mouse state.</param>
    /// <returns>True if user input was detected.</returns>
    public bool Update(GameTime gameTime, KeyboardState keyboardState, MouseState mouseState)
    {
        HasUserInput = false;
        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
        float moveAmount = MoveSpeed * deltaTime;

        // Adjust speed with shift/ctrl
        if (keyboardState.IsKeyDown(Keys.LeftShift))
            moveAmount *= 5f;
        if (keyboardState.IsKeyDown(Keys.LeftControl))
            moveAmount *= 0.2f;

        // WASD movement
        Vector3 movement = Vector3.Zero;

        if (keyboardState.IsKeyDown(Keys.W))
        {
            movement += Forward * moveAmount;
            HasUserInput = true;
        }
        if (keyboardState.IsKeyDown(Keys.S))
        {
            movement -= Forward * moveAmount;
            HasUserInput = true;
        }
        if (keyboardState.IsKeyDown(Keys.A))
        {
            movement -= Right * moveAmount;
            HasUserInput = true;
        }
        if (keyboardState.IsKeyDown(Keys.D))
        {
            movement += Right * moveAmount;
            HasUserInput = true;
        }
        if (keyboardState.IsKeyDown(Keys.Space))
        {
            movement += Vector3.Up * moveAmount;
            HasUserInput = true;
        }
        if (keyboardState.IsKeyDown(Keys.Q))
        {
            movement -= Vector3.Up * moveAmount;
            HasUserInput = true;
        }

        _position += movement;

        // Mouse look (right mouse button held)
        if (mouseState.RightButton == ButtonState.Pressed)
        {
            if (!_isMouseLookEnabled)
            {
                _isMouseLookEnabled = true;
                _previousMouseState = mouseState;
            }
            else
            {
                float deltaX = mouseState.X - _previousMouseState.X;
                float deltaY = mouseState.Y - _previousMouseState.Y;

                if (deltaX != 0 || deltaY != 0)
                {
                    _yaw -= deltaX * MouseSensitivity;
                    _pitch -= deltaY * MouseSensitivity;

                    // Clamp pitch to prevent flipping
                    _pitch = MathHelper.Clamp(_pitch, MinPitch, MaxPitch);
                    HasUserInput = true;
                }
            }
            _previousMouseState = mouseState;
        }
        else
        {
            _isMouseLookEnabled = false;
            _previousMouseState = mouseState;
        }

        // Mouse scroll zoom
        int scrollDelta = mouseState.ScrollWheelValue - _previousMouseState.ScrollWheelValue;
        if (scrollDelta != 0)
        {
            _zoom += scrollDelta * 0.001f * ZoomSpeed;
            _zoom = MathHelper.Clamp(_zoom, MinZoom, MaxZoom);
            UpdateProjectionMatrix(_centerPosition.X * 2, _centerPosition.Y * 2);
            HasUserInput = true;
        }

        UpdateViewMatrix();
        return HasUserInput;
    }

    private void UpdateViewMatrix()
    {
        Vector3 target = _position + Forward;
        ViewMatrix = Matrix.CreateLookAt(_position, target, Vector3.Up);
    }

    /// <summary>
    /// Creates a ray from screen coordinates for picking.
    /// </summary>
    public Ray GetPickRay(int screenX, int screenY, int viewportWidth, int viewportHeight)
    {
        Vector3 nearPoint = new Vector3(screenX, screenY, 0f);
        Vector3 farPoint = new Vector3(screenX, screenY, 1f);

        Matrix invViewProj = Matrix.Invert(ViewMatrix * ProjectionMatrix);

        Vector3 nearWorld = Vector3.Transform(nearPoint, invViewProj);
        Vector3 farWorld = Vector3.Transform(farPoint, invViewProj);

        // Convert from homogeneous coordinates
        var viewport = new Microsoft.Xna.Framework.Graphics.Viewport(0, 0, viewportWidth, viewportHeight);
        nearWorld = viewport.Unproject(nearPoint, ProjectionMatrix, ViewMatrix, Matrix.Identity);
        farWorld = viewport.Unproject(farPoint, ProjectionMatrix, ViewMatrix, Matrix.Identity);

        Vector3 direction = Vector3.Normalize(farWorld - nearWorld);
        return new Ray(nearWorld, direction);
    }
}
