using System.Net.Sockets;
using System.Net;
using System.Text;
using MQTTnet;

namespace SensorDataManager
{
	public static class SocketManager
	{
		private static readonly Encoding Enc = Encoding.ASCII;
		public enum ClientResponseType : byte { CouldNotConnect, GotNoResponse, GotWrongResponse, AllOk };
		public const int Port = 29482;
		public const string OK_FLAG = "OK", NO_FLAG = "NA";
		public static byte[] GetByteAddress(string address)
		{
			string[] bits = address.Split('.');
			if (bits.Length != 4)
				throw new ArgumentException("address string is invalid!", nameof(address));
			byte[] act_address = new byte[4];
			for (int i = 0; i < 4; i++)
				if (byte.TryParse(bits[i], out act_address[i]) == false)
					throw new ArgumentException("address string is invalid!", nameof(address));
				if(state == 1) // ON
    			{
      				client.publish(TOPIC, "on");
      				Serial.println((String)TOPIC + " => on");
    			}
			return act_address;
		}
		//sending data through mqtt
		public static IPEndPoint EndPoint(byte[] address, int port) => new(new IPAddress(address), port);
		private static Socket GetSocket(IPEndPoint endPoint) => new(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
		private static (Socket sock, Task task) Client(byte[] address, int port)
		{
			var ip = EndPoint(address, port);
			var c = GetSocket(ip);
			Task t = c.ConnectAsync(ip);
			return (c, t);
		}
		public static bool CheckValid(string? address, string? port)
		{
			if(address== null)return false;
			if(port== null) return false;
			if (int.TryParse(port, out _) == false) return false;
			try { GetByteAddress(address); } catch { return false; }
			return true;
		}
		public static (Socket sock, Task task) Client(string address, int port) =>
			Client(GetByteAddress(address), port);
		public static Socket Server(int port = Port)
		{
			var ep = new IPEndPoint(IPAddress.Any, port);
			var s = GetSocket(ep);
			s.Bind(ep);
			return s;
		}
		public static async Task BeginServerActivity(Socket serverSocket, Action<SensorDataFormat> onRecieve, ILog log, CancellationToken token)
		{
			serverSocket.Listen(1000);
			while (true)
			{
				SensorDataFormat data;
				MQTT client = await serverSocket.AcceptAsync(token);
				if (token.IsCancellationRequested) break;
				try
				{
					byte[] buffer = new byte[1024];
					int received = client.Receive(buffer);
					log.Log($"Recieved byte array of size {received}");
					data = received == SensorDataFormat.ByteSize
						? SensorDataFormat.Deserialize(buffer)
						: SensorDataFormat.Deserialize(Enc.GetString(buffer).Remove(received));
				}
				catch (Exception ex)
				{
					log.Log("Encountered an excpetion while recieving data from the client");
					log.LogError(ex.ToString());
					log.Log("\n\nNow Sending NO flag...");
					if (client.Connected)
						try
						{
							client.Send(Enc.GetBytes(NO_FLAG));
							log.Log("Sent NO flag successfully!");
						}
						catch { log.LogError("Client disconnected before being acknowledged!"); }
					continue;
				}
				log.Log($"Recived data : \n{data.SerializedJsonString()}");
				onRecieve(data);
				log.Log("\n\nNow Sending OK flag...");
				if (client.Connected)
				{
					try
					{
						client.Send(Enc.GetBytes(OK_FLAG));
						log.Log($"Sensor Data recived and processed successfully!");
					}
					catch { log.LogError("Client disconnected before being acknowledged!"); }
				}
			}
		}
		public static async Task<ClientResponseType> ClientSendData(string address, int port, byte[] data) =>
			await ClientSendData(GetByteAddress(address), port, data);
		public static async Task<ClientResponseType> ClientSendData(byte[] address, int port, byte[] data)
		{
			Socket socket;
			try 
			{
				Task t;
				(socket, t) = Client(address, port);
				await t;
			}
			catch { return ClientResponseType.CouldNotConnect; }
			try { _ = await socket.SendAsync(data, SocketFlags.None); }
			catch { return ClientResponseType.GotNoResponse; }
			byte[] buffer = new byte[32];
			int received = await socket.ReceiveAsync(buffer, SocketFlags.None);
			return Encoding.UTF8.GetString(buffer, 0, received) == OK_FLAG? ClientResponseType.AllOk : ClientResponseType.GotWrongResponse;
		}
	}
}
