using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Threading;

namespace MainUI.ViewModels
{
	public class ViewModelBase : INotifyPropertyChanged
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
	}
}