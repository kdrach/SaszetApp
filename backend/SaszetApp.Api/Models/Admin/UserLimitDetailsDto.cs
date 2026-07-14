namespace SaszetApp.Api.Models.Admin
{
    public class UserLimitDetailsDto
    {
        public string UserId { get; set; } = string.Empty;
        public int MaxScans { get; set; }
        public int Usage { get; set; }
        public string LastReset { get; set; } = string.Empty;
    }
}
