using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using BuzaiManagementApi.Repositories;
using BuzaiManagementApi.Models;

namespace BuzaiManagementApi.Controllers
{
    [ApiController]
    [Route("api/skl0111")] // ★ Android側のパス (api/skl0111) に合わせる
    public class SKL0111G01Controller : ControllerBase
    {
        private readonly SKL0111G01Repository _repository;
        private readonly ILogger<SKL0111G01Controller> _logger;

        public SKL0111G01Controller(SKL0111G01Repository repository, ILogger<SKL0111G01Controller> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        /// <summary>
        /// 1. 初期データ取得 / 対象データ取得
        /// 対応するAndroidパス: GET /api/skl0111/meisai?seihinBango=...
        /// </summary>
        [HttpGet("meisai")]
        public async Task<IActionResult> GetMeisaiInfo([FromQuery] string seihinBango)
        {
            try
            {
                _logger.LogInformation("SKL0111G01 GetMeisaiInfo 開始: seihinBango={SeihinBango}", seihinBango);
                var list = await _repository.GetHokanShizaiTargetAsync(seihinBango);
                return Ok(new { success = true, data = list });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetMeisaiInfo エラー: {Message}", ex.Message);
                return StatusCode(500, new { success = false, message = "システムエラーが発生しました" });
            }
        }

        /// <summary>
        /// 2. 保管エリアQRコード読み込み時チェック（ロケーション大分類マスタ検索 ＆ 使用情報確認）
        /// 対応するAndroidパス: GET /api/skl0111/dai_master?daiCode=...
        /// </summary>
        [HttpGet("dai_master")]
        public async Task<IActionResult> GetDaiMaster([FromQuery] string daiCode)
        {
            try
            {
                _logger.LogInformation("SKL0111G01 GetDaiMaster 開始: daiCode={DaiCode}", daiCode);

                // ★ 修正: "DAI" のプレフィックスチェックを外し、空でないことのチェックに変更する
                if (string.IsNullOrEmpty(daiCode))
                {
                    return BadRequest(new { success = false, message = "保管エリアが読み込まれていません" });
                }

                // 2. ロケーション大分類マスタ検索
                var master = await _repository.GetRokeshoNdaiMasterAsync(daiCode);
                if (master == null)
                {
                    return BadRequest(new { success = false, message = "読み込まれた保管エリアが存在しません" });
                }

                return Ok(new 
                { 
                    success = true, 
                    sokoCd = master.SokoCd,
                    sokoMeisho = master.SokoMeisho,
                    hokanEriaCd = master.DaibunruiCd,
                    hokanEriaMeisho = master.DaibunruiMeisho
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetDaiMaster エラー: {Message}", ex.Message);
                return StatusCode(500, new { success = false, message = "システムエラーが発生しました" });
            }
        }

/// <summary>
        /// 3. 保管資材QRコード読み込み時処理（存在チェック、明細更新、エリア使用情報登録）
        /// 対応するAndroidパス: POST /api/skl0111/register
        /// </summary>
        [HttpPost("register")]
        public async Task<IActionResult> RegisterHokanArea([FromBody] MaterialScanRequestModel request)
        {
            try
            {
                _logger.LogInformation("SKL0111G01 RegisterHokanArea 開始: ShizaiCd={ShizaiCd}", request.HokanShizaiCd);

                // 1. 部材所在明細情報に該当資材が存在するか確認
                bool exists = await _repository.CheckBuzaishozaiShizaiExistsAsync(request.SeihinBango, request.HokanShizaiCd);
                if (!exists)
                {
                    // 対象外の場合もOkの形（success = false）で返すことでメッセージを確実に表示できます
                    return Ok(new { success = false, message = "読み込まれた保管資材は、対象外です" });
                }

                // 2. トランザクション実行（明細更新 ＆ エリア使用情報登録）
                var (isSuccess, message) = await _repository.UpdateAreaAndRegisterHokanEriaAsync(
                    request.SeihinBango,
                    request.HokanShizaiCd,
                    request.SokoCd,
                    request.SokoMeisho,
                    request.HokanEriaCd,
                    request.HokanEriaMeisho,
                    request.UserCd
                );

                if (!isSuccess)
                {
                    // ★ 修正: BadRequest ではなく Ok で返すことで、Android側が詳細な message を受け取れるようにする
                    return Ok(new { success = false, message = message });
                }

                // 3. 更新後の最新ターゲット一覧を取得して返却
                var updatedList = await _repository.GetHokanShizaiTargetAsync(request.SeihinBango);

                return Ok(new { success = true, message = "登録完了しました", data = updatedList });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RegisterHokanArea エラー: {Message}", ex.Message);
                return StatusCode(500, new { success = false, message = "システムエラーが発生しました" });
            }
        }
    }
}