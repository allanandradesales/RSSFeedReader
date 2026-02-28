using RSSFeedReader.Presentation.Pages;

namespace RSSFeedReader.Presentation;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();
		Routing.RegisterRoute("ArticleList", typeof(ArticleListPage));
	}
}
