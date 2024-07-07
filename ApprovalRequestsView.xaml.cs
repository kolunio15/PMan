using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
namespace PMan;
using static ChoiceFields;
public partial class ApprovalRequestsView : UserControl
{
	class Row(ApprovalRequest ar, LoginContext login, Database db)
	{
		public bool CanReject { get; } =
			login.EmployeeId == ar.ApproverId && ar.LeaveRequest.Status is LeaveStatus.WaitingForApproval;
		public bool CanApprove { get; } =
			login.EmployeeId == ar.ApproverId && ar.LeaveRequest.Status is LeaveStatus.WaitingForApproval;

		public long ApprovalRequestId { get; } = ar.Id;
		public string Approver { get; } = EmployeeToString(db.GetEmployee(ar.ApproverId));
		public string ApproverComment { get; } = ar.ApproverComment ?? "";
		public long LeaveRequestId { get; } = ar.LeaveRequest.Id;
		public string Employee { get; } = EmployeeToString(db.GetEmployee(ar.LeaveRequest.Employee));
		public string AbsenceReason { get; set; } = AbsenceReasonToString(ar.LeaveRequest.AbsenceReason);
		public DateTime StartDate { get; } = ar.LeaveRequest.StartDate;
		public DateTime EndDate { get; } = ar.LeaveRequest.EndDate;
		public string EmployeeComment { get; } = ar.LeaveRequest.Comment ?? "";
		public string Status { get; set; } = LeaveStatusToString(ar.LeaveRequest.Status);
	}

	class Context : VmBase
	{
		public string[] AbsenceReason { get; } = ChoiceFields.AbsenceReason;
		public string[] LeaveStatus { get; } = ChoiceFields.LeaveStatus;


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
			Rows = [..db.GetApprovalRequests()
				.Where(ar => login.Position is EmployeePosition.Administrator || ar.ApproverId == login.EmployeeId || ar.LeaveRequest.Employee == login.EmployeeId)
				.Where(ar => {
					if (long.TryParse(RequestNumberSearch, out long id))
					{
						return ar.Id == id;
					}
					else
					{
						return true;
					}
				})
				.Select(lr => new Row(lr, login, db))
			] ;
		}

	}

	LoginContext login;
	Context context;
	internal ApprovalRequestsView(LoginContext login)
	{
		this.login = login;
		InitializeComponent();
		UpdateContext();
	}
	void UpdateContext()
	{
		DataContext = context = new Context(login);
	}

	void RejectButtonClick(object sender, System.Windows.RoutedEventArgs e)
	{
		if (context.SelectedRow is Row r)
		{
			new RejectLeaveRequestWindow(login, r.ApprovalRequestId).ShowDialog();
			UpdateContext();
		}
	}
	void ApproveButtonClick(object sender, System.Windows.RoutedEventArgs e)
	{
		if (context.SelectedRow is Row r)
		{
			if (
				MessageBox.Show(
					"You are about to accept leave request. This cannot be reversed.", 
					"Warning", 
					MessageBoxButton.OKCancel
				) is MessageBoxResult.OK
			)
			{
				using Database db = new();
				if (!db.ApproveLeaveRequest(login.EmployeeId, r.ApprovalRequestId))
				{
					MessageBox.Show("Failed to approve the request. Check if there enough available days off.");
				}
				UpdateContext();
			}
		}
	}
}

