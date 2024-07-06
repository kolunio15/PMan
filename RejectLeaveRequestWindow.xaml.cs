using System;
using System.Collections.Generic;
using System.Windows;

namespace PMan;
public partial class RejectLeaveRequestWindow : Window
{
	class Context
	{
		public string Comment { get; set; } = "";
	}
	LoginContext login;
	long approvalRequestId;
	Context context;
	internal RejectLeaveRequestWindow(LoginContext login, long approvalRequestId)
	{
		this.login = login;
		this.approvalRequestId = approvalRequestId;
		DataContext = context = new();
		InitializeComponent();
	}

	void CancelButtonClick(object sender, System.Windows.RoutedEventArgs e)
	{
		Close();
	}
	void RejectButtonClick(object sender, System.Windows.RoutedEventArgs e)
	{
		if (
			MessageBox.Show(
				"You are about to reject request. This cannot be reversed.",
				"Warning",
				MessageBoxButton.OKCancel
			) is MessageBoxResult.OK
		)
		{
			using Database db = new();
			if (db.RejectLeaveRequest(login.EmployeeId, approvalRequestId, context.Comment))
			{
				Close();
			}
			else
			{
				MessageBox.Show("Failed to reject the request.");
			}
		}
	}
}
