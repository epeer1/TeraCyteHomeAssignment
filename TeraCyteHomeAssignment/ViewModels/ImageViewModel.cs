using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using MainUI.Commands;
using OpenCvSharp.WpfExtensions;
using Tera.UpdatedImageModel;
using OpenCvSharp;

namespace MainUI.ViewModels
{
	public class ImageViewModel : ViewModelBase, IDisposable
	{
		private readonly ImageModel _imageModel;
		private bool _isLoading;
		private bool _isLoadingHistogram;
		private BitmapSource _image;
		private BitmapSource _originalImage; // Store the original image
		private double _brightness = 1.0;
		private bool _enableBrightnessSlider = false;
		private BitmapSource _histogramImage;

		public ImageViewModel()
		{
			_imageModel = new ImageModel();
			LoadImageCommand = new RelayCommand(async () => await LoadImageAsync());

			_imageModel.HistogramUpdated += OnHistogramUpdated;

		}

		public ICommand LoadImageCommand { get; }

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

		public BitmapSource Image
		{
			get => _image;
			set
			{
				if (_image != value)
				{
					_image = value;
					OnPropertyChanged(nameof(Image));
				}
			}
		}

		public double Brightness
		{
			get => _brightness;
			set
			{
				if (_brightness != value)
				{
					_brightness = value;
					OnPropertyChanged(nameof(Brightness));
					_ = AdjustBrightness();
				}
			}
		}

		public bool EnableBrightnessSlider
		{
			get => _enableBrightnessSlider;
			set
			{
				if (_enableBrightnessSlider != value)
				{
					_enableBrightnessSlider = value;
					OnPropertyChanged(nameof(EnableBrightnessSlider));
				}
			}
		}

		public BitmapSource HistogramImage
		{
			get => _histogramImage;
			set
			{
				_histogramImage = value;
				OnPropertyChanged(nameof(HistogramImage));
			}
		}

		public bool IsLoadingHistogram
		{
			get => _isLoadingHistogram;
			set
			{
				if (_isLoadingHistogram != value)
				{
					_isLoadingHistogram = value;
					OnPropertyChanged(nameof(IsLoadingHistogram));
				}
			}
		}

		private void OnHistogramUpdated(object sender, EventArgs e)
		{
			Application.Current.Dispatcher.Invoke(() =>	//MainUI thread
				{
					IsLoadingHistogram = true;
					HistogramImage = _imageModel.GetCloneHistogram();
					IsLoadingHistogram = false;
					EnableBrightnessSlider = true;
				});
		}

		private async Task AdjustBrightness()
		{
			if (_originalImage != null)
			{
				EnableBrightnessSlider = false;
				using (var mat = _originalImage.ToMat())
				{
					Mat adjustedMat = new Mat();
					await Task.Run(
						() =>
							{
								mat.ConvertTo(adjustedMat, -1, Brightness, 0); // Brightness adjustment
							});
					// Apply brightness adjustment
					Image = adjustedMat.ToBitmapSource();

					Task.Run(() => _imageModel.SetMatAndUpdateHistogram(adjustedMat));	//Background thread, not need to wait
				}
			}
		}

		private async Task LoadImageAsync()
		{
			OpenFileDialog openFileDialog = new OpenFileDialog
				                                {
					                                Filter = "Image files (*.bmp, *.jpg, *.jpeg, *.png)|*.bmp;*.jpg;*.jpeg;*.png",
					                                Title = "Select an Image"
				                                };

			if (openFileDialog.ShowDialog() == true)
			{
				IsLoading = true;
				EnableBrightnessSlider = false;
				string fileName = openFileDialog.FileName;

				try
				{
					var tempMat = await Task.Run(() => LoadImage(fileName));

					await Application.Current.Dispatcher.InvokeAsync(() =>	//Update UI before updating Model
						{
							using (var mat = tempMat.Clone())
							{
								_originalImage = mat.ToBitmapSource(); // Store the original image
								Image = _originalImage; // Display the original image initially
							}
							IsLoading = false;
							EnableBrightnessSlider = true;
						});

					Task.Run(() => _imageModel.SetMatAndUpdateHistogram(tempMat));	//Updating Model
				}
				catch (Exception ex)
				{
					await Application.Current.Dispatcher.InvokeAsync(() =>
						{
							MessageBox.Show($"Error loading image: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
							IsLoading = false;
						});
				}
			}
		}



		private Mat LoadImage(string fileName)
		{
			var _mat = Cv2.ImRead(fileName, ImreadModes.Color); //Efficient reading
			return _mat;
		}

		public void Dispose()
		{
			_imageModel.Dispose();
		}
	}
}
