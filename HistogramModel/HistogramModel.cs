using OpenCvSharp;
using System.Linq;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Tera.HistogramModel
{
	public class HistogramModel
	{
		private const int HistogramWidth = 256;
		private const int HistogramHeight = 200;
		private BitmapSource _histogramImage;

		public void CalculateAndCreateHistogram(Mat image)
		{
			var histograms = CalculateHistogram(image);
			CreateHistogramImage(histograms);
		}

		private (int[] Red, int[] Green, int[] Blue) CalculateHistogram(Mat image)
		{
			int[] histR = new int[256];
			int[] histG = new int[256];
			int[] histB = new int[256];

			if (image.Channels() == 1) // Grayscale
			{
				using (var hist = new Mat())
				{
					Cv2.CalcHist(new[] { image }, new[] { 0 }, null, hist, 1, new[] { 256 }, new[] { new Rangef(0, 256) });
					hist.GetArray(out float[] tempHist);
					histR = tempHist.Select(x => (int)x).ToArray();
				}
				return (histR, histR, histR); // Return the same histogram for all channels
			}
			else // Color
			{
				Mat[] bgr = Cv2.Split(image);
				using (var histMatR = new Mat())
				using (var histMatG = new Mat())
				using (var histMatB = new Mat())
				{
					Cv2.CalcHist(new[] { bgr[2] }, new[] { 0 }, null, histMatR, 1, new[] { 256 }, new[] { new Rangef(0, 256) });
					Cv2.CalcHist(new[] { bgr[1] }, new[] { 0 }, null, histMatG, 1, new[] { 256 }, new[] { new Rangef(0, 256) });
					Cv2.CalcHist(new[] { bgr[0] }, new[] { 0 }, null, histMatB, 1, new[] { 256 }, new[] { new Rangef(0, 256) });

					histMatR.GetArray(out float[] tempHistR);
					histMatG.GetArray(out float[] tempHistG);
					histMatB.GetArray(out float[] tempHistB);

					histR = tempHistR.Select(x => (int)x).ToArray();
					histG = tempHistG.Select(x => (int)x).ToArray();
					histB = tempHistB.Select(x => (int)x).ToArray();
				}
				return (histR, histG, histB);
			}
		}

		private void CreateHistogramImage((int[] Red, int[] Green, int[] Blue) histograms)
		{
			using (var histImage = new Mat(HistogramHeight, HistogramWidth, MatType.CV_8UC3, Scalar.White))
			{
				DrawHistogram(histImage, histograms.Red, new Scalar(0, 0, 255));   // Red
				DrawHistogram(histImage, histograms.Green, new Scalar(0, 255, 0)); // Green
				DrawHistogram(histImage, histograms.Blue, new Scalar(255, 0, 0));  // Blue

				// Convert Mat to byte array
				var imageData = new byte[histImage.Total() * histImage.ElemSize()];
				Marshal.Copy(histImage.Data, imageData, 0, imageData.Length);

				int bytesPerPixel = histImage.ElemSize();
				int stride = HistogramWidth * bytesPerPixel;
				int expectedBufferSize = stride * HistogramHeight;

				if (imageData.Length < expectedBufferSize)
				{
					throw new ArgumentException($"Buffer size is not sufficient. Expected: {expectedBufferSize}, Actual: {imageData.Length}");
				}

				_histogramImage = BitmapSource.Create(
					HistogramWidth, HistogramHeight, 96, 96, PixelFormats.Bgr24, null, imageData, stride);
				_histogramImage.Freeze(); // Make it immutable for thread-safety
			}
		}

		private void DrawHistogram(Mat histImage, int[] histogram, Scalar color)
		{
			int max = histogram.Max();
			for (int i = 0; i < histogram.Length; i++)
			{
				int height = (int)((histogram[i] / (double)max) * HistogramHeight);
				Cv2.Line(
					histImage,
					new Point(i, HistogramHeight),
					new Point(i, HistogramHeight - height),
					color
				);
			}
		}

		public BitmapSource GetHistogramImageClone()
		{
			if (_histogramImage == null)
				return null;

			return new FormatConvertedBitmap(_histogramImage, _histogramImage.Format, null, 0);
		}
	}
}