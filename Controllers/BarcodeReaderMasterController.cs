using BuzaiManagementApi.Models;
using BuzaiManagementApi.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;

namespace BuzaiManagementApi.Controllers
{
    /// <summary>
    /// バーコードリーダーマスタ（SAT_BCREADER_M）に関するAPIコントローラー
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
        /// バーコードリーダーマスタ（SAT_BCREADER_M）の一覧を取得するエンドポイント
        /// GET: api/barcodereaders
        /// </summary>
        [HttpGet]
        public IActionResult GetBarcodeReaders()
        {
            _logger.LogInformation("GetBarcodeReaders 開始（バーコードリーダーマスタ取得）");

            try
            {
                var readers = _repository.GetBarcodeReaderList()?.ToList() ?? new System.Collections.Generic.List<BarcodeReaderMasterDto>();

                _logger.LogInformation("GetBarcodeReaders 成功: 取得件数={Count}件", readers.Count);
                
                // Android側が List<BarcodeReaderMasterDto> としてパースできるようそのまま返す
                return Ok(readers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetBarcodeReaders エラー: {Message}", ex.Message);
                return StatusCode(500, new { message = "バーコードリーダーマスタの取得に失敗しました。", error = ex.Message });
            }
        }
    }
}