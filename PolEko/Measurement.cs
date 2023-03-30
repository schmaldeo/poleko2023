using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace PolEko;


// TODO: think about making this a structure
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
public class SmartProMeasurement : Measurement
{
  // Properties
  public bool IsRunning { get; init; }
  
  public int Temperature { get; init; }

  public override string ToString()
  {
    return $"Temperature: {Temperature}, time of request: {TimeStamp}";
  }
}
public class ExampleMeasurement : Measurement
{
  [JsonPropertyName("altitude")]
  public int Altitude { get; init; }
  [JsonPropertyName("speed")]
  public int Speed { get; init; }
  [JsonPropertyName("distanceTravelled")]
  public int DistanceTravelled { get; init; }

  
  public override string ToString()
  {
    return $"Temperature: {Altitude}, humidity: {Speed}, distance travelled: {DistanceTravelled} time of request: {TimeStamp}";
  }
}