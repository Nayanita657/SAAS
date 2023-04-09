namespace Sensor_Client;
public class AppShell : Shell
{
	public AppShell()
	{
		FlyoutBehavior = FlyoutBehavior.Disabled;
		Items.Add(
			new ShellContent()
			{
				Title = "Home",
				Route = "MainPage",
				Content = new MainPage(),
			}
		);
	}
}
