using MainUI.ViewModels;
using OpenCvSharp.WpfExtensions;

public class MainViewModel : ViewModelBase
{
	public ImageViewModel ImageViewModel { get; }
	public MainViewModel()
	{
		ImageViewModel = new ImageViewModel();

	}
}