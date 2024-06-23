using MainUI.Commands;
using Microsoft.Win32;
using OpenCvSharp;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using OpenCvSharp.WpfExtensions;
using Tera.NetworkServices;
using Tera.UpdatedImageModel;

namespace MainUI.ViewModels
{
	public interface IBrightnessManager
	{
		double Brightness { get; set; }
		event EventHandler OnBrightnessChanged;
		Task<Mat> AdjustBrightnessAsync(Mat originalMat);
	}

	public class BrightnessManager : IBrightnessManager
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

	public interface IHistogramManager : IDisposable
	{
		event EventHandler HistogramUpdated;
		BitmapSource GetHistogramImage();
		Task SendHistogramAsync();
	}

	public class HistogramManager : IHistogramManager
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

		public async Task SendHistogramAsync()
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

	public interface IImageLoader
	{
		event EventHandler<BitmapSource> ImageLoaded;
		Task<BitmapSource> LoadImageAsync();
	}

	public class ImageLoader : IImageLoader
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
							throw new ImageProcessingException("Failed to load image.");
						}

						_imageModel.SetMatAndUpdateHistogramAsync(mat.Clone());

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
	}

	public interface ICommandManager
	{
		ICommand Create(Action execute, Func<bool> canExecute = null);
	}

	public class CommandManager : ICommandManager
	{
		public ICommand Create(Action execute, Func<bool> canExecute = null)
		{
			return new RelayCommand(execute, canExecute);
		}
	}

	public class ImageProcessingException : Exception
	{
		public ImageProcessingException(string message) : base(message) { }
		public ImageProcessingException(string message, Exception innerException) : base(message, innerException) { }
	}
}