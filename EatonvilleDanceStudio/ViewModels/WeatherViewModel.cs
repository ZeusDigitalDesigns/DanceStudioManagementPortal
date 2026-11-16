namespace EatonvilleDanceStudio.ViewModels;

/// <summary>
/// Represents weather information displayed by a view, including location, temperature,
/// wind conditions, current condition details, severe weather status, and loading state.
/// </summary>
public class WeatherViewModel
{
    public string LocationName { get; set; } = "Eatonville, WA";
    public double TemperatureF { get; set; }
    public double WindSpeedMph { get; set; }
    public string ConditionText { get; set; } = "Clear";
    public string IconEmoji { get; set; } = "☀️";
    public bool IsSevereWeather { get; set; }
    public bool IsLoaded { get; set; }
}