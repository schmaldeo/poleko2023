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

/// <summary>
///   Interaction logic for App.xaml
/// </summary>
public partial class App
{
  private readonly Dictionary<Type, Type> _deviceAssociatedControls = new();
  private readonly Dictionary<string, Type> _registeredDeviceTypes = new();
  private List<Type> _registeredMeasurementTypes = new();

  private async void App_Startup(object sender, StartupEventArgs e)
  {
    // Getting types that derive from Device (except for abstract types). It does use Reflection, so while it is an idea
    // to get back to manually declaring registered device types, as using Reflection definitely is a slight performance hit,
    // I think it's better to rely on automatically getting the types with it. Main benefit is developer experience.
    // There's no need to modify many files when you add a new Device type. On top of that, it's often correct that
    // the end user (or, in case of open source software, a person that potentially makes a fork and tries to add
    // their own features) might not understand what certain features do, therefore the choice to use Reflection here.
    SetLanguageDictionary();
    var deviceTypes = FindDerivedTypes(typeof(Device));
    foreach (var t in deviceTypes)
    {
      _registeredDeviceTypes[t.Name] = t;
      _deviceAssociatedControls[t] = GetAssociatedControl(t);
    }

    _registeredMeasurementTypes = FindDerivedTypes(typeof(Measurement));

    await Database.CreateTablesAsync(_registeredMeasurementTypes);
    var devices = await Database.ExtractDevicesAsync(_registeredDeviceTypes);

    var observableDeviceCollection = new ObservableCollection<Device>(devices);

    MainWindow mainWindow = new()
    {
      Types = _registeredDeviceTypes,
      Devices = observableDeviceCollection,
      DeviceAssociatedControls = _deviceAssociatedControls
    };
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

  private static Type GetAssociatedControl(MemberInfo type)
  {
    // e.g. PolEko.ui.SmartProDeviceControl. The nameofs are useful in case the namespace change
    var controlName = $"{nameof(PolEko)}.{nameof(ui)}.{type.Name}Control";
    var t = Type.GetType(controlName);
    if (t is null) throw new Exception($"{type.Name} hasn't got an associated UserControl");
    if (!t.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDeviceControl<>)))
      throw new Exception($"{controlName} must implement IDeviceControl<>");
    return t;
  }

  private void SetLanguageDictionary()
  {
    // Default (English) MergedDictionary is specified in the markup to get Intellisense. This should be refactored
    // if there were more languages to be added
    if (!Thread.CurrentThread.CurrentCulture.Equals(new CultureInfo("pl-PL"))) return;
    Resources.MergedDictionaries.RemoveAt(0);
    var dictionary = new ResourceDictionary
    {
      Source = new Uri(@".\Resources\Resources.pl-PL.xaml", UriKind.Relative)
    };
    Resources.MergedDictionaries.Add(dictionary);
  }
}

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