using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace PMan;

enum EmployeePosition 
{ 
	Employee,
	HRManager,
	ProjectManager,
	Administrator,
}
enum AbsenceReason
{
	AnnualLeave,
	SickLeave,
	MaternityLeave,
	Other
}
enum Subdivision
{
	ITDepartament,
	HRDepartament,
	LegalDepartament,
}

enum ProjectType
{
	Internal, 
	Contract
}
enum ActiveStatus 
{
	Active,
	Inactive
}
enum LeaveStatus
{
	New,
	WaitingForApproval,
	Approved,
	Rejected,
	Cancelled
}

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
	internal static readonly string[] LeaveStatus = [
		"New",
		"Waiting for approval",
		"Approved",
		"Rejected",
		"Cancelled"
	];
	internal static readonly string[] AbsenceReason = [
		"Annual leave",
		"Sick leave",
		"Maternity leave",
		"Other"
	];
	internal static readonly string[] ProjectType = [
		"Internal",
		"Contract",
	];

	internal static string SubdivisionToString(Subdivision s) => Subdivision[(int)s];
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
	internal static string LeaveStatusToString(LeaveStatus s) => LeaveStatus[(int)s];
	internal static LeaveStatus? LeaveStatusFromString(string s)
	{
		int index = Array.IndexOf(EmployeePosition, s);
		return index == -1 ? null : (LeaveStatus)index;
	}

	internal static string AbsenceReasonToString(AbsenceReason s) => AbsenceReason[(int)s];
	internal static AbsenceReason? AbsenceReasonFromString(string s)
	{
		int index = Array.IndexOf(AbsenceReason, s);
		return index == -1 ? null : (AbsenceReason)index;
	}
	internal static string ProjectTypeToString(ProjectType s) => AbsenceReason[(int)s];
	internal static ProjectType? ProjectTypeFromString(string s)
	{
		int index = Array.IndexOf(ProjectType, s);
		return index == -1 ? null : (ProjectType)index;
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


record Employee(
	long Id,
	string FullName,
	Subdivision Subdivision,
	EmployeePosition Position,
	ActiveStatus ActiveStatus,
	long? PeoplePartnerId,
	long AvailibleDaysOff,
	string? PhotoPath
);
record LeaveRequest(
	long Id,
	long Employee,
	AbsenceReason AbsenceReason,
	DateTime StartDate,
	DateTime EndDate,
	string? Comment,
	LeaveStatus Status
);

class Database : IDisposable
{
	const int initialDaysOff = 30;

	readonly SQLiteConnection connection;
	internal Database()
	{
		string dbPath = Path.Combine(AppContext.BaseDirectory, "database.sqlite3");

		connection = new($"Data Source = {dbPath}; FOREIGN KEYS = TRUE;");
		connection.Open();

		using SQLiteCommand cmd = new(connection);
		cmd.CommandText =
		"""
			CREATE TABLE IF NOT EXISTS employees(
				id INTEGER PRIMARY KEY,
				full_name TEXT NOT NULL,
				subdivision INTEGER NOT NULL,
				position INTEGER NOT NULL,
				active_status INTEGER NOT NULL,
				people_partner INTEGER,
				availible_days_off INTEGRER NOT NULL,
				photo_path TEXT,
				FOREIGN KEY(people_partner) REFERENCES employees(id)
			);
			CREATE TABLE IF NOT EXISTS leave_requests(
				id INTEGER PRIMARY KEY,
				employee INTEGER NOT NULL,
				absence_reason INTEGER NOT NULL,
				start_date INTEGER NOT NULL,
				end_date INTEGER NOT NULL,
				comment TEXT,
				status INTEGER NOT NULL,
				FOREIGN KEY (employee) REFERENCES employees(id)
			);
			CREATE TABLE IF NOT EXISTS approval_requests(
				id INTEGER PRIMARY KEY,
				approver INTEGER NOT NULL,
				leave_request INTEGER NOT NULL,
				comment TEXT,
				FOREIGN KEY (approver) REFERENCES employees(id),
				FOREIGN KEY (leave_request) REFERENCES leave_requests(id)
			);
			CREATE TABLE IF NOT EXISTS projects(
				id INTEGER PRIMARY KEY,
				project_Type INTEGER NOT NULL,
				start_date INTEGER NOT NULL,
				end_date INTEGER,
				project_manager INTEGER NOT NULL,
				comment TEXT,
				active_status INTEGER NOT NULL,
				FOREIGN KEY (project_manager) REFERENCES employees(id)
			);
			CREATE TABLE IF NOT EXISTS project_members(
				project_id INTEGER NOT NULL,
				employee_id INTEGER NOT NULL,
				FOREIGN KEY (project_id) REFERENCES projects(id),
				FOREIGN KEY (employee_id) REFERENCES employees(id)
			);
		""";
		cmd.ExecuteNonQuery();

		bool addedAdmin = AddEmployeeInternal(0, "Admin", Subdivision.ITDepartament, EmployeePosition.Administrator, ActiveStatus.Active, null);
		Console.WriteLine($"Added admin employee: {addedAdmin}");

	}


	internal static string? ValidFullName(string fullName)
	{
		string[] lines = fullName.Split(Environment.NewLine);
		if (lines.Length == 0) return null;
		fullName = lines[0].Trim();
		if (fullName.Length == 0) return null;
		return fullName;
	}


	internal bool AddEmployee(
		string fullName, 
		Subdivision subdivision, 
		EmployeePosition position, 
		ActiveStatus active, 
		long? peoplePartnerId
	) 
	{
		using SQLiteTransaction transaction = connection.BeginTransaction();
		using SQLiteCommand cmd = new(connection);

		if (ValidFullName(fullName) is string fn)
		{
			fullName = fn;
		}
		else
		{
			return false;
		}

		cmd.CommandText =
		"""
			INSERT INTO employees(
				full_name,
				subdivision,
				position,
				active_status,
				people_partner,
				availible_days_off,
				photo_path
			) VALUES (
				@fullName, 
				@subdivision, 
				@position, 
				@activeStatus, 
				@peoplePartnerId, 
				@initialDaysOff, 
				NULL
			);
		""";
		cmd.Parameters.AddWithValue("@fullName", fullName);
		cmd.Parameters.AddWithValue("@subdivision", (long)subdivision);
		cmd.Parameters.AddWithValue("@position", (long)position);
		cmd.Parameters.AddWithValue("@activeStatus", (long)active);
		cmd.Parameters.AddWithValue("@peoplePartnerId", peoplePartnerId);
		cmd.Parameters.AddWithValue("@initialDaysOff", (long)initialDaysOff);

		try
		{
			cmd.ExecuteNonQuery();
		}
		catch (Exception e)
		{
			Console.WriteLine(e);
			return false;
		}
		transaction.Commit();
		return true;
	}

	bool AddEmployeeInternal(
		long id,
		string fullName,
		Subdivision subdivision,
		EmployeePosition position,
		ActiveStatus active,
		long? peoplePartnerId
	)
	{
		using SQLiteTransaction transaction = connection.BeginTransaction();
		using SQLiteCommand cmd = new(connection);

		if (ValidFullName(fullName) is string fn)
		{
			fullName = fn;
		}
		else
		{
			return false;
		}
		


		cmd.CommandText =
		"""
			INSERT INTO employees(
				id,
				full_name,
				subdivision,
				position,
				active_status,
				people_partner,
				availible_days_off,
				photo_path
			) VALUES (
				@id, 
				@fullName, 
				@subdivision, 
				@position, 
				@activeStatus, 
				@peoplePartnerId, 
				@initialDaysOff, 
				NULL
			);
		""";
		cmd.Parameters.AddWithValue("@id", id);
		cmd.Parameters.AddWithValue("@fullName", fullName);
		cmd.Parameters.AddWithValue("@subdivision", (long)subdivision);
		cmd.Parameters.AddWithValue("@position", (long)position);
		cmd.Parameters.AddWithValue("@activeStatus", (long)active);
		cmd.Parameters.AddWithValue("@peoplePartnerId", peoplePartnerId);
		cmd.Parameters.AddWithValue("@initialDaysOff", (long)initialDaysOff);

		try { 
			cmd.ExecuteNonQuery();
		} catch (Exception e) {
			Console.WriteLine(e);
			return false;
		}
		transaction.Commit();
		return true;
	}
	internal bool UpdateEmployee(Employee e)
	{
		using SQLiteTransaction transaction = connection.BeginTransaction();
		using SQLiteCommand cmd = new(connection);

		string fullName;
		if (ValidFullName(e.FullName) is string fn)
		{
			fullName = fn;
		}
		else
		{
			return false;
		}

		cmd.CommandText =
		"""
			UPDATE employees 
			SET
				full_name = @fullName,
				subdivision = @subdivision,
				position = @position,
				active_status = @activeStatus,
				people_partner = @peoplePartnerId,
				photo_path = @photoPath
			WHERE id = @id;
		""";
		cmd.Parameters.AddWithValue("@id", e.Id);
		cmd.Parameters.AddWithValue("@fullName", fullName);
		cmd.Parameters.AddWithValue("@subdivision", (long)e.Subdivision);
		cmd.Parameters.AddWithValue("@position", (long)e.Position);
		cmd.Parameters.AddWithValue("@activeStatus", (long)e.ActiveStatus);
		cmd.Parameters.AddWithValue("@peoplePartnerId", e.PeoplePartnerId);
		cmd.Parameters.AddWithValue("@photoPath", e.PhotoPath);

		try
		{
			cmd.ExecuteNonQuery();
		}
		catch (Exception exception)
		{
			Console.WriteLine(exception);
			return false;
		}
		transaction.Commit();
		return true;
	}


	

	internal EmployeePosition? GetEmployeePosition(long id) 
	{
		using SQLiteCommand cmd = new(connection);
		cmd.CommandText = "SELECT position FROM employees WHERE id = @id";
		cmd.Parameters.AddWithValue("@id", id);
		return (EmployeePosition?)(long?)cmd.ExecuteScalar();
	}
	internal Employee? GetEmployee(long id)
	{
		using SQLiteCommand cmd = new(connection);
		cmd.CommandText = "SELECT * FROM employees WHERE id = @id";
		cmd.Parameters.AddWithValue("@id", id);

		using SQLiteDataReader r = cmd.ExecuteReader();
		if (r.Read())
		{
			return ReadEmployee(r);
		}
		else
		{
			return null;
		}
	}
	internal List<Employee> GetEmployees()
	{
		using SQLiteCommand cmd = new(connection);
		cmd.CommandText = "SELECT * FROM employees";

		using SQLiteDataReader r = cmd.ExecuteReader();
		List<Employee> list = [];
		while (r.Read())
		{
			list.Add(ReadEmployee(r));
		}
		return list;
	}

	static Employee ReadEmployee(SQLiteDataReader r)
	{
		return new(
			Id: r.GetInt64(0),
			FullName: r.GetString(1),
			Subdivision: (Subdivision)r.GetInt64(2),
			Position: (EmployeePosition)r.GetInt64(3),
			ActiveStatus: (ActiveStatus)r.GetInt64(4),
			PeoplePartnerId: r.IsDBNull(5) ? null : r.GetInt64(5),
			AvailibleDaysOff: r.GetInt64(6),
			PhotoPath: r.IsDBNull(7) ? null : r.GetString(6)
		);
	}

	internal List<LeaveRequest> GetLeaveRequests()
	{
		using SQLiteCommand cmd = new(connection);
		cmd.CommandText =
		"""
			SELECT * FROM leave_requests;
		""";

		using SQLiteDataReader r = cmd.ExecuteReader();
		List<LeaveRequest> list = [];
		while (r.Read())
		{
			list.Add(ReadLeaveRequest(r));
		}
		return list;
	}
	internal LeaveRequest? GetLeaveRequest(long id)
	{
		using SQLiteCommand cmd = new(connection);
		cmd.CommandText =
		"""
			SELECT * FROM leave_requests WHERE id = @id;
		""";
		cmd.Parameters.AddWithValue("@id", id);

		using SQLiteDataReader r = cmd.ExecuteReader();
		
		if (r.Read())
		{
			return ReadLeaveRequest(r);
		}
		else
		{
			return null;
		}
	}

	static LeaveRequest ReadLeaveRequest(SQLiteDataReader r)
	{
		return new(
			Id: r.GetInt64(0),
			Employee: r.GetInt64(1),
			AbsenceReason: (AbsenceReason)r.GetInt64(2),
			StartDate: r.GetDateTime(3),
			EndDate: r.GetDateTime(4),
			Comment: r.IsDBNull(5) ? null : r.GetString(5),
			Status: (LeaveStatus)r.GetInt64(6)
		);
	}
	internal bool AddLeaveRequest(
		long creatingEmployeeId,
		AbsenceReason absenceReason,
		DateTime startDate,
		DateTime endDate,
		string comment
	)
	{
		using SQLiteTransaction transaction = connection.BeginTransaction();
		using SQLiteCommand cmd = new(connection);

		string? finalComment = comment.Trim();
		finalComment = finalComment.Length > 0 ? finalComment : null;

		cmd.CommandText =
		"""
			INSERT INTO leave_requests(
				employee,
				absence_reason,
				start_date,
				end_date,
				comment,
				status
			) VALUES (
				@creatingEmployeeId,
				@absenceReason,
				@startDate,
				@endDate,
				@comment,
				@status
			)
		""";
		cmd.Parameters.AddWithValue("@creatingEmployeeId", creatingEmployeeId);
		cmd.Parameters.AddWithValue("@absenceReason", (long)absenceReason);
		cmd.Parameters.AddWithValue("@startDate", startDate);
		cmd.Parameters.AddWithValue("@endDate", endDate);
		cmd.Parameters.AddWithValue("@comment", finalComment);
		cmd.Parameters.AddWithValue("@status", (long)LeaveStatus.New);
		try
		{
			cmd.ExecuteNonQuery();
		}
		catch (Exception exception)
		{
			Console.WriteLine(exception);
			return false;
		}
		transaction.Commit();
		return true;
	}

	

	internal bool UpdateLeaveRequest(
		long requestId,
		AbsenceReason absenceReason,
		DateTime startDate,
		DateTime endDate,
		string comment
	)
	{
		using SQLiteTransaction transaction = connection.BeginTransaction();
		using SQLiteCommand cmd = new(connection);


		string? finalComment = comment.Trim();
		finalComment = finalComment.Length > 0 ? finalComment : null;


		cmd.CommandText =
		"""
			UPDATE leave_requests
			SET
				absence_reason = @absenceReason,
				start_date = @startDate,
				end_date = @endDate,
				comment = @comment
			WHERE 
				id = @requestId
		""";
		cmd.Parameters.AddWithValue("@requestId", requestId);
		cmd.Parameters.AddWithValue("@absenceReason", absenceReason);
		cmd.Parameters.AddWithValue("@startDate", startDate);
		cmd.Parameters.AddWithValue("@endDate", endDate);
		cmd.Parameters.AddWithValue("@comment", finalComment);
		try
		{
			cmd.ExecuteNonQuery();
		}
		catch (Exception exception)
		{
			Console.WriteLine(exception);
			return false;
		}
		transaction.Commit();
		return true;
	}

	

	internal bool SubmitLeaveRequest(long requestId)
	{
		using SQLiteTransaction transaction = connection.BeginTransaction();
		using SQLiteCommand cmd = new(connection);

		cmd.CommandText =
		"""
			SELECT 
				leave_requests.employee, leave_requests.status, employees.people_partner
			FROM leave_requests 
			LEFT JOIN employees
			ON leave_requests.employee = employees.id
			WHERE leave_requests.id = @requestId;
		""";
		cmd.Parameters.AddWithValue("@requestId", requestId);
		using SQLiteDataReader r = cmd.ExecuteReader();
		if (!r.Read()) return false;
		long employeeId = r.GetInt64(0);
		LeaveStatus status = (LeaveStatus)r.GetInt64(1);
		long? peoplePartner = r.IsDBNull(2) ? null : r.GetInt64(2);
		r.Close();


		if (status is not LeaveStatus.New) return false;
		if (peoplePartner == null) return false;

		cmd.CommandText =
		"""
			INSERT INTO approval_requests(
				approver,
				leave_request,
				comment
			) VALUES (
				@peoplePartner,
				@requestId,
				NULL
			);
			UPDATE leave_requests
			SET status = @newStatus
			WHERE id = @requestId;
		""";
		cmd.Parameters.AddWithValue("@peoplePartner", peoplePartner.Value);
		cmd.Parameters.AddWithValue("@requestId", requestId);
		cmd.Parameters.AddWithValue("@newStatus", (long)LeaveStatus.WaitingForApproval);
		try
		{
			cmd.ExecuteNonQuery();
		}
		catch (Exception e)
		{
			Console.WriteLine(e);
			return false;
		}

		transaction.Commit();
		return true;
	}


	internal bool CancelLeaveRequest(long cancelingUserId, long requestId)
	{
		using SQLiteTransaction transaction = connection.BeginTransaction();
		using SQLiteCommand cmd = new(connection);

		cmd.CommandText =
		"""
			SELECT 
				leave_requests.employee, leave_requests.status
			FROM leave_requests 
			LEFT JOIN employees
			ON leave_requests.employee = employees.id
			WHERE leave_requests.id = @requestId;
		""";
		cmd.Parameters.AddWithValue("@requestId", requestId);
		using SQLiteDataReader r = cmd.ExecuteReader();
		if (!r.Read()) return false;
		long employeeId = r.GetInt64(0);
		LeaveStatus status = (LeaveStatus)r.GetInt64(1);
		r.Close();

		if (employeeId != cancelingUserId) return false;
		if (status is LeaveStatus.Cancelled or LeaveStatus.Rejected) return false;

		cmd.CommandText =
		"""
			UPDATE leave_requests
			SET status = @newStatus
			WHERE id = @requestId;
		""";
		cmd.Parameters.AddWithValue("@requestId", requestId);
		cmd.Parameters.AddWithValue("@newStatus", (long)LeaveStatus.Cancelled);
		try
		{
			cmd.ExecuteNonQuery();
		}
		catch (Exception e)
		{
			Console.WriteLine(e);
			return false;
		}

		transaction.Commit();
		CalculateDaysOffForEmployee(employeeId);
		
		return true;
	}
	void CalculateDaysOffForEmployee(long employeeId)
	{
		// TODO
	}

	public void Dispose()
	{
		connection.Dispose();
	}
}
