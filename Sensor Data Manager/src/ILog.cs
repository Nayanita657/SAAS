namespace SensorDataManager;
public interface ILog
{
	void Log(string message);
	void LogError(string message);
}