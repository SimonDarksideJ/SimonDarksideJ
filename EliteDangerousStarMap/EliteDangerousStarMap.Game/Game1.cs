using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace EliteDangerousStarMap;

/// <summary>
/// Elite Dangerous Star Map - MonoGame DesktopGL Application
/// Visualizes the Elite Dangerous universe using data from the EDSM API
/// </summary>
public class Game1 : Microsoft.Xna.Framework.Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch = null!;
    private StarMapManager _starMapManager = null!;
    private SpriteFont _font = null!;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this)
        {
            PreferredBackBufferWidth = 1280,
            PreferredBackBufferHeight = 720,
            PreferMultiSampling = true
        };
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        Window.AllowUserResizing = true;
        Window.Title = "Elite Dangerous Star Map";
    }

    protected override void Initialize()
    {
        // Subscribe to window resize events
        Window.ClientSizeChanged += OnWindowResize;

        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        // Load font for UI
        _font = Content.Load<SpriteFont>("DefaultFont");

        // Initialize the star map manager
        _starMapManager = new StarMapManager(GraphicsDevice);
        _starMapManager.SetFont(_font);

        // Start loading star data asynchronously
        // Prefers local file, falls back to API
        _ = LoadStarDataAsync();
    }

    private async Task LoadStarDataAsync()
    {
        await _starMapManager.LoadStarDataAsync();
    }

    private void OnWindowResize(object? sender, EventArgs e)
    {
        if (_starMapManager != null)
        {
            _starMapManager.OnViewportChanged(
                GraphicsDevice.Viewport.Width,
                GraphicsDevice.Viewport.Height);
        }
    }

    protected override void Update(GameTime gameTime)
    {
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || 
            Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();

        _starMapManager?.Update(gameTime);

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        // Clear with space black color
        GraphicsDevice.Clear(new Color(5, 5, 15));

        // Draw the star map
        _starMapManager?.Draw();

        base.Draw(gameTime);
    }

    protected override void UnloadContent()
    {
        _starMapManager?.Dispose();
        base.UnloadContent();
    }
}
