namespace SaszetApp.Api.Models.Admin
{
    public class UserLimitDto
    {
        public string UserId { get; set; } = string.Empty;
        public int MaxScans { get; set; }
    }
}
