using Microsoft.VisualStudio.TestTools.UnitTesting;

using System;
namespace SensorDataManager;
[TestClass]
public class UnitTests
{
	public static readonly Random RandomVar = new();
	public static SensorDataFormat GetRandom()
	{
		return new()
		{
			Accelerometer = new()
			{
				X = Rd01(),
				Y = Rd01(),
				Z = Rd01(),
				Enabled = true,
			},
			Barometer = new(RandomVar.NextDouble() * 1000, true),
			Compass = new(RandomVar.NextDouble() * 2 - 1, true),
			Gyroscope = new()
			{
				X = Rd01(),
				Y = Rd01(),
				Z = Rd01(),
				Enabled = true,
			},
			Orientation = new()
			{
				W = Rd01(),
				X = Rd01(),
				Y = Rd01(),
				Z = Rd01(),
				Enabled = true,
			},
		};
		static float Rd01() => RandomVar.NextSingle() * 2 - 1;
	}
	[TestMethod]
	public void EqualCheck()
	{
		int iterations = 100;
		while (iterations-- > 0)
		{
			SensorDataFormat a = GetRandom(), b = GetRandom(), c = a;
			Assert.IsTrue(a != b);
			Assert.IsTrue(b != c);
			Assert.IsTrue(a == c);
		}
	}
	[TestMethod]
	public void BinarySerializationTest()
	{
		int iterations = 100;
		while (iterations-- > 0)
		{
			SensorDataFormat x = GetRandom();
			var y = x.SerializedBytes();
			Assert.IsTrue(y.Length == SensorDataFormat.ByteSize);
			SensorDataFormat z = SensorDataFormat.Deserialize(y);
			Assert.IsTrue(x == z);
		}
	}
	[TestMethod]
	public void JsonSerializationTest()
	{
		int iterations = 100;
		while (iterations-- > 0)
		{
			SensorDataFormat x = GetRandom();
			var y = x.SerializedJsonString();
			SensorDataFormat z = SensorDataFormat.Deserialize(y);
			Assert.IsTrue(x == z);
		}
	}
}