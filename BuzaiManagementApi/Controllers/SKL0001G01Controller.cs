using BuzaiManagementApi.Models;
using BuzaiManagementApi.Repositories;
using Microsoft.AspNetCore.Mvc;
using System;

namespace BuzaiManagementApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SKL0001G01Controller : ControllerBase
    {
        private readonly SKL0001G01Repository _repository;

        public SKL0001G01Controller(SKL0001G01Repository repository)
        {
            _repository = repository;
        }

        /// <summary>
        /// 稼働条件（稼働フラグ・稼働時間）を取得するエンドポイント
        /// GET: api/SKL0001G01/conditions
        /// </summary>
        [HttpGet("conditions")]
        public IActionResult GetConditions()
        {
            try
            {
                var operationFlag = _repository.GetOperationFlag();
                var operationTime = _repository.GetOperationTime();

                return Ok(new
                {
                    operationFlag = operationFlag,
                    operationTime = operationTime
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "サーバーエラーが発生しました。", error = ex.Message });
            }
        }

        /// <summary>
        /// ログイン認証・多重ログインチェック・情報取得を行うエンドポイント
        /// POST: api/SKL0001G01/login
        /// </summary>
        [HttpPost("login")]
        public IActionResult Login([FromBody] SKL0001G01LoginRequest request)
        {
            // パスワードのチェックを外し、ユーザーコードのみをチェックする
            if (string.IsNullOrEmpty(request?.UserCd))
            {
                return BadRequest(new { success = false, message = "ユーザーコードが空です。" });
            }

            try
            {
                // 1. ユーザー情報の取得（存在チェックのみ、パスワード照合は行わない）
                var userInfo = _repository.GetUserInfo(request.UserCd);
                if (userInfo == null)
                {
                    return Ok(new { success = false, message = "ユーザー登録されていません" + "\n" + "ユーザー登録してください" });
                }

                // 2. 多重ログインチェック
                var loginStatus = _repository.GetLoginStatus(request.UserCd);
                if (loginStatus.IsLogined)
                {
                    return Ok(new { success = false, message = $"既にログインしています" + "\n" + "ログアウト後再度バーコードを読み込んでください" });
                }

                // 3. ログイン情報の登録（セッション保持など）
                request.UserName = userInfo.UserName;
                request.BushoCd = userInfo.BushoCd;
                request.BushoName = userInfo.BushoName;

                bool isInserted = _repository.InsertLoginInfo(request);
                if (!isInserted)
                {
                    return StatusCode(500, new { success = false, message = "ログイン情報の登録に失敗しました。" });
                }

                return Ok(new
                {
                    success = true,
                    message = "ログインに成功しました。",
                    data = userInfo
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "ログイン情報の登録に失敗しました" + "\n" + "システム管理者に問合せ願います", error = ex.Message });
            }
        }
    }
}