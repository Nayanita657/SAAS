namespace Sensor_Client;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.Devices;

using SensorDataManager;

using System.Numerics;

public class MainPage : ContentPage
{
	private const int DelayTime = 650;
	enum State : byte { Disconnected, Connected_Sending, Connected_Delay };
	private readonly Button _counterBtn, _cancelButton;
	private readonly Label _cell;
	private readonly ActivityIndicator _indicator;
	private Task<SensorDataFormat?> _runningTask;
	private bool _cancelledPressed;
	private readonly Entry _addressEntry;
	private const string Filename = "id.dat"; 
	private readonly string _idFile;
	private byte ID = 0;
	public MainPage()
	{
		_idFile = Path.Combine(FileSystem.Current.CacheDirectory, Filename);
		if (File.Exists(_idFile))
			ID = byte.Parse(File.ReadAllText(_idFile));
		_runningTask = null!;
		Content = new ScrollView()
		{
			Content = new VerticalStackLayout()
			{
				Spacing = 25,
				Padding = 30,
				VerticalOptions = LayoutOptions.Center,
				Children =
				{
					new Label()
					{
						Text="Pi sensor connector",
						FontSize=32,
						HorizontalOptions = LayoutOptions.Center,
					},
					new Label()
					{
						Text="Insert details to send data to the server",
						FontSize = 18,
						HorizontalOptions = LayoutOptions.Center,
						HorizontalTextAlignment = TextAlignment.Center,
					},
					(_addressEntry = new Entry() { Placeholder="Insert IP address of the destination server device", }),
					(_counterBtn = new Button(){Text="Connect",}),
					(_cancelButton = new Button(){ Text="Cancel Connection", IsEnabled = false}),
					(_indicator=new ActivityIndicator() { IsRunning = false, }),
					(_cell = new Label(){Text="Uninitialized", LineBreakMode=LineBreakMode.WordWrap, FontAutoScalingEnabled=true, HorizontalTextAlignment = TextAlignment.Center,}),
				}
			}
		};
		_counterBtn.Clicked += OnCounterClicked;
		_cancelButton.Clicked += (o, e) => _cancelledPressed = true;
	}
	private async void OnCounterClicked(object? sender, EventArgs e)
	{
		_cancelledPressed = false;
		_indicator.IsEnabled = true;
		_indicator.IsRunning = true;
		_cancelButton.IsEnabled = true;
		_counterBtn.IsEnabled = false;
		_cell.Text = "Please wait...";
		SocketManager.ClientResponseType response;
		do
		{
			_runningTask = Task.Run(() => GetSensorDataInstance());
			SensorDataFormat data_obj;
			{
				SensorDataFormat? data = await _runningTask;
				if (data == null)
				{
					_cell.Text = "Error fetching Sensor Data!";
					_cell.TextColor = Color.FromRgb(255, 0, 0);
					break;
				}
				byte device = 0;
				DeviceIdiom current = DeviceInfo.Current.Idiom;
				if (current == DeviceIdiom.Phone) device = 1;
				else if (current == DeviceIdiom.Tablet) device = 2;
				else if (current == DeviceIdiom.TV) device = 3;
				else if (current == DeviceIdiom.Desktop) device = 4;
				else if (current == DeviceIdiom.Watch) device = 5;
				else if (current == DeviceIdiom.Unknown) device = 6;
				data_obj = data.Value;
				data_obj.DeviceID = device;
			}
			_cell.Text = $"Sending data\n{data_obj.SerializedJsonString()}";
			string address = _addressEntry.Text;
			int port = SensorDataManager.SocketManager.Port;
			if(SocketManager.CheckValid(address, port.ToString())==false)
			{
				_cell.Text = "Enter valid details!";
				_cell.TextColor = Color.FromRgb(255, 0, 0);
				break;
			}
			byte mid;
			(response, mid) = await SocketManager.ClientSendData(_addressEntry.Text, port, data_obj.SerializedBytes());
			if(ID == 0)
			{
				ID = mid;
				File.WriteAllText(_idFile, ID.ToString());
			}
			_cell.TextColor = response == SocketManager.ClientResponseType.AllOk ? Color.FromRgb(0, 255, 0) : Color.FromRgb(255, 0, 0);
			_cell.Text = response switch
			{
				SocketManager.ClientResponseType.AllOk => $"Data sent successfully! Identified as MachineID = {mid}",
				SocketManager.ClientResponseType.CouldNotConnect => "Connection could not be made to the server",
				SocketManager.ClientResponseType.GotWrongResponse => "Server denined the sent data",
				_ => "Did not get a valid response from the server",
			};
		}
		while (_cancelledPressed == false && response == SocketManager.ClientResponseType.AllOk);
		_indicator.IsRunning = false;
		_indicator.IsEnabled = false;
		_cancelButton.IsEnabled = false;
		_counterBtn.IsEnabled = true;
	}
	public SensorDataFormat? GetSensorDataInstance()
	{
		SensorDataFormat.AccelerometerDataFormat GetAccelerometerData()
		{
			SensorDataFormat.AccelerometerDataFormat fail = new(Vector3.Zero, false), data = fail;
			var sensor = Accelerometer.Default;
			if (sensor.IsSupported && sensor.IsMonitoring == false)
			{
				// Turn on sensor
				sensor.ReadingChanged += WaitForReading;
				sensor.Start(SensorSpeed.UI);
				Task.Delay(DelayTime).Wait();
				// Turn off sensor
				sensor.Stop();
				sensor.ReadingChanged -= WaitForReading;
			}
			return data;
			void WaitForReading(object? o, AccelerometerChangedEventArgs arg)
			{
				data = new(arg.Reading.Acceleration, true);
			}
		}
		SensorDataFormat.BarometerDataFormat GetBarometerData()
		{
			SensorDataFormat.BarometerDataFormat fail = new(-1, false), data = fail;
			var sensor = Barometer.Default;
			if (sensor.IsSupported && sensor.IsMonitoring == false)
			{
				// Turn on sensor
				sensor.ReadingChanged += WaitForReading;
				sensor.Start(SensorSpeed.UI);
				Task.Delay(DelayTime).Wait();
				// Turn off sensor
				sensor.Stop();
				sensor.ReadingChanged -= WaitForReading;
			}
			return data;
			void WaitForReading(object? o, BarometerChangedEventArgs arg)
			{
				data = new(arg.Reading.PressureInHectopascals, true);
			}
		}
		SensorDataFormat.CompassDataFormat GetCompassData()
		{
			SensorDataFormat.CompassDataFormat fail = new(-100, false), data = fail;
			var sensor = Compass.Default;
			if (sensor.IsSupported && sensor.IsMonitoring == false)
			{
				// Turn on sensor
				sensor.ReadingChanged += WaitForReading;
				sensor.Start(SensorSpeed.UI);
				Task.Delay(DelayTime).Wait();
				// Turn off sensor
				sensor.Stop();
				sensor.ReadingChanged -= WaitForReading;
			}
			return data;
			void WaitForReading(object? o, CompassChangedEventArgs arg)
			{
				data = new(arg.Reading.HeadingMagneticNorth, true);
			}
		}
		SensorDataFormat.GyroscopeDataFormat GetGyroscopeData()
		{
			SensorDataFormat.GyroscopeDataFormat fail = new(Vector3.Zero, false), data = fail;
			var sensor = Gyroscope.Default;
			if (sensor.IsSupported && sensor.IsMonitoring == false)
			{
				// Turn on sensor
				sensor.ReadingChanged += WaitForReading;
				sensor.Start(SensorSpeed.UI);
				Task.Delay(DelayTime).Wait();
				// Turn off sensor
				sensor.Stop();
				sensor.ReadingChanged -= WaitForReading;
			}
			return data;
			void WaitForReading(object? o, GyroscopeChangedEventArgs arg)
			{
				data = new(arg.Reading.AngularVelocity, true);
			}
		}
		SensorDataFormat.OrientationDataFormat GetOrientationData()
		{
			SensorDataFormat.OrientationDataFormat fail = new(new(0, 0, 0, 0), false), data = fail;
			var sensor = OrientationSensor.Default;
			if (sensor.IsSupported && sensor.IsMonitoring == false)
			{
				// Turn on sensor
				sensor.ReadingChanged += WaitForReading;
				sensor.Start(SensorSpeed.UI);
				Task.Delay(DelayTime).Wait();
				// Turn off sensor
				sensor.Stop();
				sensor.ReadingChanged -= WaitForReading;
			}
			return data;
			void WaitForReading(object? o, OrientationSensorChangedEventArgs arg)
			{
				data = new(arg.Reading.Orientation, true);
			}
		}
		try
		{
			return new SensorDataFormat()
			{
				Accelerometer = GetAccelerometerData(),
				Barometer = GetBarometerData(),
				Compass = GetCompassData(),
				Gyroscope = GetGyroscopeData(),
				Orientation = GetOrientationData(),
				MachineID = ID,
			};
		}
		catch { return null; }
	}
}

