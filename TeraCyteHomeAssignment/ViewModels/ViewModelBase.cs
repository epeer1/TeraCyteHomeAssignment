using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Threading;

namespace MainUI.ViewModels
{
	public class ViewModelBase : INotifyPropertyChanged	//ViewModelBase class to wrap the property change events ensure called from UI main thread
	{
		public event PropertyChangedEventHandler PropertyChanged;

		protected void OnPropertyChanged(string propertyName)
		{
			if (Application.Current.Dispatcher.CheckAccess())
			{
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
			}
			else
			{
				Application.Current.Dispatcher.Invoke(() =>
					PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName)));
			}
		}

		protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
		{
			if (EqualityComparer<T>.Default.Equals(field, value)) return false;
			field = value;
			OnPropertyChanged(propertyName);
			return true;
		}
	}
}