using RSSFeedReader.Domain.Interfaces.Services;

namespace RSSFeedReader.Domain.Tests.Services;

public sealed class ResultTests
{
    [Fact]
    public void Result_Ok_IsSuccess()
    {
        var result = Result.Ok<int, string>(42);

        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void Result_Fail_IsNotSuccess()
    {
        var result = Result.Fail<int, string>("error");

        Assert.False(result.IsSuccess);
        Assert.Equal("error", result.Error);
    }

    [Fact]
    public void Result_Ok_ThrowsOnErrorAccess()
    {
        var result = Result.Ok<int, string>(1);

        Assert.Throws<InvalidOperationException>(() => _ = result.Error);
    }

    [Fact]
    public void Result_Fail_ThrowsOnValueAccess()
    {
        var result = Result.Fail<int, string>("bad");

        Assert.Throws<InvalidOperationException>(() => _ = result.Value);
    }
}
