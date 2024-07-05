using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Controls;

namespace PMan;
public partial class EmployeesView : UserControl
{
	static readonly string[] Subdivision = [
	    "IT Departament",
	    "HR Departament",
	    "Legal Departament",
    ];
	static readonly string[] ActiveStatus = [
		"Active",
		"Inactive",
	];
	static readonly string[] EmployeePosition = [
		"Employee",
		"HR Manager",
		"Project Manager",
		"Administrator",
	];

	string defaultPhotoPath = Path.GetFullPath("./assets/default.png");

	static string SelectedEmployeeToString(Employee? e) => 
		e == null ? "None" : $"({e.Id}) {e.FullName}";
	static long? SelectedEmployeeStringToId(string e) 
	{
		if (e == "None") return null;
		int start = e.IndexOf('(');
		int end = e.IndexOf(')');
		Debug.Assert(start > 0 && end > 0);

		return long.Parse(e[start..end]);
	}
	public class Row
	{
		EmployeesView view;
		public long Id { get; }
		string fullName;
		public string FullName {
			get => fullName;
			set
			{
				if (view.context.Position is PMan.EmployeePosition.HRManager or PMan.EmployeePosition.Administrator)
				{
					fullName = value;
					UpdateDatabase();
				}
			}
		}
		string subdivision;
		public string Subdivision {
			get => subdivision;
			set
			{
				if (view.context.Position is PMan.EmployeePosition.HRManager or PMan.EmployeePosition.Administrator)
				{
					subdivision = value;
					UpdateDatabase();
				}
			}
		}
		string position;
		public string Position {
			get => position;
			set
			{
				if (view.context.EmployeeId != Id && view.context.Position is PMan.EmployeePosition.HRManager or PMan.EmployeePosition.Administrator)
				{
					position = value;
					UpdateDatabase();
				}
			}
		}
		string activeStatus;
		public string ActiveStatus {
			get => activeStatus;
			set
			{
				if (view.context.Position is PMan.EmployeePosition.HRManager or PMan.EmployeePosition.Administrator)
				{
					activeStatus = value;
					UpdateDatabase();
				}
			}
		}
		string peoplePartner;
		public string PeoplePartner {
			get => peoplePartner;
			set
			{
				if (view.context.Position is PMan.EmployeePosition.HRManager or PMan.EmployeePosition.Administrator)
				{
					if (SelectedEmployeeStringToId(PeoplePartner) is long id && id != this.Id)
					{
						peoplePartner = value;
						UpdateDatabase();
					}
				}
			}
		}
		public long AvailibleDaysOff { get; }
		public string PhotoPath { get; }
		void UpdateDatabase()
		{
			Employee e = new(
				Id,
				fullName,
				(Subdivision)Array.IndexOf(EmployeesView.Subdivision, subdivision),
				(EmployeePosition)Array.IndexOf(EmployeesView.EmployeePosition, Position),
				(ActiveStatus)Array.IndexOf(EmployeesView.ActiveStatus, ActiveStatus),
				SelectedEmployeeStringToId(PeoplePartner),
				AvailibleDaysOff,
				PhotoPath == view.defaultPhotoPath ? null : PhotoPath
			);
			view.database.UpdateEmployee(e);
		}

		internal Row(Employee e, EmployeesView view)
		{
			this.view = view;
			Id = e.Id;
			fullName = e.FullName;
			subdivision = EmployeesView.Subdivision[(int)e.Subdivision];
			position = EmployeesView.EmployeePosition[(int)e.Position];
			activeStatus = EmployeesView.ActiveStatus[(int)e.ActiveStatus];
			peoplePartner = SelectedEmployeeToString(
				e.PeoplePartnerId == null ? null : view.database.GetEmployee(e.PeoplePartnerId.Value));
			AvailibleDaysOff = e.AvailibleDaysOff;
			PhotoPath = e.PhotoPath ?? view.defaultPhotoPath;
		}
	}

	public class Context
    {
        public bool ReadOnly { get; }
		public string[] Subdivision { get; }
		public string[] ActiveStatus { get; }
		public string[] EmployeePosition { get; }
		public string[] HRPeople { get; }
        public Row[] Employees { get; }

		internal Context(bool readOnly, List<Employee> employees, EmployeesView view)
		{
			ReadOnly = readOnly;
            Subdivision = EmployeesView.Subdivision;
			ActiveStatus = EmployeesView.ActiveStatus;
			EmployeePosition = EmployeesView.EmployeePosition;
			HRPeople = [.. employees
				.Where(e => e.Position is PMan.EmployeePosition.HRManager)
				.Select(SelectedEmployeeToString), "None"];
			Employees = [..employees.Select(e => new Row(e, view))];
		}
	}

	Database database = new();
	LoginContext context;

	internal EmployeesView(LoginContext context)
    {
		this.context = context;
      
		bool readOnly = context.Position is not PMan.EmployeePosition.HRManager and not PMan.EmployeePosition.Administrator;

		DataContext = new Context(
			readOnly,
			 context.Position == PMan.EmployeePosition.Employee 
                ? [database.GetEmployee(context.EmployeeId)] 
                : database.GetEmployees(),
			this
        );
		InitializeComponent();
	}
}
