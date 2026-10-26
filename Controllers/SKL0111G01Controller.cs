using System;
using System.Linq; // ← 追加
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using BuzaiManagementApi.Models;
using BuzaiManagementApi.Repositories;

namespace BuzaiManagementApi.Controllers
{
    [ApiController]
    [Route("api/skl0110")]
    public class SKL0110G01Controller : ControllerBase
    {
        private readonly SKL0110G01Repository _repository;
        private readonly ILogger<SKL0110G01Controller> _logger;

        public SKL0110G01Controller(SKL0110G01Repository repository, ILogger<SKL0110G01Controller> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        /// <summary>
        /// 初期表示時の保管情報および対象データの取得
        /// </summary>
        [HttpGet("hokan-info")]
        public async Task<IActionResult> GetHokanInfo([FromQuery] string seihinBango)
        {
            _logger.LogInformation("API GetHokanInfo 呼び出し: seihinBango={SeihinBango}", seihinBango);
            try
            {
                if (string.IsNullOrEmpty(seihinBango))
                {
                    return BadRequest(new { success = false, message = "製品No.が指定されていません" });
                }

                var targets = await _repository.GetHokanZaimuTargetAsync(seihinBango);
                var targetList = targets.ToList(); // ← AsList() から ToList() に変更

                if (targetList.Count == 0)
                {
                    _logger.LogWarning("保管資材確定対象データが存在しません: seihinBango={SeihinBango}", seihinBango);
                    return Ok(new { success = false, message = "保管資材確定対象データが存在しません", count = 0 });
                }

                return Ok(new {
                    success = true,
                    count = targetList.Count,
                    hokanBoxCd = targetList[0].HokanBoxCd,
                    hokanBoxMeisho = targetList[0].HokanBoxMeisho
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API GetHokanInfo エラー: seihinBango={SeihinBango}", seihinBango);
                return StatusCode(500, new { success = false, message = "システム管理者に問合せ願います" });
            }
        }

        /// <summary>
        /// 中分類マスタ検索（保管資材コードの検証と名称取得）
        /// </summary>
        [HttpGet("master-chu")]
        public async Task<IActionResult> GetChuMaster([FromQuery] string chubunruiCd)
        {
            _logger.LogInformation("API GetChuMaster 呼び出し: chubunruiCd={ChubunruiCd}", chubunruiCd);
            try
            {
                if (string.IsNullOrEmpty(chubunruiCd))
                {
                    return BadRequest(new { success = false, message = "中分類コードが指定されていません" });
                }

                string? meisho = await _repository.GetChuMasterMeishoAsync(chubunruiCd);
                if (string.IsNullOrEmpty(meisho))
                {
                    return Ok(new { success = false, message = "読み込まれた保管資材がマスタに存在しません\n確認してください" });
                }

                return Ok(new { success = true, chubunruiMeisho = meisho });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API GetChuMaster エラー");
                return StatusCode(500, new { success = false, message = "システム管理者に問合せ願います" });
            }
        }

        /// <summary>
        /// 保管エリア・保管資材の確定登録処理（更新＆登録トランザクション）
        /// </summary>
        [HttpPost("register")]
        public async Task<IActionResult> RegisterHokanArea([FromBody] SKL0110RegisterRequestModel request)
        {
            _logger.LogInformation("API RegisterHokanArea 呼び出し: seihinBango={SeihinBango}, hokanBoxCd={HokanBoxCd}, hokanShizaiCd={HokanShizaiCd}", 
                request.SeihinBango, request.HokanBoxCd, request.HokanShizaiCd);

            try
            {
                string? usingSeihinBango = await _repository.GetHokanBoxSiyoJohoAsync(request.HokanBoxCd);
                if (!string.IsNullOrEmpty(usingSeihinBango) && usingSeihinBango != request.SeihinBango)
                {
                    return Ok(new { success = false, message = "読み込まれた保管BOXが製品No.内に存在しません\n保管BOXを確認してください" });
                }

                string? shizaiMeisho = await _repository.GetChuMasterMeishoAsync(request.HokanShizaiCd);
                if (string.IsNullOrEmpty(shizaiMeisho))
                {
                    return Ok(new { success = false, message = "読み込まれた保管資材がマスタに存在しません\n確認してください" });
                }

                var (isSuccess, errorMessage) = await _repository.UpdateAndRegisterHokanAreaAsync(
                    request.SeihinBango, request.HokanBoxCd, request.HokanShizaiCd, shizaiMeisho, request.UserCd);

                if (!isSuccess)
                {
                    return Ok(new { success = false, message = errorMessage ?? "保管資材使用情報の登録に失敗しました\nシステム管理者に連絡してください" });
                }

                return Ok(new { success = true, message = "保管エリアを確定しました" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API RegisterHokanArea エラー");
                return StatusCode(500, new { success = false, message = "システム管理者に連絡してください" });
            }
        }
    }
}