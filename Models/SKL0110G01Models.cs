using System.ComponentModel.DataAnnotations;

namespace BuzaiManagementApi.Models
{
    // 初期表示時の保管資材確定対象検索結果モデル
    public class HokanZaimuTargetModel
    {
        public string HokanBoxCd { get; set; } = string.Empty;
        public string HokanBoxMeisho { get; set; } = string.Empty;
    }

    // 登録・更新リクエストモデル
    public class SKL0110RegisterRequestModel
    {
        [Required]
        public string SeihinBango { get; set; } = string.Empty;

        [Required]
        public string HokanBoxCd { get; set; } = string.Empty;

        [Required]
        public string HokanShizaiCd { get; set; } = string.Empty; // 中分類コード（保管資材コード）

        public string UserCd { get; set; } = string.Empty;
    }
}