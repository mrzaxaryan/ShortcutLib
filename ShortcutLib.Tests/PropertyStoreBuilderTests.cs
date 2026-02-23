using ShortcutLib;
using Xunit;

namespace ShortcutLib.Tests;

public class PropertyStoreBuilderTests
{
    [Fact]
    public void Build_NoProperties_ReturnsTerminal()
    {
        var builder = new PropertyStoreBuilder();
        byte[] result = builder.Build();
        // Should be just 4 bytes of terminal (0x00000000)
        Assert.Equal(4, result.Length);
        Assert.Equal(0u, BitConverter.ToUInt32(result, 0));
    }

    [Fact]
    public void Build_AppUserModelId_ContainsVersion()
    {
        var builder = new PropertyStoreBuilder { AppUserModelId = "MyApp" };
        byte[] result = builder.Build();
        // Should contain "1SPS" version marker (0x53505331)
        Assert.True(ContainsUInt32(result, 0x53505331u));
    }

    [Fact]
    public void Build_AppUserModelId_ContainsFormatId()
    {
        var builder = new PropertyStoreBuilder { AppUserModelId = "MyApp" };
        byte[] result = builder.Build();
        // Should contain the AppUserModel format GUID bytes
        Guid expectedGuid = new("9F4C2855-9F79-4B39-A8D0-E1D42DE1D5F3");
        byte[] guidBytes = expectedGuid.ToByteArray();
        Assert.True(ContainsBytes(result, guidBytes));
    }

    [Fact]
    public void Build_AppUserModelId_EndsWithTerminal()
    {
        var builder = new PropertyStoreBuilder { AppUserModelId = "MyApp" };
        byte[] result = builder.Build();
        // Last 4 bytes should be 0x00000000 (terminal storage)
        Assert.Equal(0u, BitConverter.ToUInt32(result, result.Length - 4));
    }

    [Fact]
    public void Build_AppUserModelId_RoundTripsThroughPropertyStoreData()
    {
        var builder = new PropertyStoreBuilder { AppUserModelId = "ShortcutLib.Test" };
        byte[] lnk = Shortcut.Create(new ShortcutOptions
        {
            Target = @"C:\test.exe",
            PropertyStoreData = builder.Build()
        });
        var options = Shortcut.Open(lnk);
        Assert.NotNull(options.PropertyStoreData);
        Assert.True(options.PropertyStoreData!.Length > 4);
    }

    [Fact]
    public void Build_BoolProperty_ContainsVtBool()
    {
        var builder = new PropertyStoreBuilder { PreventPinning = true };
        byte[] result = builder.Build();
        // VT_BOOL = 11 (0x000B) should appear
        Assert.True(ContainsUInt16(result, 11));
    }

    [Fact]
    public void Build_GuidProperty_ContainsVtClsid()
    {
        var builder = new PropertyStoreBuilder { ToastActivatorCLSID = Guid.Parse("12345678-1234-1234-1234-123456789ABC") };
        byte[] result = builder.Build();
        // VT_CLSID = 72 (0x0048) should appear
        Assert.True(ContainsUInt16(result, 72));
    }

    [Fact]
    public void Build_MultipleProperties_AllPresent()
    {
        var builder = new PropertyStoreBuilder
        {
            AppUserModelId = "Test.App",
            PreventPinning = true,
            RelaunchCommand = "test.exe --relaunch"
        };
        byte[] result = builder.Build();
        // VT_LPWSTR = 31 (0x001F) should appear (string properties)
        Assert.True(ContainsUInt16(result, 31));
        // VT_BOOL = 11 should appear
        Assert.True(ContainsUInt16(result, 11));
        // Terminal
        Assert.Equal(0u, BitConverter.ToUInt32(result, result.Length - 4));
    }

    [Fact]
    public void Build_StringValue_ContainsUtf16Text()
    {
        var builder = new PropertyStoreBuilder { AppUserModelId = "MyCompany.MyApp" };
        byte[] result = builder.Build();
        // The string "MyCompany.MyApp" in UTF-16LE should appear in the output
        byte[] expected = System.Text.Encoding.Unicode.GetBytes("MyCompany.MyApp");
        Assert.True(ContainsBytes(result, expected));
    }

    [Fact]
    public void Build_GuidValue_ContainsGuidBytes()
    {
        Guid testGuid = Guid.Parse("AABBCCDD-1122-3344-5566-778899AABBCC");
        var builder = new PropertyStoreBuilder { ToastActivatorCLSID = testGuid };
        byte[] result = builder.Build();
        Assert.True(ContainsBytes(result, testGuid.ToByteArray()));
    }

    private static bool ContainsUInt32(byte[] data, uint value)
    {
        byte[] needle = BitConverter.GetBytes(value);
        return ContainsBytes(data, needle);
    }

    private static bool ContainsUInt16(byte[] data, ushort value)
    {
        byte[] needle = BitConverter.GetBytes(value);
        return ContainsBytes(data, needle);
    }

    private static bool ContainsBytes(byte[] haystack, byte[] needle)
    {
        for (int i = 0; i <= haystack.Length - needle.Length; i++)
        {
            bool match = true;
            for (int j = 0; j < needle.Length; j++)
            {
                if (haystack[i + j] != needle[j])
                {
                    match = false;
                    break;
                }
            }
            if (match) return true;
        }
        return false;
    }
}
