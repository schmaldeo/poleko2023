using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Net;
using System.Windows;
using System.Windows.Controls;

namespace PolEko;

public partial class SideMenu
{
  private readonly Action<IPAddress, ushort, string?> _newDeviceDelegate;
  public SideMenu(ObservableCollection<Device> devices, Action<IPAddress, ushort, string?> addNewDevice)
  {
    _newDeviceDelegate = addNewDevice;

    InitializeComponent();
    devices.CollectionChanged += delegate(object? sender, NotifyCollectionChangedEventArgs args)
    {
      if (args.NewItems == null) return;
      foreach (var item in args.NewItems)
      {
        Button btn = new()
        {
          Content = item.ToString()
        };
        Stack.Children.Add(btn);
      }
    };
  }

  private void AddNewDevice_Click(object sender, RoutedEventArgs e)
  {
    IpPrompt prompt = new(_newDeviceDelegate);
    prompt.Show();
  }
}