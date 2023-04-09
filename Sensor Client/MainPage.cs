namespace Sensor_Client;
using Microsoft.Maui.Devices.Sensors;

using SensorDataManager;

using System.Numerics;

public class MainPage : ContentPage
{
	private const int DelayTime = 750;
	enum State : byte { Disconnected, Connected_Sending, Connected_Delay };
	private readonly Button _counterBtn, _cancelButton;
	private State _state;
	private Label _cell;
	private ActivityIndicator _indicator;
	private Task<SensorDataFormat?> runningTask;
	private bool CancelledPressed;
	private Entry _addressEntry, _portEntry; 
	public MainPage()
	{
		_state = State.Disconnected;
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
					(_portEntry =  new Entry() { Placeholder="Insert Port number destination server process", Keyboard=Keyboard.Numeric, }),
					(_counterBtn = new Button(){Text="Connect",}),
					(_cancelButton = new Button(){ Text="Cancel Connection", IsEnabled = false}),
					(_indicator=new ActivityIndicator() { IsRunning = false, }),
					(_cell = new Label(){Text="Uninitialized", LineBreakMode=LineBreakMode.WordWrap, FontAutoScalingEnabled=true, HorizontalTextAlignment = TextAlignment.Center,}),
				}
			}
		};
		_counterBtn.Clicked += OnCounterClicked;
		_cancelButton.Clicked += (o, e) => CancelledPressed = true;
	}
	private async void OnCounterClicked(object? sender, EventArgs e)
	{
		CancelledPressed = false;
		_indicator.IsEnabled = true;
		_indicator.IsRunning = true;
		_cancelButton.IsEnabled = true;
		_counterBtn.IsEnabled = false;
		_cell.Text = "Please wait...";
		SocketManager.ClientResponseType response;
		do
		{
			runningTask = Task.Run(() => GetSensorDataInstance());
			SensorDataFormat? data = await runningTask;
			if (data == null)
			{
				_cell.Text = "Error fetching Sensor Data!";
				_cell.TextColor = Color.FromRgb(255, 0, 0);
				break;
			}
			_cell.Text = $"Sending data\n{data.Value.SerializedJsonString()}";
			string address = _addressEntry.Text, port_text = _portEntry.Text;
			if(SocketManager.CheckValid(address, port_text)==false)
			{
				_cell.Text = "Enter valid details!";
				_cell.TextColor = Color.FromRgb(255, 0, 0);
				break;
			}
			response = await SocketManager.ClientSendData(_addressEntry.Text, int.Parse(_portEntry.Text), data.Value.SerializedBytes());
			_cell.TextColor = response == SocketManager.ClientResponseType.AllOk ? Color.FromRgb(0, 255, 0) : Color.FromRgb(255, 0, 0);
			_cell.Text = response switch
			{
				SocketManager.ClientResponseType.AllOk => $"Data sent successfully!, {response}",
				SocketManager.ClientResponseType.CouldNotConnect => "Connection could not be made to the server",
				SocketManager.ClientResponseType.GotWrongResponse => "Server denined the sent data",
				_ => "Did not get a valid response from the server",
			};
		}
		while (CancelledPressed == false && response == SocketManager.ClientResponseType.AllOk);
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
				sensor.ReadingChanged += waitForReading;
				sensor.Start(SensorSpeed.UI);
				Task.Delay(DelayTime).Wait();
				// Turn off sensor
				sensor.Stop();
				sensor.ReadingChanged -= waitForReading;
			}
			return data;
			void waitForReading(object? o, AccelerometerChangedEventArgs arg)
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
				sensor.ReadingChanged += waitForReading;
				sensor.Start(SensorSpeed.UI);
				Task.Delay(DelayTime).Wait();
				// Turn off sensor
				sensor.Stop();
				sensor.ReadingChanged -= waitForReading;
			}
			return data;
			void waitForReading(object? o, BarometerChangedEventArgs arg)
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
				sensor.ReadingChanged += waitForReading;
				sensor.Start(SensorSpeed.UI);
				Task.Delay(DelayTime).Wait();
				// Turn off sensor
				sensor.Stop();
				sensor.ReadingChanged -= waitForReading;
			}
			return data;
			void waitForReading(object? o, CompassChangedEventArgs arg)
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
				sensor.ReadingChanged += waitForReading;
				sensor.Start(SensorSpeed.UI);
				Task.Delay(DelayTime).Wait();
				// Turn off sensor
				sensor.Stop();
				sensor.ReadingChanged -= waitForReading;
			}
			return data;
			void waitForReading(object? o, GyroscopeChangedEventArgs arg)
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
				sensor.ReadingChanged += waitForReading;
				sensor.Start(SensorSpeed.UI);
				Task.Delay(DelayTime).Wait();
				// Turn off sensor
				sensor.Stop();
				sensor.ReadingChanged -= waitForReading;
			}
			return data;
			void waitForReading(object? o, OrientationSensorChangedEventArgs arg)
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
			};
		}
		catch { return null; }
	}
}

