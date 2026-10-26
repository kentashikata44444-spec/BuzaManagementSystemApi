using System.Text.Json.Serialization;

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
        public string? SeihinHinmei { get; set; }  // 製品品名
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

    // 登録リクエストモデル
    public class RegisterRequestModel
    {
        [JsonPropertyName("DENPYOSHUBETSUCD")]
        public string DenpyoShubetsuCd { get; set; } = string.Empty;

        [JsonPropertyName("DENPYOBANGO")]
        public string DenpyoBango { get; set; } = string.Empty;

        [JsonPropertyName("SEIHINBANGO")]
        public string SeihinBango { get; set; } = string.Empty;

        [JsonPropertyName("BUHINKANRINO")]
        public string BuhinKanriNo { get; set; } = string.Empty;

        [JsonPropertyName("HOKANBOXCD")]
        public string HokanBoxCd { get; set; } = string.Empty;

        [JsonPropertyName("HOKANBOXMEISHO")]
        public string HokanBoxMeisho { get; set; } = string.Empty;

        [JsonPropertyName("HOKANSIZAICD")]
        public string HokanShizaiCd { get; set; } = string.Empty;

        [JsonPropertyName("HOKANSIZAIMEISHO")]
        public string HokanShizaiMeisho { get; set; } = string.Empty;

        [JsonPropertyName("CHUBUNRUICODE")]
        public string ChubunruiCode { get; set; } = string.Empty;

        [JsonPropertyName("CHUBUNRUIMEISHO")]
        public string ChubunruiMeisho { get; set; } = string.Empty;

        [JsonPropertyName("SHOBUNRUICODE")]
        public string ShobunruiCode { get; set; } = string.Empty;

        [JsonPropertyName("SHOBUNRUIMEISHO")]
        public string ShobunruiMeisho { get; set; } = string.Empty;

        [JsonPropertyName("KONKAISHIKYUSU")]
        public decimal KonkaiShikyuSu { get; set; }

        [JsonPropertyName("USERCD")]
        public string UserCd { get; set; } = string.Empty;

        public string? KinkyuKbn { get; set; }
    }

    // 削除リクエストモデル
    public class DeleteRequestModel
    {
        public string DenpyoBango { get; set; } = string.Empty;
        public string SeihinBango { get; set; } = string.Empty;
        public string HokanBoxCd { get; set; } = string.Empty;
        public string HokanShizaiCd { get; set; } = string.Empty;
        public bool HasDetailData { get; set; }
    }
}