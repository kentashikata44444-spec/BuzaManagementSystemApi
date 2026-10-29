using System.Text.Json.Serialization;

namespace BuzaiManagementApi.Models
{
    public class BarcodeReaderMasterDto
    {
        [JsonPropertyName("HOZON_NM")]
        public string HOZON_NM { get; set; } = string.Empty;

        [JsonPropertyName("MAC_ADS")]
        public string? MAC_ADS { get; set; }

        [JsonPropertyName("MEISHO")]
        public string? MEISHO { get; set; }

        [JsonPropertyName("SEIZOMOTO")]
        public string? SEIZOMOTO { get; set; }

        [JsonPropertyName("MODERU_NO")]
        public string? MODERU_NO { get; set; }

        [JsonPropertyName("SHIRIARU_NO")]
        public string? SHIRIARU_NO { get; set; }
    }
}