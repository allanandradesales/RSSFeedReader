# Quickstart: OPML Export

## Prerequisites

- .NET 8 SDK at `~/.dotnet` with MAUI workload
- `export DOTNET_ROOT="$HOME/.dotnet" && export PATH="$HOME/.dotnet:$HOME/.dotnet/tools:$PATH"`
- Branch: `003-opml-export`

---

## Build & test

```sh
# Build non-MAUI projects
dotnet build src/RSSFeedReader.Domain/RSSFeedReader.Domain.csproj --configuration Release
dotnet build src/RSSFeedReader.Application/RSSFeedReader.Application.csproj --configuration Release
dotnet build src/RSSFeedReader.Infrastructure/RSSFeedReader.Infrastructure.csproj --configuration Release

# Run all tests
dotnet test tests/RSSFeedReader.Domain.Tests/RSSFeedReader.Domain.Tests.csproj --configuration Release
dotnet test tests/RSSFeedReader.Application.Tests/RSSFeedReader.Application.Tests.csproj --configuration Release
dotnet test tests/RSSFeedReader.Infrastructure.Tests/RSSFeedReader.Infrastructure.Tests.csproj --configuration Release
```

---

## Unit test scenario: ExportSubscriptionsAsOpmlHandler

```csharp
// 1. Mock IFeedRepository returns 2 feeds
// 2. Mock IOpmlFileExporter captures the XML argument and returns "/Downloads/subscriptions.opml"
// 3. Call ExportSubscriptionsAsOpmlHandler.HandleAsync(new ExportSubscriptionsAsOpmlCommand())
// 4. Result.IsSuccess == true
// 5. Result.FilePath == "/Downloads/subscriptions.opml"
// 6. Captured XML contains both feed titles and URLs as OPML <outline> elements
```

---

## New files checklist

```text
src/RSSFeedReader.Domain/Interfaces/Services/IOpmlFileExporter.cs
src/RSSFeedReader.Application/UseCases/ExportSubscriptionsAsOpml/ExportSubscriptionsAsOpmlCommand.cs
src/RSSFeedReader.Application/UseCases/ExportSubscriptionsAsOpml/ExportSubscriptionsAsOpmlHandler.cs
src/RSSFeedReader.Infrastructure/OpmlExport/DownloadsOpmlFileExporter.cs
tests/RSSFeedReader.Application.Tests/UseCases/ExportSubscriptionsAsOpmlHandlerTests.cs
```

## Updated files checklist

```text
src/RSSFeedReader.Presentation/MauiProgram.cs                   — register IOpmlFileExporter + ExportSubscriptionsAsOpmlHandler
src/RSSFeedReader.Presentation/ViewModels/FeedListViewModel.cs  — add ExportFeedsCommand
src/RSSFeedReader.Presentation/Pages/FeedListPage.xaml          — add Export ToolbarItem
```
