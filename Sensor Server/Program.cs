using System;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using SensorDataManager;

using InfluxDB.Client;
using InfluxDB.Client.Writes;
using System.Diagnostics;

namespace Program;
public class Worker : BackgroundService
{
	public readonly string SettingFile;
	public readonly string Bucket, Org, Token;

	protected class CustomLog : ILog
	{
		private readonly ILogger<Worker> _logger;
		public void Log(string message) => _logger.LogInformation("{message} || {time}", message, DateTimeOffset.Now);
		public void LogError(string message) => _logger.LogError("ERROR: {}", message);
		public CustomLog(ILogger<Worker> arg) => _logger = arg;
	}
	public readonly string DatabaseFile;
	protected readonly CustomLog _logger;
	protected readonly Stopwatch _globalTimer;
	protected byte CurrentMachineID;
	protected Dictionary<byte, ulong> MachineCumulativeBytes;
	public Worker(ILogger<Worker> logger)
	{
		string appdatafolder = System.Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
		DatabaseFile = appdatafolder + "/database.txt";
		if (File.Exists(DatabaseFile))
			CurrentMachineID = byte.Parse(File.ReadAllText(DatabaseFile));
		else
			CurrentMachineID = 0;
		SettingFile = appdatafolder + "/sensor-service.txt";
		_logger = new(logger);
		_logger.Log($"Database saved at {DatabaseFile}, Max Machine ID: {CurrentMachineID}");
		if (System.IO.File.Exists(SettingFile) == false)
			throw new Exception($"File {SettingFile} not found! Set up a file with Token, Org and Bucket values in that order!");
		string[] lines = System.IO.File.ReadAllLines(SettingFile);
		Token = lines[0];
		Org = lines[1];
		Bucket = lines[2];
		_globalTimer = new();
		_globalTimer.Start();
		MachineCumulativeBytes = new();
	}
	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		await SocketManager.BeginServerActivity(SocketManager.Server(), OnRecieve, _logger, stoppingToken);
		if (stoppingToken.IsCancellationRequested)	
			_logger.Log("Task Cancellation requested. Now closing the task...");
	}
	protected byte OnRecieve(SensorDataFormat data, double time)
	{
		var watch = new Stopwatch();
		watch.Start();
		using var client = new InfluxDBClient("http://localhost:8086", Token);
		byte mid = data.MachineID;
		if (mid == 0)
		{
			++CurrentMachineID;
			File.WriteAllText(DatabaseFile, CurrentMachineID.ToString());
			mid = CurrentMachineID;
			_logger.Log($"New device found! Setting MachineID = {mid} for this device ...\n");
		}
		if(MachineCumulativeBytes.ContainsKey(mid) == false)
			MachineCumulativeBytes.Add(mid, SensorDataFormat.ByteSize);
		else
			MachineCumulativeBytes[mid] += SensorDataFormat.ByteSize;
		string tag = data.DeviceID == 1 ? "Phone" : "SmartWatch";
		using var writeApi = client.GetWriteApi();
		{
			var a = data.Accelerometer;
			writeApi.WriteRecord($"Accelerometer SensorID={mid},DeviceType=\"{tag}\",Enabled={a.Enabled},X={a.X},Y={a.Y},Z={a.Z}", bucket: Bucket, org: Org);
		}
		{
			var b = data.Barometer;
			writeApi.WriteRecord($"Barometer SensorID={mid},DeviceType=\"{tag}\",Enabled={b.Enabled},Pressure={b.PressureInHectoPascals}", bucket: Bucket, org: Org);
		}
		{
			var c = data.Compass;
			writeApi.WriteRecord($"Compass SensorID={mid},DeviceType=\"{tag}\",Enabled={c.Enabled},Angle={c.NorthAngleInDegree}", bucket: Bucket, org: Org);
		}
		{
			var g = data.Gyroscope;
			writeApi.WriteRecord($"Gyroscope SensorID={mid},DeviceType=\"{tag}\",Enabled={g.Enabled},X={g.X},Y={g.Y},Z={g.Z}", bucket: Bucket, org: Org);
		}
		{
			var o = data.Orientation;
			writeApi.WriteRecord($"Orientation SensorID={mid},DeviceType=\"{tag}\",Enabled={o.Enabled},X={o.X},Y={o.Y},Z={o.Z},W={o.W}", bucket: Bucket, org: Org);
		}
		watch.Stop();
		System.IO.File.AppendAllText("b2s.csv", $"{mid},{watch.ElapsedMilliseconds} \n");
		ulong bytes_recived = MachineCumulativeBytes[mid];
		double speed = SensorDataFormat.ByteSize/time;
		long timestamp = _globalTimer.ElapsedMilliseconds;
		System.IO.File.AppendAllText("p2b.csv", $"{speed},{time},{bytes_recived},{timestamp},{mid} \n");
		return mid;
	}
	public void HandleEvent(object? o, EventArgs e)
	{
		string data, errorMessage;
		switch (e)
		{
			case WriteSuccessEvent:
				_logger.Log("Write to influxDB is successful!");
				break;
			case WriteErrorEvent error:
				{
					data = error.LineProtocol;
					errorMessage = error.Exception.Message;
					_logger.LogError($"ERROR: {errorMessage}\n\n--------------\nData:{data}");
				}
				break;
			case WriteRetriableErrorEvent error:
				{
					data = error.LineProtocol;
					errorMessage = error.Exception.Message;
					_logger.LogError($"ERROR: {errorMessage}\n\n--------------\nData:{data}");
				}
				break;
			case WriteRuntimeExceptionEvent error:
				{
					errorMessage = @error.Exception.Message;
					_logger.LogError($"ERROR: {errorMessage}\n\n--------------");
				}
				break;
		}
	}
}
public static class Program
{
	public static void Main(string[] args)
	{
		if (args.Length == 0)
		{
			Host.CreateDefaultBuilder()
				.ConfigureServices(services =>
					services.AddHostedService<Worker>())
						.Build().Run();
		}
		else
		{
			SensorDataFormat data = UnitTests.GetRandom();
			string json_content = data.SerializedJsonString();
			byte[] bytes = data.SerializedBytes();
			SensorDataFormat x = SensorDataFormat.Deserialize(bytes);
			var (result, mid) = SocketManager.ClientSendData(args[0], int.Parse(args[1]), bytes).Result;
			Console.WriteLine($"Sent object\n{json_content}");
			if (result == SocketManager.ClientResponseType.AllOk)
				Console.WriteLine("Recieved OK from server");
			else if (result == SocketManager.ClientResponseType.GotWrongResponse)
				Console.WriteLine("Recieved NO from server!");
			else if (result == SocketManager.ClientResponseType.CouldNotConnect)
				Console.WriteLine("Could not connect to the server!");
			else
				Console.WriteLine("Did not recive any acknowledgement from the server!");
		}
	}
}
