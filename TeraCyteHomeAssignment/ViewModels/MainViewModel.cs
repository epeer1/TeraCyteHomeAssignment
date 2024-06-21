using MainUI.ViewModels;
using OpenCvSharp.WpfExtensions;

public class MainViewModel : ViewModelBase
{
	public ImageViewModel ImageViewModel { get; }
	public HistogramViewModel HistogramViewModel { get; }

	public MainViewModel()
	{
		ImageViewModel = new ImageViewModel();
		HistogramViewModel = new HistogramViewModel();

		ImageViewModel.PropertyChanged += async (sender, args) =>
			{
				if (args.PropertyName == nameof(ImageViewModel.Image) ||
				    args.PropertyName == nameof(ImageViewModel.Brightness))
				{
					using (var mat = ImageViewModel.Image.ToMat())
					{
						await HistogramViewModel.UpdateHistogramAsync(mat);
					}
				}
			};
	}
}