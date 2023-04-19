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
  /// <summary>
  /// ~english Types derived from <see cref="Device{TMeasurement,TControl}"/> being <c>TKey</c> and the <see cref="DeviceControl{TDevice,TMeasurement,TOwner}"/>
  /// associated with it being <c>TValue</c>
  /// ~polish Typy dziedziczące z klasy <see cref="Device{TMeasurement,TControl}"/> i odpowiadające im <see cref="DeviceControl{TDevice,TMeasurement,TOwner}"/>
  /// odpowiednio jako <c>TKey</c> i <c>TValue</c>
  /// </summary>
  private readonly Dictionary<Type, Type> _deviceAssociatedControls = new();
  
  /// <summary>
  /// ~english <see cref="Dictionary{TKey,TValue}"/> of <see cref="Type"/>s of <see cref="Device{TMeasurement,TControl}"/> and the
  /// <c>Name</c> property of that Type as <c>TKey</c>
  /// ~polish Słownik typów urządzeń jako <c>TKey</c> i ich reprezentacja typu string
  /// </summary>
  private readonly Dictionary<string, Type> _registeredDeviceTypes = new();
  
  /// <summary>
  /// ~english List of <see cref="Type"/>s derived from <see cref="Measurement"/> that are associated with at least one device
  /// ~polish Lista typów dziedziczących z klasy <see cref="Measurement"/>, które są powiązane z co najmniej jednym urządzeniem
  /// </summary>
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
  /// ~english Finds derived types (ignoring abstract classes)
  /// ~polish Znajduje powiązane typy (ignorując klasy z modyfikatorem abstract)
  /// </summary>
  /// <param name="type">
  /// ~english <see cref="Type"/> whose derived types to look for
  /// ~polish Typy, które chcemy znaleźć, które dziedziczą z tego parametru
  /// </param>
  /// <returns>
  /// ~english A <see cref="List{T}"/> of types derived from <paramref name="type"/> parameter
  /// ~polish Lista typów, które dziedziczą z parametru <paramref name="type"/>
  /// </returns>
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
  /// ~english Sets correct MergedDictionary based on current culture
  /// ~polish Ustawia słownik języków bazując na obecnej kulturze
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
/// ~english IValueConverter inverting booleans
/// ~polish IValueConverter odwracający typ logiczny
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