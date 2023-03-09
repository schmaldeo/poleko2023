using System;
using System.ComponentModel;
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
      // handle saving settings on closing
      MessageBox.Show("Quittin");
    };
    mainWindow.Show();
  }
}