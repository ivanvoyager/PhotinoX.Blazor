using System;
using System.Text.Json.Serialization;

namespace Photino.Blazor.NativeAOT.Models;

public class WeatherForecast
{
    [JsonInclude] public DateTime Date { get; set; }

    [JsonInclude] public int TemperatureC { get; set; }

    [JsonInclude] public string Summary { get; set; }

    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
