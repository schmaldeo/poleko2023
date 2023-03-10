using System.Windows;

namespace PolEko;

/// <summary>
///   Interaction logic for App.xaml
/// </summary>
public partial class App
{
  private void App_Startup(object sender, StartupEventArgs e)
  {
    MainWindow mainWindow = new();
    mainWindow.Closing += delegate
    {
      // On main windows closing, dont close instantly, rather send potential leftover buffer to database
      // Database calls should be happening every whatever items in the buffer (Queue<> ?)
      MessageBox.Show("Quittin");
    };
    mainWindow.Show();
  }
}