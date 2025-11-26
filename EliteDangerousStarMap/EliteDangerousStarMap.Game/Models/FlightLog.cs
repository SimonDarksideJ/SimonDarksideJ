using System.IO;
using System.Text;

namespace EliteDangerousStarMap.Models;

/// <summary>
/// Manages the flight log during ship travel, including random events
/// </summary>
public class FlightLog
{
    private readonly List<FlightLogEntry> _entries = new();
    private readonly Random _random = new();
    private float _fadeTimer;
    private const float FadeDelay = 5f; // 5 seconds after arrival before fading
    
    // Random event messages
    private static readonly string[] RandomEvents = new[]
    {
        "Saw a flying space whale, that was cool!",
        "Oh no pirates... phew, that was close!",
        "Beautiful nebula on the starboard side",
        "Picked up a distress signal... probably nothing",
        "Coffee machine is broken, this is a disaster",
        "Spotted a Thargoid vessel, keeping distance...",
        "Fuel scoop engaged, soaking up some rays",
        "Passing through an asteroid field",
        "Strange readings on the sensor array",
        "Ship cat knocked something off the console again",
        "Received a message from home, nice!",
        "Space madness setting in... just kidding",
        "Found a derelict ship, looks abandoned for years",
        "Nice view of a binary star system",
        "Automated systems running a diagnostic",
        "Playing some space jazz on the radio",
        "Reminder: dock the ship for maintenance soon",
        "Saw a shooting star... wait, we're in space",
        "Navigation computer hiccupped, recalibrating",
        "Enjoying the silence of deep space"
    };

    /// <summary>
    /// Gets all log entries
    /// </summary>
    public IReadOnlyList<FlightLogEntry> Entries => _entries;

    /// <summary>
    /// Whether the log is currently fading out
    /// </summary>
    public bool IsFading { get; private set; }

    /// <summary>
    /// Fade progress (0 = fully visible, 1 = fully faded)
    /// </summary>
    public float FadeProgress => Math.Clamp(_fadeTimer / FadeDelay, 0f, 1f);

    /// <summary>
    /// Maximum entries to display
    /// </summary>
    public int MaxDisplayEntries { get; set; } = 8;

    /// <summary>
    /// The source system for this journey
    /// </summary>
    public string? FromSystem { get; private set; }

    /// <summary>
    /// The destination system for this journey
    /// </summary>
    public string? ToSystem { get; private set; }

    /// <summary>
    /// Journey start time
    /// </summary>
    public DateTime? JourneyStartTime { get; private set; }

    /// <summary>
    /// Starts a new flight log for a journey
    /// </summary>
    public void StartJourney(string fromSystem, string toSystem)
    {
        Clear();
        FromSystem = fromSystem;
        ToSystem = toSystem;
        JourneyStartTime = DateTime.Now;
        IsFading = false;
        _fadeTimer = 0;
        
        AddEntry($"Ship targeting system {toSystem}", LogEntryType.Status);
    }

    /// <summary>
    /// Adds an entry to the log
    /// </summary>
    public void AddEntry(string message, LogEntryType type = LogEntryType.Status)
    {
        _entries.Add(new FlightLogEntry
        {
            Message = message,
            Type = type,
            Timestamp = DateTime.Now
        });
        
        // Keep log size reasonable
        while (_entries.Count > 50)
        {
            _entries.RemoveAt(0);
        }
    }

    /// <summary>
    /// Adds a random event to the log
    /// </summary>
    public void AddRandomEvent()
    {
        var randomEvent = RandomEvents[_random.Next(RandomEvents.Length)];
        AddEntry(randomEvent, LogEntryType.Event);
    }

    /// <summary>
    /// Marks arrival at destination and starts fade timer
    /// </summary>
    public void MarkArrival()
    {
        AddEntry($"Arrived at system {ToSystem}, travel complete", LogEntryType.Arrival);
        IsFading = true;
        _fadeTimer = 0;
    }

    /// <summary>
    /// Updates fade timer
    /// </summary>
    public void Update(float deltaTime)
    {
        if (IsFading)
        {
            _fadeTimer += deltaTime;
        }
    }

    /// <summary>
    /// Clears the log
    /// </summary>
    public void Clear()
    {
        _entries.Clear();
        IsFading = false;
        _fadeTimer = 0;
    }

    /// <summary>
    /// Gets entries for display (most recent, limited count)
    /// </summary>
    public IEnumerable<FlightLogEntry> GetDisplayEntries()
    {
        return _entries.TakeLast(MaxDisplayEntries);
    }

    /// <summary>
    /// Saves the flight log to a file
    /// </summary>
    public void SaveToFile(string directory = "FlightLogs")
    {
        if (string.IsNullOrEmpty(FromSystem) || string.IsNullOrEmpty(ToSystem) || !JourneyStartTime.HasValue)
            return;

        try
        {
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            string timestamp = JourneyStartTime.Value.ToString("yyyy-MM-dd_HH-mm-ss");
            string fileName = $"{FromSystem}_to_{ToSystem}_{timestamp}.log";
            // Sanitize filename
            fileName = string.Join("_", fileName.Split(Path.GetInvalidFileNameChars()));
            
            string filePath = Path.Combine(directory, fileName);

            var sb = new StringBuilder();
            sb.AppendLine($"Flight Log: {FromSystem} → {ToSystem}");
            sb.AppendLine($"Started: {JourneyStartTime.Value:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine(new string('-', 50));
            
            foreach (var entry in _entries)
            {
                sb.AppendLine($"[{entry.Timestamp:HH:mm:ss}] [{entry.Type}] {entry.Message}");
            }

            File.WriteAllText(filePath, sb.ToString());
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to save flight log: {ex.Message}");
        }
    }
}

/// <summary>
/// A single entry in the flight log
/// </summary>
public class FlightLogEntry
{
    public string Message { get; set; } = "";
    public LogEntryType Type { get; set; }
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Type of log entry
/// </summary>
public enum LogEntryType
{
    Status,
    Event,
    Warning,
    Arrival
}
