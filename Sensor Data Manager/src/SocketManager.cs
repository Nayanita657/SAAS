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
		public static byte[] GetByteAddress(string address)
		{
			string[] bits = address.Split('.');
			if (bits.Length != 4)
				throw new ArgumentException("address string is invalid!", nameof(address));
			byte[] act_address = new byte[4];
			for (int i = 0; i < 4; i++)
				if (byte.TryParse(bits[i], out act_address[i]) == false)
					throw new ArgumentException("address string is invalid!", nameof(address));
			return act_address;
		}
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
			if (address == null) return false;
			if (port == null) return false;
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
		public static async Task BeginServerActivity(Socket serverSocket, Func<SensorDataFormat, double, byte> onRecieve, ILog log, CancellationToken token)
		{
			serverSocket.Listen(1000);
			while (true)
			{
				SensorDataFormat data;
				Socket client = await serverSocket.AcceptAsync(token);
				var watch = new Stopwatch();
				watch.Start();
				if (token.IsCancellationRequested) break;
				byte[] sendbuffer = new byte[2];
				try
				{
					byte[] buffer = new byte[1024];
					int received = client.Receive(buffer);
					watch.Stop();
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
							sendbuffer[0] = (byte)ClientResponseType.GotWrongResponse;
							client.Send(sendbuffer);
							log.Log("Sent NO flag successfully!");
						}
						catch { log.LogError("Client disconnected before being acknowledged!"); }
					continue;
				}
				double time = -1;
				if (watch.IsRunning == false) time = watch.Elapsed.TotalSeconds;
				log.Log($"Recived data : \n{data.SerializedJsonString()}");
				byte mid = onRecieve(data, time);
				log.Log("\n\nNow Sending OK flag...");
				sendbuffer[0] = (byte)ClientResponseType.AllOk;
				sendbuffer[1] = mid;
				if (client.Connected)
				{
					try
					{
						client.Send(sendbuffer);
						log.Log($"Sensor Data recived and processed successfully!");
					}
					catch { log.LogError("Client disconnected before being acknowledged!"); }
				}
			}
		}
		public static async Task<(ClientResponseType, byte)> ClientSendData(string address, int port, byte[] data) =>
			await ClientSendData(GetByteAddress(address), port, data);
		public static async Task<(ClientResponseType, byte)> ClientSendData(byte[] address, int port, byte[] data)
		{
			Socket socket;
			try
			{
				Task t;
				(socket, t) = Client(address, port);
				await t;
			}
			catch { return (ClientResponseType.CouldNotConnect, 0); }
			try { _ = await socket.SendAsync(data, SocketFlags.None); }
			catch { return (ClientResponseType.GotNoResponse, 0); }
			byte[] buffer = new byte[2];
			int received = await socket.ReceiveAsync(buffer, SocketFlags.None);
			ClientResponseType responseType = (ClientResponseType)buffer[0];
			byte mid = buffer[1];
			return (responseType, mid);
		}
	}
}
