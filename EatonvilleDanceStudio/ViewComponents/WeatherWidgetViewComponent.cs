using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using EatonvilleDanceStudio.ViewModels;

namespace EatonvilleDanceStudio.ViewComponents;

/// <summary>
/// Provides an ASP.NET Core view component that renders a weather widget for Eatonville, Washington.
/// </summary>
/// <remarks>
/// This component retrieves current weather information from an external weather service, maps the
/// response to a weather view model, and returns the corresponding widget view. If weather data
/// cannot be retrieved, the component renders an offline fallback state.
/// </remarks>
public class WeatherWidgetViewComponent : ViewComponent
{
    private readonly IHttpClientFactory _httpClientFactory;

    public WeatherWidgetViewComponent(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    /// <summary>
    /// Retrieves current weather data for Eatonville, Washington,
    /// maps it to a weather view model, and renders the weather
    /// widget view. If the weather data cannot be retrieved,
    /// renders the view with an offline fallback state.
    /// </summary>
    /// <return>
    /// A task representing the asynchronous operation,
    /// containing the rendered weather widget view component result.
    /// </return>
    public async Task<IViewComponentResult> InvokeAsync()
    {
        WeatherViewModel model = new WeatherViewModel();
        string apiUrl =
            "https://api.open-meteo.com/v1/forecast" +
            "?latitude=46.8673&longitude=-122.2665" +
            "&current=temperature_2m,weather_code,wind_speed_10m" +
            "&temperature_unit=fahrenheit&wind_speed_unit=mph";

        try
        {
            HttpClient client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(10);

            HttpResponseMessage response = await client.GetAsync(apiUrl);

            if (response.IsSuccessStatusCode)
            {
                string json =
                    await response.Content.ReadAsStringAsync();

                using JsonDocument doc = JsonDocument.Parse(json);
                JsonElement current = doc.RootElement.GetProperty("current");

                model.TemperatureF = Math.Round(current.GetProperty("temperature_2m").GetDouble(), 0);

                model.WindSpeedMph = Math.Round(current.GetProperty("wind_speed_10m").GetDouble(), 1);

                int weatherCode = current.GetProperty("weather_code").GetInt32();

                MapWeatherCode(weatherCode, model);
                model.IsLoaded = true;
            }
        }
        catch (Exception)
        {
            model.ConditionText = "Weather Offline";
            model.IconEmoji = "🌡️";
            model.IsLoaded = false;
        }

        return View(model);
    }

    /// <summary>
    /// Maps a numeric weather condition code to user-facing weather details
    /// and updates the provided weather view model with condition text,
    /// an icon, and severe weather status when applicable.
    /// </summary>
    /// <param name="code">
    /// The weather condition code returned by the weather service.
    /// </param>
    /// <param name="model">
    /// The weather view model to update with the mapped condition details.
    /// </param>
    private static void MapWeatherCode(
        int code,
        WeatherViewModel model)
    {
        if (code == 0)
        {
            model.ConditionText = "Clear Skies";
            model.IconEmoji = "☀️";
        }
        else if (code <= 3)
        {
            model.ConditionText = "Partly Cloudy";
            model.IconEmoji = "⛅";
        }
        else if (code == 45 || code == 48)
        {
            model.ConditionText = "Foggy";
            model.IconEmoji = "🌫️";
        }
        else if (code >= 51 && code <= 67)
        {
            model.ConditionText = "Rain / Drizzle";
            model.IconEmoji = "🌧️";
            model.IsSevereWeather = code == 66 || code == 67;
        }
        else if (code >= 71 && code <= 86)
        {
            model.ConditionText = "Snow / Winter Alert";
            model.IconEmoji = "❄️";
            model.IsSevereWeather = true;
        }
        else if (code >= 95)
        {
            model.ConditionText = "Thunderstorm";
            model.IconEmoji = "⛈️";
            model.IsSevereWeather = true;
        }
        else
        {
            model.ConditionText = "Overcast";
            model.IconEmoji = "☁️";
        }
    }
}