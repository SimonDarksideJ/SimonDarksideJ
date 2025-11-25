using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace EliteDangerousStarMap.Input;

/// <summary>
/// FPS-style camera controller with WASD movement and mouse look
/// </summary>
public class FpsCameraController
{
    // Camera properties
    public Vector3 Position { get; private set; }
    public float Yaw { get; private set; }
    public float Pitch { get; private set; }
    public float Zoom { get; private set; } = 1.0f;

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

    public Vector3 Forward => Vector3.Normalize(new Vector3(
        (float)(Math.Cos(Pitch) * Math.Sin(Yaw)),
        (float)Math.Sin(Pitch),
        (float)(Math.Cos(Pitch) * Math.Cos(Yaw))
    ));

    public Vector3 Right => Vector3.Normalize(Vector3.Cross(Forward, Vector3.Up));

    public Vector3 Up => Vector3.Normalize(Vector3.Cross(Right, Forward));

    public FpsCameraController(Vector3 startPosition, int viewportWidth, int viewportHeight)
    {
        Position = startPosition;
        Yaw = 0;
        Pitch = 0;
        _previousMouseState = Mouse.GetState();
        _centerPosition = new Point(viewportWidth / 2, viewportHeight / 2);

        UpdateProjectionMatrix(viewportWidth, viewportHeight);
        UpdateViewMatrix();
    }

    /// <summary>
    /// Sets the camera position to center on a world position
    /// </summary>
    public void SetPosition(Vector3 position)
    {
        Position = position;
        UpdateViewMatrix();
    }

    /// <summary>
    /// Updates the projection matrix when viewport changes
    /// </summary>
    public void UpdateProjectionMatrix(int viewportWidth, int viewportHeight)
    {
        _centerPosition = new Point(viewportWidth / 2, viewportHeight / 2);
        float aspectRatio = (float)viewportWidth / viewportHeight;
        ProjectionMatrix = Matrix.CreatePerspectiveFieldOfView(
            MathHelper.ToRadians(60f / Zoom),
            aspectRatio,
            0.1f,
            100000f
        );
    }

    /// <summary>
    /// Updates camera based on input
    /// </summary>
    public void Update(GameTime gameTime, KeyboardState keyboardState, MouseState mouseState)
    {
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
            movement += Forward * moveAmount;
        if (keyboardState.IsKeyDown(Keys.S))
            movement -= Forward * moveAmount;
        if (keyboardState.IsKeyDown(Keys.A))
            movement -= Right * moveAmount;
        if (keyboardState.IsKeyDown(Keys.D))
            movement += Right * moveAmount;
        if (keyboardState.IsKeyDown(Keys.Space))
            movement += Vector3.Up * moveAmount;
        if (keyboardState.IsKeyDown(Keys.Q))
            movement -= Vector3.Up * moveAmount;

        Position += movement;

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

                Yaw -= deltaX * MouseSensitivity;
                Pitch -= deltaY * MouseSensitivity;

                // Clamp pitch to prevent flipping
                Pitch = MathHelper.Clamp(Pitch, MinPitch, MaxPitch);
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
            Zoom += scrollDelta * 0.001f * ZoomSpeed;
            Zoom = MathHelper.Clamp(Zoom, MinZoom, MaxZoom);
            UpdateProjectionMatrix(_centerPosition.X * 2, _centerPosition.Y * 2);
        }

        UpdateViewMatrix();
    }

    private void UpdateViewMatrix()
    {
        Vector3 target = Position + Forward;
        ViewMatrix = Matrix.CreateLookAt(Position, target, Vector3.Up);
    }

    /// <summary>
    /// Creates a ray from screen coordinates for picking
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
