using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using System.Text.Json;
using BuzaiManagementApi.Repositories;
using BuzaiManagementApi.Models;
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

        [HttpGet("emergency-check")]
        public async Task<IActionResult> CheckEmergency([FromQuery] string seihinBango)
        {
            try
            {
                var result = await _repository.CheckEmergencyAsync(seihinBango);
                return Ok(new { kinkyu = result ?? "" });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "CheckEmergency エラー: {Message}", ex.Message);
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpGet("buhinhyo-info")]
        public async Task<IActionResult> GetBuhinHyoInfo([FromQuery] string? seihinBango, [FromQuery] string? buhinKanriNo, [FromQuery] string? hinban)
        {
            try
            {
                var result = await _repository.GetBuhinHyoInfoAsync(seihinBango, buhinKanriNo, hinban);
                if (result == null)
                {
                    return NotFound(new { message = "データがTECHS部品表情報に存在しません" });
                }
                return Ok(result);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "GetBuhinHyoInfo エラー: {Message}", ex.Message);
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpGet("hokanbox-siyo")]
        public async Task<IActionResult> CheckHokanBoxSiyo([FromQuery] string hokanBoxCd)
        {
            try
            {
                var result = await _repository.GetHokanBoxSiyoJohoAsync(hokanBoxCd);
                return Ok(new { seihinBango = result ?? "" });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "CheckHokanBoxSiyo エラー: {Message}", ex.Message);
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] JsonElement jsonElement)
        {
            try
            {
                string rawJson = jsonElement.GetRawText();
                Log.Information("Register受信 生JSON: {RawJson}", rawJson);

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var request = JsonSerializer.Deserialize<RegisterRequestModel>(rawJson, options);

                if (request == null)
                {
                    return BadRequest(new { success = false, message = "リクエストデータの解析に失敗しました" });
                }

                Log.Information("デシリアライズ結果確認 -> HokanShizaiCd='{ShizaiCd}', HokanShizaiMeisho='{ShizaiMei}'", 
                    request?.HokanShizaiCd, request?.HokanShizaiMeisho);

                // マッピング処理（BOX / 資材）※ ! で null ではないことをコンパイラに伝える
                string finalBoxCd = !string.IsNullOrEmpty(request!.HokanBoxCd) ? request.HokanBoxCd : request.ShobunruiCode;
                string finalBoxMeisho = !string.IsNullOrEmpty(request.HokanBoxMeisho) ? request.HokanBoxMeisho : request.ShobunruiMeisho;
                string finalShizaiCd = !string.IsNullOrEmpty(request.HokanShizaiCd) ? request.HokanShizaiCd : request.ChubunruiCode;
                string finalShizaiMeisho = !string.IsNullOrEmpty(request.HokanShizaiMeisho) ? request.HokanShizaiMeisho : request.ChubunruiMeisho;

                // 2. 伝票番号から「製品No」と「品番」を正しく切り出す
                string searchSeihinBango = request.SeihinBango;
                if (searchSeihinBango.Length > 9) {
                    searchSeihinBango = searchSeihinBango.Substring(0, 9);
                }

                string? hinban = null;
                if (!string.IsNullOrEmpty(request.DenpyoBango) && request.DenpyoBango.Length >= 13)
                {
                    hinban = request.DenpyoBango.Substring(9, 4);
                }

                // 3. 部品表情報取得
                var buhinInfo = await _repository.GetBuhinHyoInfoAsync(
                    seihinBango: searchSeihinBango, 
                    buhinKanriNo: request.BuhinKanriNo, 
                    hinban: hinban
                );

                if (buhinInfo == null)
                {
                    return BadRequest(new { success = false, message = "データがTECHS部品表情報に存在しません" });
                }

                // 4. 取得した情報を変数に詰める
                string buhinKanriNo = buhinInfo.BuhinKanriNo ?? "";
                string hinmei = buhinInfo.Hinmei ?? "";
                string katashiki = buhinInfo.KoshikiZuhyo ?? "";
                decimal tehaiSu = buhinInfo.TehaiSu;
                string tehaiTaniMei = buhinInfo.TehaiTaniMei ?? "個";

                // 5. Fe工場製番とSeqの切り出し
                string feKojoSeiban = searchSeihinBango;
                string feSeq = hinban ?? "0001";
                
                // 6. 現品管理番号の採番
                var (genpkanribango, status) = await _repository.GetSaibanCodeAsync("SL現品管理番号", "KN", request.UserCd);
                if (status != 0 || string.IsNullOrEmpty(genpkanribango))
                {
                    return BadRequest(new { success = false, message = "現品管理Noの取得に失敗しました システム管理者に連絡してください" });
                }

                // Androidから送られてきた緊急品区分（なければ "1"）
                string kinkyuKbn = !string.IsNullOrEmpty(request.KinkyuKbn) ? request.KinkyuKbn : "1";

                // 7. 登録実行
                var (isSuccess, errorMessage) = await _repository.RegisterDataAsync(
                    denpyoShubetsuCd: request.DenpyoShubetsuCd,
                    denpyoBango: request.DenpyoBango,
                    seihinBango: buhinInfo?.SeihinBango ?? request.SeihinBango,
                    buhinKanriNo: buhinKanriNo,
                    hokanBoxCd: finalBoxCd,
                    hokanBoxMeisho: finalBoxMeisho,
                    hokanShizaiCd: finalShizaiCd,
                    hokanShizaiMeisho: finalShizaiMeisho,
                    konkaiShikyuSu: request.KonkaiShikyuSu,
                    userCd: request.UserCd,
                    genpkanribango: genpkanribango,
                    feKojoSeiban: feKojoSeiban,
                    feSeq: feSeq,
                    hinban: buhinInfo?.Hinban ?? hinban ?? "",
                    hinmei: hinmei,
                    katashiki: katashiki,
                    tehaiTaniMei: tehaiTaniMei,
                    tehaiSu: tehaiSu,
                    shikyuruiRuisekiSu: 0m,
                    hinbanGrpCd: buhinInfo?.HinbanGroupCode ?? "",
                    hinbanGrpNm: buhinInfo?.HinbanGroupName ?? "",
                    zuban: buhinInfo?.Zuban ?? "",
                    seiban: buhinInfo?.Seiban ?? "",
                    juchuNo: buhinInfo?.JuchuNo ?? "",
                    seihinSu: buhinInfo?.SeihinSu ?? 0m,
                    seihinBunCd: buhinInfo?.SeihinBunruiCode ?? "",
                    seihinBunNm: buhinInfo?.SeihinBunruiMei ?? "",
                    nonyuCd: buhinInfo?.NonyusakiCode ?? "",
                    nonyuNm: buhinInfo?.NonyusakiSeishikiMei ?? "",
                    tokuiCd: buhinInfo?.TokuisakiCode ?? "",
                    tokuiNm: buhinInfo?.TokuisakiSeishikiMei ?? "",
                    seihinHinmei: hinmei,
                    kinkyuKbn: kinkyuKbn // ← ここに最後の引数を追加！
                );

                if (isSuccess)
                {
                    return Ok(new { success = true });
                }
                else
                {
                    return BadRequest(new { success = false, message = errorMessage ?? "登録処理に失敗しました" });
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Register エラー: {Message}", ex.Message);
                return StatusCode(500, new { success = false, message = "システム管理者に連絡してください" });
            }
        }

        [HttpPost("delete")]
        public async Task<IActionResult> Delete([FromBody] DeleteRequestModel request)
        {
            try
            {
                bool success = await _repository.DeleteDataAsync(
                    request.SeihinBango, 
                    request.HokanBoxCd, 
                    request.HokanShizaiCd
                );
                return Ok(new { success });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Delete 処理エラー: {Message}", ex.Message);
                return StatusCode(500, new { message = ex.Message });
            }
        }
    }
}