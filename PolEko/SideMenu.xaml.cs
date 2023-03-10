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
  private readonly Action<IPAddress, ushort, string?> _newDeviceAction;
  private readonly RoutedEventHandler _changeDisplayedDeviceAction;
  
  public SideMenu(ObservableCollection<Device> devices, Action<IPAddress, ushort, string?> addNewDevice, RoutedEventHandler changeDisplayedDevice)
  {
    _newDeviceAction = addNewDevice;
    _changeDisplayedDeviceAction = changeDisplayedDevice;

    InitializeComponent();
    devices.CollectionChanged += delegate(object? sender, NotifyCollectionChangedEventArgs args)
    {
      if (args.NewItems == null) return;
      foreach (var item in args.NewItems)
      {
        var dev = (Device)item;
        Button btn = new()
        {
          Content = dev
        };
        btn.Click += _changeDisplayedDeviceAction; 
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