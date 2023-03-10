using System;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows;

namespace PolEko;

public abstract class Device
{
  /// <summary>
  /// Lazily initiated cache of the result of ToString() method
  /// </summary>
  private string? _toString;
  private IPAddress _ipAddress;
  private ushort _port;
  private string? _id;
  
  protected Device(IPAddress ipAddress, ushort port, string? id = null, int refreshRate = 5)
  {
    _ipAddress = ipAddress;
    _port = port;
    _id = id;
    RefreshRate = refreshRate;

    DeviceUri = new Uri($"http://{ipAddress}:{port}/");
  }

  public DateTime LastMeasurement { get; protected init; } = DateTime.Now;
  public int RefreshRate { get; set; }

  public IPAddress IpAddress
  {
    get => _ipAddress;
    set
    {
      _toString = null;
      _ipAddress = value;
    }
  }

  public ushort Port
  {
    get => _port;
    set
    {
      _toString = null;
      _port = value;
    }
  }

  public string? Id
  {
    get => _id;
    set
    {
      _toString = null;
      _id = value;
    }
  }
  
  public abstract string Type { get; }
  public abstract string Description { get; }
  protected Uri DeviceUri { get; }

  /// <summary>
  ///   Custom <c>ToString()</c> implementation
  /// </summary>
  /// <returns>String with device's ID/type (if no ID), IP address and port</returns>
  public override string ToString()
  {
    return _toString ??= $"{Id ?? Type}@{IpAddress}:{Port}";
  }

  public static bool operator ==(Device a, Device b)
  {
    return a.IpAddress.Equals(b.IpAddress) && a.Port == b.Port;
  }

  public static bool operator !=(Device a, Device b)
  {
    return !(a.IpAddress.Equals(b.IpAddress) && a.Port == b.Port);
  }

  public override bool Equals(object? obj)
  {
    if (obj == null || GetType() != obj.GetType()) return false;

    var device = (Device)obj;
    return IpAddress.Equals(device.IpAddress) && Port == device.Port;
  }

  public override int GetHashCode()
  {
    return HashCode.Combine(IpAddress, Port, Id);
  }
}

public abstract class Measurement
{
  [JsonIgnore]
  public DateTime TimeStamp { get; protected init; }
}

/// <summary>
///   Device that logs temperature and humidity
/// </summary>
public class WeatherDevice : Device
{
  // Constructors
  public WeatherDevice(IPAddress ipAddress, ushort port, string? id = null, int refreshRate = 5)
    : base(ipAddress, port, id, refreshRate)
  {
  }

  // Properties
  public override string Type => "Weather Device";

  public override string Description => "Device used to measure temperature and humidity";

  // Methods
  public async Task<WeatherMeasurement> GetMeasurement(HttpClient client)
  {
    try
    {
      var data = await client.GetFromJsonAsync<WeatherMeasurement>(DeviceUri);
      if (data != null) return data;
      throw new HttpRequestException("Data returned from request was null");
    }
    catch (HttpRequestException e)
    {
      // TODO: send a cancellation token here
      // messagebox is a placeholder
      // TODO: in the fetch interval loop do a few retries before aborting
      MessageBox.Show(e.Message);
      return new WeatherMeasurement(0, 0);
    }
    catch (Exception e)
    {
      MessageBox.Show(e.Message);
      return new WeatherMeasurement(0, 0);
    }
  }

  [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
  public class WeatherMeasurement : Measurement
  {
    // Constructor
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
}
