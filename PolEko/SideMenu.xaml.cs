using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Net;
using System.Windows;
using System.Windows.Controls;

namespace PolEko;

public partial class SideMenu
{
  private readonly Action<IPAddress, ushort, string?> _newDeviceAction;

  public SideMenu(ObservableCollection<Device> devices, Action<IPAddress, ushort, string?> addNewDevice, RoutedEventHandler changeDisplayedDevice)
  {
    _newDeviceAction = addNewDevice;

    InitializeComponent();
    devices.CollectionChanged += delegate(object? _, NotifyCollectionChangedEventArgs args)
    {
      if (args.NewItems == null) return;
      foreach (var item in args.NewItems)
      {
        var dev = (Device)item;
        Button btn = new()
        {
          Content = dev
        };
        btn.Click += changeDisplayedDevice; 
        Stack.Children.Add(btn);
      }
    };
  }

  private void AddNewDevice_Click(object sender, RoutedEventArgs e)
  {
    IpPrompt prompt = new(_newDeviceAction);
    prompt.Show();
  }
}