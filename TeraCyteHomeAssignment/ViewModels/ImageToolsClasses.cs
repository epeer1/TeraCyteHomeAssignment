using MainUI.Commands;
using Microsoft.Win32;
using OpenCvSharp;
using System;
using System.IO;
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

		event EventHandler HistogramReadyToSend;

		BitmapSource GetHistogramImage();
		Task SendHistogramAsync();
	}

	public class HistogramManager : IHistogramManager
	{
		private readonly ImageModel _imageModel;
		private readonly HistogramSender _histogramSender;
		private readonly object sendLock = new object();

		public HistogramManager(ImageModel imageModel, string azureFunctionUrl)
		{
			_imageModel = imageModel;
			_histogramSender = new HistogramSender(azureFunctionUrl);
			_imageModel.HistogramUpdated += OnHistogramUpdated;
			_imageModel.HistogramReadyToSend += OnHistogramReadyToSend;
		}

		public event EventHandler HistogramUpdated;

		public event EventHandler HistogramReadyToSend;

		private void OnHistogramUpdated(object sender, EventArgs e)
		{
			HistogramUpdated?.Invoke(this, EventArgs.Empty);
		}

		private void OnHistogramReadyToSend(object sender, EventArgs e)
		{
			HistogramReadyToSend?.Invoke(this,EventArgs.Empty);
		}

		public BitmapSource GetHistogramImage()
		{
			return _imageModel.GetCloneHistogram();
		}

		public async Task SendHistogramAsync()
		{
			try
			{
				lock(sendLock)
				{
					_histogramSender.SendHistogramAsync(_imageModel.GetHisotgramByteArray());
				}
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
		event EventHandler ImageLoaded;

		event EventHandler ImageUpdated;
		void LoadImage();
		Task<BitmapSource> GetImageCloneAsync();
	}

	public class ImageLoader : IImageLoader
	{
		private readonly ImageModel _imageModel;
		private readonly ILogger _logger;

		public ImageLoader(ImageModel imageModel, ILogger logger)
		{
			_imageModel = imageModel;
			_logger = logger;
			_imageModel.ImageLoaded += OnImageLoaded;
		}

		public event EventHandler ImageLoaded;

		public event EventHandler ImageUpdated;

		private void OnImageLoaded(object sender, EventArgs e)
		{
			ImageLoaded?.Invoke(this, EventArgs.Empty);
		}

		public void LoadImage()
		{
			var openFileDialog = new OpenFileDialog
				                     {
					                     Filter = "Image files (*.bmp;*.jpg;*.jpeg;*.png)|*.bmp;*.jpg;*.jpeg;*.png",
					                     Title = "Select an Image"
				                     };

			if (openFileDialog.ShowDialog() != true) 
				return;

			string fileName = openFileDialog.FileName;
			string extension = Path.GetExtension(fileName).ToLower();

			try
			{
				Task.Run(
					() =>
						{
							Mat mat;
							if(extension == ".bmp")
							{
								// Use method for BMP files
								mat = Cv2.ImRead(fileName, ImreadModes.Color);
							}
							else
							{
								// Use a more robust method for other formats
								byte[] fileBytes = File.ReadAllBytes(fileName);
								mat = Mat.FromImageData(fileBytes, ImreadModes.Color);
							}

							if(mat.Empty())
							{
								throw new Exception("Failed to load image.");
							}

							_imageModel.SetMatAndHistogram(mat);

						});
			}
			catch(Exception e)
			{
				Console.WriteLine(e);
				throw;
			}
		}

		public async Task<BitmapSource> GetImageCloneAsync()
		{
			return _imageModel.GetImageClone();
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