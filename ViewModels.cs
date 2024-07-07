using System;

namespace PMan;

record struct LoginContext(long EmployeeId, EmployeePosition Position);

class LoginVm : VmBase, IDisposable
{
	long? parsedEmployeeId = null;
	string employeeId = "";
	public string EmployeeId { 
		get => employeeId;
		set
		{
			bool ok = long.TryParse(value, out long id);
			if (id < 0) ok = false;

			parsedEmployeeId = ok ? id : null;
			SetNotify(ref employeeId, value);
			SetError(ok ? null : "Expected positive integer");
		}
	}

	readonly Database database = new();
	internal LoginVm() {}

	internal bool IsInputValid() => parsedEmployeeId != null;
	internal LoginContext? Login() 
	{
		if (parsedEmployeeId is long id && database.GetEmployeePosition(id) is EmployeePosition pos)
		{
			return new(id, pos);
		}
		else
		{
			return null;
		}
	}

	public void Dispose()
	{
		database.Dispose();
	}
}

class MainWindowVm : VmBase
{
	static string EmployeePositionString(EmployeePosition p) => p switch
	{
		EmployeePosition.Employee => "Employee",
		EmployeePosition.HRManager => "HR Manager",
		EmployeePosition.ProjectManager => "Project Manager",
		EmployeePosition.Administrator => "Administrator",
		_ => throw new NotImplementedException()
	};
	
	public string LoggedInEmployeeString { get; private set; }
	
	readonly Database database = new();
	internal MainWindowVm(LoginContext context) 
	{
		Employee e = database.GetEmployee(context.EmployeeId)!;

		LoggedInEmployeeString = $"Logged in as {e.FullName} ({context.EmployeeId}) ({EmployeePositionString(context.Position)})";
	}

	public void Dispose()
	{
		database.Dispose();
	}
}