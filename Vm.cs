using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Permissions;
using System.Windows.Input;

namespace PMan;
class VmBase : INotifyPropertyChanged, INotifyDataErrorInfo
{
	public event PropertyChangedEventHandler? PropertyChanged;
	public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;
	
	Dictionary<string, string> propertyErrors = [];

	protected void SetNotify<T>(ref T property, T value, [CallerMemberName] string propertyName = "")
	{
		if (EqualityComparer<T>.Default.Equals(property, value)) return;
		property = value;
		PropertyChanged?.Invoke(this, new(propertyName));
	}
	protected void Notify(string propertyName)
	{
		PropertyChanged?.Invoke(this, new(propertyName));
	}

	public bool HasErrors => propertyErrors.Count > 0;
	public IEnumerable GetErrors(string? propertyName)
	{
		propertyErrors.TryGetValue(propertyName!, out string? error);
		return error == null ? [] : new List<string>([error]);
	}
	protected void SetError(string? error, [CallerMemberName] string property = "") 
	{ 
		propertyErrors.Remove(property ?? "");
		if (error != null)
		{
			propertyErrors.Add(property, error);
		}
	}

	

}
class RelayCommand<T>(Action<T> execute, Func<T, bool>? canExecute = null) : ICommand
{
	public event EventHandler? CanExecuteChanged
	{ 
		add => CommandManager.RequerySuggested += value;
		remove => CommandManager.RequerySuggested -= value;
	}
	public void Execute(object? parameter)
	{
		execute((T)parameter!);
	}
	public bool CanExecute(object? parameter)
	{
		return canExecute == null || canExecute((T)parameter!);
	}
}
