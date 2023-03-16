using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace PolEko;


public abstract class Measurement
{
  /// <summary>
  /// Indicates that the measurement is invalid
  /// </summary>

  public bool Error { get; set; }

  public DateTime TimeStamp { get; init; } = DateTime.Now;

  public abstract override string ToString();
}

[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
[SuppressMessage("ReSharper", "AutoPropertyCanBeMadeGetOnly.Global")]
public class WeatherMeasurement : Measurement
{
  // Constructor
  public WeatherMeasurement() {}
  
  public WeatherMeasurement(float temperature, int humidity) 
  {
    Temperature = temperature;
    Humidity = humidity;
    TimeStamp = DateTime.Now;
  }

  // Properties
  [JsonPropertyName("temperature")]
  public float Temperature { get; set; }
  [JsonPropertyName("humidity")]
  public int Humidity { get; set; }

  public override string ToString()
  {
    return $"Temperature: {Temperature}, humidity: {Humidity}, time of request: {TimeStamp}";
  }
}