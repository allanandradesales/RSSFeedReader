using System.Net;
using System.Net.Sockets;

namespace RSSFeedReader.Infrastructure.FeedFetcher;

/// <summary>
/// Pre-request SSRF guard: resolves the hostname and verifies that no returned IP
/// address falls within a private, loopback, or link-local range.
/// </summary>
internal static class SsrfGuard
{
    private static readonly IPNetwork[] BlockedNetworks =
    [
        // IPv4 loopback
        new IPNetwork(IPAddress.Parse("127.0.0.0"), 8),
        // IPv4 private — Class A
        new IPNetwork(IPAddress.Parse("10.0.0.0"), 8),
        // IPv4 private — Class B
        new IPNetwork(IPAddress.Parse("172.16.0.0"), 12),
        // IPv4 private — Class C
        new IPNetwork(IPAddress.Parse("192.168.0.0"), 16),
        // IPv4 link-local
        new IPNetwork(IPAddress.Parse("169.254.0.0"), 16),
        // IPv4 CGNAT
        new IPNetwork(IPAddress.Parse("100.64.0.0"), 10),
    ];

    /// <summary>
    /// Returns <see langword="true"/> when <paramref name="url"/> resolves to a safe, routable address;
    /// returns <see langword="false"/> when the URL is invalid, the host is unresolvable, or any
    /// resolved address is private/loopback/link-local.
    /// </summary>
    public static async Task<bool> IsAllowedAsync(string url, CancellationToken cancellationToken = default)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return false;

        if (uri.Scheme is not "http" and not "https")
            return false;

        IPAddress[] addresses;
        try
        {
            addresses = await Dns.GetHostAddressesAsync(uri.Host, cancellationToken);
        }
        catch (SocketException)
        {
            return false;
        }

        if (addresses.Length == 0)
            return false;

        foreach (var address in addresses)
        {
            if (address.AddressFamily == AddressFamily.InterNetworkV6 &&
                (IPAddress.IsLoopback(address) || address.IsIPv6LinkLocal || address.IsIPv6SiteLocal))
                return false;

            if (address.AddressFamily == AddressFamily.InterNetwork &&
                BlockedNetworks.Any(network => network.Contains(address)))
                return false;
        }

        return true;
    }
}
