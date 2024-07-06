using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
namespace PMan;
using static ChoiceFields;
public partial class EditProjectWindow : Window
{
	class Row : ErrorNotify
	{
		string projectManager;
		public string ProjectManager { get => projectManager; set { projectManager = value; Validate(); } }

		string projectType;
		public string ProjectType { get => projectType; set { projectType = value; Validate(); } }

		DateTime startDate;
		public DateTime StartDate { get => startDate; set { startDate = value; Validate(); } }
		DateTime? endDate;
		public DateTime? EndDate { get => endDate; set { endDate = value; Validate(); } }
		string comment;
		public string Comment { get => comment; set { comment = value; Validate(); } }
		string status;
		public string Status { get => status; set { status = value; Validate(); } }

		internal Row(Database db, long? projectManager, ProjectType projectType, DateTime startDate, DateTime? endDate, string comment, ActiveStatus status)
		{
			this.projectManager = EmployeeToString(projectManager == null ? null : db.GetEmployee(projectManager.Value));
			this.projectType = ProjectTypeToString(projectType);
			this.startDate = startDate;
			this.endDate = endDate;
			this.comment = comment;
			this.status = ActiveStatusToString(status);
			Validate();
		}

		void Validate()
		{
			ClearErrors();
			if (EmployeeFromString(ProjectManager) == null)
				SetError(nameof(ProjectManager), "Project manager must be set");
			if (StartDate > EndDate)
				EndDate = null;
		}

	}

	class Context : VmBase
	{
		readonly Row row;

		public string[] ProjectType => ChoiceFields.ProjectType;
		public string[] ActiveStatus => ChoiceFields.ActiveStatus;

		public string[] ProjectManagers { get; }
		public string[] PotentialMembers { get; set; }




		public string? selectedProjectMember;
		public string? SelectedProjectMember { 
			get => selectedProjectMember;
			set { selectedProjectMember = value; CanRemoveMember = value != null; }
		}
		public ObservableCollection<string> ProjectMembers { get; }
		string employeeToAdd = "None";
		public string EmployeeToAdd { 
			get => employeeToAdd; 
			set
			{
				employeeToAdd = value;
				UpdatePotentialMembers();
			}
		}
		bool canAddMember;
		public bool CanAddMember { get => canAddMember; set => SetNotify(ref canAddMember, value); }
		bool canRemoveMember;
		public bool CanRemoveMember { get => canRemoveMember; set => SetNotify(ref canRemoveMember, value); }

		public bool CanSave { get => !row.HasErrors; }
		public Row? SelectedRow { get; set; }
		public Row[] Rows { get; }

		internal Context(Row row, string[] projectMembers)
		{
			this.row = row;
			Rows = [row];

			ProjectMembers = [..projectMembers];

			row.ErrorsChanged += (_, _) => Notify(nameof(CanSave));
			ProjectMembers.CollectionChanged += (_, _) => UpdatePotentialMembers();

			using Database db = new();
			ProjectManagers = ["None", .. db.GetEmployees().Where(p => p.Position is EmployeePosition.ProjectManager).Select(EmployeeToString)];
			
			UpdatePotentialMembers();

		}
		internal void UpdatePotentialMembers()
		{
			using Database db = new();
			PotentialMembers = ["None", ..db.GetEmployees().Where(p => p.Position is not EmployeePosition.Administrator).Select(EmployeeToString).Where(p => !ProjectMembers.Contains(p))];

			CanAddMember = !ProjectMembers.Contains(EmployeeToAdd) && EmployeeToAdd != "None";
		}


	}

	LoginContext login;
	Context context;
	long? projectId;
	internal EditProjectWindow(LoginContext login, long? projectId)
	{
		this.login = login;
		this.projectId = projectId;

		Row r;
		string[] members;
		using Database db = new();
		if (projectId is long id)
		{
			var p = db.GetProject(id)!;
			r = new(db, p.ProjectManagerId, p.ProjectType, p.StartDate, p.EndDate, p.Comment ?? "", p.Status);
			members = [..db.GetProjectMembers(id).Select(m => EmployeeToString(db.GetEmployee(m)))];
		}
		else
		{
			r = new(db, null, ProjectType.Internal, DateTime.Today, null, "", ActiveStatus.Active);
			members = [];
		}

		DataContext = context = new(r, members);
		InitializeComponent();
	}
	void SaveButtonClick(object sender, RoutedEventArgs e)
	{
		if (context.CanSave)
		{
			Row r = context.Rows[0];
			using Database db = new();
			bool ok;
			if (projectId is long id)
			{
				ok = db.UpdateProject(
					id,
					ProjectTypeFromString(r.ProjectType)!.Value,
					r.StartDate,
					r.EndDate,
					EmployeeFromString(r.ProjectManager)!.Value,
					r.Comment,
					ActiveStatusFromString(r.Status)!.Value,
					[.. context.ProjectMembers.Select(m => EmployeeFromString(m)!.Value)]
				);
			}
			else
			{
				ok = db.AddProject(
					ProjectTypeFromString(r.ProjectType)!.Value,
					r.StartDate,
					r.EndDate,
					EmployeeFromString(r.ProjectManager)!.Value,
					r.Comment,
					ActiveStatusFromString(r.Status)!.Value,
					[..context.ProjectMembers.Select(m => EmployeeFromString(m)!.Value)]
				);
			}

			if (ok)
			{
				Close();
			}
			else
			{
				MessageBox.Show("Failed to save project.");
			}

		}
	}
	void AddMemberButtonClick(object sender, RoutedEventArgs e)
	{
		if (context.CanAddMember)
		{
			context.ProjectMembers.Add(context.EmployeeToAdd);
			context.UpdatePotentialMembers();
		}
	}
	void RemoveMemberButtonClick(object sender, RoutedEventArgs e)
	{
		if (context.CanRemoveMember && context.SelectedProjectMember is string member)
		{
			context.ProjectMembers.Remove(member);
		}
	}
}
