using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using OpenCvSharp;
using Tera.HistogramModel;

namespace MainUI.ViewModels
{
	public class HistogramViewModel : ViewModelBase
	{
		private readonly HistogramModel _histogramModel;
		private BitmapSource _histogramImage;
		private bool _isLoading;

		public HistogramViewModel()
		{
			_histogramModel = new HistogramModel();
		}

		public BitmapSource HistogramImage
		{
			get => _histogramImage;
			set
			{
				if (_histogramImage != value)
				{
					_histogramImage = value;
					OnPropertyChanged(nameof(HistogramImage));
				}
			}
		}

		public bool IsLoading
		{
			get => _isLoading;
			set
			{
				if (_isLoading != value)
				{
					_isLoading = value;
					OnPropertyChanged(nameof(IsLoading));
				}
			}
		}

		public async Task UpdateHistogramAsync(Mat image)
		{
			IsLoading = true;
			try
			{
				await Task.Run(() => _histogramModel.CalculateAndCreateHistogram(image));

				await Application.Current.Dispatcher.InvokeAsync(() =>
					{
						HistogramImage = _histogramModel.GetHistogramImageClone();
					});
			}
			finally
			{
				IsLoading = false;
			}
		}
	}
}