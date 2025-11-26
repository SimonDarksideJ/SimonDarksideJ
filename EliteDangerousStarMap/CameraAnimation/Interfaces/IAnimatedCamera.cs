using Microsoft.Xna.Framework;

namespace CameraAnimation.Interfaces;

/// <summary>
/// Interface for cameras that can be controlled by the animation system.
/// Implement this interface to make any camera compatible with the animation controller.
/// </summary>
/// <remarks>
/// Extension Points:
/// - Add additional properties for camera-specific features (e.g., shake intensity, lens effects)
/// - Add events for camera state changes
/// - Add methods for camera bookmarks/waypoints
/// </remarks>
public interface IAnimatedCamera
{
    /// <summary>
    /// Gets or sets the camera position in world space.
    /// </summary>
    Vector3 Position { get; set; }

    /// <summary>
    /// Gets or sets the camera's yaw rotation (horizontal rotation around Y-axis) in radians.
    /// </summary>
    float Yaw { get; set; }

    /// <summary>
    /// Gets or sets the camera's pitch rotation (vertical rotation) in radians.
    /// </summary>
    float Pitch { get; set; }

    /// <summary>
    /// Gets or sets the camera's zoom level.
    /// </summary>
    float Zoom { get; set; }

    /// <summary>
    /// Gets the view matrix computed from the camera's current state.
    /// </summary>
    Matrix ViewMatrix { get; }

    /// <summary>
    /// Gets the projection matrix for the camera.
    /// </summary>
    Matrix ProjectionMatrix { get; }

    /// <summary>
    /// Gets the forward direction vector of the camera.
    /// </summary>
    Vector3 Forward { get; }

    /// <summary>
    /// Gets the right direction vector of the camera.
    /// </summary>
    Vector3 Right { get; }

    /// <summary>
    /// Gets the up direction vector of the camera.
    /// </summary>
    Vector3 Up { get; }

    /// <summary>
    /// Updates the camera's internal matrices after external position/rotation changes.
    /// </summary>
    void UpdateMatrices();

    /// <summary>
    /// Updates the projection matrix with new viewport dimensions.
    /// </summary>
    /// <param name="viewportWidth">The width of the viewport in pixels.</param>
    /// <param name="viewportHeight">The height of the viewport in pixels.</param>
    void UpdateProjectionMatrix(int viewportWidth, int viewportHeight);
}
