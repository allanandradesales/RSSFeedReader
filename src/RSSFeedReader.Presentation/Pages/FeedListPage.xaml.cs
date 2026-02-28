using RSSFeedReader.Presentation.ViewModels;

namespace RSSFeedReader.Presentation.Pages;

/// <summary>Code-behind for <see cref="FeedListPage"/>.</summary>
public sealed partial class FeedListPage : ContentPage
{
    private readonly FeedListViewModel _viewModel;

    /// <summary>Initializes a new instance of <see cref="FeedListPage"/>.</summary>
    public FeedListPage(FeedListViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    /// <inheritdoc/>
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadFeedsAsync();
    }
}
