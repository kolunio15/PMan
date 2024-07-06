using System.Collections.Generic;
using System.Windows;
namespace PMan;
using static ChoiceFields;
public partial class AddEmployeeWindow : Window
{
    public class Row(bool CanAddAdmin) 
    {
        string fullName = "";
        public string FullName { get => fullName; set => fullName = Database.ValidFullName(value) ?? throw new(); }
        public string Subdivision { get; set; } = ChoiceFields.Subdivision[0];
        string position = ChoiceFields.EmployeePosition[0];
		public string Position { 
            get => position;
            set
            {
                if (!CanAddAdmin && EmployeePositionFromString(value) == EmployeePosition.Administrator) throw new();
                position = value;
            }
        }
		public string ActiveStatus { get; set; } = ChoiceFields.ActiveStatus[0];
        public string PeoplePartner { get; set; } = "None";
	}
  
    class Context(bool CanAddAdmin, List<Employee> employees)
    {
        public string[] Subdivision { get; } = ChoiceFields.Subdivision;
        public string[] ActiveStatus { get; } = ChoiceFields.ActiveStatus;
        public string[] EmployeePosition { get; } = ChoiceFields.EmployeePosition;
        public string[] HRPeople { get; } = ChoiceFields.HRPeople(employees);
		public Row[] SingleRow { get; } = [new(CanAddAdmin)];
    }

    readonly Context context;
    internal AddEmployeeWindow(LoginContext login)
    {
        using Database db = new();
        DataContext = context = new(login.Position is EmployeePosition.Administrator, db.GetEmployees());        
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
