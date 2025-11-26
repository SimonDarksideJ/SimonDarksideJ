using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using EliteDangerousStarMap.Models;
using CameraAnimation.Core;

namespace EliteDangerousStarMap.UI;

/// <summary>
/// Renders UI elements like info cards and HUD
/// </summary>
public class UiRenderer : IDisposable
{
    private readonly GraphicsDevice _graphicsDevice;
    private SpriteBatch _spriteBatch;
    private SpriteFont? _font;
    private Texture2D? _pixelTexture;
    private bool _disposed;

    public UiRenderer(GraphicsDevice graphicsDevice)
    {
        _graphicsDevice = graphicsDevice;
        _spriteBatch = new SpriteBatch(graphicsDevice);
        CreatePixelTexture();
    }

    /// <summary>
    /// Sets the font to use for text rendering
    /// </summary>
    public void SetFont(SpriteFont font)
    {
        _font = font;
    }

    private void CreatePixelTexture()
    {
        _pixelTexture = new Texture2D(_graphicsDevice, 1, 1);
        _pixelTexture.SetData(new[] { Color.White });
    }

    /// <summary>
    /// Begins the UI rendering batch
    /// </summary>
    public void Begin()
    {
        _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
    }

    /// <summary>
    /// Ends the UI rendering batch
    /// </summary>
    public void End()
    {
        _spriteBatch.End();
    }

    /// <summary>
    /// Draws a filled rectangle
    /// </summary>
    public void DrawRectangle(Rectangle bounds, Color color)
    {
        if (_pixelTexture != null)
        {
            _spriteBatch.Draw(_pixelTexture, bounds, color);
        }
    }

    /// <summary>
    /// Draws a rectangle border
    /// </summary>
    public void DrawRectangleBorder(Rectangle bounds, Color color, int thickness = 1)
    {
        if (_pixelTexture == null) return;

        // Top
        _spriteBatch.Draw(_pixelTexture, new Rectangle(bounds.X, bounds.Y, bounds.Width, thickness), color);
        // Bottom
        _spriteBatch.Draw(_pixelTexture, new Rectangle(bounds.X, bounds.Bottom - thickness, bounds.Width, thickness), color);
        // Left
        _spriteBatch.Draw(_pixelTexture, new Rectangle(bounds.X, bounds.Y, thickness, bounds.Height), color);
        // Right
        _spriteBatch.Draw(_pixelTexture, new Rectangle(bounds.Right - thickness, bounds.Y, thickness, bounds.Height), color);
    }

    /// <summary>
    /// Draws text at the specified position
    /// </summary>
    public void DrawText(string text, Vector2 position, Color color)
    {
        if (_font != null)
        {
            _spriteBatch.DrawString(_font, text, position, color);
        }
    }

    /// <summary>
    /// Draws text with a shadow effect
    /// </summary>
    public void DrawTextWithShadow(string text, Vector2 position, Color textColor, Color shadowColor)
    {
        if (_font != null)
        {
            _spriteBatch.DrawString(_font, text, position + new Vector2(1, 1), shadowColor);
            _spriteBatch.DrawString(_font, text, position, textColor);
        }
    }

    /// <summary>
    /// Draws an info card for a selected star system
    /// </summary>
    public void DrawSystemInfoCard(StarSystem system, float distanceFromPlayer, Vector2 screenPosition)
    {
        if (_font == null) return;

        // Card dimensions
        int padding = 10;
        int cardWidth = 250;
        int lineHeight = 20;
        int cardHeight = padding * 2 + lineHeight * 4;

        // Card position (offset from click position)
        int cardX = (int)screenPosition.X + 20;
        int cardY = (int)screenPosition.Y - cardHeight / 2;

        // Keep card on screen
        if (cardX + cardWidth > _graphicsDevice.Viewport.Width)
            cardX = (int)screenPosition.X - cardWidth - 20;
        if (cardY < 0)
            cardY = 0;
        if (cardY + cardHeight > _graphicsDevice.Viewport.Height)
            cardY = _graphicsDevice.Viewport.Height - cardHeight;

        Rectangle cardBounds = new Rectangle(cardX, cardY, cardWidth, cardHeight);

        // Draw card background
        DrawRectangle(cardBounds, new Color(0, 0, 0, 200));
        DrawRectangleBorder(cardBounds, Color.Orange, 2);

        // Draw content
        Vector2 textPosition = new Vector2(cardX + padding, cardY + padding);

        // System name
        DrawTextWithShadow($"System: {system.Name}", textPosition, Color.Orange, Color.Black);
        textPosition.Y += lineHeight;

        // Coordinates
        var coords = system.Coords;
        if (coords != null)
        {
            DrawTextWithShadow($"Coords: ({coords.X:F1}, {coords.Y:F1}, {coords.Z:F1})", 
                textPosition, Color.White, Color.Black);
        }
        textPosition.Y += lineHeight;

        // Distance
        DrawTextWithShadow($"Distance: {distanceFromPlayer:F2} ly", textPosition, Color.Cyan, Color.Black);
        textPosition.Y += lineHeight;

        // Permit required
        if (system.RequirePermit)
        {
            DrawTextWithShadow($"Permit: {system.PermitName ?? "Required"}", textPosition, Color.Red, Color.Black);
        }
    }

    /// <summary>
    /// Draws the HUD with current state information including animation status
    /// </summary>
    public void DrawHud(PlayerShip ship, int systemCount, Vector3 cameraPosition, 
        AnimationMode animationMode = AnimationMode.UserControl, 
        float idleTime = 0, float idleTimeout = 30)
    {
        if (_font == null) return;

        int padding = 10;
        int lineHeight = 18;
        Vector2 position = new Vector2(padding, padding);

        // Draw semi-transparent background
        Rectangle hudBounds = new Rectangle(5, 5, 320, lineHeight * 8 + padding);
        DrawRectangle(hudBounds, new Color(0, 0, 0, 150));

        // Title
        DrawTextWithShadow("ELITE DANGEROUS STAR MAP", position, Color.Orange, Color.Black);
        position.Y += lineHeight + 5;

        // System count
        DrawTextWithShadow($"Systems loaded: {systemCount}", position, Color.White, Color.Black);
        position.Y += lineHeight;

        // Current system
        string currentSystem = ship.CurrentSystem?.Name ?? "Unknown";
        DrawTextWithShadow($"Current: {currentSystem}", position, Color.Cyan, Color.Black);
        position.Y += lineHeight;

        // Target system
        if (ship.TargetSystem != null)
        {
            DrawTextWithShadow($"Target: {ship.TargetSystem.Name}", position, Color.Yellow, Color.Black);
            position.Y += lineHeight;
            DrawTextWithShadow($"Distance: {ship.DistanceToTarget:F2} ly", position, Color.Yellow, Color.Black);
        }
        else
        {
            DrawTextWithShadow("Target: None (Click to select)", position, Color.Gray, Color.Black);
        }
        position.Y += lineHeight;

        // Camera position
        DrawTextWithShadow($"Cam: ({cameraPosition.X:F0}, {cameraPosition.Y:F0}, {cameraPosition.Z:F0})", 
            position, Color.Gray, Color.Black);
        position.Y += lineHeight;

        // Animation status
        string modeText = animationMode switch
        {
            AnimationMode.UserControl => "Mode: User Control",
            AnimationMode.WaitingForIdle => $"Mode: Idle in {idleTimeout - idleTime:F0}s",
            AnimationMode.Animating => "Mode: Auto Camera",
            AnimationMode.ReturningToControl => "Mode: Returning...",
            _ => "Mode: Unknown"
        };

        Color modeColor = animationMode switch
        {
            AnimationMode.UserControl => Color.Green,
            AnimationMode.WaitingForIdle => Color.Yellow,
            AnimationMode.Animating => Color.Cyan,
            AnimationMode.ReturningToControl => Color.Orange,
            _ => Color.White
        };

        DrawTextWithShadow(modeText, position, modeColor, Color.Black);
    }

    /// <summary>
    /// Draws the settings screen
    /// </summary>
    public void DrawSettingsScreen(AnimationSettings settings)
    {
        if (_font == null) return;

        int screenWidth = _graphicsDevice.Viewport.Width;
        int screenHeight = _graphicsDevice.Viewport.Height;
        int cardWidth = 400;
        int cardHeight = 300;
        int cardX = (screenWidth - cardWidth) / 2;
        int cardY = (screenHeight - cardHeight) / 2;

        // Dim background
        DrawRectangle(new Rectangle(0, 0, screenWidth, screenHeight), new Color(0, 0, 0, 180));

        // Settings panel
        Rectangle cardBounds = new Rectangle(cardX, cardY, cardWidth, cardHeight);
        DrawRectangle(cardBounds, new Color(20, 20, 40, 240));
        DrawRectangleBorder(cardBounds, Color.Orange, 2);

        int padding = 20;
        int lineHeight = 24;
        Vector2 position = new Vector2(cardX + padding, cardY + padding);

        // Title
        DrawTextWithShadow("ANIMATION SETTINGS", position, Color.Orange, Color.Black);
        position.Y += lineHeight + 10;

        // Idle timeout setting
        DrawTextWithShadow($"Idle Timeout: {settings.IdleTimeout:F0} seconds", position, Color.White, Color.Black);
        position.Y += lineHeight;
        DrawTextWithShadow("  (Up/Down arrows to adjust)", position, Color.Gray, Color.Black);
        position.Y += lineHeight + 5;

        // Auto animation enabled
        string autoEnabled = settings.AutoAnimationEnabled ? "ENABLED" : "DISABLED";
        Color autoColor = settings.AutoAnimationEnabled ? Color.Green : Color.Red;
        DrawTextWithShadow($"Auto Animation: {autoEnabled}", position, autoColor, Color.Black);
        position.Y += lineHeight;
        DrawTextWithShadow("  (Press A to toggle)", position, Color.Gray, Color.Black);
        position.Y += lineHeight + 5;

        // Animation types
        DrawTextWithShadow("Active Animations:", position, Color.Cyan, Color.Black);
        position.Y += lineHeight;
        
        string orbitStatus = settings.OrbitEnabled ? "[X]" : "[ ]";
        DrawTextWithShadow($"  {orbitStatus} Orbit ({settings.OrbitDuration:F0}s)", position, Color.White, Color.Black);
        position.Y += lineHeight;

        string zoomStatus = settings.ZoomEnabled ? "[X]" : "[ ]";
        DrawTextWithShadow($"  {zoomStatus} Zoom ({settings.ZoomDuration:F0}s)", position, Color.White, Color.Black);
        position.Y += lineHeight;

        string flybyStatus = settings.FlybyEnabled ? "[X]" : "[ ]";
        DrawTextWithShadow($"  {flybyStatus} Flyby ({settings.FlybyDuration:F0}s)", position, Color.White, Color.Black);
        position.Y += lineHeight + 10;

        // Instructions
        DrawTextWithShadow("Press ENTER to save, TAB/ESC to close", position, Color.Yellow, Color.Black);
    }

    /// <summary>
    /// Draws control hints at the bottom of the screen
    /// </summary>
    public void DrawControlHints()
    {
        if (_font == null) return;

        int padding = 10;
        int lineHeight = 16;
        int y = _graphicsDevice.Viewport.Height - padding - lineHeight * 3;

        string controls = "WASD: Move | Right-Click+Drag: Look | Scroll: Zoom | Click: Select | Tab: Settings | ESC: Exit";
        Vector2 textSize = _font.MeasureString(controls);
        Vector2 position = new Vector2((_graphicsDevice.Viewport.Width - textSize.X) / 2, y);

        // Background
        Rectangle bgBounds = new Rectangle(
            (int)position.X - 10, 
            (int)position.Y - 5, 
            (int)textSize.X + 20, 
            lineHeight + 10);
        DrawRectangle(bgBounds, new Color(0, 0, 0, 150));

        DrawTextWithShadow(controls, position, Color.White, Color.Black);
    }

    /// <summary>
    /// Draws a loading indicator
    /// </summary>
    public void DrawLoadingScreen(string message)
    {
        if (_font == null) return;

        // Full screen semi-transparent background
        DrawRectangle(new Rectangle(0, 0, _graphicsDevice.Viewport.Width, _graphicsDevice.Viewport.Height), 
            new Color(0, 0, 20, 220));

        // Loading message
        Vector2 textSize = _font.MeasureString(message);
        Vector2 position = new Vector2(
            (_graphicsDevice.Viewport.Width - textSize.X) / 2,
            (_graphicsDevice.Viewport.Height - textSize.Y) / 2);

        DrawTextWithShadow(message, position, Color.Orange, Color.Black);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _spriteBatch?.Dispose();
            _pixelTexture?.Dispose();
            _disposed = true;
        }
    }
}
