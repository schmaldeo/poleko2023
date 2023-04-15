using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace PolEko;

public abstract class Measurement
{
  /// <summary>
  /// Indicates a network error
  /// </summary>
  public bool NetworkError { get; init; }
  
  /// <summary>
  /// Indicates a device-side error
  /// </summary>
  public bool Error { get; init; }

  /// <summary>
  /// Indicates when the request was sent to the device
  /// </summary>
  public DateTime TimeStamp { get; init; } = DateTime.Now;

  public abstract override string ToString();
}

/// <summary>
/// Provides support for POL-EKO Smart Pro's measurements 
/// </summary>
[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
[SuppressMessage("ReSharper", "AutoPropertyCanBeMadeGetOnly.Global")]
public class SmartProMeasurement : Measurement
{
  public bool IsRunning { get; init; }
  
  public int Temperature { get; init; }

  public override string ToString()
  {
    return
      $"Temperature: {Temperature}, time of request: {TimeStamp}, device error: {Error}, network error: {NetworkError}";
  }
}

/// <summary>
/// Presentation example
/// </summary>
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