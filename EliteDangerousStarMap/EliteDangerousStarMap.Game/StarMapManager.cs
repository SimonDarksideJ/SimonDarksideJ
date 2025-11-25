using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using EliteDangerousStarMap.Models;
using EliteDangerousStarMap.Services;
using EliteDangerousStarMap.Rendering;
using EliteDangerousStarMap.Input;
using EliteDangerousStarMap.UI;

namespace EliteDangerousStarMap;

/// <summary>
/// Manages the star map visualization and interaction
/// </summary>
public class StarMapManager : IDisposable
{
    private readonly GraphicsDevice _graphicsDevice;
    private readonly EdsmApiService _apiService;
    private readonly SphereRenderer _sphereRenderer;
    private readonly LineRenderer _lineRenderer;
    private readonly FpsCameraController _camera;
    private readonly UiRenderer _uiRenderer;
    private readonly PlayerShip _playerShip;
    private readonly Random _random;

    private List<StarSystem> _starSystems;
    private StarSystem? _selectedSystem;
    private bool _isLoading;
    private string _loadingMessage;
    private MouseState _previousMouseState;
    private bool _disposed;

    // Rendering settings
    private const float BaseStarSize = 0.5f;
    private const float SelectionRadius = 5.0f;

    public StarMapManager(GraphicsDevice graphicsDevice)
    {
        _graphicsDevice = graphicsDevice;
        _apiService = new EdsmApiService();
        _sphereRenderer = new SphereRenderer(graphicsDevice);
        _lineRenderer = new LineRenderer(graphicsDevice);
        _uiRenderer = new UiRenderer(graphicsDevice);
        _playerShip = new PlayerShip();
        _random = new Random();
        _starSystems = new List<StarSystem>();
        _isLoading = true;
        _loadingMessage = "Initializing...";
        _previousMouseState = Mouse.GetState();

        // Initialize camera at origin, will be repositioned after loading
        _camera = new FpsCameraController(
            Vector3.Zero,
            graphicsDevice.Viewport.Width,
            graphicsDevice.Viewport.Height);
    }

    /// <summary>
    /// Sets the font for UI rendering
    /// </summary>
    public void SetFont(SpriteFont font)
    {
        _uiRenderer.SetFont(font);
    }

    /// <summary>
    /// Loads star system data from the EDSM API
    /// </summary>
    public async Task LoadStarDataAsync()
    {
        _isLoading = true;
        _loadingMessage = "Connecting to EDSM API...";

        try
        {
            // First, try to get systems around Sol (the center of the Elite universe)
            _loadingMessage = "Loading star systems around Sol...";
            _starSystems = await _apiService.GetSystemsInSphereAsync("Sol", 100);

            if (_starSystems.Count == 0)
            {
                _loadingMessage = "No systems found near Sol, loading general systems...";
                // Fallback: get any systems with known coordinates
                _starSystems = await _apiService.GetSystemsAsync(
                    showCoordinates: true,
                    onlyKnownCoordinates: true);
            }

            // Filter out systems without coordinates
            _starSystems = _starSystems
                .Where(s => s.Coords != null)
                .Take(1000) // Limit for performance
                .ToList();

            _loadingMessage = $"Loaded {_starSystems.Count} star systems";

            // Set player to random starting system
            _playerShip.SetRandomStartSystem(_starSystems, _random);

            // Center camera on player's starting position
            if (_playerShip.CurrentSystem != null)
            {
                var startPos = _playerShip.CurrentSystem.WorldPosition;
                _camera.SetPosition(startPos - new Vector3(0, 0, 50)); // Offset so we can see the system
            }

            _isLoading = false;
        }
        catch (Exception ex)
        {
            _loadingMessage = $"Error loading data: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"Error: {ex}");
            
            // Create some sample data for testing if API fails
            CreateSampleData();
            _isLoading = false;
        }
    }

    /// <summary>
    /// Creates sample star system data for testing when API is unavailable
    /// </summary>
    private void CreateSampleData()
    {
        _loadingMessage = "Creating sample data...";
        _starSystems = new List<StarSystem>
        {
            new StarSystem { Name = "Sol", Id = 1, Coords = new Coordinates { X = 0, Y = 0, Z = 0 } },
            new StarSystem { Name = "Alpha Centauri", Id = 2, Coords = new Coordinates { X = 3.03, Y = -0.08, Z = 3.15 } },
            new StarSystem { Name = "Barnard's Star", Id = 3, Coords = new Coordinates { X = -3.03, Y = 1.37, Z = 4.94 } },
            new StarSystem { Name = "Wolf 359", Id = 4, Coords = new Coordinates { X = 3.86, Y = 6.47, Z = 1.93 } },
            new StarSystem { Name = "Lalande 21185", Id = 5, Coords = new Coordinates { X = -6.54, Y = 1.65, Z = 4.93 } },
            new StarSystem { Name = "Sirius", Id = 6, Coords = new Coordinates { X = 6.22, Y = -1.01, Z = -1.79 } },
            new StarSystem { Name = "Luyten 726-8", Id = 7, Coords = new Coordinates { X = 5.82, Y = -3.00, Z = -0.67 } },
            new StarSystem { Name = "Ross 154", Id = 8, Coords = new Coordinates { X = 1.93, Y = -0.94, Z = -9.46 } },
            new StarSystem { Name = "Ross 248", Id = 9, Coords = new Coordinates { X = 7.45, Y = -2.42, Z = 7.19 } },
            new StarSystem { Name = "Epsilon Eridani", Id = 10, Coords = new Coordinates { X = 1.93, Y = -7.76, Z = -6.92 } },
        };

        // Add more random systems for testing
        for (int i = 11; i <= 100; i++)
        {
            _starSystems.Add(new StarSystem
            {
                Name = $"System {i}",
                Id = i,
                Coords = new Coordinates
                {
                    X = (_random.NextDouble() - 0.5) * 200,
                    Y = (_random.NextDouble() - 0.5) * 200,
                    Z = (_random.NextDouble() - 0.5) * 200
                }
            });
        }

        _playerShip.SetRandomStartSystem(_starSystems, _random);

        if (_playerShip.CurrentSystem != null)
        {
            var startPos = _playerShip.CurrentSystem.WorldPosition;
            _camera.SetPosition(startPos - new Vector3(0, 0, 50));
        }

        _loadingMessage = $"Sample data created: {_starSystems.Count} systems";
    }

    /// <summary>
    /// Updates the star map state
    /// </summary>
    public void Update(GameTime gameTime)
    {
        if (_isLoading) return;

        var keyboardState = Keyboard.GetState();
        var mouseState = Mouse.GetState();

        // Update camera
        _camera.Update(gameTime, keyboardState, mouseState);

        // Handle system selection on left click
        if (mouseState.LeftButton == ButtonState.Pressed && 
            _previousMouseState.LeftButton == ButtonState.Released)
        {
            TrySelectSystem(mouseState.X, mouseState.Y);
        }

        _previousMouseState = mouseState;
    }

    /// <summary>
    /// Attempts to select a star system at the given screen coordinates
    /// </summary>
    private void TrySelectSystem(int screenX, int screenY)
    {
        Ray pickRay = _camera.GetPickRay(
            screenX, screenY,
            _graphicsDevice.Viewport.Width,
            _graphicsDevice.Viewport.Height);

        StarSystem? closestSystem = null;
        float closestDistance = float.MaxValue;

        foreach (var system in _starSystems)
        {
            // Create a bounding sphere for the star
            var boundingSphere = new BoundingSphere(system.WorldPosition, SelectionRadius);

            // Check if ray intersects
            float? distance = pickRay.Intersects(boundingSphere);
            if (distance.HasValue && distance.Value < closestDistance)
            {
                closestDistance = distance.Value;
                closestSystem = system;
            }
        }

        // Update selection
        if (_selectedSystem != null)
            _selectedSystem.IsSelected = false;

        _selectedSystem = closestSystem;

        if (_selectedSystem != null)
        {
            _selectedSystem.IsSelected = true;
            _playerShip.TargetSystem = _selectedSystem;
        }
    }

    /// <summary>
    /// Renders the star map
    /// </summary>
    public void Draw()
    {
        // Draw loading screen if loading
        if (_isLoading)
        {
            _uiRenderer.Begin();
            _uiRenderer.DrawLoadingScreen(_loadingMessage);
            _uiRenderer.End();
            return;
        }

        // Set up 3D rendering state
        _graphicsDevice.DepthStencilState = DepthStencilState.Default;
        _graphicsDevice.BlendState = BlendState.Opaque;
        _graphicsDevice.RasterizerState = RasterizerState.CullCounterClockwise;

        var viewMatrix = _camera.ViewMatrix;
        var projectionMatrix = _camera.ProjectionMatrix;

        // Draw reference grid at origin
        _lineRenderer.DrawGrid(500, 20, new Color(30, 30, 50), viewMatrix, projectionMatrix);
        _lineRenderer.DrawAxes(50, viewMatrix, projectionMatrix);

        // Draw route line if target selected
        if (_playerShip.CurrentSystem != null && _playerShip.TargetSystem != null)
        {
            _lineRenderer.DrawDashedLine(
                _playerShip.Position,
                _playerShip.TargetSystem.WorldPosition,
                Color.Yellow,
                5.0f,
                viewMatrix, projectionMatrix);
        }

        // Draw all star systems
        foreach (var system in _starSystems)
        {
            bool isPlayerLocation = system == _playerShip.CurrentSystem;
            bool isSelected = system == _selectedSystem;

            _sphereRenderer.DrawStar(
                system.WorldPosition,
                BaseStarSize * system.StarSize,
                isSelected,
                isPlayerLocation,
                viewMatrix, projectionMatrix);
        }

        // Draw UI elements
        _graphicsDevice.DepthStencilState = DepthStencilState.None;
        _uiRenderer.Begin();

        // Draw HUD
        _uiRenderer.DrawHud(_playerShip, _starSystems.Count, _camera.Position);

        // Draw info card for selected system
        if (_selectedSystem != null)
        {
            var mouseState = Mouse.GetState();
            float distance = _playerShip.CurrentSystem != null 
                ? _playerShip.CurrentSystem.DistanceTo(_selectedSystem)
                : 0;

            _uiRenderer.DrawSystemInfoCard(
                _selectedSystem,
                distance,
                new Vector2(mouseState.X, mouseState.Y));
        }

        // Draw control hints
        _uiRenderer.DrawControlHints();

        _uiRenderer.End();
    }

    /// <summary>
    /// Handle viewport size changes
    /// </summary>
    public void OnViewportChanged(int width, int height)
    {
        _camera.UpdateProjectionMatrix(width, height);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _apiService?.Dispose();
            _sphereRenderer?.Dispose();
            _lineRenderer?.Dispose();
            _uiRenderer?.Dispose();
            _disposed = true;
        }
    }
}
