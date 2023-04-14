using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace PolEko;

public abstract class Measurement
{
  public bool NetworkError { get; init; }
  
  public bool Error { get; init; }

  public DateTime TimeStamp { get; init; } = DateTime.Now;

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
    return
      $"Temperature: {Temperature}, time of request: {TimeStamp}, device error: {Error}, network error: {NetworkError}";
  }
}

public class ExampleMeasurement : Measurement
{
  [JsonPropertyName("altitude")] public int Speed { get; init; }

  [JsonPropertyName("speed")] public int Rpm { get; init; }

  [JsonPropertyName("distanceTravelled")]
  public int DistanceTravelled { get; init; }


  public override string ToString()
  {
    return
      $"Speed: {Speed}, RPM: {Rpm}, distance travelled: {DistanceTravelled} time of request: {TimeStamp}";
  }
}