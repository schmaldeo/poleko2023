using System;
using System.Windows;
using Microsoft.Data.Sqlite;

namespace PolEko;

/// <summary>
///   Interaction logic for App.xaml
/// </summary>
public partial class App
{
  private readonly Type[] _registeredMeasurementTypes =
  {
    typeof(WeatherMeasurement),
    typeof(ExampleMeasurement)
  };

  private async void App_Startup(object sender, StartupEventArgs e)
  {
    await using var connection = new SqliteConnection("Data Source=Measurements.db");
    await Database.CreateTables(connection, _registeredMeasurementTypes); 
    
    MainWindow mainWindow = new();
    mainWindow.Closing += delegate
    {
      // TODO: On main windows closing, dont close instantly, rather send potential leftover buffer to database
      // Database calls should be happening every whatever items in the buffer (Queue<> ?)
      MessageBox.Show("Quittin");
    };
    mainWindow.Show();
  }
}