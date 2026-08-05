using VistaSoftUI.Models;
using VistaSoftUI.Services;

namespace VistaSoftUI.Tests;

public sealed class OptionsFileServiceTests
{
    [Fact]
    public async Task ImportAndExportPreservesEditableAndAdditionalOptions()
    {
        string optionsPath = Path.Combine(Path.GetTempPath(), $"vistasoft-{Guid.NewGuid():N}.options");

        try
        {
            await File.WriteAllLinesAsync(
                optionsPath,
                [
                    "mode=unattended",
                    "unattendedmodeui=minimal",
                    "autosetup=0",
                    "operationmode=client",
                    "hostoverride=server.example.test:3113",
                    "installer-language=de",
                    "InstallTwainPlugin=1",
                ]);

            VistaSoftInstallOptions options = await OptionsFileService.ReadAsync(optionsPath);
            string exportedContent = OptionsFileService.CreateFileContent(options);

            Assert.False(options.AutoSetup);
            Assert.Equal("minimal", options.UnattendedModeUi);
            Assert.Equal("server.example.test:3113", options.AdditionalOptions["hostoverride"]);
            Assert.Contains("unattendedmodeui=minimal", exportedContent);
            Assert.Contains("autosetup=0", exportedContent);
            Assert.Contains("hostoverride=server.example.test:3113", exportedContent);
            Assert.Contains("installer-language=de", exportedContent);
        }
        finally
        {
            File.Delete(optionsPath);
        }
    }

    [Fact]
    public void AdditionalOptionsCannotOverrideManagedOptions()
    {
        VistaSoftInstallOptions options = new()
        {
            AutoSetup = true,
        };
        options.AdditionalOptions["autosetup"] = "0";

        string content = OptionsFileService.CreateFileContent(options);

        Assert.Contains("autosetup=1", content);
        Assert.DoesNotContain("autosetup=0", content);
        Assert.Equal(1, content.Split("autosetup=", StringSplitOptions.None).Length - 1);
    }

    [Fact]
    public void MultilineValuesAreFlattenedBeforeWriting()
    {
        VistaSoftInstallOptions options = new()
        {
            PracticeAddress = "First line\r\nSecond line",
        };

        string content = OptionsFileService.CreateFileContent(options);

        Assert.Contains("address=First line Second line", content);
    }
}
