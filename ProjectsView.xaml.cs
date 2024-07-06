using System;
using System.Linq;
using System.Windows.Controls;
namespace PMan;
using static ChoiceFields;
public partial class ProjectsView : UserControl
{

	class Row(Project p, LoginContext login, Database db)
	{
		public bool CanEdit { get; } = login.Position is EmployeePosition.ProjectManager or EmployeePosition.Administrator;
		public string[] ProjectMembers { get; } = [..db.GetProjectMembers(p.Id).Select(m => EmployeeToString(db.GetEmployee(m)))];

		public long Id { get; } = p.Id;
		public string ProjectManager { get; } = EmployeeToString(db.GetEmployee(p.ProjectManagerId));
		public string ProjectType { get; } = ProjectTypeToString(p.ProjectType);
		public DateTime StartDate { get; } = p.StartDate;
		public DateTime? EndDate { get; } = p.EndDate;
		public string Comment { get; } = p.Comment ?? "";
		public string Status { get; } = ActiveStatusToString(p.Status); 


	}
	class Context(LoginContext login, Database db)
	{
		public bool CanAdd { get; } = login.Position is EmployeePosition.ProjectManager or EmployeePosition.Administrator;
		public Row? SelectedRow { get; set; }


		

		public Row[] Rows { get; } = [
			..db.GetProjects()
			.Where(p => login.Position is EmployeePosition.ProjectManager or EmployeePosition.Administrator
			|| db.GetProjectMembers(p.Id).Contains(login.EmployeeId)).Select(p => new Row(p, login, db))
		];
	}

	LoginContext login;
	Context context;
	internal ProjectsView(LoginContext login)
	{
		this.login = login;
		UpdateContext();
		InitializeComponent();
	}
	void UpdateContext()
	{
		using Database db = new();
		DataContext = context = new(login, db);
	}


	void AddButtonClick(object sender, System.Windows.RoutedEventArgs e)
	{
		if (context.CanAdd)
		{
			new EditProjectWindow(login, null).ShowDialog();
			UpdateContext();
		}
	}
	void EditButtonClick(object sender, System.Windows.RoutedEventArgs e)
	{
		if (context.SelectedRow is Row r && r.CanEdit)
		{
			new EditProjectWindow(login, context.SelectedRow.Id).ShowDialog();
			UpdateContext();
		}
	}
}
