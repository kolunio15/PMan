using System.Collections.Generic;
using System.Windows;
namespace PMan;
using static ChoiceFields;
public partial class EditEmployeeWindow : Window
{
    class Row : ErrorNotify
    {
        string fullName;
        public string FullName { get => fullName; set { fullName = value; Validate(); } }
        string subdivision;
        public string Subdivision { get => subdivision; set { subdivision = value; Validate(); } }
        string position;
		public string Position { get => position; set { position = value; Validate(); } }
        string activeStatus;
		public string ActiveStatus { get => activeStatus; set { activeStatus = value; Validate(); } }
        string peoplePartner;
		public string PeoplePartner { get => peoplePartner; set { peoplePartner = value; Validate(); } }


        EmployeePosition originalPosition;
        bool canChangePosition;
        
        internal Row(string fullName, Subdivision subdivision, EmployeePosition position, ActiveStatus activeStatus, long? peoplePartner, bool canChangePosition)
        {
            using Database db = new();
            this.fullName = fullName;
            this.subdivision = SubdivisionToString(subdivision);
            this.position = EmployeePositionToString(position);
            this.activeStatus = ActiveStatusToString(activeStatus);
            this.peoplePartner = EmployeeToString(peoplePartner == null ? null : db.GetEmployee(peoplePartner.Value));

            this.originalPosition = position;
			this.canChangePosition = canChangePosition;
            Validate();
        }
		void Validate()
        {
            ClearErrors();
            if (Database.ValidFullName(FullName) == null)
                SetError(nameof(FullName), "Name must be present");
            if (!canChangePosition && originalPosition != EmployeePositionFromString(Position))
				SetError(nameof(Position), "Only admin can change position");
			if (EmployeeFromString(PeoplePartner) == null)
				SetError(nameof(PeoplePartner), "People partner must not be null");
		}

	}
  
    class Context : VmBase
    {
        public bool CanSave { get => !SingleRow[0].HasErrors; }
        public string[] Subdivision { get; } = ChoiceFields.Subdivision;
        public string[] ActiveStatus { get; } = ChoiceFields.ActiveStatus;
        public string[] EmployeePosition { get; } = ChoiceFields.EmployeePosition;
        public string[] HRPeople { get; } 
		public Row[] SingleRow { get; }

        internal Context(Row row, List<Employee> employees)
        {
            HRPeople = ChoiceFields.HRPeople(employees);
            SingleRow = [row];
            row.ErrorsChanged += (_, _) => Notify(nameof(CanSave));
		}
    }

    readonly Context context;
    readonly long? employeeId;
    internal EditEmployeeWindow(LoginContext login, long? employeeId)
    {
        this.employeeId = employeeId;
     
        bool canChangePosition = login.Position is EmployeePosition.Administrator && (employeeId == null || employeeId.Value != login.EmployeeId);
		
        using Database db = new();
		Row r;
        if (employeeId is long id)
        {
			Employee e = db.GetEmployee(id)!;
            r = new(e.FullName, e.Subdivision, e.Position, e.ActiveStatus, e.PeoplePartnerId, canChangePosition);
        }
        else
        {
			r = new("", Subdivision.ITDepartament, EmployeePosition.Employee, ActiveStatus.Active, null, canChangePosition);
		}

		DataContext = context = new(r, db.GetEmployees());        
        InitializeComponent();
    }

	void SaveButtonClick(object sender, RoutedEventArgs e)
	{
        using Database db = new();
        Row r = context.SingleRow[0];

        if (context.CanSave)
        {
			bool ok;
			if (employeeId is long id)
			{
				ok = db.UpdateEmployee(
					id,
					r.FullName,
					SubdivisionFromString(r.Subdivision)!.Value,
					EmployeePositionFromString(r.Position)!.Value,
					ActiveStatusFromString(r.ActiveStatus)!.Value,
					EmployeeFromString(r.PeoplePartner)!.Value
				);
			}
			else
			{
				ok = db.AddEmployee(
					r.FullName,
					SubdivisionFromString(r.Subdivision)!.Value,
					EmployeePositionFromString(r.Position)!.Value,
					ActiveStatusFromString(r.ActiveStatus)!.Value,
					EmployeeFromString(r.PeoplePartner)!.Value
				);
			}


			if (ok)
			{
				Close();
			}
			else
			{
				MessageBox.Show("Failed to save employee.");
			}
		}
	}
}
