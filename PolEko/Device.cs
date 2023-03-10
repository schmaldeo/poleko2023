using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Windows;

namespace PolEko;

public abstract class Device
{
  /// <summary>
  /// Lazily initiated cache of the result of ToString() method
  /// </summary>
  private string? _toString;
  
  protected Device(IPAddress ipAddress, int port, string? id = null, int refreshRate = 5)
  {
    IpAddress = ipAddress;
    Port = port;
    Id = id;
    RefreshRate = refreshRate;

    DeviceUri = new Uri($"http://{ipAddress}:{port}/");
  }

  // perhaps try that lazy implementation of ToString() as this object isnt really ever disposed
  public DateTime LastMeasurement { get; protected init; } = DateTime.Now;
  public int RefreshRate { get; set; }
  protected IPAddress IpAddress { get; }
  protected abstract string Type { get; }
  protected abstract string Description { get; }
  protected Uri DeviceUri { get; }
  private string? Id { get; }
  private int Port { get; }

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
  public DateTime TimeStamp { get; protected init; }
}

/// <summary>
///   Device that logs temperature and humidity
/// </summary>
public class WeatherDevice : Device
{
  // Constructors
  public WeatherDevice(IPAddress ipAddress, int port, string? id = null, int refreshRate = 5)
    : base(ipAddress, port, id, refreshRate)
  {
  }

  // Properties
  protected override string Type => "Weather Device";

  protected override string Description => "Device used to measure temperature and humidity";

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
      MessageBox.Show(e.Message);
      return new WeatherMeasurement(0, 0);
    }
    catch (Exception e)
    {
      MessageBox.Show(e.Message);
      return new WeatherMeasurement(0, 0);
    }
  }

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
    private float Temperature { get; }
    private int Humidity { get; }
  }
}
