using System;
using System.Windows.Media.Imaging;
using OpenCvSharp;
using OpenCvSharp.WpfExtensions;


namespace Tera.ImageModel
{
	public class ImageModel
	{
		public string ImagePath { get; private set; }
		public BitmapSource Image { get; private set; }

		public void LoadImage(string path)
		{
			ImagePath = path;
			using var mat = Cv2.ImRead(path, ImreadModes.Color);
			Image = mat.ToBitmapSource(); // This returns a BitmapSource
		}
	}
}