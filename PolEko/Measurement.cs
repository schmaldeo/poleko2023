using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace PolEko;


public abstract class Measurement
{
  /// <summary>
  /// Indicates that the measurement is invalid
  /// </summary>

  public bool Error { get; init; }

  public DateTime TimeStamp { get; } = DateTime.Now;

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
public class ExampleMeasurement : Measurement
{
  public ExampleMeasurement() {}
  
  [JsonPropertyName("altitude")]
  public int Altitude { get; set; }
  [JsonPropertyName("speed")]
  public int Speed { get; set; }
  [JsonPropertyName("distanceTravelled")]
  public int DistanceTravelled { get; set; }

  
  public override string ToString()
  {
    return $"Temperature: {Altitude}, humidity: {Speed}, distance travelled: {DistanceTravelled} time of request: {TimeStamp}";
  }
}