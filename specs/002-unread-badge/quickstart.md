# Quickstart: Unread Article Count Badge

## Prerequisites

- .NET 8 SDK at `~/.dotnet` with MAUI workload
- `export DOTNET_ROOT="$HOME/.dotnet" && export PATH="$HOME/.dotnet:$HOME/.dotnet/tools:$PATH"`
- Branch: `002-unread-badge`

---

## Build & test

```sh
# Build all non-MAUI projects
dotnet build src/RSSFeedReader.Domain/RSSFeedReader.Domain.csproj --configuration Release
dotnet build src/RSSFeedReader.Application/RSSFeedReader.Application.csproj --configuration Release
dotnet build src/RSSFeedReader.Infrastructure/RSSFeedReader.Infrastructure.csproj --configuration Release

# Run all tests
dotnet test tests/RSSFeedReader.Domain.Tests/RSSFeedReader.Domain.Tests.csproj --configuration Release
dotnet test tests/RSSFeedReader.Application.Tests/RSSFeedReader.Application.Tests.csproj --configuration Release
dotnet test tests/RSSFeedReader.Infrastructure.Tests/RSSFeedReader.Infrastructure.Tests.csproj --configuration Release
```

---

## Integration test scenario: Badge reflects unread count

```csharp
// 1. Create feed + 3 unread articles
// 2. Call GetFeedsHandler → FeedDto.UnreadCount == 3
// 3. Call MarkArticleAsReadHandler for 1 article → returns newUnreadCount == 2
// 4. Call GetFeedsHandler again → FeedDto.UnreadCount == 2
// 5. Call ToggleArticleReadStatusHandler for same article → newIsRead=false, newUnreadCount=3
// 6. Call GetFeedsHandler again → FeedDto.UnreadCount == 3
```

---

## Scenario: Badge updates after refresh

```csharp
// 1. Feed has 2 unread articles in DB
// 2. Mock FeedFetcherService returns 5 new articles
// 3. Call RefreshFeedSubscriptionHandler → NewUnreadCount == 7, NewLastRefreshedAt set
// 4. GetFeedsHandler → FeedDto.UnreadCount == 7
```

---

## New files checklist

```text
src/RSSFeedReader.Application/DTOs/ArticleDto.cs
src/RSSFeedReader.Application/UseCases/RefreshFeedSubscription/RefreshFeedSubscriptionCommand.cs
src/RSSFeedReader.Application/UseCases/RefreshFeedSubscription/RefreshFeedSubscriptionHandler.cs
src/RSSFeedReader.Application/UseCases/MarkArticleAsRead/MarkArticleAsReadCommand.cs
src/RSSFeedReader.Application/UseCases/MarkArticleAsRead/MarkArticleAsReadHandler.cs
src/RSSFeedReader.Application/UseCases/ToggleArticleReadStatus/ToggleArticleReadStatusCommand.cs
src/RSSFeedReader.Application/UseCases/ToggleArticleReadStatus/ToggleArticleReadStatusHandler.cs
src/RSSFeedReader.Presentation/Pages/ArticleListPage.xaml
src/RSSFeedReader.Presentation/Pages/ArticleListPage.xaml.cs
src/RSSFeedReader.Presentation/ViewModels/ArticleListViewModel.cs
tests/RSSFeedReader.Application.Tests/UseCases/RefreshFeedSubscriptionHandlerTests.cs
tests/RSSFeedReader.Application.Tests/UseCases/MarkArticleAsReadHandlerTests.cs
tests/RSSFeedReader.Application.Tests/UseCases/ToggleArticleReadStatusHandlerTests.cs
tests/RSSFeedReader.Infrastructure.Tests/Persistence/ArticleRepositoryReadStatusTests.cs
```

## Updated files checklist

```text
src/RSSFeedReader.Presentation/ViewModels/FeedListViewModel.cs  — add RefreshFeedCommand, NavigateToArticlesCommand, UpdateUnreadCount
src/RSSFeedReader.Presentation/Pages/FeedListPage.xaml           — add Refresh + tap-to-articles per cell
src/RSSFeedReader.Presentation/Pages/FeedListPage.xaml.cs        — wire navigation
src/RSSFeedReader.Presentation/AppShell.xaml                     — register ArticleList route
src/RSSFeedReader.Presentation/MauiProgram.cs                    — register new handlers, VMs, pages
```
