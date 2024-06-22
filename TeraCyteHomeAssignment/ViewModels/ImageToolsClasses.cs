using MainUI.Commands;
using Microsoft.Win32;
using OpenCvSharp;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows;
using OpenCvSharp.WpfExtensions;
using Tera.NetworkServices;
using Tera.UpdatedImageModel;
using System.IO;
using System.Windows.Media;

namespace MainUI.ViewModels
{
	// BrightnessManager.cs
	public class BrightnessManager
	{
		private double _brightness = 1.0;

		public double Brightness
		{
			get => _brightness;
			set
			{
				if (_brightness != value)
				{
					_brightness = value;
					OnBrightnessChanged?.Invoke(this, EventArgs.Empty);
				}
			}
		}

		public event EventHandler OnBrightnessChanged;

		public async Task<Mat> AdjustBrightnessAsync(Mat originalMat)
		{
			return await Task.Run(() =>
			{
				Mat adjustedMat = new Mat();
				originalMat.ConvertTo(adjustedMat, -1, Brightness, 0);
				return adjustedMat;
			});
		}
	}

	// HistogramManager.cs
	public class HistogramManager
	{
		private readonly ImageModel _imageModel;
		private readonly HistogramSender _histogramSender;

		public HistogramManager(ImageModel imageModel, string azureFunctionUrl)
		{
			_imageModel = imageModel;
			_histogramSender = new HistogramSender(azureFunctionUrl);
			_imageModel.HistogramUpdated += OnHistogramUpdated;
		}

		public event EventHandler HistogramUpdated;

		private void OnHistogramUpdated(object sender, EventArgs e)
		{
			HistogramUpdated?.Invoke(this, EventArgs.Empty);
			_ = SendHistogramAsync();
		}

		public BitmapSource GetHistogramImage()
		{
			return _imageModel.GetCloneHistogram();
		}

		private async Task SendHistogramAsync()
		{
			try
			{
				await _histogramSender.SendHistogramAsync(_imageModel);
			}
			catch (Exception ex)
			{
				// Log the error
				Console.WriteLine($"Error sending histogram: {ex.Message}");
			}
		}

		public void Dispose()
		{
			_histogramSender.Dispose();
		}
	}

	// ImageLoader.cs
	public class ImageLoader
	{
		private readonly ImageModel _imageModel;
		private readonly ILogger _logger;

		public ImageLoader(ImageModel imageModel, ILogger logger)
		{
			_imageModel = imageModel;
			_logger = logger;
		}

		public event EventHandler<BitmapSource> ImageLoaded;

		public async Task<BitmapSource> LoadImageAsync()
		{
			var openFileDialog = new OpenFileDialog
				                     {
					                     Filter = "Image files (*.bmp, *.jpg, *.jpeg, *.png)|*.bmp;*.jpg;*.jpeg;*.png",
					                     Title = "Select an Image"
				                     };

			if (openFileDialog.ShowDialog() != true) return null;

			string fileName = openFileDialog.FileName;

			try
			{
				return await Task.Run(() =>
					{
						using (var mat = Cv2.ImRead(fileName, ImreadModes.Color))
						{
							if (mat.Empty())
							{
								throw new Exception("Failed to load image.");
							}

							_imageModel.SetMatAndUpdateHistogram(mat.Clone());

							var bitmapSource = mat.ToBitmapSource();
							bitmapSource.Freeze(); // Make it immutable for thread-safety

							Application.Current.Dispatcher.Invoke(() =>
								{
									ImageLoaded?.Invoke(this, bitmapSource);
								});

							return bitmapSource;
						}
					});
			}
			catch (Exception ex)
			{
				_logger.LogError($"Error loading image: {ex.Message}", ex);
				Application.Current.Dispatcher.Invoke(() =>
					{
						MessageBox.Show($"Error loading image: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
					});
				return null;
			}
		}

		private Mat BitmapSourceToMat(BitmapSource source)
		{
			if (source.Format != PixelFormats.Bgr24)
			{
				throw new ArgumentException("BitmapSource must be in Bgr24 format");
			}

			int width = source.PixelWidth;
			int height = source.PixelHeight;
			int stride = width * ((source.Format.BitsPerPixel + 7) / 8);

			byte[] pixels = new byte[height * stride];
			source.CopyPixels(pixels, stride, 0);

			return Mat.FromImageData(pixels, ImreadModes.Color);
		}
	}

	// CommandManager.cs
	public class CommandManager
	{
		public ICommand Create(Action execute, Func<bool> canExecute = null)
		{
			return new RelayCommand(execute, canExecute);
		}
	}

	//Exception class
	public class ImageProcessingException : Exception
	{
		public ImageProcessingException(string message) : base(message) { }
		public ImageProcessingException(string message, Exception innerException) : base(message, innerException) { }
	}
}
