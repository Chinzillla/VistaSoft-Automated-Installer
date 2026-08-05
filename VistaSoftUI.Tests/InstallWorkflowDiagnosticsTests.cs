using VistaSoftUI.Services;

namespace VistaSoftUI.Tests;

public sealed class InstallWorkflowDiagnosticsTests
{
    [Fact]
    public void VendorExitCode255GetsAUsefulExplanation()
    {
        const string output = """
            WORKFLOW_RESULT=FAILED
            WORKFLOW_ERROR=VISTASOFT_INSTALLER_FAILED
            WORKFLOW_ERROR_DETAIL=The verified VistaSoft installer started but reported a failure.
            VISTASOFT_INSTALLER_EXIT_CODE=255
            """;

        string message = InstallWorkflowDiagnostics.CreateFailureMessage(
            80,
            output,
            string.Empty,
            @"C:\Logs\install.log");

        Assert.Contains("VistaSoft installer code: 255", message);
        Assert.Contains("undocumented VistaSoft installer failure", message);
        Assert.Contains(@"C:\Logs\install.log", message);
    }

    [Fact]
    public void LegacyWorkflowExitCode255IsStillExplained()
    {
        string message = InstallWorkflowDiagnostics.CreateFailureMessage(
            255,
            string.Empty,
            string.Empty,
            null);

        Assert.Contains("code 255", message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("diagnostic log could not be written", message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void WindowsSecurityCode225ExplainsTheBlockAndNextStep()
    {
        string message = InstallWorkflowDiagnostics.CreateFailureMessage(
            225,
            string.Empty,
            string.Empty,
            @"C:\Logs\security.log");

        Assert.Contains("antivirus", message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Protection history", message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("unexpected code", message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void PathCode3ExplainsThatAPathIsMissing()
    {
        string message = InstallWorkflowDiagnostics.CreateFailureMessage(
            3,
            string.Empty,
            string.Empty,
            @"C:\Logs\path.log");

        Assert.Contains("path or folder", message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Select the ISO again", message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void NestedAccessDeniedCodeExplainsAdministratorPermissions()
    {
        const string output = """
            HELPER_ERROR=ISO_MOUNT_FAILED
            Could not open the selected ISO as a virtual disk. Error 5: Access is denied.
            WORKFLOW_ERROR_DETAIL=Windows could not mount and identify the selected VistaSoft ISO.
            """;

        string message = InstallWorkflowDiagnostics.CreateFailureMessage(
            70,
            output,
            string.Empty,
            @"C:\Logs\permission.log");

        Assert.Contains("Windows error 5", message);
        Assert.Contains("permissions issue", message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("administrator", message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void VistaSoftFatalCode1603IncludesRecoveryAdvice()
    {
        const string output = """
            WORKFLOW_ERROR_DETAIL=The verified VistaSoft installer started but reported a failure.
            VISTASOFT_INSTALLER_EXIT_CODE=1603
            """;

        string message = InstallWorkflowDiagnostics.CreateFailureMessage(
            80,
            output,
            string.Empty,
            @"C:\Logs\fatal.log");

        Assert.Contains("VistaSoft installer code: 1603", message);
        Assert.Contains("Restart Windows", message);
        Assert.Contains("diagnostic log", message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("1641")]
    [InlineData("3010")]
    public void RestartRequiredSuccessCodesAreHumanReadable(string installerCode)
    {
        string message = InstallWorkflowDiagnostics.CreateSuccessMessage(
            $"VISTASOFT_INSTALLER_EXIT_CODE={installerCode}",
            @"C:\Logs\success.log");

        Assert.Contains("succeeded", message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("restart", message, StringComparison.OrdinalIgnoreCase);
    }
}
