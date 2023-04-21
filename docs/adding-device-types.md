# Adding new device types to the application

To add a new device type you need to follow these steps:
1. Create a new class deriving from `PolEko.Measurement`
```csharp
public class ExampleMeasurement : Measurement
{
  public int Speed { get; init; }
  public int Rpm { get; init; }
  
  public override string ToString()
  {
  return $"Speed: {Speed}, RPM: {Rpm}";
  }
}
```
2. Create a new class deriving from from `PolEko.Device<TMeasurement, TControl>`
```csharp
// This attribute specifies what name the device will be displayed under in the dropdown list in
// device details prompt. It's not required, if you don't specify it the dropdown will just display
// the class name
[DeviceModel("Example device")]
// First attribute is the measurement class created in the previous step, second attribute is yet
// to be created
public class ExampleDevice : Device<ExampleMeasurement, ExampleControl>
{
  public ExampleDevice(IPAddress ipAddress, ushort port, string? id = null)
    : base(ipAddress, port, id)
  {
    // This is only necessary if API's URL is different than http://<ip_address>:<port>/
    DeviceUri = new Uri($"http://{ipAddress}:{port}/api/");
  }

  public override string Model => "Example device";

  public override string Description => "Device used in docs";
  
  // This is only needed if you need to parse a more complicated JSON response. 
  // If API's JSON response has the same property names and nesting as the TMeasurement class, 
  // it's not necessary to override GetMeasurementFromDeviceAsync()
  protected override async Task<ExampleMeasurement> GetMeasurementFromDeviceAsync(HttpClient client)
  {
    var data = await client.GetStringAsync(DeviceUri);
    using var document = JsonDocument.Parse(data);
    var root = document.RootElement;
    var speed = root.GetProperty("speed").GetInt32();
    var rpmElement = root.GetProperty("rpm_props");
    var rpm = rpmElement.GetProperty("rpm").GetInt32();
    var error = rpmElement.GetProperty("error").GetBoolean();

    var measurement = new ExampleMeasurement
    {
      Speed = speed,
      Rpm = rpm,
      Error = error
    };

    return measurement;
  }
}
```
3. Create a new class deriving from `PolEko.DeviceControl<TDevice, TMeasurement, TOwner>`
```csharp
public partial class ExampleControl
{
  public ExampleControl()
  {
    InitializeComponent();
  }
  
  // It's very important to add this constructor as this is how the controls are initialised
  public ExampleControl(ExampleDevice device, HttpClient httpClient) : base(device, httpClient)
  {
    InitializeComponent();
  }
}
```
and XAML for it:
```xaml
<local:DeviceControl x:Class="PolEko.ui.ExampleControl"
                     xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                     xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                     xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
                     xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
                     xmlns:polEko="clr-namespace:PolEko"
                     xmlns:local="clr-namespace:PolEko.ui"
                     x:TypeArguments="polEko:ExampleDevice, polEko:ExampleMeasurement, local:ExampleControl"
                     mc:Ignorable="d"
                     DataContext="{Binding RelativeSource={RelativeSource Self}}"
                     d:DesignHeight="450" d:DesignWidth="800">
  <Grid>
    <Grid.ColumnDefinitions>
      <ColumnDefinition Width="*" />
      <ColumnDefinition Width="*" />
    </Grid.ColumnDefinitions>
    <Grid.RowDefinitions>
      <RowDefinition Height="*" />
      <RowDefinition Height="10*" />
    </Grid.RowDefinitions>
    
    <Button HorizontalAlignment="Left" Click="FetchData_Click" Content="Fetch" />
    <Button Grid.Row="0" Grid.Column="0" HorizontalAlignment="Right" Click="FetchData_Click" Content="Stop" />
    <Button Grid.Row="0" Grid.Column="1" HorizontalAlignment="Right" Click="DeleteDevice_Click" Content="Delete" />
    <Button Grid.Row="0" Grid.Column="1" HorizontalAlignment="Left" Click="CloseDevice_Click" Content="Close" />
    
    <StackPanel Grid.Column="0" Grid.Row="1">
      <TextBlock HorizontalAlignment="Center" FontSize="18" Text="RPM" />
      <TextBlock HorizontalAlignment="Center" FontSize="24" Text="{Binding Device.LastMeasurement.Rpm, FallbackValue=0}" />
    </StackPanel>
    <StackPanel Grid.Row="1" Grid.Column="1">
      <TextBlock HorizontalAlignment="Center" FontSize="18" Text="Speed" />
      <TextBlock HorizontalAlignment="Center" FontSize="24" Text="{Binding Device.LastMeasurement.Speed, FallbackValue=0}" />
    </StackPanel>
  </Grid>
</local:DeviceControl>
```
Of course it's a very simple UI and it lacks a bit of features like buttons enabled based on fetching status or a status bar. 
If you want to see how to implement these things you can look at the implementation of `PolEko.SmartProMeasurement`.
Unfortunately it's not possible to inherit XAML in WPF, that's why there's a need to create separate XAML files for each Control class.

#### Now you can open the app and create a new device of a type you just implemented.