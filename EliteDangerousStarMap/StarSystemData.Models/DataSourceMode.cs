namespace StarSystemData.Models;

/// <summary>
/// Specifies the preferred data source for the content pipeline
/// </summary>
public enum DataSourceMode
{
    /// <summary>
    /// Try API first, fall back to local file if API fails
    /// </summary>
    ApiFirst,
    
    /// <summary>
    /// Use local file only, ignore API entirely
    /// </summary>
    LocalFirst
}
