using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using BuzaiManagementApi.Repositories;

namespace BuzaiManagementApi.Controllers
{
    /// <summary>
    /// SKL0101G01（仕分け：保管BOX・保管資材確定）画面に関するAPIコントローラー
    /// </summary>
    [ApiController]
    [Route("api/skl0101")]
    public class SKL0101G01Controller : ControllerBase
    {
        private readonly SKL0101G01Repository _repository;
        private readonly ILogger<SKL0101G01Controller> _logger;

        public SKL0101G01Controller(SKL0101G01Repository repository, ILogger<SKL0101G01Controller> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        /// <summary>
        /// ロケーション中分類マスタを検索します。
        /// </summary>
        /// <param name="chubunruiCd">中分類コード</param>
        /// <returns>中分類マスタ情報</returns>
        [HttpGet("chubunrui")]
        public async Task<IActionResult> GetChuMaster([FromQuery] string chubunruiCd)
        {
            _logger.LogInformation("GetChuMaster 開始: chubunruiCd={ChubunruiCd}", chubunruiCd);

            if (string.IsNullOrEmpty(chubunruiCd))
            {
                _logger.LogWarning("GetChuMaster 警告: 中分類コードが未指定です。");
                return BadRequest("中分類コードを指定してください。");
            }

            try
            {
                var result = await _repository.GetChuMasterAsync(chubunruiCd);
                if (result == null)
                {
                    _logger.LogWarning("GetChuMaster 警告: 指定された中分類マスタが見つかりません (chubunruiCd={ChubunruiCd})", chubunruiCd);
                    return NotFound();
                }

                _logger.LogInformation("GetChuMaster 成功: chubunruiCd={ChubunruiCd}", chubunruiCd);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetChuMaster エラー: chubunruiCd={ChubunruiCd}", chubunruiCd);
                return StatusCode(500, "サーバー内部エラーが発生しました。");
            }
        }

        /// <summary>
        /// ロケーション小分類マスタを検索します。
        /// </summary>
        /// <param name="shobunruiCd">小分類コード</param>
        /// <returns>小分類マスタ情報</returns>
        [HttpGet("shobunrui")]
        public async Task<IActionResult> GetShoMaster([FromQuery] string shobunruiCd)
        {
            _logger.LogInformation("GetShoMaster 開始: shobunruiCd={ShobunruiCd}", shobunruiCd);

            if (string.IsNullOrEmpty(shobunruiCd))
            {
                _logger.LogWarning("GetShoMaster 警告: 小分類コードが未指定です。");
                return BadRequest("小分類コードを指定してください。");
            }

            try
            {
                var result = await _repository.GetShoMasterAsync(shobunruiCd);
                if (result == null)
                {
                    _logger.LogWarning("GetShoMaster 警告: 指定された小分類マスタが見つかりません (shobunruiCd={ShobunruiCd})", shobunruiCd);
                    return NotFound();
                }

                _logger.LogInformation("GetShoMaster 成功: shobunruiCd={ShobunruiCd}", shobunruiCd);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetShoMaster エラー: shobunruiCd={ShobunruiCd}", shobunruiCd);
                return StatusCode(500, "サーバー内部エラーが発生しました。");
            }
        }

        /// <summary>
        /// 保管BOX使用情報を検索します。
        /// </summary>
        /// <param name="hokanBoxCd">保管BOXコード</param>
        /// <returns>保管BOX使用情報</returns>
        [HttpGet("hokanboxsiyojoho")]
        public async Task<IActionResult> GetHokanBoxSiyoJoho([FromQuery] string hokanBoxCd)
        {
            _logger.LogInformation("GetHokanBoxSiyoJoho 開始: hokanBoxCd={HokanBoxCd}", hokanBoxCd);

            if (string.IsNullOrEmpty(hokanBoxCd))
            {
                _logger.LogWarning("GetHokanBoxSiyoJoho 警告: 保管BOXコードが未指定です。");
                return BadRequest("保管BOXコードを指定してください。");
            }

            try
            {
                var result = await _repository.GetHokanBoxSiyoJohoAsync(hokanBoxCd);
                if (result == null)
                {
                    _logger.LogInformation("GetHokanBoxSiyoJoho 情報: 対象データなし（新規登録・確定へ進む判定） (hokanBoxCd={HokanBoxCd})", hokanBoxCd);
                    return NotFound(); // データがない場合は404（新規登録・確定へ進む判定用）
                }

                _logger.LogInformation("GetHokanBoxSiyoJoho 成功: hokanBoxCd={HokanBoxCd}", hokanBoxCd);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetHokanBoxSiyoJoho エラー: hokanBoxCd={HokanBoxCd}", hokanBoxCd);
                return StatusCode(500, "サーバー内部エラーが発生しました。");
            }
        }

        /// <summary>
        /// 保管資材使用情報を検索します。
        /// </summary>
        /// <param name="hokanShizaiCd">保管資材コード</param>
        /// <returns>保管資材使用情報</returns>
        [HttpGet("hokansizaisiyojoho")]
        public async Task<IActionResult> GetHokanShizaiSiyoJoho([FromQuery] string hokanShizaiCd)
        {
            _logger.LogInformation("GetHokanShizaiSiyoJoho 開始: hokanShizaiCd={HokanShizaiCd}", hokanShizaiCd);

            if (string.IsNullOrEmpty(hokanShizaiCd))
            {
                _logger.LogWarning("GetHokanShizaiSiyoJoho 警告: 保管資材コードが未指定です。");
                return BadRequest("保管資材コードを指定してください。");
            }

            try
            {
                var result = await _repository.GetHokanShizaiSiyoJohoAsync(hokanShizaiCd);
                if (result == null)
                {
                    _logger.LogInformation("GetHokanShizaiSiyoJoho 情報: 対象データなし (hokanShizaiCd={HokanShizaiCd})", hokanShizaiCd);
                    return NotFound(); // データがない場合は404
                }

                _logger.LogInformation("GetHokanShizaiSiyoJoho 成功: hokanShizaiCd={HokanShizaiCd}", hokanShizaiCd);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetHokanShizaiSiyoJoho エラー: hokanShizaiCd={HokanShizaiCd}", hokanShizaiCd);
                return StatusCode(500, "サーバー内部エラーが発生しました。");
            }
        }
    }
}