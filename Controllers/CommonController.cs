using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using BuzaiManagementApi.Repositories;

namespace BuzaiManagementApi.Controllers
{
    /// <summary>
    /// アプリケーション全体で共通して使用される機能を提供するAPIコントローラー
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class CommonController : ControllerBase
    {
        private readonly CommonRepository _commonRepository;
        private readonly ILogger<CommonController> _logger;

        public CommonController(CommonRepository commonRepository, ILogger<CommonController> logger)
        {
            _commonRepository = commonRepository;
            _logger = logger;
        }

        /// <summary>
        /// 指定されたユーザーのログイン情報を削除します。
        /// DELETE: api/Common/login/{userCd}
        /// </summary>
        /// <param name="userCd">ユーザーコード</param>
        /// <returns>削除処理結果</returns>
        [HttpDelete("login/{userCd}")]
        public async Task<IActionResult> DeleteLogin(string userCd)
        {
            _logger.LogInformation("DeleteLogin 開始: userCd={UserCd}", userCd);

            if (string.IsNullOrWhiteSpace(userCd))
            {
                _logger.LogWarning("DeleteLogin 警告: ユーザーIDが未指定です。");
                return BadRequest(new { success = false, message = "ユーザーIDが指定されていません。" });
            }

            try
            {
                int affectedRows = await _commonRepository.DeleteLoginInfoAsync(userCd);

                _logger.LogInformation("DeleteLogin 成功: userCd={UserCd}, 削除件数={AffectedRows}", userCd, affectedRows);
                return Ok(new { success = true, deletedRows = affectedRows });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DeleteLogin エラー: userCd={UserCd}, メッセージ={Message}", userCd, ex.Message);
                return StatusCode(500, new { success = false, message = "サーバーエラーが発生しました。", error = ex.Message });
            }
        }
    }
}