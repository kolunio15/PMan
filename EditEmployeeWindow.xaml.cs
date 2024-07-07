using System.Collections.Generic;
using System.Windows;
namespace PMan;
using static ChoiceFields;
public partial class EditEmployeeWindow : Window
{
    class Row : ErrorNotify
    {
        string fullName = "";
        public string FullName { get => fullName; set { fullName = value; Validate(); } }
        public string Subdivision { get; set; } = ChoiceFields.Subdivision[0];
        string position = ChoiceFields.EmployeePosition[0];
		public string Position { get => position; set { position = value; Validate(); } }
        string activeStatus = ChoiceFields.ActiveStatus[0];
		public string ActiveStatus { get => activeStatus; set { activeStatus = value; Validate(); } }
        string peoplePartner = "None";
		public string PeoplePartner { get => peoplePartner; set { peoplePartner = value; Validate(); } }

        bool canAddAdmin;
        
        internal Row(bool canAddAdmin)
        {
            this.canAddAdmin = canAddAdmin;
            Validate();
        }

        
		void Validate()
        {
            ClearErrors();
            if (Database.ValidFullName(FullName) == null)
                SetError(nameof(FullName), "Single line non empty");

            if (!canAddAdmin && EmployeePositionFromString(Position) == EmployeePosition.Administrator)
                SetError(nameof(Position), "Not enough permissions to add administrator");

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
    internal EditEmployeeWindow(LoginContext login)
    {
        using Database db = new();

        bool canAddAdmin = login.Position is EmployeePosition.Administrator;
        Row r = new(canAddAdmin);

		DataContext = context = new(r, db.GetEmployees());        
        InitializeComponent();
    }

	void SaveButtonClick(object sender, RoutedEventArgs e)
	{
        using Database db = new();
        Row r = context.SingleRow[0];
        bool ok = db.AddEmployee(
            r.FullName,
			SubdivisionFromString(r.Subdivision)!.Value, 
            EmployeePositionFromString(r.Position)!.Value, 
            ActiveStatusFromString(r.ActiveStatus)!.Value, 
            EmployeeFromString(r.PeoplePartner)
        );
        if (ok)
        {
            Close();
        }
        else
        {
            MessageBox.Show("Failed to add an employee.");
        }
	}
}
