using BuzaiManagementApi.Models;
using BuzaiManagementApi.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BuzaiManagementApi.Controllers
{
    /// <summary>
    /// Reader.txt のキャッシュデータに基づくバーコードリーダーデバイスマッピングAPIコントローラー
    /// </summary>
    [ApiController]
    [Route("api/barcodereaders")]
    public class BarcodeReaderMasterController : ControllerBase
    {
        private readonly BarcodeReaderMasterRepository _repository;
        private readonly ILogger<BarcodeReaderMasterController> _logger;

        public BarcodeReaderMasterController(BarcodeReaderMasterRepository repository, ILogger<BarcodeReaderMasterController> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        /// <summary>
        /// Reader.txt から取得したリーダー一覧を返すエンドポイント
        /// GET: api/barcodereaders
        /// </summary>
        [HttpGet]
        public IActionResult GetBarcodeReaders()
        {
            _logger.LogInformation("GetBarcodeReaders 開始（Reader.txt データの返却）");

            try
            {
                var rawList = _repository.GetBarcodeReaderList();
                // 型を BarcodeReaderMasterDto ではなく DeviceMappingDto に統一
                var readers = rawList?.ToList() ?? new List<DeviceMappingDto>();

                _logger.LogInformation("GetBarcodeReaders 成功: 取得件数={Count}件", readers.Count);
                foreach (var r in readers)
                {
                    _logger.LogInformation("  -> Reader.txtデータ: 端末IP=[{Ip}], MAC=[{Mac}], 銘称=[{Meisho}]", 
                        r.TargetIpAddress, r.TargetMacAddress, r.TargetMeisho);
                }

                return Ok(readers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetBarcodeReaders エラー: {Message}", ex.Message);
                return StatusCode(500, new { message = "Reader.txtデータの取得に失敗しました。", error = ex.Message });
            }
        }

        /// <summary>
        /// Reader.txt の内容からデバイスマッピングを取得する
        /// GET: api/barcodereaders/mapping?ipAddress=192.168.137.48
        /// </summary>
        [HttpGet("mapping")]
        public IActionResult GetDeviceMapping(string? ipAddress = null)
        {
            string targetIp = ipAddress;
            if (string.IsNullOrEmpty(targetIp))
            {
                targetIp = HttpContext.Connection.RemoteIpAddress?.MapToIPv4().ToString() ?? string.Empty;
            }

            _logger.LogInformation("GetDeviceMapping 開始: 対象IP = {Ip}", targetIp);

            try
            {
                var rawList = _repository.GetBarcodeReaderList();
                var readers = rawList?.ToList() ?? new List<DeviceMappingDto>();

                _logger.LogInformation("GetDeviceMapping: Reader.txt 取得総数={Count}件", readers.Count);

                // ★ 修正: リストをループし、Reader.txtに記載の端末IP（TargetIpAddress）と、リクエストされた対象IP（targetIp）が一致する行を厳格に探す
                var matchedReader = readers.FirstOrDefault(r => 
                    !string.IsNullOrEmpty(r.TargetIpAddress) &&
                    r.TargetIpAddress.Trim().Equals(targetIp.Trim(), StringComparison.OrdinalIgnoreCase) &&
                    !string.IsNullOrEmpty(r.TargetMacAddress)
                );

                if (matchedReader == null)
                {
                    _logger.LogWarning("GetDeviceMapping 警告: 指定されたIP ({Ip}) に一致するデバイス設定がReader.txtに見つかりませんでした。", targetIp);
                    return NotFound(new { success = false, message = $"指定されたIP ({targetIp}) に一致するリーダー設定が見つかりませんでした。" });
                }

                var mapping = new
                {
                    success = true,
                    targetIpAddress = targetIp,
                    targetMacAddress = matchedReader.TargetMacAddress,
                    targetMeisho = matchedReader.TargetMeisho
                };

                _logger.LogInformation("GetDeviceMapping 成功: 対象IP={Ip}, 選択Mac={Mac}, 銘称={Meisho}", 
                    targetIp, mapping.targetMacAddress, mapping.targetMeisho);
                return Ok(mapping);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetDeviceMapping エラー: {Message}", ex.Message);
                return StatusCode(500, new { success = false, message = "デバイスマッピングの取得に失敗しました。", error = ex.Message });
            }
        }
    }
}