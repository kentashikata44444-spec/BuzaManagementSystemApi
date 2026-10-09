namespace BuzaiManagementApi.Models
{
    // 緊急品チェック結果
    public class EmergencyCheckResult
    {
        public string? Kinkyu { get; set; }
    }

    // 保管BOX使用情報
    public class HokanBoxSiyoJoho
    {
        public string? SeihinBango { get; set; }
    }

    // 保管資材使用情報
    public class HokanShizaiSiyoJoho
    {
        public string? SeihinBango { get; set; }
    }

    // 部材所在明細製品No検索結果
    public class GenkanribangoCountResult
    {
        public int Count { get; set; }
    }

    // 部品表情報（詳細データ）
    public class BuhinDataModel
    {
        public string? HinbanGroupCode { get; set; }
        public string? HinbanGroupName { get; set; }
        public string? Hinban { get; set; }
        public string? Zuban { get; set; }
        public string? Hinmei { get; set; }
        public string? KoshikiZuhyo { get; set; }
        public string? BuhinKanriNo { get; set; }
        public string? TokuisakiCode { get; set; }
        public string? TokuisakiSeishikiMei { get; set; }
        public string? Seiban { get; set; }
        public string? JuchuNo { get; set; }
        public decimal SeihinSu { get; set; }
        public string? SeihinBunruiCode { get; set; }
        public string? SeihinBunruiMei { get; set; }
        public string? NonyusakiCode { get; set; }
        public string? NonyusakiSeishikiMei { get; set; }
        public decimal TehaiSu { get; set; }
        public string? TehaiTaniMei { get; set; }
        public string? SeihinBango { get; set; }
    }
}