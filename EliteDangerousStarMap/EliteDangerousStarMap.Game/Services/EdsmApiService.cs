using System.Net.Http;
using Newtonsoft.Json;
using EliteDangerousStarMap.Models;

namespace EliteDangerousStarMap.Services;

/// <summary>
/// Service for communicating with the EDSM (Elite Dangerous Star Map) API
/// </summary>
public class EdsmApiService : IDisposable
{
    private const string BaseUrl = "https://www.edsm.net/api-v1";
    private readonly HttpClient _httpClient;
    private bool _disposed;

    public EdsmApiService()
    {
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(30)
        };
    }

    /// <summary>
    /// Gets a list of star systems from the EDSM API
    /// </summary>
    /// <param name="systemName">Optional system name filter</param>
    /// <param name="showCoordinates">Include coordinates in response</param>
    /// <param name="onlyKnownCoordinates">Only return systems with known coordinates</param>
    /// <param name="startDateTime">Filter by start date (YYYY-MM-DD HH:MM:SS)</param>
    /// <param name="endDateTime">Filter by end date (YYYY-MM-DD HH:MM:SS)</param>
    public async Task<List<StarSystem>> GetSystemsAsync(
        string? systemName = null,
        bool showCoordinates = true,
        bool onlyKnownCoordinates = true,
        string? startDateTime = null,
        string? endDateTime = null)
    {
        var queryParams = new List<string>();

        if (!string.IsNullOrEmpty(systemName))
            queryParams.Add($"systemName={Uri.EscapeDataString(systemName)}");

        if (showCoordinates)
            queryParams.Add("showCoordinates=1");

        if (onlyKnownCoordinates)
            queryParams.Add("onlyKnownCoordinates=1");

        if (!string.IsNullOrEmpty(startDateTime))
            queryParams.Add($"startDateTime={Uri.EscapeDataString(startDateTime)}");

        if (!string.IsNullOrEmpty(endDateTime))
            queryParams.Add($"endDateTime={Uri.EscapeDataString(endDateTime)}");

        var queryString = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : "";
        var url = $"{BaseUrl}/systems{queryString}";

        try
        {
            var response = await _httpClient.GetStringAsync(url);
            var systems = JsonConvert.DeserializeObject<List<StarSystem>>(response);
            return systems ?? new List<StarSystem>();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error fetching systems: {ex.Message}");
            return new List<StarSystem>();
        }
    }

    /// <summary>
    /// Gets details about a specific star system
    /// </summary>
    /// <param name="systemName">Name of the system</param>
    public async Task<StarSystem?> GetSystemAsync(string systemName)
    {
        var url = $"{BaseUrl}/system?systemName={Uri.EscapeDataString(systemName)}&showCoordinates=1";

        try
        {
            var response = await _httpClient.GetStringAsync(url);
            var system = JsonConvert.DeserializeObject<StarSystem>(response);
            return system;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error fetching system {systemName}: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Gets systems within a sphere around a reference point
    /// </summary>
    /// <param name="systemName">Reference system name</param>
    /// <param name="radius">Radius in light years</param>
    public async Task<List<StarSystem>> GetSystemsInSphereAsync(string systemName, int radius = 50)
    {
        var queryParams = new List<string>
        {
            $"systemName={Uri.EscapeDataString(systemName)}",
            $"radius={radius}",
            "showCoordinates=1"
        };

        var url = $"{BaseUrl}/sphere?{string.Join("&", queryParams)}";

        try
        {
            var response = await _httpClient.GetStringAsync(url);
            var systems = JsonConvert.DeserializeObject<List<StarSystem>>(response);
            return systems ?? new List<StarSystem>();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error fetching systems in sphere: {ex.Message}");
            return new List<StarSystem>();
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _httpClient.Dispose();
            _disposed = true;
        }
    }
}
