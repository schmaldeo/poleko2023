using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows;
using Microsoft.Data.Sqlite;

namespace PolEko;

/// <summary>
///   Interaction logic for App.xaml
/// </summary>
public partial class App
{
  private List<Type> _registeredMeasurementTypes = new();

  private readonly Dictionary<string, Type> _registeredDeviceTypes = new();

  private async void App_Startup(object sender, StartupEventArgs e)
  {
    // Getting types that derive from Device (except for abstract types). It does use Reflection, so while it is an idea
    // to get back to manually declaring registered device types, as using Reflection definitely is a slight performance hit,
    // I think it's better to rely on automatically getting the types with it. Main benefit is developer experience.
    // There's no need to modify many files when you add a new Device type. On top of that, it's often correct that
    // the end user (or, in case of open source software, a person that potentially makes a fork and tries to add
    // their own features) might not understand what certain features do, therefore the choice to use Reflection here.
    var deviceTypes = FindDerivedTypes(typeof(Device));
    foreach (var t in deviceTypes)
    {
      _registeredDeviceTypes[t.Name] = t;
    }
    
    _registeredMeasurementTypes = FindDerivedTypes(typeof(Measurement));
    
    await Database.CreateTablesAsync(_registeredMeasurementTypes);
    var devices = await Database.ExtractDevicesAsync(_registeredDeviceTypes);
    
    MainWindow mainWindow = new(devices);
    mainWindow.Show();
  }

  private static List<Type> FindDerivedTypes(Type type)
  {
    return Assembly.GetAssembly(type)?
      .GetTypes()
      .Where(t =>
        t != type &&
        type.IsAssignableFrom(t) &&
        !t.IsAbstract
      ).ToList() ?? new List<Type>();
  }
}