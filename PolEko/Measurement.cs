using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace PolEko;


public abstract class Measurement
{
  /// <summary>
  /// Indicates that the measurement is invalid
  /// </summary>

  protected Measurement()
  {
    Error = true;
  }

  public bool Error { get; protected init; }
  [JsonIgnore]
  public DateTime TimeStamp { get; protected init; }

  public abstract override string ToString();
}

[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
public class WeatherMeasurement : Measurement
{
  // Constructor
  public WeatherMeasurement()
  {
    Error = true;
  }
  
  public WeatherMeasurement(float temperature, int humidity) 
  {
    Temperature = temperature;
    Humidity = humidity;
    TimeStamp = DateTime.Now;
  }

  // Properties
  [JsonPropertyName("temperature")]
  public float Temperature { get; }
  [JsonPropertyName("humidity")]
  public int Humidity { get; }

  public override string ToString()
  {
    return $"Temperature: {Temperature}, humidity: {Humidity}, time of request: {TimeStamp}";
  }
}