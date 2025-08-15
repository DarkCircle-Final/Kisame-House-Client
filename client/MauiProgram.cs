using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;

namespace client;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.UseMauiCommunityToolkit()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});
		builder.Services.AddSingleton<ViewModels.DashBoardViewModel>();
        builder.Services.AddSingleton<Views.DashBoardView>();
		builder.Services.AddSingleton<ViewModels.SettingsViewModel>();
        builder.Services.AddSingleton<Views.SettingsView>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
