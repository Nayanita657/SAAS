using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using SensorDataManager;

using InfluxDB.Client;
using InfluxDB.Client.Writes;

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
	protected readonly CustomLog _logger;
	public Worker(ILogger<Worker> logger)
	{
		SettingFile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "/sensor-service.txt";
		_logger = new(logger);
		if (System.IO.File.Exists(SettingFile) == false)
			throw new Exception($"File {SettingFile} not found! Set up a file with Token, Org and Bucket values in that order!");
		string[] lines = System.IO.File.ReadAllLines(SettingFile);
		Token = lines[0];
		Org = lines[1];
		Bucket = lines[2];
	}
	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		await SocketManager.BeginServerActivity(SocketManager.Server(), OnRecieve, _logger, stoppingToken);
		if (stoppingToken.IsCancellationRequested)
			_logger.Log("Task Cancellation requested. Now closing the task...");
	}
	protected void OnRecieve(SensorDataFormat data)
	{
		using var client = new InfluxDBClient("http://localhost:8086", Token);
		using var writeApi = client.GetWriteApi();
		{
			var a = data.Accelerometer;
			writeApi.WriteRecord($"Accelerometer Enabled={a.Enabled},X={a.X},Y={a.Y},Z={a.Z}", bucket: Bucket, org: Org);
		}
		{
			var b = data.Barometer;
			writeApi.WriteRecord($"Barometer Enabled={b.Enabled},Pressure={b.PressureInHectoPascals}", bucket: Bucket, org: Org);
		}
		{
			var c = data.Compass;
			writeApi.WriteRecord($"Compass Enabled={c.Enabled},Angle={c.NorthAngleInDegree}", bucket: Bucket, org: Org);
		}
		{
			var g = data.Gyroscope;
			writeApi.WriteRecord($"Gyroscope Enabled={g.Enabled},X={g.X},Y={g.Y},Z={g.Z}", bucket: Bucket, org: Org);
		}
		{
			var o = data.Orientation;
			writeApi.WriteRecord($"Orientation Enabled={o.Enabled},X={o.X},Y={o.Y},Z={o.Z},W={o.W}", bucket: Bucket, org: Org);
		}
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
			var result = SocketManager.ClientSendData(args[0], int.Parse(args[1]), bytes).Result;
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
