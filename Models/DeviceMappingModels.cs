namespace BuzaiManagementApi.Models
{
    public class DeviceMappingDto
    {
        public string TargetIpAddress { get; set; } = string.Empty;
        public string? TargetMacAddress { get; set; }
        public string? TargetMeisho { get; set; }
    }
}