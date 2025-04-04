﻿using Microsoft.Extensions.Logging;
using RenPyReader.Services;

namespace RenPyReader;

public static class MauiProgram 
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder.UseMauiApp<App>().ConfigureFonts(fonts => { fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular"); });
        builder.Services.AddMauiBlazorWebView();
		builder.Services.AddBlazorBootstrap();

		#if DEBUG
		builder.Services.AddBlazorWebViewDeveloperTools();
		builder.Logging.AddDebug();
		#endif

        builder.Services.AddSingleton<IApplicationStateService, ApplicationStateService>();
		builder.Services.AddSingleton<IStringTransformerService, StringTransformerService>();
		builder.Services.AddSingleton<IAudioPlayerService, AudioPlayerService>();
        builder.Services.AddSingleton<ISQLiteService, SQLiteService>();
        return builder.Build();
    }
}
