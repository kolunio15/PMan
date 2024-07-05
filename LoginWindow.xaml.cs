using System.Windows;
namespace PMan;
public partial class LoginWindow : Window
{
	readonly LoginVm vm;
	public LoginWindow()
	{
		InitializeComponent();
		DataContext = vm = new();			
	}

	void LoginButtonClick(object sender, RoutedEventArgs e)
	{
		if (!vm.IsInputValid()) return;

		if (vm.Login() is LoginContext context)
		{
			MainWindow mainWindow = new(context);
			Close();
			mainWindow.Show();
		}
		else
		{
			MessageBox.Show(this, "No such employee.");
		}
	}
}