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

	class Context : VmBase
	{
		public string[] AbsenceReason { get; } = ChoiceFields.AbsenceReason;
		public string[] LeaveStatus { get;  } = ChoiceFields.LeaveStatus;

		public string requestNumberSearch = "";
		public string RequestNumberSearch { get => requestNumberSearch; set { SetNotify(ref requestNumberSearch, value); UpdateRows(); } }

		public Row? SelectedRow { get; set; }

		Row[] rows;
		public Row[] Rows { get => rows; set => SetNotify(ref rows, value); }

		LoginContext login;
		internal Context(LoginContext login)
		{
			this.login = login;
			UpdateRows();
		}
		void UpdateRows()
		{
			using Database db = new();
			Rows = [.. db.GetLeaveRequests()
			.Where(lr => login.Position is EmployeePosition.Administrator || lr.Employee == login.EmployeeId)
			.Where(lr => {
				if (long.TryParse(RequestNumberSearch, out long id))
				{
					return lr.Id == id;
				}
				else
				{
					return true;
				}
			})
			.Select(lr => new Row(lr, login, db))];
		}
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
		DataContext = context = new Context(login);
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
					"You are about to cancel leave request. This cannot be reversed. Continue?", 
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