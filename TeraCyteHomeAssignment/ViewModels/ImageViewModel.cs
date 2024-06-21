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
		private BitmapSource _image;
		private BitmapSource _originalImage; // Store the original image
		private double _brightness = 1.0;
		private bool _enableSlider = false;

		public ImageViewModel()
		{
			_imageModel = new ImageModel();
			LoadImageCommand = new RelayCommand(async () => await LoadImageAsync());
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
					AdjustBrightness();
				}
			}
		}

		public bool EnableSlider
		{
			get => _enableSlider;
			set
			{
				if (_enableSlider != value)
				{
					_enableSlider = value;
					OnPropertyChanged(nameof(EnableSlider));
				}
			}
		}

		private void AdjustBrightness()
		{
			if (_originalImage != null)
			{
				EnableSlider = false;
				using (var mat = _originalImage.ToMat())
				{
					// Apply brightness adjustment
					Mat adjustedMat = new Mat();
					mat.ConvertTo(adjustedMat, -1, Brightness, 0); // Brightness adjustment

					Image = adjustedMat.ToBitmapSource();
				}
				EnableSlider = true;
			}
		}

		public string ImagePath => _imageModel.ImagePath;

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
				EnableSlider = false;
				string fileName = openFileDialog.FileName;

				try
				{
					await Task.Run(() => _imageModel.LoadImage(fileName));

					await Application.Current.Dispatcher.InvokeAsync(() =>
					{
						using (var mat = _imageModel.GetMat())
						{
							_originalImage = mat.ToBitmapSource(); // Store the original image
							Image = _originalImage; // Display the original image initially
						}
						IsLoading = false;
						EnableSlider = true;
						OnPropertyChanged(nameof(ImagePath));
						ResetImageDimensions(); // Reset image dimensions to auto
					});
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

		private void ResetImageDimensions()
		{
			Application.Current.Dispatcher.Invoke(() =>
			{
				if (Application.Current.MainWindow is MainWindow mainWindow)
				{
					mainWindow.ResetImageDimensions();
				}
			});
		}

		public void Dispose()
		{
			_imageModel.Dispose();
		}
	}
}
