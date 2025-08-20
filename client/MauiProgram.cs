using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using client.Services;
using LibVLCSharp.MAUI;
using LibVLCSharp.Shared;

namespace client;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		Core.Initialize();

		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.UseMauiCommunityToolkit()
			.UseLibVLCSharp()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});
		builder.Services.AddSingleton<ViewModels.DashBoardViewModel>();
        builder.Services.AddSingleton<Views.DashBoardView>();
		builder.Services.AddSingleton<ViewModels.SettingsViewModel>();
        builder.Services.AddSingleton<Views.SettingsView>();
        builder.Services.AddSingleton<ViewModels.LogsViewModel>();
        builder.Services.AddSingleton<Views.LogsView>();
        builder.Services.AddTransient<client.ViewModels.CameraViewModel>();
        builder.Services.AddTransient<client.Views.CameraView>();
        builder.Services.AddSingleton<ViewModels.EntryViewModel>();
        builder.Services.AddSingleton<Views.EntryView>();

#if ANDROID
		builder.Services.AddSingleton<IOrientationService, OrientationService>();
#else
        builder.Services.AddSingleton<IOrientationService, NoOpOrientationService>();
#endif

#if DEBUG
		builder.Logging.AddDebug();
#endif
        var app = builder.Build();
        client.Helps.ServiceHelper.Initialize(app.Services);
        return app;
    }
}
