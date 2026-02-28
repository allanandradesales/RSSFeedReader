using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RSSFeedReader.Application.UseCases.AddFeedSubscription;
using RSSFeedReader.Application.UseCases.GetArticlesByFeed;
using RSSFeedReader.Application.UseCases.GetFeeds;
using RSSFeedReader.Application.UseCases.MarkArticleAsRead;
using RSSFeedReader.Application.UseCases.RefreshFeedSubscription;
using RSSFeedReader.Application.UseCases.ToggleArticleReadStatus;
using RSSFeedReader.Domain.Interfaces.Repositories;
using RSSFeedReader.Domain.Interfaces.Services;
using RSSFeedReader.Infrastructure.ContentSanitizer;
using RSSFeedReader.Infrastructure.FeedFetcher;
using RSSFeedReader.Infrastructure.Persistence;
using RSSFeedReader.Infrastructure.Persistence.Repositories;
using RSSFeedReader.Presentation.Pages;
using RSSFeedReader.Presentation.ViewModels;

namespace RSSFeedReader.Presentation;

/// <summary>Entry point for the MAUI application; configures the DI container and runs migrations.</summary>
public static class MauiProgram
{
    /// <summary>Builds and returns the MAUI application.</summary>
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // Database
        var dbPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "rssfeedreader.db");

        builder.Services.AddDbContext<AppDbContext>(options =>
            options.UseSqlite($"Data Source={dbPath}"));

        // Infrastructure
        builder.Services.AddHttpClient("FeedFetcher");
        builder.Services.AddScoped<IFeedRepository, FeedRepository>();
        builder.Services.AddScoped<IArticleRepository, ArticleRepository>();
        builder.Services.AddScoped<IContentSanitizerService, HtmlSanitizerAdapter>();
        builder.Services.AddScoped<IFeedFetcherService, FeedFetcherService>();

        // Application handlers
        builder.Services.AddScoped<AddFeedSubscriptionHandler>();
        builder.Services.AddScoped<GetFeedsHandler>();
        builder.Services.AddScoped<RefreshFeedSubscriptionHandler>();
        builder.Services.AddScoped<MarkArticleAsReadHandler>();
        builder.Services.AddScoped<ToggleArticleReadStatusHandler>();
        builder.Services.AddScoped<GetArticlesByFeedHandler>();

        // Presentation
        builder.Services.AddTransient<FeedListViewModel>();
        builder.Services.AddTransient<FeedListPage>();
        builder.Services.AddTransient<ArticleListViewModel>();
        builder.Services.AddTransient<ArticleListPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        var app = builder.Build();

        // Run migrations on startup
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.Migrate();
        }

        return app;
    }
}
