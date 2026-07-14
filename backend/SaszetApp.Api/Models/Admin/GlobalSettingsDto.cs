namespace SaszetApp.Api.Models.Admin
{
    public class GlobalSettingsDto
    {
        public int GlobalScanLimit { get; set; } = 5;
        public int ScanLimitRollingDays { get; set; } = 7;
    }
}
