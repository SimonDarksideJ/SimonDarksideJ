using Microsoft.Xna.Framework;
using CameraAnimation.Interfaces;

namespace CameraAnimation.Animations;

/// <summary>
/// Smoothly zooms the camera in and out.
/// Can be combined with other animations like Orbit for dynamic effects.
/// </summary>
/// <remarks>
/// Extension Points:
/// - Add zoom curves (exponential, stepped)
/// - Add focus pull effect (change focus distance during zoom)
/// - Add dolly zoom effect (change position to maintain object size)
/// </remarks>
public class ZoomAnimation : BaseCameraAnimation
{
    /// <inheritdoc/>
    public override string Name => "Zoom";

    /// <summary>
    /// The duration of one zoom cycle in seconds.
    /// </summary>
    public float ZoomDuration { get; set; } = 10f;

    /// <inheritdoc/>
    public override float Duration => ZoomDuration;

    /// <summary>
    /// The minimum zoom level (zoomed out).
    /// </summary>
    public float MinZoom { get; set; } = 0.5f;

    /// <summary>
    /// The maximum zoom level (zoomed in).
    /// </summary>
    public float MaxZoom { get; set; } = 2.0f;

    /// <summary>
    /// Whether to also move the camera forward/backward with zoom.
    /// Creates a dolly-zoom style effect.
    /// </summary>
    public bool DollyZoom { get; set; } = false;

    /// <summary>
    /// The distance to move during dolly zoom.
    /// </summary>
    public float DollyDistance { get; set; } = 50f;

    private float _targetZoom;
    private bool _zoomingIn;

    /// <inheritdoc/>
    public override void Initialize(IAnimatedCamera camera, Vector3? focusPoint = null)
    {
        base.Initialize(camera, focusPoint);
        
        // Start from current zoom, determine direction based on position in range
        float normalizedZoom = (camera.Zoom - MinZoom) / (MaxZoom - MinZoom);
        _zoomingIn = normalizedZoom < 0.5f;
        _targetZoom = _zoomingIn ? MaxZoom : MinZoom;
    }

    /// <inheritdoc/>
    protected override CameraTransform CalculateTransform(GameTime gameTime, IAnimatedCamera camera)
    {
        // Use sine wave for smooth in-out-in-out pattern
        float t = (MathF.Sin(Progress * 2 * MathF.PI - MathF.PI / 2) + 1) / 2;
        
        float targetZoom = MathHelper.Lerp(MinZoom, MaxZoom, t);
        float zoomDelta = targetZoom - camera.Zoom;
        
        Vector3 positionDelta = Vector3.Zero;
        
        if (DollyZoom)
        {
            // Move forward/backward based on zoom level
            float dollyOffset = MathHelper.Lerp(-DollyDistance / 2, DollyDistance / 2, t);
            float currentOffset = MathHelper.Lerp(-DollyDistance / 2, DollyDistance / 2, 
                (camera.Zoom - MinZoom) / (MaxZoom - MinZoom));
            
            positionDelta = camera.Forward * (dollyOffset - currentOffset);
        }
        
        return new CameraTransform(positionDelta, 0, 0, zoomDelta);
    }

    /// <inheritdoc/>
    public override void Reset()
    {
        base.Reset();
        _zoomingIn = true;
    }
}
