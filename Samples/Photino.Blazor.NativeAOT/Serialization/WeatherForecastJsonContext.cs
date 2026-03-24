using System.Text.Json.Serialization;
using Photino.Blazor.NativeAOT.Models;

namespace Photino.Blazor.NativeAOT.Serialization;

[JsonSerializable(typeof(WeatherForecast))]
[JsonSerializable(typeof(WeatherForecast[]))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
internal partial class WeatherForecastJsonContext : JsonSerializerContext { }