using System;
using System.ComponentModel;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Tasks;
using PolEko.ui;
using PolEko.util;

namespace PolEko;

/// <summary>
/// <b>Shouldn't be derived from</b>
/// </summary>
public abstract class Device
{
  #region Fields
  
  /// <summary>
  ///   Optional friendly name for a device
  /// </summary>
  private string? _id;

  private IPAddress _ipAddress;
  private ushort _port;

  /// <summary>
  ///   Lazily initiated cache of the result of <see cref="ToString"/> method
  /// </summary>
  private string? _toString;
  
  #endregion

  #region Constructor
  
  protected Device(IPAddress ipAddress, ushort port, string? id = null)
  {
    _ipAddress = ipAddress;
    _port = port;
    _id = id;

    DeviceUri = new Uri($"http://{ipAddress}:{port}/");
  }
  
  #endregion

  #region Properties
  
  /// <summary>
  /// Interval (in seconds) in which data will be fetched
  /// </summary>
  public virtual int RefreshRate => 1;

  public IPAddress IpAddress
  {
    get => _ipAddress;
    set
    {
      if (value.Equals(_ipAddress)) return;
      _toString = null;
      _ipAddress = value;
    }
  }

  public ushort Port
  {
    get => _port;
    set
    {
      if (value == _port) return;
      _toString = null;
      _port = value;
    }
  }

  public string? Id
  {
    get => _id;
    set
    {
      if (value == _id) return;
      _toString = null;
      _id = value;
    }
  }

  /// <summary>
  /// Device's model, used in <see cref="ToString"/> if <see cref="Id"/> is null
  /// </summary>
  public abstract string Model { get; }
  
  /// <summary>
  /// Rough description of the device
  /// </summary>
  public abstract string Description { get; }
  
  /// <summary>
  /// Init-only Uri which is used in data fetching. By default it initialises to http://ipAddress:port/, but it can
  /// be overriden in derived type's constructor
  /// </summary>
  protected Uri DeviceUri { get; init; }
  
  #endregion

  #region Overrides
  
  /// <summary>
  ///   Custom <see cref="ToString"/> implementation
  /// </summary>
  /// <returns>String with device's ID/type (if no ID), IP address and port</returns>
  public override string ToString()
  {
    return _toString ??= $"{Id ?? Model}@{IpAddress}:{Port}";
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
  
  #endregion
}

public abstract class Device<TMeasurement, TControl> : Device, INotifyPropertyChanged 
  where TMeasurement : Measurement, new()
  where TControl: IDeviceControl<Device<TMeasurement, TControl>>
{
  #region Fields
  
  private TMeasurement? _lastMeasurement;
  
  /// <summary>
  /// Last <typeparamref name="TMeasurement"/> in which <c>Error</c> and <c>NetworkError</c> properties are <c>false</c>
  /// </summary>
  private TMeasurement? _lastValidMeasurement;
  
  #endregion

  #region Constructor
  
  protected Device(IPAddress ipAddress, ushort port, string? id = null) : base(ipAddress, port, id)
  {
    MeasurementBuffer.BufferOverflow += OnBufferOverflow;
  }
  
  #endregion

  #region Fields
  
  /// <summary>
  /// Last <typeparamref name="TMeasurement"/> in which <c>Error</c> and <c>NetworkError</c> properties are <c>false</c>
  /// </summary>
  public TMeasurement? LastValidMeasurement
  {
    get => _lastValidMeasurement;
    protected set
    {
      _lastValidMeasurement = value;
      OnPropertyChanged();
    }
  }

  public TMeasurement? LastMeasurement
  {
    get => _lastMeasurement;
    private set
    {
      _lastMeasurement = value;
      OnPropertyChanged();
    }
  }

  /// <summary>
  /// <see cref="Buffer{T}"/> initialised with a <c>size</c> of 60
  /// </summary>
  public Buffer<TMeasurement> MeasurementBuffer { get; } = new(60);
  
  /// <summary>
  /// <see cref="DateTime"/> when last <see cref="TMeasurement"/> was fetched
  /// </summary>
  public DateTime TimeOfLastMeasurement { get; protected set; }
  
  #endregion
  
  #region Events
  
  public event PropertyChangedEventHandler? PropertyChanged;
  
  #endregion

  #region Methods
  
  /// <summary>
  /// Specifies how device's response is mapped to a <see cref="TMeasurement"/> object.
  /// If the properties and nesting of <see cref="TMeasurement"/> are the same as JSON response of a device,
  /// you shouldn't override this.
  /// </summary>
  /// <param name="client"><see cref="HttpClient"/> used for the request</param>
  /// <returns><see cref="TMeasurement"/> object</returns>
  /// <exception cref="HttpRequestException">Thrown if query returns no data</exception>
  protected virtual async Task<TMeasurement> GetMeasurementFromDeviceAsync(HttpClient client)
  {
    var data = await client.GetFromJsonAsync<TMeasurement>(DeviceUri);
    return data ?? throw new HttpRequestException("No data was returned from query");
  }

  /// <summary>
  /// Handles setting all the <see cref="TMeasurement"/>-related properties.
  /// </summary>
  /// <param name="client"><see cref="HttpClient"/> used to request data from the <see cref="Device{TMeasurement,TControl}"/></param>
  /// <returns><see cref="TMeasurement"/> object</returns>
  public async Task<TMeasurement> GetMeasurementAsync(HttpClient client)
  {
    try
    {
      var data = await GetMeasurementFromDeviceAsync(client);
      MeasurementBuffer.Add(data);
      if (!data.Error) LastValidMeasurement = data;
      LastMeasurement = data;
      TimeOfLastMeasurement = data.TimeStamp;
      return data;
    }
    catch (Exception)
    {
      var errorMeasurement = new TMeasurement
      {
        NetworkError = true
      };
      MeasurementBuffer.Add(errorMeasurement);
      LastMeasurement = errorMeasurement;
      TimeOfLastMeasurement = DateTime.Now;
      return errorMeasurement;
    }
  }

  /// <summary>
  /// Inserts current iteration of <see cref="Buffer"/> into the database
  /// </summary>
  public async Task InsertMeasurementsAsync()
  {
    if (MeasurementBuffer.Size == 0) return;
    await Database.InsertMeasurementsAsync<TMeasurement>(MeasurementBuffer.GetCurrentIteration(), this);
  }
  
  #endregion

  #region Event handlers
  
  private async void OnBufferOverflow(object? sender, EventArgs e)
  {
    await InsertMeasurementsAsync();
  }

  private void OnPropertyChanged([CallerMemberName] string? name = null)
  {
    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
  }
  
  #endregion
}

/// <summary>
///   \~english Device that logs temperature
///   \~polish Urządzenie mierzące temperaturę
/// </summary>
// ReSharper disable once ClassNeverInstantiated.Global
public class SmartProDevice : Device<SmartProMeasurement, SmartProDeviceControl>
{
  #region Constructor
  
  public SmartProDevice(IPAddress ipAddress, ushort port, string? id = null)
    : base(ipAddress, port, id)
  {
    DeviceUri = new Uri($"http://{ipAddress}:{port}/api/v1/school/status");
  }
  
  #endregion

  #region Overrides
  
  public override string Model => "POL-EKO Smart Pro";

  public override string Description =>
    "Inkubator laboratoryjny z układem chłodzenia opartym na technologii ogniw Peltiera";

  protected override async Task<SmartProMeasurement> GetMeasurementFromDeviceAsync(HttpClient client)
  {
    var data = await client.GetStringAsync(DeviceUri);
    using var document = JsonDocument.Parse(data);
    var root = document.RootElement;
    var isRunning = root.GetProperty("IS_RUNNING").GetBoolean();
    var temperatureElement = root.GetProperty("TEMPERATURE_MAIN");
    var temperature = temperatureElement.GetProperty("value").GetInt32();
    var error = temperatureElement.GetProperty("error").GetBoolean();

    var measurement = new SmartProMeasurement
    {
      IsRunning = isRunning,
      Temperature = temperature,
      NetworkError = error
    };

    return measurement;
  }
  
  #endregion
}

// ReSharper disable once ClassNeverInstantiated.Global
public class ExampleDevice : Device<ExampleMeasurement, ExampleDeviceControl>
{
  #region Constructor
  
  public ExampleDevice(IPAddress ipAddress, ushort port, string? id = null)
    : base(ipAddress, port, id)
  {
  }
  
  #endregion

  #region Overrides
  
  public override string Model => "Example Device";

  public override string Description => "Device used for presentation";
  
  #endregion
}