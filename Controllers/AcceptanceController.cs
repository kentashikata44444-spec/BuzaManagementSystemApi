using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using BuzaiManagementApi.Repositories;
using BuzaiManagementApi.Models;
using Serilog;

namespace BuzaiManagementApi.Controllers
{
    [ApiController]
    [Route("api/acceptance")]
    public class AcceptanceController : ControllerBase
    {
        private readonly AcceptanceRepository _repository;

        public AcceptanceController(AcceptanceRepository repository)
        {
            _repository = repository;
        }

        // ① 発注伝票No（注文番号）をキーに受入検査詳細情報を取得
        [HttpGet("detail")]
        public async Task<IActionResult> GetAcceptanceDetail([FromQuery] string chuumonNo)
        {
            try
            {
                Log.Information("GetAcceptanceDetail 処理開始: chuumonNo={ChuumonNo}", chuumonNo);
                var result = await _repository.GetAcceptanceDetailAsync(chuumonNo);
                
                if (result == null)
                {
                    return Ok(new { success = false, message = "受入検査詳細情報がありません", data = (object?)null });
                }
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "GetAcceptanceDetail エラー: {Message}", ex.Message);
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // ② 分納伝票Noをキーに受入検査明細情報を取得
        [HttpGet("detail/bunno")]
        public async Task<IActionResult> GetAcceptanceDetailByBunno([FromQuery] string bunnoDenpyoNo)
        {
            try
            {
                Log.Information("GetAcceptanceDetailByBunno 処理開始: bunnoDenpyoNo={BunnoDenpyoNo}", bunnoDenpyoNo);
                var result = await _repository.GetAcceptanceDetailByBunnoAsync(bunnoDenpyoNo);

                if (result == null)
                {
                    return Ok(new { success = false, message = "この伝票は無効です", data = (object?)null });
                }
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "GetAcceptanceDetailByBunno エラー: {Message}", ex.Message);
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // 6. 納入可否調査
        [HttpGet("check-availability")]
        public async Task<IActionResult> CheckAvailability([FromQuery] string denpyoNo)
        {
            try
            {
                Log.Information("CheckAvailability 処理開始: denpyoNo={DenpyoNo}", denpyoNo);
                bool isAvailable = await _repository.CheckAvailabilityAsync(denpyoNo);

                if (!isAvailable)
                {
                    return Ok(new { success = true, isAvailable = false, message = "納入完了しているため編集不可です" });
                }
                return Ok(new { success = true, isAvailable = true });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "CheckAvailability エラー: {Message}", ex.Message);
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // 緊急品検索
        [HttpGet("emergency")]
        public async Task<IActionResult> CheckEmergency()
        {
            try
            {
                Log.Information("CheckEmergency 処理開始");
                bool hasEmergency = await _repository.CheckEmergencyItemsAsync();
                return Ok(new { hasEmergency });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "CheckEmergency エラー: {Message}", ex.Message);
                return StatusCode(500, new { hasEmergency = false, message = ex.Message });
            }
        }

        // データ登録 (INSERT): 受入検査詳細情報の追加 (SNT_UKEIREKENSA_S)
        [HttpPost("detail/s")]
        public async Task<IActionResult> InsertAcceptanceDetailS([FromBody] AcceptanceDetailDto? request)
        {
            try
            {
                if (request == null || string.IsNullOrEmpty(request.ChuumonNo))
                {
                    return BadRequest(new { success = false, message = "リクエストデータが不正です" });
                }

                Log.Information("InsertAcceptanceDetailS 処理開始: chuumonNo={ChuumonNo}", request.ChuumonNo);
                string userId = "SYSTEM"; 
                bool success = await _repository.InsertAcceptanceDetailSAsync(request, userId);
                return Ok(new { success, message = success ? null : "詳細情報の登録に失敗しました" });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "InsertAcceptanceDetailS エラー: {Message}", ex.Message);
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // データ登録 (INSERT): 受入検査明細情報の追加 (SNT_UKEIREKENSA_M)
        [HttpPost("detail/m")]
        public async Task<IActionResult> InsertAcceptanceDetailM([FromBody] AcceptanceDetailDto? request)
        {
            try
            {
                if (request == null || string.IsNullOrEmpty(request.ChuumonNo))
                {
                    return BadRequest(new { success = false, message = "リクエストデータが不正です" });
                }

                Log.Information("InsertAcceptanceDetailM 処理開始: chuumonNo={ChuumonNo}", request.ChuumonNo);
                string userId = "SYSTEM";
                bool success = await _repository.InsertAcceptanceDetailMAsync(request, userId);
                return Ok(new { success, message = success ? null : "明細情報の登録に失敗しました" });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "InsertAcceptanceDetailM エラー: {Message}", ex.Message);
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // 7. 全数合格として書き込み
        [HttpPut("update/zenkan")]
        public async Task<IActionResult> UpdateZenkan([FromBody] AcceptanceWriteRequestModel? request)
        {
            try
            {
                if (request == null || string.IsNullOrEmpty(request.ChuumonNo))
                {
                    return BadRequest(new { success = false, message = "リクエストデータが不正です" });
                }

                Log.Information("UpdateZenkan 処理開始: chuumonNo={ChuumonNo}", request.ChuumonNo);
                (bool isSuccess, string? errorMessage) = await _repository.UpdateZenkanAsync(request);
                return Ok(new { success = isSuccess, message = errorMessage });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "UpdateZenkan エラー: {Message}", ex.Message);
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // 8. 受入書き込み（未検査・不合格あり等）
        [HttpPut("update/ukeire")]
        public async Task<IActionResult> UpdateUkeire([FromBody] AcceptanceWriteRequestModel? request)
        {
            try
            {
                if (request == null || string.IsNullOrEmpty(request.ChuumonNo))
                {
                    return BadRequest(new { success = false, message = "リクエストデータが不正です" });
                }

                Log.Information("UpdateUkeire 処理開始: chuumonNo={ChuumonNo}", request.ChuumonNo);
                (bool isSuccess, string? errorMessage) = await _repository.UpdateUkeireAsync(request);
                return Ok(new { success = isSuccess, message = errorMessage });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "UpdateUkeire エラー: {Message}", ex.Message);
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }
    }
}