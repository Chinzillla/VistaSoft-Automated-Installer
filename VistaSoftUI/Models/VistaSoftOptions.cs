namespace VistaSoftUI.Models
{
    public sealed class VistaSoftInstallOptions
    {
        public bool? AutoSetup { get; set; }
        public bool? ConnectMode { get; set; }
        public string? OperationMode { get; set; }
        public string? PracticeName { get; set; }
        public string? PracticeAddress { get; set; }
        public bool? InstallScanXPlugin { get; set; }
        public bool? InstallScanXClassicPlugin { get; set; }
        public bool? InstallCamXPlugin { get; set; }
        public bool? InstallSensorXPlugin { get; set; }
        public bool? InstallTwainPlugin { get; set; }
    }
}
