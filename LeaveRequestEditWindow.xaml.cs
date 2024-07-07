using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;

namespace PMan;
using static ChoiceFields;

class ErrorNotify : INotifyDataErrorInfo
{
	public bool HasErrors => errors.Count > 0;
	public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

	Dictionary<string, string> errors = [];
	public IEnumerable GetErrors(string? propertyName)
	{
		if (errors.TryGetValue(propertyName, out string? error))
			return new string[] { error };
		else
			return Array.Empty<string>();
	}

	protected void ClearErrors()
	{
		string[] properties = [.. errors.Keys];
		errors.Clear();
		foreach (string property in properties)
		{
			ErrorsChanged?.Invoke(this, new(property));
		}
	}

	protected void SetError(string propertyName, string? error)
	{
		errors.Remove(propertyName ?? "");
		if (error != null)
		{
			errors.Add(propertyName, error);
		}
		ErrorsChanged?.Invoke(this, new(propertyName));
	}
}

public partial class LeaveRequestEditWindow : Window
{
	class Row : ErrorNotify
	{
		string absenceReason;
		public string AbsenceReason { get => absenceReason; set { absenceReason = value; Validate(); } }
		DateTime startDate;
		public DateTime StartDate { get => startDate; set { startDate = value; Validate(); } }
		DateTime endDate;
		public DateTime EndDate { get => endDate; set { endDate = value; Validate(); } }
		string comment;
		public string Comment { get => comment; set { comment = value; Validate(); } }
		
		public Row(AbsenceReason absenceReason, DateTime startDate, DateTime endDate, string comment)
		{
			this.absenceReason = AbsenceReasonToString(absenceReason);
			this.startDate = startDate;
			this.endDate = endDate;
			this.comment = comment;
			Validate();
		}
		void Validate()
		{
			ClearErrors();
			if (StartDate <= DateTime.Today)
				SetError(nameof(StartDate), "Start must be after today");
			if (StartDate > EndDate)
				SetError(nameof(EndDate), "End must be after start date");
		}
	}
	class Context : VmBase
	{
		public bool CanSave => SingleRow[0].HasErrors == false;
		public string[] AbsenceReason { get; } = ChoiceFields.AbsenceReason;
		public string[] LeaveStatus { get; } = ChoiceFields.LeaveStatus;

		public Row[] SingleRow { get; }

		internal Context(Row row)
		{
			SingleRow = [row];
			row.ErrorsChanged += (_, _) => Notify(nameof(CanSave));
		}

	}

	LoginContext login;
	Context context;
	Row row;
	long? requestId;

	internal LeaveRequestEditWindow(LoginContext login, long? requestId)
	{
		this.login = login;
		this.requestId = requestId;
		if (requestId is long id)
		{
			using Database db = new();
			var lr = db.GetLeaveRequest(id)!;
			row = new(lr.AbsenceReason, lr.StartDate, lr.EndDate, lr.Comment ?? "");
		}
		else
		{
			row = new(AbsenceReason.AnnualLeave, DateTime.Today, DateTime.Today, "");
		}
		DataContext = context = new(row); 
		InitializeComponent();
	}

	void SaveButtonClick(object sender, RoutedEventArgs e)
	{
		if (context.CanSave)
		{
			using Database db = new();
			bool ok;
			if (requestId is long id)
			{
				ok = db.UpdateLeaveRequest(id, AbsenceReasonFromString(row.AbsenceReason)!.Value, row.StartDate, row.EndDate, row.Comment);
			}
			else
			{
				ok = db.AddLeaveRequest(login.EmployeeId, AbsenceReasonFromString(row.AbsenceReason)!.Value, row.StartDate, row.EndDate, row.Comment);
			}
			if (ok)
			{
				Close();
			}
			else
			{
				MessageBox.Show("Failed to save request.");
			}

		}
	}
}
