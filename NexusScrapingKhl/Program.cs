using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using ScrapingKhl;
using ScrapingKhl.Services;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console()
    .CreateLogger();

using (new TimeMeasurer("Время выполнения программы"))
{
    Log.Information("Программа запущена");

    var builder = new ConfigurationBuilder();
    builder.SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
    IConfigurationRoot configuration = builder.Build();

    var services = new ServiceCollection();
    services.AddHttpClient<IHttpService, HttpService>(client =>
    {
        client.DefaultRequestHeaders.UserAgent.ParseAdd(configuration["UserAgent"]);
        client.BaseAddress = new Uri(configuration["BaseAddress"]);
        client.Timeout = TimeSpan.FromMinutes(1);
    }).AddTransientHttpErrorPolicy(p => p.WaitAndRetryAsync(3, i => TimeSpan.FromSeconds(i * 2)));
    services.AddScoped<IHtmlLoader, HtmlLoader>();
    services.AddSingleton<IRequestThrottler, RequestThrottler>();
    services.AddScoped<IScrapingClubService, ScrapingClubService>();
    services.AddTransient<App>();

    var provider = services.BuildServiceProvider();
    var app = provider.GetRequiredService<App>();
    await app.Run();
}

