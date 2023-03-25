using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Net;
using System.Windows;
using System.Windows.Controls;

namespace PolEko;

public partial class SideMenu
{
  /// <summary>
  /// Action to fire when a new device is added through the prompt
  /// </summary>
  private readonly Action<IPAddress, ushort, string?> _newDeviceAction;

  /// <summary>
  /// 
  /// </summary>
  /// <param name="devices"><c>ObservableCollection</c>, which the <c>SideMenu</c>'s content will be based on></param>
  /// <param name="addNewDevice"><c>Action</c> to fire when a new device is added through a prompt</param>
  /// <param name="changeDisplayedDevice"><c>RoutedEventHandler</c> which will be fired when device to display is changed</param>
  public SideMenu(ObservableCollection<Device> devices, Action<IPAddress, ushort, string?> addNewDevice, RoutedEventHandler changeDisplayedDevice)
  {
    _newDeviceAction = addNewDevice;

    InitializeComponent();
    // When items are added to devices collection, create a WPF item for them
    devices.CollectionChanged += delegate(object? _, NotifyCollectionChangedEventArgs args)
    {
      if (args.NewItems == null) return;
      foreach (var item in args.NewItems)
      {
        var dev = (Device)item;
        ListBoxItem listBoxItem = new()
        {
          Content = dev
        };
        listBoxItem.Selected += changeDisplayedDevice;
        ListBox.Items.Add(listBoxItem);
      }
    };
  }

  private void AddNewDevice_Click(object sender, RoutedEventArgs e)
  {
    IpPrompt prompt = new(_newDeviceAction);
    prompt.Show();
  }
}