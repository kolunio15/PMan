using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace PMan;
using static ChoiceFields;

public class PathToImageConverter : IValueConverter
{
	readonly static public PathToImageConverter Instance = new();

	public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value is string path)
		{
			
			BitmapImage? image = new();
			using FileStream stream = File.OpenRead(path);

			image.BeginInit();
			image.StreamSource = stream;
			image.CacheOption = BitmapCacheOption.OnLoad;
			image.EndInit();

			return image;
		}
		else
		{
			return null;
		}
	}

	public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return null;
	}
}

public partial class EmployeesView : UserControl
{
	internal static readonly string DefaultPhotoPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "./assets/default.png"));


	internal class Row(LoginContext login, Employee e, Database db) : VmBase
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
		string photoPath = e.PhotoPath ?? DefaultPhotoPath;
		public string PhotoPath { get => photoPath; set => SetNotify(ref photoPath, value); }

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
