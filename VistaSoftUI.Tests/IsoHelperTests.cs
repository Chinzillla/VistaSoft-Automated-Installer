namespace VistaSoftUI.Tests;

public sealed class IsoHelperTests
{
    [Theory]
    [InlineData(@"\\.\CDROM3", "cdrom3")]
    [InlineData(@"\Device\CdRom3", "cdrom3")]
    public void CdRomIdentityMatchesVirtualDiskAndDosDevicePaths(string path, string expected)
    {
        Assert.Equal(expected, MountedDriveLocator.GetCdRomDeviceIdentity(path));
    }

    [Theory]
    [InlineData("DÜRR DENTAL SE")]
    [InlineData("Duerr Dental SE")]
    [InlineData("Air Techniques, Inc.")]
    public void KnownVistaSoftPublishersAreAllowed(string publisher)
    {
        Assert.True(AuthenticodeVerifier.IsAllowedVistaSoftPublisher(publisher));
    }

    [Fact]
    public void UnrelatedPublisherIsRejected()
    {
        Assert.False(AuthenticodeVerifier.IsAllowedVistaSoftPublisher("Unrelated Software Company"));
    }
}
