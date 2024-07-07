using System.Runtime.InteropServices;
using System.Windows;
namespace PMan;
public partial class App : Application
{
	[LibraryImport("kernel32", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static partial bool AllocConsole();

	protected override void OnStartup(StartupEventArgs e)
	{
		//AllocConsole();
		base.OnStartup(e);
	}
}
