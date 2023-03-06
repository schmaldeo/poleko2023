using System;
using System.Net;

namespace PolEko;

public class Device
{
  public IPAddress IpAddress { get; }
  public DateTime LastMeasurement { get; private set; } = DateTime.Now;
  public int RefreshRate { get; set; } = 5;

  public Device(IPAddress ipAddress)
  {
    IpAddress = ipAddress;
  }

  public Device(IPAddress ipAddress, int refreshRate)
  {
    IpAddress = ipAddress;
    RefreshRate = refreshRate;
  }
}

public class Measurement
{
  public float Temperature { get; }
  public int Humidity { get; }
  public DateTime TimeOfMeasurement { get; }

  public Measurement(float temperature, int humidity, DateTime dateTime)
  {
    Temperature = temperature;
    Humidity = humidity;
    TimeOfMeasurement = dateTime;
  }
}