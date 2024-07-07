using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Controls;

namespace PMan;
using static ChoiceFields;

public partial class EmployeesView : UserControl
{
	static readonly string defaultPhotoPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "./assets/default.png"));


	internal class Row(LoginContext login, Employee e, Database db)
	{
		public bool CanEdit { get; } = 
			(login.Position is EmployeePosition.Administrator) || 
			(login.Position is EmployeePosition.HRManager && e.Position is EmployeePosition.ProjectManager or EmployeePosition.Employee);
		public long Id { get; } = e.Id;
		public string FullName { get; } = e.FullName;
		public string Subdivision { get; } = SubdivisionToString(e.Subdivision);
		public string Position { get; } = EmployeePositionToString(e.Position);
		public string ActiveStatus { get; } = ActiveStatusToString(e.ActiveStatus);
		public string PeoplePartner { get; } = EmployeeToString(e.PeoplePartnerId == null ? null : db.GetEmployee(e.PeoplePartnerId.Value));
		public long AvailibleDaysOff { get; } = e.AvailibleDaysOff;
		public string PhotoPath { get; } = e.PhotoPath ?? defaultPhotoPath;
		
		void UpdateDatabase()
		{
			//Employee e = new(
				//Id,
				//fullName,
				//SubdivisionFromString(Subdivision)!.Value,
				//EmployeePositionFromString(Position)!.Value,
				//ActiveStatusFromString(ActiveStatus)!.Value,
				//EmployeeFromString(PeoplePartner),
				//AvailibleDaysOff,
				//PhotoPath == view.defaultPhotoPath ? null : PhotoPath
			//);
			//view.database.UpdateEmployee(e);
		}
	}

	class Context
    {
		public string[] Subdivision { get; }
		public string[] ActiveStatus { get; }
		public string[] EmployeePosition { get; }
        public Row[] Employees { get; }
		public Row? SelectedRow { get; set; }
		public bool CanAddEmployees { get; }

		internal Context(LoginContext login)
		{
			using Database db = new();
			var employees = db.GetEmployees().Where(e => login.EmployeeId == e.Id || login.Position is not PMan.EmployeePosition.Employee);

            Subdivision = ChoiceFields.Subdivision;
			ActiveStatus = ChoiceFields.ActiveStatus;
			EmployeePosition = ChoiceFields.EmployeePosition;
			Employees = [..employees.Select(e => new Row(login, e, db))];
			CanAddEmployees = login.Position is PMan.EmployeePosition.HRManager or PMan.EmployeePosition.Administrator;
		}
	}

	Context context;
	LoginContext login;

	internal EmployeesView(LoginContext login)
    {
		this.login = login;
		UpdateContext();
	}

	void UpdateContext()
	{
		DataContext = context = new Context(login);
		InitializeComponent();
	}


	void AddEmployeeClick(object sender, System.Windows.RoutedEventArgs e)
	{
		if (context.CanAddEmployees)
		{
			new EditEmployeeWindow(login, null).ShowDialog();
			UpdateContext();
		}
	}

	void EditButtonClick(object sender, System.Windows.RoutedEventArgs e)
	{
		if (context.SelectedRow is Row r && r.CanEdit)
		{
			new EditEmployeeWindow(login, r.Id).ShowDialog();
			UpdateContext();
		}
    }
}
