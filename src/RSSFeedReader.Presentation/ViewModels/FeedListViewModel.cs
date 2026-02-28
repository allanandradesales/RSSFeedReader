using System.Collections.ObjectModel;
using System.Windows.Input;
using RSSFeedReader.Application.DTOs;
using RSSFeedReader.Application.UseCases.AddFeedSubscription;
using RSSFeedReader.Application.UseCases.GetFeeds;
using RSSFeedReader.Application.UseCases.RefreshFeedSubscription;

namespace RSSFeedReader.Presentation.ViewModels;

/// <summary>ViewModel for the feed list page.</summary>
public sealed class FeedListViewModel : INotifyPropertyChanged
{
    private readonly AddFeedSubscriptionHandler _addHandler;
    private readonly GetFeedsHandler _getHandler;
    private readonly RefreshFeedSubscriptionHandler _refreshHandler;

    private bool _isBusy;
    private string _newFeedUrl = string.Empty;
    private string _statusMessage = string.Empty;

    /// <summary>Initializes a new instance of <see cref="FeedListViewModel"/>.</summary>
    public FeedListViewModel(
        AddFeedSubscriptionHandler addHandler,
        GetFeedsHandler getHandler,
        RefreshFeedSubscriptionHandler refreshHandler)
    {
        _addHandler = addHandler;
        _getHandler = getHandler;
        _refreshHandler = refreshHandler;

        AddFeedCommand = new Command(
            execute: async () => await AddFeedAsync(),
            canExecute: () => !IsBusy && !string.IsNullOrWhiteSpace(NewFeedUrl));

        RefreshCommand = new Command(
            execute: async () => await LoadFeedsAsync(),
            canExecute: () => !IsBusy);

        RefreshFeedCommand = new Command<FeedDto>(
            execute: async (feed) => await RefreshFeedAsync(feed),
            canExecute: (feed) => !IsBusy);

        NavigateToArticlesCommand = new Command<FeedDto>(
            execute: async (feed) => await NavigateToArticlesAsync(feed));
    }

    /// <inheritdoc/>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>Gets the list of subscribed feeds.</summary>
    public ObservableCollection<FeedDto> Feeds { get; } = [];

    /// <summary>Gets or sets whether an async operation is in progress.</summary>
    public bool IsBusy
    {
        get => _isBusy;
        set
        {
            if (_isBusy == value) return;
            _isBusy = value;
            OnPropertyChanged(nameof(IsBusy));
            ((Command)AddFeedCommand).ChangeCanExecute();
            ((Command)RefreshCommand).ChangeCanExecute();
            ((Command)RefreshFeedCommand).ChangeCanExecute();
        }
    }

    /// <summary>Gets or sets the URL entered by the user for a new feed subscription.</summary>
    public string NewFeedUrl
    {
        get => _newFeedUrl;
        set
        {
            if (_newFeedUrl == value) return;
            _newFeedUrl = value;
            OnPropertyChanged(nameof(NewFeedUrl));
            ((Command)AddFeedCommand).ChangeCanExecute();
        }
    }

    /// <summary>Gets or sets a status/error message shown below the entry field.</summary>
    public string StatusMessage
    {
        get => _statusMessage;
        set
        {
            if (_statusMessage == value) return;
            _statusMessage = value;
            OnPropertyChanged(nameof(StatusMessage));
        }
    }

    /// <summary>Gets the command that adds a new feed subscription.</summary>
    public ICommand AddFeedCommand { get; }

    /// <summary>Gets the command that reloads all feeds from the database.</summary>
    public ICommand RefreshCommand { get; }

    /// <summary>Gets the command that refreshes a single feed and updates its badge in-place.</summary>
    public ICommand RefreshFeedCommand { get; }

    /// <summary>Gets the command that navigates to the article list for the given feed.</summary>
    public ICommand NavigateToArticlesCommand { get; }

    /// <summary>Loads all feeds from the database.</summary>
    public async Task LoadFeedsAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        StatusMessage = string.Empty;
        try
        {
            var feeds = await _getHandler.HandleAsync(new GetFeedsQuery());
            Feeds.Clear();
            foreach (var f in feeds)
                Feeds.Add(f);
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>Updates the unread count badge for the specified feed in-place.</summary>
    public void UpdateUnreadCount(Guid feedId, int newCount)
    {
        var idx = -1;
        for (var i = 0; i < Feeds.Count; i++)
        {
            if (Feeds[i].Id == feedId)
            {
                idx = i;
                break;
            }
        }

        if (idx >= 0)
            Feeds[idx] = Feeds[idx] with { UnreadCount = newCount };
    }

    private async Task AddFeedAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        StatusMessage = string.Empty;
        try
        {
            var result = await _addHandler.HandleAsync(new AddFeedSubscriptionCommand(NewFeedUrl.Trim()));
            if (result.IsSuccess)
            {
                NewFeedUrl = string.Empty;
                Feeds.Add(result.Feed!);
                StatusMessage = $"Added: {result.Feed!.Title}";
            }
            else
            {
                StatusMessage = result.Error switch
                {
                    AddFeedSubscriptionError.AlreadyExists => "This feed is already subscribed.",
                    AddFeedSubscriptionError.FetchFailed => result.FetchError switch
                    {
                        FeedFetchError.InvalidUrl => "Invalid URL.",
                        FeedFetchError.SsrfBlocked => "That URL is not allowed.",
                        FeedFetchError.SelfSignedCertificate => "The feed uses an untrusted certificate.",
                        FeedFetchError.HttpError => "The server returned an error.",
                        FeedFetchError.Timeout => "Request timed out.",
                        FeedFetchError.ParseError => "Could not parse the feed.",
                        FeedFetchError.NotAFeed => "The URL does not point to a valid feed.",
                        _ => "Failed to fetch the feed.",
                    },
                    _ => "An unexpected error occurred.",
                };
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task RefreshFeedAsync(FeedDto feed)
    {
        if (IsBusy) return;
        IsBusy = true;
        StatusMessage = string.Empty;
        try
        {
            var result = await _refreshHandler.HandleAsync(
                new RefreshFeedSubscriptionCommand(feed.Id, feed.Url));

            if (result.IsSuccess)
            {
                var idx = -1;
                for (var i = 0; i < Feeds.Count; i++)
                {
                    if (Feeds[i].Id == feed.Id) { idx = i; break; }
                }
                if (idx >= 0)
                    Feeds[idx] = Feeds[idx] with
                    {
                        UnreadCount = result.NewUnreadCount,
                        LastRefreshedAt = result.NewLastRefreshedAt,
                    };
            }
            else
            {
                StatusMessage = result.Error == RefreshFeedError.FeedNotFound
                    ? "Feed not found."
                    : "Failed to refresh feed.";
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task NavigateToArticlesAsync(FeedDto feed)
    {
        await Shell.Current.GoToAsync("ArticleList", new ShellNavigationQueryParameters
        {
            ["feedId"] = feed.Id.ToString(),
            ["feedTitle"] = feed.Title,
            ["parentVm"] = this,
        });
    }

    private void OnPropertyChanged(string name) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
