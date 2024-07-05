using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Controls;

namespace PMan;
using static ChoiceFields;

static class ChoiceFields
{
	internal static readonly string[] Subdivision = [
		"IT Departament",
		"HR Departament",
		"Legal Departament",
	];
	internal static readonly string[] ActiveStatus = [
		"Active",
		"Inactive",
	];
	internal static readonly string[] EmployeePosition = [
		"Employee",
		"HR Manager",
		"Project Manager",
		"Administrator",
	];
	internal static string SubdivisionToString(Subdivision s) => Subdivision[(int) s];
	internal static Subdivision? SubdivisionFromString(string s)
	{
		int index = Array.IndexOf(Subdivision, s);
		return index == -1 ? null : (Subdivision)index;
	}
	internal static string ActiveStatusToString(ActiveStatus s) => ActiveStatus[(int)s];
	internal static ActiveStatus? ActiveStatusFromString(string s)
	{
		int index = Array.IndexOf(ActiveStatus, s);
		return index == -1 ? null : (ActiveStatus)index;
	}
	internal static string EmployeePositionToString(EmployeePosition s) => EmployeePosition[(int)s];
	internal static EmployeePosition? EmployeePositionFromString(string s)
	{
		int index = Array.IndexOf(EmployeePosition, s);
		return index == -1 ? null : (EmployeePosition)index;
	}


	internal static string EmployeeToString(Employee? e) =>
		e == null ? "None" : $"({e.Id}) {e.FullName}";
	internal static long? EmployeeFromString(string e)
	{
		if (e == "None") return null;
		int start = e.IndexOf('(');
		int end = e.IndexOf(')');
		Debug.Assert(start >= 0 && end > 0);

		return long.Parse(e[(start + 1)..end]);
	}

	internal static string[] HRPeople(List<Employee> employees) => [
		"None", 
		..employees
		.Where(e => e.Position is PMan.EmployeePosition.HRManager)
		.Select(EmployeeToString)
	];

}



public partial class EmployeesView : UserControl
{

	readonly string defaultPhotoPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "./assets/default.png"));

	public class Row
	{
		EmployeesView view;

		bool CannotModify() =>
			!(view.login.Position == EmployeePosition.Administrator
			|| view.login.Position is EmployeePosition.HRManager && EmployeePositionFromString(position) != EmployeePosition.Administrator);

		public long Id { get; }
		string fullName;
		public string FullName {
			get => fullName;
			set
			{
				if (CannotModify()) throw new();
				fullName = Database.ValidFullName(value) ?? throw new();
				UpdateDatabase();
			}
		}
		string subdivision;
		public string Subdivision {
			get => subdivision;
			set
			{
				if (CannotModify()) throw new();
				subdivision = value;
				UpdateDatabase();
			}
		}
		string position;
		public string Position {
			get => position;
			set
			{
				if (CannotModify() || view.login.EmployeeId == Id) throw new();
				position = value;
				UpdateDatabase();
			}
		}
		string activeStatus;
		public string ActiveStatus {
			get => activeStatus;
			set
			{
				if (CannotModify()) throw new();
				activeStatus = value;
				UpdateDatabase();
			}
		}
		string peoplePartner;
		public string PeoplePartner {
			get => peoplePartner;
			set
			{
				if (CannotModify() || (EmployeeFromString(PeoplePartner) is long id && id == Id)) throw new();
				peoplePartner = value;
				UpdateDatabase();
			}
		}
		public long AvailibleDaysOff { get; }
		public string PhotoPath { get; }
		void UpdateDatabase()
		{
			Employee e = new(
				Id,
				fullName,
				SubdivisionFromString(Subdivision)!.Value,
				EmployeePositionFromString(Position)!.Value,
				ActiveStatusFromString(ActiveStatus)!.Value,
				EmployeeFromString(PeoplePartner),
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
			subdivision = SubdivisionToString(e.Subdivision);
			position = EmployeePositionToString(e.Position);
			activeStatus = ActiveStatusToString(e.ActiveStatus);
			peoplePartner = EmployeeToString(
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
		public bool CanAddEmployees { get; }

		internal Context(bool readOnly, List<Employee> employees, EmployeesView view)
		{
			ReadOnly = readOnly;
            Subdivision = ChoiceFields.Subdivision;
			ActiveStatus = ChoiceFields.ActiveStatus;
			EmployeePosition = ChoiceFields.EmployeePosition;
			HRPeople = ChoiceFields.HRPeople(employees);
			Employees = [..employees.Select(e => new Row(e, view))];
			CanAddEmployees = view.login.Position is PMan.EmployeePosition.HRManager or PMan.EmployeePosition.Administrator;
		}
	}

	Database database = new();
	Context context;
	LoginContext login;

	internal EmployeesView(LoginContext login)
    {
		this.login = login;
		UpdateContext();
	}

	void UpdateContext()
	{
		bool readOnly = login.Position is not PMan.EmployeePosition.HRManager and not PMan.EmployeePosition.Administrator;
		DataContext = context = new(
			readOnly,
			 login.Position == PMan.EmployeePosition.Employee
				? [database.GetEmployee(login.EmployeeId)]
				: database.GetEmployees(),
			this
		);
		InitializeComponent();
	}


	void AddEmployeeClick(object sender, System.Windows.RoutedEventArgs e)
	{
		if (context.CanAddEmployees)
		{
			AddEmployeeWindow window = new(login);
			window.ShowDialog();
			UpdateContext();
		}
	}
}
