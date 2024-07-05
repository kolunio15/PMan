using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Windows.Ink;

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
	// Only annual leave affects days off
	AnnualLeave,
	SickLeave,
	MaternityLeave,
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
				FOREIGN KEY (employee) REFERENCES employees(id)
			);
			CREATE TABLE IF NOT EXISTS approval_requests(
				id INTEGER PRIMARY KEY,
				approver INTEGER NOT NULL,
				leave_request INTEGER NOT NULL,
				status INTEGER NOT NULL,
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


	string? FullNameFixed(string fullName)
	{
		string[] lines = fullName.Split(Environment.NewLine);
		if (lines.Length == 0) return null;
		fullName = lines[0].Trim();
		if (fullName.Length == 0) return null;
		return fullName;
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

		if (FullNameFixed(fullName) is string fn)
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
		if (FullNameFixed(e.FullName) is string fn)
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


	



	public void Dispose()
	{
		connection.Dispose();
	}
}
