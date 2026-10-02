namespace BuzaiManagementApi.Models
{
    // ロケーション中分類マスタ
    public class SltRokeshonchuM
    {
        public string ChubunruiCd { get; set; } = string.Empty;
        public string? ChubunruiMeisho { get; set; }
        public string JotaiKbn { get; set; } = string.Empty;
    }

    // ロケーション小分類マスタ
    public class SltRokeshonshoM
    {
        public string ShobunruiCd { get; set; } = string.Empty;
        public string? ShobunruiMeisho { get; set; }
        public string JotaiKbn { get; set; } = string.Empty;
    }

    // 保管BOX使用情報
    public class SltHokanBoxSiyoJoho
    {
        public string HokanBoxCd { get; set; } = string.Empty;
        public string SeihinBango { get; set; } = string.Empty;
        public string? HokanShizaiMeisho { get; set; }
        public string JotaiKbn { get; set; } = string.Empty;
    }

    // 保管資材使用情報
    public class SltHokanShizaiSiyoJoho
    {
        public string HokanShizaiCd { get; set; } = string.Empty;
        public string SeihinBango { get; set; } = string.Empty;
        public string? HokanShizaiMeisho { get; set; }
        public string JotaiKbn { get; set; } = string.Empty;
    }
}