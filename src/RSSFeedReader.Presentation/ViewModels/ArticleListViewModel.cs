using System.Collections.ObjectModel;
using System.Windows.Input;
using RSSFeedReader.Application.DTOs;
using RSSFeedReader.Application.UseCases.GetArticlesByFeed;
using RSSFeedReader.Application.UseCases.ToggleArticleReadStatus;

namespace RSSFeedReader.Presentation.ViewModels;

/// <summary>Wraps a single article for display and read-status toggling in <see cref="ArticleListViewModel"/>.</summary>
public sealed class ArticleItemViewModel : INotifyPropertyChanged
{
    private bool _isRead;

    /// <summary>Initializes a new instance from an <see cref="ArticleDto"/>.</summary>
    public ArticleItemViewModel(ArticleDto dto)
    {
        Id = dto.Id;
        FeedId = dto.FeedId;
        Title = dto.Title;
        Summary = dto.Summary;
        OriginalUrl = dto.OriginalUrl;
        PublishedAt = dto.PublishedAt;
        _isRead = dto.IsRead;
    }

    /// <inheritdoc/>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>Gets the article ID.</summary>
    public Guid Id { get; }

    /// <summary>Gets the parent feed ID.</summary>
    public Guid FeedId { get; }

    /// <summary>Gets the article title.</summary>
    public string Title { get; }

    /// <summary>Gets the optional article summary.</summary>
    public string? Summary { get; }

    /// <summary>Gets the original article URL.</summary>
    public string OriginalUrl { get; }

    /// <summary>Gets when the article was published.</summary>
    public DateTimeOffset PublishedAt { get; }

    /// <summary>Gets or sets whether the article has been read. Raises <see cref="PropertyChanged"/> on change.</summary>
    public bool IsRead
    {
        get => _isRead;
        set
        {
            if (_isRead == value) return;
            _isRead = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsRead)));
        }
    }
}

/// <summary>ViewModel for the article list page (US3: toggle read status, update badge).</summary>
public sealed class ArticleListViewModel : INotifyPropertyChanged, IQueryAttributable
{
    private readonly GetArticlesByFeedHandler _getArticlesHandler;
    private readonly ToggleArticleReadStatusHandler _toggleHandler;
    private FeedListViewModel? _feedListVm;
    private Guid _feedId;
    private string _feedTitle = string.Empty;
    private bool _isBusy;
    private string _statusMessage = string.Empty;

    /// <summary>Initializes a new instance of <see cref="ArticleListViewModel"/>.</summary>
    public ArticleListViewModel(
        GetArticlesByFeedHandler getArticlesHandler,
        ToggleArticleReadStatusHandler toggleHandler)
    {
        _getArticlesHandler = getArticlesHandler;
        _toggleHandler = toggleHandler;

        ToggleReadCommand = new Command<ArticleItemViewModel>(
            execute: async (item) => await ToggleReadAsync(item),
            canExecute: (item) => !IsBusy);
    }

    /// <inheritdoc/>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>Gets the articles for the current feed.</summary>
    public ObservableCollection<ArticleItemViewModel> Articles { get; } = [];

    /// <summary>Gets the title of the feed being displayed.</summary>
    public string FeedTitle
    {
        get => _feedTitle;
        private set
        {
            if (_feedTitle == value) return;
            _feedTitle = value;
            OnPropertyChanged(nameof(FeedTitle));
        }
    }

    /// <summary>Gets or sets whether an async operation is in progress.</summary>
    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (_isBusy == value) return;
            _isBusy = value;
            OnPropertyChanged(nameof(IsBusy));
            ((Command)ToggleReadCommand).ChangeCanExecute();
        }
    }

    /// <summary>Gets a status/error message.</summary>
    public string StatusMessage
    {
        get => _statusMessage;
        private set
        {
            if (_statusMessage == value) return;
            _statusMessage = value;
            OnPropertyChanged(nameof(StatusMessage));
        }
    }

    /// <summary>Gets the command that toggles the read status of an article.</summary>
    public ICommand ToggleReadCommand { get; }

    /// <inheritdoc/>
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("feedId", out var feedId))
            _feedId = Guid.Parse(feedId.ToString()!);

        if (query.TryGetValue("feedTitle", out var feedTitle))
            FeedTitle = feedTitle.ToString() ?? string.Empty;

        if (query.TryGetValue("parentVm", out var vm) && vm is FeedListViewModel feedListVm)
            _feedListVm = feedListVm;
    }

    /// <summary>Loads all articles for the current feed.</summary>
    public async Task LoadArticlesAsync()
    {
        if (_feedId == Guid.Empty || IsBusy) return;
        IsBusy = true;
        StatusMessage = string.Empty;
        try
        {
            var dtos = await _getArticlesHandler.HandleAsync(new GetArticlesByFeedQuery(_feedId));
            Articles.Clear();
            foreach (var dto in dtos)
                Articles.Add(new ArticleItemViewModel(dto));
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ToggleReadAsync(ArticleItemViewModel item)
    {
        if (IsBusy) return;
        IsBusy = true;
        StatusMessage = string.Empty;
        try
        {
            var result = await _toggleHandler.HandleAsync(
                new ToggleArticleReadStatusCommand(item.Id, item.FeedId));

            if (result is not null)
            {
                item.IsRead = result.NewIsRead;
                _feedListVm?.UpdateUnreadCount(item.FeedId, result.NewUnreadCount);
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void OnPropertyChanged(string name) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
