using RSSFeedReader.Presentation.ViewModels;

namespace RSSFeedReader.Presentation.Pages;

/// <summary>Code-behind for <see cref="ArticleListPage"/>.</summary>
public sealed partial class ArticleListPage : ContentPage
{
    private readonly ArticleListViewModel _viewModel;

    /// <summary>Initializes a new instance of <see cref="ArticleListPage"/>.</summary>
    public ArticleListPage(ArticleListViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    /// <inheritdoc/>
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadArticlesAsync();
    }
}
