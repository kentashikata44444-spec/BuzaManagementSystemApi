using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using BuzaiManagementApi.Repositories;
using Serilog;

namespace BuzaiManagementApi.Controllers
{
    [ApiController]
    [Route("api/skl0104")]
    public class SKL0104G01Controller : ControllerBase
    {
        private readonly SKL0104G01Repository _repository;

        public SKL0104G01Controller(SKL0104G01Repository repository)
        {
            _repository = repository;
        }

        // 緊急品チェック用エンドポイント
        [HttpGet("emergency-check")]
        public async Task<IActionResult> CheckEmergency([FromQuery] string seihinBango)
        {
            try
            {
                var result = await _repository.CheckEmergencyAsync(seihinBango);
                Log.Information("CheckEmergency 成功: seihinBango={SeihinBango}, result={Result}", seihinBango, result);
                return Ok(new { kinkyu = result ?? "" });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "CheckEmergency エラー: {Message}", ex.Message);
                return StatusCode(500, new { message = ex.Message });
            }
        }

        // 削除処理エンドポイント
        [HttpPost("delete")]
        public async Task<IActionResult> Delete([FromBody] DeleteRequestModel request)
        {
            try
            {
                Log.Information("Delete 処理開始: seihinBango={SeihinBango}", request.SeihinBango);
                bool success = await _repository.DeleteDataAsync(
                    request.Genpkanribango, 
                    request.SeihinBango, 
                    request.HokanBoxCd, 
                    request.HokanShizaiCd, 
                    request.HasDetailData
                );

                Log.Information("Delete 処理成功: seihinBango={SeihinBango}", request.SeihinBango);
                return Ok(new { success });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Delete 処理エラー: {Message}", ex.Message);
                return StatusCode(500, new { message = ex.Message });
            }
        }
    }

    public class DeleteRequestModel
    {
        public string Genpkanribango { get; set; } = string.Empty;
        public string SeihinBango { get; set; } = string.Empty;
        public string HokanBoxCd { get; set; } = string.Empty;
        public string HokanShizaiCd { get; set; } = string.Empty;
        public bool HasDetailData { get; set; }
    }
}