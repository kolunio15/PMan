using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
namespace PMan;
using static ChoiceFields;
public partial class LeaveRequestsView : UserControl
{
	class Row(LeaveRequest lr, LoginContext login, Database db)
	{
		public bool CanEdit { get; } = 
			login.EmployeeId == lr.Employee && lr.Status is LeaveStatus.New;
		public bool CanCancel { get; } =
			login.EmployeeId == lr.Employee && lr.Status is not LeaveStatus.Cancelled and not LeaveStatus.Rejected;
		public bool CanSubmit { get; } =
			login.EmployeeId == lr.Employee && lr.Status is LeaveStatus.New;
	
		public long Id { get; } = lr.Id;
		public string Employee { get; } = EmployeeToString(db.GetEmployee(lr.Employee));
		public string AbsenceReason { get; set; } = AbsenceReasonToString(lr.AbsenceReason);
		public DateTime StartDate { get; } = lr.StartDate;
		public DateTime EndDate { get; } = lr.EndDate;
		public string Comment { get; } = lr.Comment ?? "";
		public string Status { get; set; } = LeaveStatusToString(lr.Status);
	}

	class Context(LoginContext login, Database db) : VmBase
	{
		public string[] AbsenceReason { get; } = ChoiceFields.AbsenceReason;
		public string[] LeaveStatus { get;  } = ChoiceFields.LeaveStatus;

		
		public Row? SelectedRow { get; set; }
	
		public Row[] Rows { get; } = [.. db.GetLeaveRequests()
			.Where(lr => login.Position is EmployeePosition.Administrator || lr.Employee == login.EmployeeId)
			.Select(lr => new Row(lr, login, db))];
	}

	LoginContext login;
	Context context;
	internal LeaveRequestsView(LoginContext login)
	{
		this.login = login;
		InitializeComponent();
		UpdateContext();
	}
	void UpdateContext()
	{
		using Database db = new();
		DataContext = context = new Context(login, db);
	}

	void AddButtonClick(object sender, System.Windows.RoutedEventArgs e)
	{
		new LeaveRequestEditWindow(login, null).ShowDialog();
		UpdateContext();
	}
	void EditButtonClick(object sender, System.Windows.RoutedEventArgs e)
	{
		if (context.SelectedRow is Row r && r.CanEdit)
		{
			new LeaveRequestEditWindow(login, r.Id).ShowDialog();
			UpdateContext();
		}
	}
	void CancelButtonClick(object sender, System.Windows.RoutedEventArgs e)
	{
		if (context.SelectedRow is Row r && r.CanCancel)
		{
			using Database db = new();
			if (
				MessageBox.Show(
					"You are about to cancel leave request. This cannot be reversed.", 
					"Warning", 
					MessageBoxButton.OKCancel
				) is MessageBoxResult.OK
			)
			{
				if (!db.CancelLeaveRequest(cancelingUserId: login.EmployeeId, r.Id))
				{
					MessageBox.Show("Failed to cancel.");
				}
				UpdateContext();
			}
		}
	}
	void SubmitButtonClick(object sender, System.Windows.RoutedEventArgs e)
	{
		if (context.SelectedRow is Row r && r.CanSubmit)
		{
			using Database db = new();
			if (!db.SubmitLeaveRequest(r.Id))
			{
				MessageBox.Show("Failed to submit, check if HR partner is assigned.");
			}
			UpdateContext();
		}
	}
}