using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Data;
using PolEko.ui;
using PolEko.util;

namespace PolEko;

public partial class App
{
  private readonly Dictionary<Type, Type> _deviceAssociatedControls = new();
  private readonly Dictionary<string, Type> _registeredDeviceTypes = new();
  private readonly List<Type> _registeredMeasurementTypes = new();

  private async void App_Startup(object sender, StartupEventArgs e)
  {
    // Getting types that derive from Device using System.Reflection. It's not a massive performance hit while it is
    // a big improvement in developer experience as they don't need to add the type to a Dictionary manually,
    // potentially even doing it wrong.
    // This gets derived types of Device type and filters non-generic types out, which assures that it only takes
    // types derived from Device<>. Need to do it this way because it's impossible to find derived types of a generic
    // type without specifying the generic arguments.
    var deviceTypes = FindDerivedTypes(typeof(Device)).Where(x => x.BaseType!.IsGenericType);
    foreach (var t in deviceTypes)
    {
      _registeredDeviceTypes[t.Name] = t;
      
      // Works because the definition is Device<TMeasurement, TControl>
      _registeredMeasurementTypes.Add(t.BaseType!.GetGenericArguments()[0]);
      _deviceAssociatedControls[t] = t.BaseType!.GetGenericArguments()[1];
    }

    await Database.CreateTablesAsync(_registeredMeasurementTypes);
    
    var devices = await Database.ExtractDevicesAsync(_registeredDeviceTypes);
    var observableDeviceCollection = new ObservableCollection<Device>(devices);

    SetLanguageDictionary();
    
    MainWindow mainWindow = new()
    {
      Types = _registeredDeviceTypes,
      Devices = observableDeviceCollection,
      DeviceAssociatedControls = _deviceAssociatedControls
    };
    mainWindow.Show();
  }

  /// <summary>
  /// Finds derived types (ignoring abstract classes)
  /// </summary>
  /// <param name="type"><see cref="Type"/> whose derived types to look for</param>
  /// <returns>A <see cref="List{T}"/> of types derived from <paramref name="type"/> parameter</returns>
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

  /// <summary>
  /// Sets correct MergedDictionary based on current culture
  /// </summary>
  private void SetLanguageDictionary()
  {
    // Default (English) MergedDictionary is specified in the markup to get Intellisense. This should be refactored
    // if there were more languages to be added
    if (!Thread.CurrentThread.CurrentCulture.Equals(new CultureInfo("pl-PL"))) return;
    // It's important that the English MergedDictionary remains on first index in the markup because of this
    Resources.MergedDictionaries.RemoveAt(0);
    var dictionary = new ResourceDictionary
    {
      Source = new Uri(@".\Resources\Resources.pl-PL.xaml", UriKind.Relative)
    };
    Resources.MergedDictionaries.Add(dictionary);
  }
}

/// <summary>
/// IValueConverter inverting booleans
/// </summary>
[ValueConversion(typeof(bool), typeof(bool))]
public class BooleanInversionConverter : IValueConverter
{
  public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
  {
    var b = (bool)value;
    return !b;
  }

  public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
  {
    var b = (bool)value;
    return !b;
  }
}