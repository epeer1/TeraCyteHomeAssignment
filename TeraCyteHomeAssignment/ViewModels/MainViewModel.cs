using MainUI.ViewModels;
using OpenCvSharp.WpfExtensions;

public class MainViewModel : ViewModelBase
{
	public ImageViewModel ImageViewModel { get; }
	public MainViewModel()	//Composition of view models for future features
	{
		ImageViewModel = new ImageViewModel();
	}
}