using System;

namespace BuzaiManagementApi.Models
{
    /// <summary>
    /// 保管資材ターゲットモデル（部材所在明細情報資材検索用）
    /// </summary>
    public class HokanShizaiTargetModel
    {
        public string HokanShizaiCd { get; set; } = string.Empty;
        public string HokanShizaiMeisho { get; set; } = string.Empty;
    }

    /// <summary>
    /// ロケーション大分類マスタ検索結果モデル
    /// </summary>
    public class RokeshoNdaiMasterModel
    {
        public string SokoCd { get; set; } = string.Empty;
        public string SokoMeisho { get; set; } = string.Empty;
        public string DaibunruiCd { get; set; } = string.Empty;
        public string DaibunruiMeisho { get; set; } = string.Empty;
    }

    /// <summary>
    /// 保管エリア読み込みリクエストモデル
    /// </summary>
    public class AreaScanRequestModel
    {
        public string SeihinBango { get; set; } = string.Empty;
        public string AreaCd { get; set; } = string.Empty;
        public string UserCd { get; set; } = string.Empty;
    }

    /// <summary>
    /// 保管資材読み込み・登録リクエストモデル
    /// </summary>
    public class MaterialScanRequestModel
    {
        public string SeihinBango { get; set; } = string.Empty;
        public string HokanShizaiCd { get; set; } = string.Empty;
        public string SokoCd { get; set; } = string.Empty;
        public string SokoMeisho { get; set; } = string.Empty;
        public string HokanEriaCd { get; set; } = string.Empty;
        public string HokanEriaMeisho { get; set; } = string.Empty;
        public string UserCd { get; set; } = string.Empty;
    }
}