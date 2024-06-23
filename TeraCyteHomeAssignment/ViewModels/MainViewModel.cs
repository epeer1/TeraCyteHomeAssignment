using MainUI;
using MainUI.ViewModels;
using OpenCvSharp.WpfExtensions;
using Tera.UpdatedImageModel;

public class MainViewModel : ViewModelBase
{
	public ImageViewModel ImageViewModel { get; }

	public MainViewModel()
	{
		var logger = new Logger("application.log");
		var imageModel = new ImageModel();
		IBrightnessManager brightnessManager = new BrightnessManager();
		IHistogramManager histogramManager = new HistogramManager(imageModel, "https://teraimagefunctionapp.azurewebsites.net");
		IImageLoader imageLoader = new ImageLoader(imageModel, logger);
		ICommandManager commandManager = new CommandManager();

		ImageViewModel = new ImageViewModel(imageModel, brightnessManager, histogramManager, imageLoader, commandManager, logger);
	}
}