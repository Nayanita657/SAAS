using System.Numerics;
using System.Text;
using System.Text.Json;
namespace SensorDataManager
{
	[Serializable]
	public struct SensorDataFormat : IEquatable<SensorDataFormat>
	{
		[Serializable] public struct AccelerometerDataFormat
		{
			public float X;
			public float Y;
			public float Z;
			public bool Enabled;
			public AccelerometerDataFormat(Vector3 data, bool e)
			{
				Enabled = e;
				X = data.X;
				Y = data.Y;
				Z = data.Z;
			}
		}
		[Serializable] public struct BarometerDataFormat
		{
			public double PressureInHectoPascals;
			public bool Enabled;
			public BarometerDataFormat(double data, bool e)
			{
				PressureInHectoPascals = data;
				Enabled = e;
			}
		}
		[Serializable] public struct CompassDataFormat
		{
			public double NorthAngleInDegree;
			public bool Enabled;
			public CompassDataFormat(double data, bool e)
			{
				NorthAngleInDegree = data;
				Enabled = e;
			}
		}
		[Serializable] public struct GyroscopeDataFormat
		{
			public float X;
			public float Y;
			public float Z;
			public bool Enabled;
			public GyroscopeDataFormat(Vector3 data, bool e)
			{
				Enabled = e;
				X = data.X;
				Y = data.Y;
				Z = data.Z;
			}
		}
		/**
        <summary>
        The device (generally a phone or tablet) has a 3D coordinate system with the following axes: <br/>

		The positive X-axis points to the right of the display in portrait mode.<br/>
		The positive Y-axis points to the top of the device in portrait mode.<br/>
		The positive Z-axis points out of the screen.<br/><br/>
		The 3D coordinate system of the Earth has the following axes:<br/>

		The positive X-axis is tangent to the surface of the Earth and points east.<br/>
		The positive Y-axis is also tangent to the surface of the Earth and points north.<br/>
		The positive Z-axis is perpendicular to the surface of the Earth and points up.<br/>
		The Quaternion describes the rotation of the device's coordinate 
		system relative to the Earth's coordinate system.<br/><br/>

		A Quaternion value is closely related to rotation around an axis. If an axis of 
		rotation is the normalized vector (x, y, z), 
		and the rotation angle is t, then the (X, Y, Z, W) components of the quaternion are: <br/>
        (x.sin(t/2), y.sin(t/2), z.sin(t/2), cos(t/2))</summary>
         */
		[Serializable] public struct OrientationDataFormat
		{
			public float X;
			public float Y;
			public float Z;
			public float W;
			public bool Enabled;
			public OrientationDataFormat(Quaternion data, bool e)
			{
				Enabled = e;
				X = data.X;
				Y = data.Y;
				Z = data.Z;
				W = data.W;
			}
		}
		public AccelerometerDataFormat Accelerometer;
		public BarometerDataFormat Barometer;
		public CompassDataFormat Compass;
		public GyroscopeDataFormat Gyroscope;
		public OrientationDataFormat Orientation;
		public byte DeviceID;
		public byte MachineID;
		public const int ByteSize = 63;
		public string SerializedJsonString() =>
			JsonSerializer.Serialize(this, new JsonSerializerOptions() { IncludeFields = true });
		public static SensorDataFormat Deserialize(string jsonString) =>
			JsonSerializer.Deserialize<SensorDataFormat>(jsonString, new JsonSerializerOptions() { IncludeFields = true });
		public byte[] SerializedBytes()
		{
			using MemoryStream stream = new();
			using (BinaryWriter writer = new(stream, Encoding.UTF8, false))
			{
				writer.Write(Accelerometer.Enabled);
				writer.Write(Accelerometer.X);
				writer.Write(Accelerometer.Y);
				writer.Write(Accelerometer.Z);

				writer.Write(Barometer.Enabled);
				writer.Write(Barometer.PressureInHectoPascals);

				writer.Write(Compass.Enabled);
				writer.Write(Compass.NorthAngleInDegree);

				writer.Write(Gyroscope.Enabled);
				writer.Write(Gyroscope.X);
				writer.Write(Gyroscope.Y);
				writer.Write(Gyroscope.Z);

				writer.Write(Orientation.Enabled);
				writer.Write(Orientation.X);
				writer.Write(Orientation.Y);
				writer.Write(Orientation.Z);
				writer.Write(Orientation.W);

				writer.Write(DeviceID);
				writer.Write(MachineID);
			}// Total size: (sizeof(bool) * 5) + (sizeof(float) * 10) + (sizeof(double)*2) + (2*sizeof(byte)) = 63 bytes
			return stream.ToArray();
		}
		public static SensorDataFormat Deserialize(byte[] bytes)
		{
			SensorDataFormat x = new();
			using MemoryStream stream = new(bytes);
			using (BinaryReader reader = new(stream, Encoding.UTF8, false))
			{

				x.Accelerometer.Enabled = reader.ReadBoolean();
				x.Accelerometer.X = reader.ReadSingle();
				x.Accelerometer.Y = reader.ReadSingle();
				x.Accelerometer.Z = reader.ReadSingle();

				x.Barometer.Enabled = reader.ReadBoolean();
				x.Barometer.PressureInHectoPascals = reader.ReadDouble();

				x.Compass.Enabled = reader.ReadBoolean();
				x.Compass.NorthAngleInDegree = reader.ReadDouble();

				x.Gyroscope.Enabled = reader.ReadBoolean();
				x.Gyroscope.X = reader.ReadSingle();
				x.Gyroscope.Y = reader.ReadSingle();
				x.Gyroscope.Z = reader.ReadSingle();

				x.Orientation.Enabled = reader.ReadBoolean();
				x.Orientation.X = reader.ReadSingle();
				x.Orientation.Y = reader.ReadSingle();
				x.Orientation.Z = reader.ReadSingle();
				x.Orientation.W = reader.ReadSingle();

				x.DeviceID = reader.ReadByte();
				x.MachineID = reader.ReadByte();
			}
			return x;
		}
		public bool Equals(SensorDataFormat other) => base.Equals(other);
		public override bool Equals(object? obj) => obj is SensorDataFormat format && Equals(format);
		public override int GetHashCode() => Accelerometer.X.GetHashCode();
		public static bool operator ==(SensorDataFormat left, SensorDataFormat right) => left.Equals(right);
		public static bool operator !=(SensorDataFormat left, SensorDataFormat right) => !(left == right);
	}
}
