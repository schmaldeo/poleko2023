using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Data.Sqlite;

namespace PolEko;

public abstract class Device
{
  /// <summary>
  /// Lazily initiated cache of the result of ToString() method
  /// </summary>
  private string? _toString;
  
  private IPAddress _ipAddress;
  private ushort _port;
  
  /// <summary>
  /// Optional friendly name for a device
  /// </summary>
  private string? _id;
  
  public int RefreshRate => 2;

  protected Device(IPAddress ipAddress, ushort port, string? id = null)
  {
    _ipAddress = ipAddress;
    _port = port;
    _id = id;

    DeviceUri = new Uri($"http://{ipAddress}:{port}/");
  }

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

public abstract class Device<T> : Device where T : Measurement, new()
{
  protected Device(IPAddress ipAddress, ushort port, string? id = null) : base(ipAddress, port, id)
  {
    BufferSize.BufferOverflow += HandleBufferOverflow;
  }
  
  public T? LastValidMeasurement { get; protected set; }
  public T? LastMeasurement { get; protected set; }
  public Queue<T> MeasurementBuffer { get; } = new();
  public BufferSize BufferSize { get; protected set; } = new(); 
  public DateTime TimeOfLastMeasurement { get; protected set; }
  
  public async Task<T> GetMeasurement(HttpClient client)
  {
    try
    {
      var data = await client.GetFromJsonAsync<T>(DeviceUri);
      if (data is null) throw new HttpRequestException("No data was returned from query");
      MeasurementBuffer.Enqueue(data);
      BufferSize++;
      LastValidMeasurement = data;
      LastMeasurement = data;
      TimeOfLastMeasurement = DateTime.Now;
      return data;
    }
    catch (Exception)
    {
      var errorMeasurement = new T();
      errorMeasurement.Error = true;
      MeasurementBuffer.Enqueue(errorMeasurement);
      BufferSize++;
      LastMeasurement = errorMeasurement;
      return errorMeasurement;
    }
  }

  protected abstract void HandleBufferOverflow(object? sender, EventArgs e);
}

/// <summary>
///   Device that logs temperature and humidity
/// </summary>
public class WeatherDevice : Device<WeatherMeasurement>
{
  // Constructors
  public WeatherDevice(IPAddress ipAddress, ushort port, string? id = null)
    : base(ipAddress, port, id)
  {
  }

  // Properties
  public override string Type => "Weather Device";

  public override string Description => "Device used to measure temperature and humidity";

  // Methods
  protected override async void HandleBufferOverflow(object? sender, EventArgs e)
  {
    MessageBox.Show("buffer overflown");
    // TODO: insert into db on overflow
  }
}

public class BufferSize
{
  // TODO: perhaps add ability to change the limit
  private const ushort Limit = 5;
  private ushort _count;
  public event EventHandler? BufferOverflow;
  
  public void Increment()
  {
    _count++;
    if (_count < Limit) return;
    BufferOverflow?.Invoke(this,EventArgs.Empty);
    _count = 0;
  }
  public static BufferSize operator ++(BufferSize a)
  {
    a.Increment();
    return a;
  }
}
