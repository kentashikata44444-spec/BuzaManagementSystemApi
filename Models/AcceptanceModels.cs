using System.Text.Json.Serialization;

namespace BuzaiManagementApi.Models
{
    // 受入検査詳細・明細情報DTO
    public class AcceptanceDetailDto
    {
        [JsonPropertyName("chuumonNo")]
        public string? ChuumonNo { get; set; }

        [JsonPropertyName("bunnoDenpyoNo")]
        public string? BunnoDenpyoNo { get; set; }

        [JsonPropertyName("kanriNo")]
        public string? KanriNo { get; set; }

        [JsonPropertyName("shiharaiSeiMei")]
        public string? ShiharaiSeiMei { get; set; }

        [JsonPropertyName("hatsuTantoMei")]
        public string? HatsuTantoMei { get; set; }

        [JsonPropertyName("hinban")]
        public string? Hinban { get; set; }

        [JsonPropertyName("seihinNo")]
        public string? SeihinNo { get; set; }

        [JsonPropertyName("nnyuSeiMei")]
        public string? NnyuSeiMei { get; set; }

        [JsonPropertyName("hinmei")]
        public string? Hinmei { get; set; }

        [JsonPropertyName("kshikiSunpo")]
        public string? KshikiSunpo { get; set; }

        [JsonPropertyName("zuban")]
        public string? Zuban { get; set; }

        [JsonPropertyName("buhinBiko")]
        public string? BuhinBiko { get; set; }

        [JsonPropertyName("hatsuSu")]
        public string? HatsuSu { get; set; }
    }

    // 全数合格・受入書き込み用リクエストモデル
    public class AcceptanceWriteRequestModel
    {
        [JsonPropertyName("chuumonNo")]
        public string ChuumonNo { get; set; } = string.Empty;

        [JsonPropertyName("bunnoDenpyoNo")]
        public string? BunnoDenpyoNo { get; set; }

        [JsonPropertyName("nounyuusu")]
        public int Nounyuusu { get; set; }

        [JsonPropertyName("goukakusu")]
        public int Goukakusu { get; set; }

        [JsonPropertyName("fugoukakusu")]
        public int Fugoukakusu { get; set; }

        [JsonPropertyName("loginUserCd")]
        public string LoginUserCd { get; set; } = string.Empty;
    }
}