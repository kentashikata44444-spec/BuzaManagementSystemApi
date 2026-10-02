using BuzaiManagementApi.Models;
using BuzaiManagementApi.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;

namespace BuzaiManagementApi.Controllers
{
    /// <summary>
    /// SKL0001G01（ログイン・起動条件チェック）画面に関するAPIコントローラー
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class SKL0001G01Controller : ControllerBase
    {
        private readonly SKL0001G01Repository _repository;
        private readonly ILogger<SKL0001G01Controller> _logger;

        public SKL0001G01Controller(SKL0001G01Repository repository, ILogger<SKL0001G01Controller> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        /// <summary>
        /// 稼働条件（稼働フラグ・稼働時間）を取得するエンドポイント
        /// GET: api/SKL0001G01/conditions
        /// </summary>
        /// <returns>稼働条件情報</returns>
        [HttpGet("conditions")]
        public IActionResult GetConditions()
        {
            _logger.LogInformation("GetConditions 開始");

            try
            {
                var operationFlag = _repository.GetOperationFlag();
                var operationTime = _repository.GetOperationTime();

                _logger.LogInformation("GetConditions 成功: operationFlag={Flag}", operationFlag);
                return Ok(new
                {
                    operationFlag = operationFlag,
                    operationTime = operationTime
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetConditions エラー: {Message}", ex.Message);
                return StatusCode(500, new { message = "サーバーエラーが発生しました。", error = ex.Message });
            }
        }

        /// <summary>
        /// ログイン認証・多重ログインチェック・情報取得を行うエンドポイント
        /// POST: api/SKL0001G01/login
        /// </summary>
        /// <param name="request">ログインリクエスト情報</param>
        /// <returns>ログイン処理結果</returns>
        [HttpPost("login")]
        public IActionResult Login([FromBody] SKL0001G01LoginRequest request)
        {
            _logger.LogInformation("Login 開始: userCd={UserCd}", request?.UserCd);

            // パスワードのチェックを外し、ユーザーコードのみをチェックする
            if (string.IsNullOrEmpty(request?.UserCd))
            {
                _logger.LogWarning("Login 警告: ユーザーコードが空です。");
                return BadRequest(new { success = false, message = "ユーザーコードが空です。" });
            }

            try
            {
                // 1. ユーザー情報の取得（存在チェックのみ、パスワード照合は行わない）
                var userInfo = _repository.GetUserInfo(request.UserCd);
                if (userInfo == null)
                {
                    _logger.LogWarning("Login 警告: ユーザー登録されていません (userCd={UserCd})", request.UserCd);
                    return Ok(new { success = false, message = "ユーザー登録されていません" + "\n" + "ユーザー登録してください" });
                }

                // 2. 多重ログインチェック
                var loginStatus = _repository.GetLoginStatus(request.UserCd);
                if (loginStatus.IsLogined)
                {
                    _logger.LogWarning("Login 警告: 既にログイン中です (userCd={UserCd}, sysName={SysName})", request.UserCd, loginStatus.SysName);
                    return Ok(new { success = false, message = $"既にログインしています" + "\n" + "ログアウト後再度バーコードを読み込んでください" });
                }

                // 3. ログイン情報の登録（セッション保持など）
                request.UserName = userInfo.UserName;
                request.BushoCd = userInfo.BushoCd;
                request.BushoName = userInfo.BushoName;

                bool isInserted = _repository.InsertLoginInfo(request);
                if (!isInserted)
                {
                    _logger.LogError("Login エラー: ログイン情報の登録に失敗しました (userCd={UserCd})", request.UserCd);
                    return StatusCode(500, new { success = false, message = "ログイン情報の登録に失敗しました。" });
                }

                _logger.LogInformation("Login 成功: userCd={UserCd}, userName={UserName}", request.UserCd, request.UserName);
                return Ok(new
                {
                    success = true,
                    message = "ログインに成功しました。",
                    data = userInfo
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login エラー: userCd={UserCd}, メッセージ={Message}", request?.UserCd, ex.Message);
                return StatusCode(500, new { success = false, message = "ログイン情報の登録に失敗しました" + "\n" + "システム管理者に問合せ願います", error = ex.Message });
            }
        }
    }
}