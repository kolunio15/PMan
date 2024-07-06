using System.Windows;
namespace PMan;
public partial class MainWindow : Window
{
	readonly LoginContext context;

	internal MainWindow(LoginContext context)
    {
		DataContext = new MainWindowVm(context);
		this.context = context;
        InitializeComponent();
    }

	void EmployeesButtonClick(object sender, RoutedEventArgs e)
	{
		ContentHost.Child = new EmployeesView(context);
	}

	void ApprovalRequestsButtonClick(object sender, RoutedEventArgs e)
	{
		ContentHost.Child = new ApprovalRequestsView(context);
	}

	void LeaveRequestsButtonClick(object sender, RoutedEventArgs e)
	{
		ContentHost.Child = new LeaveRequestsView(context);
	}

	void ProjectsButtonClick(object sender, RoutedEventArgs e)
	{

	}
}
