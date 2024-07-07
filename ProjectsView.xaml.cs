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
	class Context : VmBase
	{
		public bool CanAdd { get; }  
		public Row? SelectedRow { get; set; }


		public string idSearch = "";
		public string IdSearch { get => idSearch; set { SetNotify(ref idSearch, value); UpdateRows(); } }

		Row[] rows;
		public Row[] Rows { get => rows; set => SetNotify(ref rows, value); }

		LoginContext login;

		internal Context(LoginContext login)
		{
			CanAdd = login.Position is EmployeePosition.ProjectManager or EmployeePosition.Administrator;
			this.login = login;
			UpdateRows();
		}
		void UpdateRows()
		{
			using Database db = new();
			Rows = [
				..db.GetProjects()
				.Where(p => 
					login.Position is EmployeePosition.ProjectManager or EmployeePosition.Administrator
					|| db.GetProjectMembers(p.Id).Contains(login.EmployeeId)).Select(p => new Row(p, login, db))
				.Where(p => {
					if (long.TryParse(IdSearch, out long id))
					{
						return p.Id == id;
					}
					else
					{
						return true;
					}
				})
			];
		}
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
		DataContext = context = new(login);
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
