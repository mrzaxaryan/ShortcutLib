using ShortcutLib;
using Xunit;

namespace ShortcutLib.Tests;

public class NetworkPathEnhancedTests
{
    [Fact]
    public void DeviceName_RoundTrips()
    {
        byte[] lnk = Shortcut.Create(new ShortcutOptions
        {
            Target = @"\\server\share\file.txt",
            LinkInfo = new LinkInfo
            {
                Network = new NetworkPathInfo
                {
                    ShareName = @"\\server\share",
                    CommonPathSuffix = "file.txt",
                    DeviceName = "Z:"
                }
            }
        });
        var options = Shortcut.Open(lnk);
        Assert.NotNull(options.LinkInfo?.Network);
        Assert.Equal("Z:", options.LinkInfo!.Network!.DeviceName);
    }

    [Fact]
    public void DeviceName_NullByDefault()
    {
        byte[] lnk = Shortcut.Create(new ShortcutOptions
        {
            Target = @"\\server\share\file.txt",
            LinkInfo = new LinkInfo
            {
                Network = new NetworkPathInfo
                {
                    ShareName = @"\\server\share",
                    CommonPathSuffix = "file.txt"
                }
            }
        });
        var options = Shortcut.Open(lnk);
        Assert.Null(options.LinkInfo!.Network!.DeviceName);
    }

    [Fact]
    public void NetworkProviderType_RoundTrips()
    {
        byte[] lnk = Shortcut.Create(new ShortcutOptions
        {
            Target = @"\\server\share\file.txt",
            LinkInfo = new LinkInfo
            {
                Network = new NetworkPathInfo
                {
                    ShareName = @"\\server\share",
                    CommonPathSuffix = "file.txt",
                    NetworkProviderType = NetworkProviderTypes.Lanman
                }
            }
        });
        var options = Shortcut.Open(lnk);
        Assert.Equal(NetworkProviderTypes.Lanman, options.LinkInfo!.Network!.NetworkProviderType);
    }

    [Fact]
    public void NetworkProviderType_NullByDefault()
    {
        byte[] lnk = Shortcut.Create(new ShortcutOptions
        {
            Target = @"\\server\share\file.txt",
            LinkInfo = new LinkInfo
            {
                Network = new NetworkPathInfo
                {
                    ShareName = @"\\server\share",
                    CommonPathSuffix = "file.txt"
                }
            }
        });
        var options = Shortcut.Open(lnk);
        Assert.Null(options.LinkInfo!.Network!.NetworkProviderType);
    }

    [Fact]
    public void DeviceName_And_NetworkProviderType_Combined_RoundTrip()
    {
        byte[] lnk = Shortcut.Create(new ShortcutOptions
        {
            Target = @"\\server\share\docs\report.docx",
            LinkInfo = new LinkInfo
            {
                Network = new NetworkPathInfo
                {
                    ShareName = @"\\server\share",
                    CommonPathSuffix = @"docs\report.docx",
                    DeviceName = "X:",
                    NetworkProviderType = NetworkProviderTypes.Dfs
                }
            }
        });
        var options = Shortcut.Open(lnk);
        Assert.Equal(@"\\server\share", options.LinkInfo!.Network!.ShareName);
        Assert.Equal(@"docs\report.docx", options.LinkInfo.Network.CommonPathSuffix);
        Assert.Equal("X:", options.LinkInfo.Network.DeviceName);
        Assert.Equal(NetworkProviderTypes.Dfs, options.LinkInfo.Network.NetworkProviderType);
    }
}
