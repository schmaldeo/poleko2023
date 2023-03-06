using System.Net;

namespace PolEko;

public class Device
{
  public IPAddress IpAddress { get; }

  public Device(IPAddress ipAddress)
  {
    IpAddress = ipAddress;
  }
}