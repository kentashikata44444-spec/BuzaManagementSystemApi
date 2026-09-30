namespace BuzaiManagementApi.Models
{
    public class SKL0001G01ConditionDto
    {
        public string Unyocd { get; set; } = string.Empty;
        public string? Moji1 { get; set; }
        public string? Moji2 { get; set; }
        public string? Suji1 { get; set; }
        public string? Suji2 { get; set; }
    }

    public class SKL0001G01UserDto
    {
        public string UserCd { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string BushoCd { get; set; } = string.Empty;
        public string BushoName { get; set; } = string.Empty;
        
        // ▼ 追加：SAT_GYOMTNT_MB から取得する業務担当情報
        public string GymTntCd { get; set; } = string.Empty;
        public string GymTntMei { get; set; } = string.Empty;
    }

    public class SKL0001G01LoginStatusDto
    {
        public bool IsLogined { get; set; }
        public string SysName { get; set; } = string.Empty;
    }

    public class SKL0001G01LoginRequest
    {
        public string UserCd { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string BushoCd { get; set; } = string.Empty;
        public string BushoName { get; set; } = string.Empty;
    }
}