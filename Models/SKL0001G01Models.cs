namespace BuzaiManagementApi.Models
{
    /// <summary>
    /// 稼働条件情報のDTO
    /// </summary>
    public class SKL0001G01ConditionDto
    {
        public string Unyocd { get; set; } = string.Empty;
        public string? Moji1 { get; set; }
        public string? Moji2 { get; set; }
        public string? Suji1 { get; set; }
        public string? Suji2 { get; set; }
    }

    /// <summary>
    /// ユーザー・部署・業務担当情報のDTO
    /// </summary>
    public class SKL0001G01UserDto
    {
        public string UserCd { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string BushoCd { get; set; } = string.Empty;
        public string BushoName { get; set; } = string.Empty;
        
        // SAT_GYOMTNT_MB から取得する業務担当情報
        public string GymTntCd { get; set; } = string.Empty;
        public string GymTntMei { get; set; } = string.Empty;
    }

    /// <summary>
    /// 多重ログインチェック用DTO
    /// </summary>
    public class SKL0001G01LoginStatusDto
    {
        public bool IsLogined { get; set; }
        public string SysName { get; set; } = string.Empty;
    }

    /// <summary>
    /// ログインリクエスト用DTO
    /// </summary>
    public class SKL0001G01LoginRequest
    {
        public string UserCd { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty; // 修正: get; に直しました
        public string BushoCd { get; set; } = string.Empty;
        public string BushoName { get; set; } = string.Empty;
    }

    /// <summary>
    /// ユーザーマスタ・部署マスタ・業務担当マスタの個別検証ステータス
    /// </summary>
    public enum UserErrorStatus
    {
        Success = 0,
        UserNotFound = 1,
        BushoNotFound = 2,
        GymTntNotFound = 3
    }

    /// <summary>
    /// ユーザー詳細検証の結果を保持するクラス
    /// </summary>
    public class SKL0001G01UserValidationResult
    {
        public UserErrorStatus Status { get; set; }
        public string Message { get; set; } = string.Empty;
        public SKL0001G01UserDto? UserDto { get; set; }
    }
}