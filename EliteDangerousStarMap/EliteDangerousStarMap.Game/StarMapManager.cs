using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using EliteDangerousStarMap.Models;
using EliteDangerousStarMap.Services;
using EliteDangerousStarMap.Rendering;
using EliteDangerousStarMap.Input;
using EliteDangerousStarMap.UI;
using CameraAnimation.Core;
using CameraAnimation.Animations;

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
    private readonly AnimationController _animationController;
    private readonly AnimationSettings _animationSettings;

    private List<StarSystem> _starSystems;
    private StarSystem? _selectedSystem;
    private bool _isLoading;
    private string _loadingMessage;
    private MouseState _previousMouseState;
    private KeyboardState _previousKeyboardState;
    private bool _disposed;
    private bool _showSettings;

    // Rendering settings
    private const float BaseStarSize = 0.5f;
    private const float SelectionRadius = 5.0f;
    private const string SettingsFilePath = "animation_settings.json";

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
        _previousKeyboardState = Keyboard.GetState();
        _showSettings = false;

        // Initialize camera at origin, will be repositioned after loading
        _camera = new FpsCameraController(
            Vector3.Zero,
            graphicsDevice.Viewport.Width,
            graphicsDevice.Viewport.Height);

        // Load animation settings
        _animationSettings = AnimationSettings.Load(SettingsFilePath);

        // Initialize animation controller
        _animationController = new AnimationController();
        _animationController.SetTargetCamera(_camera);
        _animationSettings.ApplyTo(_animationController);

        // Register default animations
        RegisterAnimations();
    }

    private void RegisterAnimations()
    {
        if (_animationSettings.OrbitEnabled)
        {
            var orbit = new OrbitAnimation
            {
                OrbitDuration = _animationSettings.OrbitDuration,
                HeightOffset = 20f,
                Weight = 1.0f
            };
            _animationController.RegisterAnimation(orbit);
        }

        if (_animationSettings.ZoomEnabled)
        {
            var zoom = new ZoomAnimation
            {
                ZoomDuration = _animationSettings.ZoomDuration,
                MinZoom = _animationSettings.MinZoom,
                MaxZoom = _animationSettings.MaxZoom,
                Weight = 0.5f
            };
            _animationController.RegisterAnimation(zoom);
        }

        if (_animationSettings.FlybyEnabled)
        {
            var flyby = new FlybyAnimation
            {
                FlybyDuration = _animationSettings.FlybyDuration,
                Speed = 30f,
                LookAtFocus = true,
                Weight = 1.0f
            };
            _animationController.RegisterAnimation(flyby);
        }
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
                
                // Set the animation focus point to player's system
                _animationController.FocusPoint = startPos;
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
            _animationController.FocusPoint = startPos;
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

        // Toggle settings screen with Tab
        if (keyboardState.IsKeyDown(Keys.Tab) && !_previousKeyboardState.IsKeyDown(Keys.Tab))
        {
            _showSettings = !_showSettings;
            if (_showSettings)
            {
                _animationController.ForceUserControl(); // Stop animations when showing settings
            }
        }

        // Handle settings screen input
        if (_showSettings)
        {
            HandleSettingsInput(keyboardState, mouseState);
            _previousKeyboardState = keyboardState;
            _previousMouseState = mouseState;
            return;
        }

        // Only update camera when not animating
        bool hasUserInput = false;
        if (_animationController.Mode == AnimationMode.UserControl || 
            _animationController.Mode == AnimationMode.WaitingForIdle)
        {
            hasUserInput = _camera.Update(gameTime, keyboardState, mouseState);
        }

        // Update animation controller
        _animationController.Update(gameTime, hasUserInput);

        // Handle system selection on left click (only when not animating)
        if (_animationController.Mode == AnimationMode.UserControl || 
            _animationController.Mode == AnimationMode.WaitingForIdle)
        {
            if (mouseState.LeftButton == ButtonState.Pressed && 
                _previousMouseState.LeftButton == ButtonState.Released)
            {
                TrySelectSystem(mouseState.X, mouseState.Y);
            }
        }

        _previousKeyboardState = keyboardState;
        _previousMouseState = mouseState;
    }

    private void HandleSettingsInput(KeyboardState keyboardState, MouseState mouseState)
    {
        // Close settings with Tab or Escape
        if (keyboardState.IsKeyDown(Keys.Escape) && !_previousKeyboardState.IsKeyDown(Keys.Escape))
        {
            _showSettings = false;
        }

        // Adjust idle timeout with Up/Down arrows
        if (keyboardState.IsKeyDown(Keys.Up) && !_previousKeyboardState.IsKeyDown(Keys.Up))
        {
            _animationSettings.IdleTimeout = Math.Min(_animationSettings.IdleTimeout + 5f, 300f);
            _animationController.IdleTimeout = _animationSettings.IdleTimeout;
        }
        if (keyboardState.IsKeyDown(Keys.Down) && !_previousKeyboardState.IsKeyDown(Keys.Down))
        {
            _animationSettings.IdleTimeout = Math.Max(_animationSettings.IdleTimeout - 5f, 5f);
            _animationController.IdleTimeout = _animationSettings.IdleTimeout;
        }

        // Toggle auto-animation with A key
        if (keyboardState.IsKeyDown(Keys.A) && !_previousKeyboardState.IsKeyDown(Keys.A))
        {
            _animationSettings.AutoAnimationEnabled = !_animationSettings.AutoAnimationEnabled;
            _animationController.AutoAnimationEnabled = _animationSettings.AutoAnimationEnabled;
        }

        // Save settings with Enter
        if (keyboardState.IsKeyDown(Keys.Enter) && !_previousKeyboardState.IsKeyDown(Keys.Enter))
        {
            _animationSettings.Save(SettingsFilePath);
            _showSettings = false;
        }
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
            
            // Update animation focus point to selected system
            _animationController.FocusPoint = _selectedSystem.WorldPosition;
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

        // Draw HUD with animation status
        _uiRenderer.DrawHud(_playerShip, _starSystems.Count, _camera.Position, 
            _animationController.Mode, _animationController.CurrentIdleTime, 
            _animationController.IdleTimeout);

        // Draw info card for selected system
        if (_selectedSystem != null && !_showSettings)
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

        // Draw settings screen if open
        if (_showSettings)
        {
            _uiRenderer.DrawSettingsScreen(_animationSettings);
        }
        else
        {
            // Draw control hints
            _uiRenderer.DrawControlHints();
        }

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
