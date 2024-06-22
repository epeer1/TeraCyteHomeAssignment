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
		var brightnessManager = new BrightnessManager();
		var histogramManager = new HistogramManager(imageModel, "https://teraimagefunctionapp.azurewebsites.net");
		var imageLoader = new ImageLoader(imageModel,logger);
		var commandManager = new CommandManager();

		ImageViewModel = new ImageViewModel(imageModel, brightnessManager, histogramManager, imageLoader, commandManager, logger);
	}
}