using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows.Controls;

namespace PolEko;

public partial class SideMenu
{
  public SideMenu(ObservableCollection<Device> devices)
  {
    InitializeComponent();
    
    devices.CollectionChanged += delegate(object? sender, NotifyCollectionChangedEventArgs args)
    {
      if (args.NewItems == null) return;
      foreach (var item in args.NewItems)
      {
        Button btn = new();
        btn.Content = item.ToString();
        Stack.Children.Add(btn);
      }
    };
  }
}