using Xunit;

namespace ShortcutLib.Tests;

public class ShortcutEditTests
{
    [Fact]
    public void Edit_ModifyTarget_ChangesTarget()
    {
        byte[] original = Shortcut.Create(new ShortcutOptions { Target = @"C:\Windows\notepad.exe" });

        byte[] edited = Shortcut.Edit(original, o => o.Target = @"C:\Windows\System32\cmd.exe");

        var options = Shortcut.Open(edited);
        Assert.Equal(@"C:\Windows\System32\cmd.exe", options.Target);
    }

    [Fact]
    public void Edit_AddArguments_AddsArguments()
    {
        byte[] original = Shortcut.Create(new ShortcutOptions { Target = @"C:\test.exe" });

        byte[] edited = Shortcut.Edit(original, o => o.Arguments = "--verbose");

        var options = Shortcut.Open(edited);
        Assert.Equal("--verbose", options.Arguments);
    }

    [Fact]
    public void Edit_ChangeWindowStyle_UpdatesWindowStyle()
    {
        byte[] original = Shortcut.Create(new ShortcutOptions
        {
            Target = @"C:\test.exe",
            WindowStyle = ShortcutWindowStyle.Normal
        });

        byte[] edited = Shortcut.Edit(original, o => o.WindowStyle = ShortcutWindowStyle.Maximized);

        var options = Shortcut.Open(edited);
        Assert.Equal(ShortcutWindowStyle.Maximized, options.WindowStyle);
    }

    [Fact]
    public void Edit_SetRunAsAdmin_SetsFlag()
    {
        byte[] original = Shortcut.Create(new ShortcutOptions { Target = @"C:\test.exe" });

        byte[] edited = Shortcut.Edit(original, o => o.RunAsAdmin = true);

        var options = Shortcut.Open(edited);
        Assert.True(options.RunAsAdmin);
    }

    [Fact]
    public void Edit_ModifyDescription_UpdatesDescription()
    {
        byte[] original = Shortcut.Create(new ShortcutOptions
        {
            Target = @"C:\test.exe",
            Description = "Old description"
        });

        byte[] edited = Shortcut.Edit(original, o => o.Description = "New description");

        var options = Shortcut.Open(edited);
        Assert.Equal("New description", options.Description);
    }

    [Fact]
    public void Edit_PreservesUnmodifiedFields()
    {
        var creationTime = new DateTime(2024, 1, 15, 12, 0, 0, DateTimeKind.Utc);

        byte[] original = Shortcut.Create(new ShortcutOptions
        {
            Target = @"C:\Windows\notepad.exe",
            Description = "My shortcut",
            WorkingDirectory = @"C:\Windows",
            WindowStyle = ShortcutWindowStyle.Maximized,
            HotkeyKey = 0x54,
            HotkeyModifiers = HotkeyModifiers.Control | HotkeyModifiers.Alt,
            CreationTime = creationTime,
            FileSize = 193536,
            IconIndex = 3
        });

        // Only modify arguments, everything else should be preserved
        byte[] edited = Shortcut.Edit(original, o => o.Arguments = "--new-arg");

        var options = Shortcut.Open(edited);
        Assert.Equal(@"C:\Windows\notepad.exe", options.Target);
        Assert.Equal("--new-arg", options.Arguments);
        Assert.Equal("My shortcut", options.Description);
        Assert.Equal(@"C:\Windows", options.WorkingDirectory);
        Assert.Equal(ShortcutWindowStyle.Maximized, options.WindowStyle);
        Assert.Equal(0x54, options.HotkeyKey);
        Assert.Equal(HotkeyModifiers.Control | HotkeyModifiers.Alt, options.HotkeyModifiers);
        Assert.Equal(creationTime, options.CreationTime);
        Assert.Equal(193536u, options.FileSize);
        Assert.Equal(3, options.IconIndex);
    }

    [Fact]
    public void Edit_NullData_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => Shortcut.Edit(null!, o => o.Target = "test"));
    }

    [Fact]
    public void Edit_NullModify_ThrowsArgumentNullException()
    {
        byte[] lnk = Shortcut.Create(new ShortcutOptions { Target = @"C:\test.exe" });
        Assert.Throws<ArgumentNullException>(() => Shortcut.Edit(lnk, null!));
    }
}
