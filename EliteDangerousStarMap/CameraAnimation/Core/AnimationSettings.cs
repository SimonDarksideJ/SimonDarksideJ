using System.Text.Json;

namespace CameraAnimation.Core;

/// <summary>
/// Manages animation settings with save/load functionality.
/// </summary>
/// <remarks>
/// Extension Points:
/// - Add per-animation settings
/// - Add animation profiles/presets
/// - Add cloud sync for settings
/// - Add settings validation
/// </remarks>
public class AnimationSettings
{
    /// <summary>
    /// Default file name for settings.
    /// </summary>
    public const string DefaultFileName = "camera_animation_settings.json";

    /// <summary>
    /// Gets or sets the idle timeout in seconds.
    /// </summary>
    public float IdleTimeout { get; set; } = 30f;

    /// <summary>
    /// Gets or sets whether auto animation is enabled.
    /// </summary>
    public bool AutoAnimationEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the orbit animation duration.
    /// </summary>
    public float OrbitDuration { get; set; } = 20f;

    /// <summary>
    /// Gets or sets the flyby animation duration.
    /// </summary>
    public float FlybyDuration { get; set; } = 15f;

    /// <summary>
    /// Gets or sets the zoom animation duration.
    /// </summary>
    public float ZoomDuration { get; set; } = 10f;

    /// <summary>
    /// Gets or sets whether orbit animation is enabled.
    /// </summary>
    public bool OrbitEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets whether flyby animation is enabled.
    /// </summary>
    public bool FlybyEnabled { get; set; } = false;

    /// <summary>
    /// Gets or sets whether zoom animation is enabled.
    /// </summary>
    public bool ZoomEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the minimum zoom level.
    /// </summary>
    public float MinZoom { get; set; } = 0.5f;

    /// <summary>
    /// Gets or sets the maximum zoom level.
    /// </summary>
    public float MaxZoom { get; set; } = 2.0f;

    /// <summary>
    /// Gets or sets the return animation duration.
    /// </summary>
    public float ReturnDuration { get; set; } = 0.5f;

    /// <summary>
    /// Saves the settings to a JSON file.
    /// </summary>
    /// <param name="filePath">The path to save to.</param>
    public void Save(string filePath)
    {
        try
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(this, options);
            File.WriteAllText(filePath, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to save animation settings: {ex.Message}");
        }
    }

    /// <summary>
    /// Loads settings from a JSON file.
    /// </summary>
    /// <param name="filePath">The path to load from.</param>
    /// <returns>The loaded settings, or default settings if load fails.</returns>
    public static AnimationSettings Load(string filePath)
    {
        try
        {
            if (File.Exists(filePath))
            {
                var json = File.ReadAllText(filePath);
                return JsonSerializer.Deserialize<AnimationSettings>(json) ?? new AnimationSettings();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load animation settings: {ex.Message}");
        }

        return new AnimationSettings();
    }

    /// <summary>
    /// Applies these settings to an animation controller.
    /// </summary>
    /// <param name="controller">The controller to configure.</param>
    public void ApplyTo(AnimationController controller)
    {
        controller.IdleTimeout = IdleTimeout;
        controller.AutoAnimationEnabled = AutoAnimationEnabled;
    }
}
