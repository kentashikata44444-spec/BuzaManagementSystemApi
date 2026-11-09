using System;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using Serilog;
using BuzaiManagementApi.Models;

namespace BuzaiManagementApi.Repositories
{
    public class AcceptanceRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public AcceptanceRepository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        // 1. 注文番号（発注伝票No）による受入検査詳細情報取得 (4-1-2)
        public async Task<AcceptanceDetailDto?> GetAcceptanceDetailAsync(string chuumonNo)
        {
            Log.Information("GetAcceptanceDetailAsync 処理開始: chuumonNo={ChuumonNo}", chuumonNo);
            try
            {
                using var connection = _connectionFactory.CreatePostgresConnection();
                if (connection.State != ConnectionState.Open)
                    connection.Open();

                string query = @"
                    SELECT 
                        ""CHUUMONNO"" AS ChuumonNo,
                        ""BUNDENNO"" AS BunnoDenpyoNo,
                        ""KANRINO"" AS KanriNo,
                        ""TOKIHORIGAIKBN"" AS ShiharaiSeiMei,
                        ""KOUBAITANTOU"" AS HatsuTantoMei,
                        ""HINBAN"" AS Hinban,
                        ""SEIHINBANGO"" AS SeihinNo,
                        ""NOUNYUUSAKIMEI"" AS NnyuSeiMei,
                        ""HINMEI"" AS Hinmei,
                        ""KATASHIKISUNPOUs"" AS KshikiSunpo,
                        ""ZUBAN"" AS Zuban,
                        ""BUHINKANRINO"" AS BuhinBiko,
                        CAST(""HATCHUUSU"" AS TEXT) AS HatsuSu
                    FROM ""COMPLEMENTARY"".""SNT_UKEIREKENSA_S""
                    WHERE ""CHUUMONNO"" = @ChuumonNo
                    LIMIT 1;";

                var result = await connection.QueryFirstOrDefaultAsync<AcceptanceDetailDto>(query, new { ChuumonNo = chuumonNo });
                return result;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "GetAcceptanceDetailAsync エラー: {Message}", ex.Message);
                throw;
            }
        }

        // 2. 分納伝票Noによる受入検査明細情報取得 (4-1-1)
        public async Task<AcceptanceDetailDto?> GetAcceptanceDetailByBunnoAsync(string bunnoDenpyoNo)
        {
            Log.Information("GetAcceptanceDetailByBunnoAsync 処理開始: bunnoDenpyoNo={BunnoDenpyoNo}", bunnoDenpyoNo);
            try
            {
                using var connection = _connectionFactory.CreatePostgresConnection();
                if (connection.State != ConnectionState.Open)
                    connection.Open();

                string query = @"
                    SELECT 
                        ""CHUUMONNO"" AS ChuumonNo,
                        ""BUNDENNO"" AS BunnoDenpyoNo,
                        ""KANRINO"" AS KanriNo,
                        ""TOKIHORIGAIKBN"" AS ShiharaiSeiMei,
                        ""KOUBAITANTOU"" AS HatsuTantoMei,
                        ""HINBAN"" AS Hinban,
                        ""SEIHINBANGO"" AS SeihinNo,
                        ""NOUNYUUSAKIMEI"" AS NnyuSeiMei,
                        ""HINMEI"" AS Hinmei,
                        ""KATASHIKISUNPOUs"" AS KshikiSunpo,
                        ""ZUBAN"" AS Zuban,
                        ""BUHINKANRINO"" AS BuhinBiko,
                        CAST(""HATCHUUSU"" AS TEXT) AS HatsuSu
                    FROM ""COMPLEMENTARY"".""SNT_UKEIREKENSA_M""
                    WHERE ""BUNDENNO"" = @BunnoDenpyoNo
                    LIMIT 1;";

                var result = await connection.QueryFirstOrDefaultAsync<AcceptanceDetailDto>(query, new { BunnoDenpyoNo = bunnoDenpyoNo });
                return result;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "GetAcceptanceDetailByBunnoAsync エラー: {Message}", ex.Message);
                throw;
            }
        }

        // 3. オリジナル伝票の受入検査明細情報を取得 (CHUUMONNO = :注文番号 AND BUNDENNO IS NULL)
        public async Task<AcceptanceDetailDto?> GetAcceptanceDetailOriginalAsync(string chuumonNo)
        {
            Log.Information("GetAcceptanceDetailOriginalAsync 処理開始: chuumonNo={ChuumonNo}", chuumonNo);
            try
            {
                using var connection = _connectionFactory.CreatePostgresConnection();
                if (connection.State != ConnectionState.Open)
                    connection.Open();

                string query = @"
                    SELECT 
                        ""CHUUMONNO"" AS ChuumonNo,
                        ""BUNDENNO"" AS BunnoDenpyoNo,
                        ""KANRINO"" AS KanriNo,
                        ""TOKIHORIGAIKBN"" AS ShiharaiSeiMei,
                        ""KOUBAITANTOU"" AS HatsuTantoMei,
                        ""HINBAN"" AS Hinban,
                        ""SEIHINBANGO"" AS SeihinNo,
                        ""NOUNYUUSAKIMEI"" AS NnyuSeiMei,
                        ""HINMEI"" AS Hinmei,
                        ""KATASHIKISUNPOUs"" AS KshikiSunpo,
                        ""ZUBAN"" AS Zuban,
                        ""BUHINKANRINO"" AS BuhinBiko,
                        CAST(""HATCHUUSU"" AS TEXT) AS HatsuSu
                    FROM ""COMPLEMENTARY"".""SNT_UKEIREKENSA_M""
                    WHERE ""CHUUMONNO"" = @ChuumonNo
                      AND ""BUNDENNO"" IS NULL
                    LIMIT 1;";

                var result = await connection.QueryFirstOrDefaultAsync<AcceptanceDetailDto>(query, new { ChuumonNo = chuumonNo });
                return result;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "GetAcceptanceDetailOriginalAsync エラー: {Message}", ex.Message);
                throw;
            }
        }

        // 4. 共通採番処理 (ストアードプロシージャ呼び出し)
        public async Task<(string? SaibanCode, decimal Status)> GetSaibanCodeAsync(string saibanBunruiCd, string settogo, string userId)
        {
            Log.Information("GetSaibanCodeAsync 処理開始: saibanBunruiCd={SaibanBunruiCd}, settogo={Settogo}, userId={UserId}", saibanBunruiCd, settogo, userId);
            try
            {
                using var connection = _connectionFactory.CreatePostgresConnection();
                if (connection.State != ConnectionState.Open)
                    connection.Open();

                var parameters = new DynamicParameters();
                parameters.Add("pv_saibanbunruicd", saibanBunruiCd, DbType.String, ParameterDirection.Input);
                parameters.Add("pv_settogo", settogo, DbType.String, ParameterDirection.Input);
                parameters.Add("pv_userid", userId, DbType.String, ParameterDirection.Input);
                
                parameters.Add("pv_ret_saibancode", dbType: DbType.String, direction: ParameterDirection.Output, size: 100);
                parameters.Add("pn_ret_status", dbType: DbType.Decimal, direction: ParameterDirection.Output);

                await connection.ExecuteAsync(@"""COMPLEMENTARY"".""GetSaibanCode""", parameters, commandType: CommandType.StoredProcedure);

                decimal status = parameters.Get<decimal>("pn_ret_status");
                string? code = parameters.Get<string>("pv_ret_saibancode");

                if (!string.IsNullOrEmpty(code) && code.Length > 10)
                {
                    code = code.Substring(0, 10);
                }

                Log.Information("GetSaibanCode 実行結果: status={Status}, code={Code}", status, code);
                return (code, status);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "GetSaibanCodeAsync エラー発生: saibanBunruiCd={SaibanBunruiCd}", saibanBunruiCd);
                throw;
            }
        }

        // 5. 納入可否調査 (6-1)
        public async Task<bool> CheckAvailabilityAsync(string denpyoNo)
        {
            Log.Information("CheckAvailabilityAsync 処理開始: denpyoNo={DenpyoNo}", denpyoNo);
            try
            {
                using var connection = _connectionFactory.CreatePostgresConnection();
                if (connection.State != ConnectionState.Open)
                    connection.Open();

                string query = @"
                    SELECT COUNT(1)
                    FROM ""COMPLEMENTARY"".""050310_全発注情報一覧情報""
                    WHERE ""伝票No"" = @DenpyoNo
                      AND (""納入完了区分の名称"" = '完了' OR ""検収完了区分の名称"" = '完了')";

                int count = await connection.ExecuteScalarAsync<int>(query, new { DenpyoNo = denpyoNo });
                return count == 0;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "CheckAvailabilityAsync エラー: {Message}", ex.Message);
                throw;
            }
        }

        // 6. 緊急品チェック
        public async Task<bool> CheckEmergencyItemsAsync()
        {
            Log.Information("CheckEmergencyItemsAsync 処理開始");
            try
            {
                using var connection = _connectionFactory.CreatePostgresConnection();
                if (connection.State != ConnectionState.Open)
                    connection.Open();

                string query = @"
                    SELECT COUNT(1)
                    FROM ""COMPLEMENTARY"".""050310_全発注情報一覧情報"" t3
                    WHERE t3.""納入完了区分の名称"" <> '完了'
                      AND t3.""検収完了区分の名称"" <> '完了'
                      AND t3.""部品表備考"" LIKE '%急%'
                      AND t3.""発注確定納期"" >= CURRENT_DATE - INTERVAL '5 days'
                    LIMIT 1;";

                int count = await connection.ExecuteScalarAsync<int>(query);
                return count > 0;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "CheckEmergencyItemsAsync エラー: {Message}", ex.Message);
                return false;
            }
        }

        // 7. 受入検査詳細情報の追加 (4-1-2: SNT_UKEIREKENSA_S への登録)
        public async Task<bool> InsertAcceptanceDetailSAsync(AcceptanceDetailDto dto, string userId)
        {
            Log.Information("InsertAcceptanceDetailSAsync 処理開始: chuumonNo={ChuumonNo}", dto.ChuumonNo);
            using var connection = _connectionFactory.CreatePostgresConnection();
            if (connection.State != ConnectionState.Open) connection.Open();

            string query = @"
                INSERT INTO ""COMPLEMENTARY"".""SNT_UKEIREKENSA_S"" 
                (""CHUUMONNO"", ""HINBAN"", ""SEIHINBANGO"", ""HATCHUUSU"", ""TOROKUDTM"", ""TOROKUSHACD"", ""JOTAIKBN"")
                VALUES (@ChuumonNo, @Hinban, @SeihinNo, CAST(NULLIF(@HatsuSu, '') AS DECIMAL), CURRENT_TIMESTAMP, @UserId, '1');";

            int rows = await connection.ExecuteAsync(query, new { dto.ChuumonNo, dto.Hinban, dto.SeihinNo, dto.HatsuSu, UserId = userId });
            return rows > 0;
        }

        // 8. 受入検査明細情報の追加 (4-1-2: 採番マスタ等を利用した SNT_UKEIREKENSA_M への登録)
        public async Task<bool> InsertAcceptanceDetailMAsync(AcceptanceDetailDto dto, string userId)
        {
            Log.Information("InsertAcceptanceDetailMAsync 処理開始: chuumonNo={ChuumonNo}", dto.ChuumonNo);
            using var connection = _connectionFactory.CreatePostgresConnection();
            if (connection.State != ConnectionState.Open) connection.Open();

            // 共通採番処理を利用して管理番号（KNなど）を取得
            var (kanriNo, status) = await GetSaibanCodeAsync("SL現品管理番号", "KN", userId);
            if (status != 0 || string.IsNullOrEmpty(kanriNo))
            {
                Log.Warning("現品管理Noの採番に失敗しました (status={Status})", status);
                kanriNo = dto.KanriNo ?? "";
            }

            string query = @"
                INSERT INTO ""COMPLEMENTARY"".""SNT_UKEIREKENSA_M"" 
                (""CHUUMONNO"", ""KANRINO"", ""BUNDENNO"", ""HINBAN"", ""SEIHINBANGO"", ""HATCHUUSU"", ""TOROKUDTM"", ""TOROKUSHACD"", ""JOTAIKBN"")
                VALUES (@ChuumonNo, @KanriNo, @BunnoDenpyoNo, @Hinban, @SeihinNo, CAST(NULLIF(@HatsuSu, '') AS DECIMAL), CURRENT_TIMESTAMP, @UserId, '1');";

            int rows = await connection.ExecuteAsync(query, new { 
                dto.ChuumonNo, 
                KanriNo = kanriNo, 
                dto.BunnoDenpyoNo, 
                dto.Hinban, 
                dto.SeihinNo, 
                dto.HatsuSu, 
                UserId = userId 
            });
            return rows > 0;
        }

        // 9. 全数合格としての書き込み & 検査情報集計・UPDATE処理 (7.)
        public async Task<(bool IsSuccess, string? Message)> UpdateZenkanAsync(AcceptanceWriteRequestModel request)
        {
            Log.Information("=== UpdateZenkanAsync 処理開始 ===");
            using var connection = _connectionFactory.CreatePostgresConnection();
            if (connection.State != ConnectionState.Open)
                connection.Open();

            using var transaction = connection.BeginTransaction();
            try
            {
                string updateQuery = @"
                    UPDATE ""COMPLEMENTARY"".""SNT_UKEIREKENSA_M""
                    SET 
                        ""UKEIREBI"" = CURRENT_DATE,
                        ""NOUNYUUSUSU"" = @Nounyuusu,
                        ""UKEIRESHA"" = @LoginUserCd,
                        ""UKEIRESHACD"" = @LoginUserCd,
                        ""KENSABI"" = CURRENT_DATE,
                        ""GOUKAKUSUSU"" = @Goukakusu,
                        ""FUGOUKAKUSUSU"" = @Fugoukakusu,
                        ""KOSHINDTM"" = CURRENT_TIMESTAMP,
                        ""KOSHINSHACD"" = @LoginUserCd
                    WHERE ""CHUUMONNO"" = @ChuumonNo
                      AND (""BUNDENNO"" = @BunnoDenpyoNo OR (@BunnoDenpyoNo IS NULL AND ""BUNDENNO"" IS NULL));";

                int affectedRows = await connection.ExecuteAsync(updateQuery, new {
                    Nounyuusu = request.Nounyuusu,
                    Goukakusu = request.Goukakusu,
                    Fugoukakusu = request.Fugoukakusu,
                    LoginUserCd = request.LoginUserCd,
                    ChuumonNo = request.ChuumonNo,
                    BunnoDenpyoNo = request.BunnoDenpyoNo
                }, transaction);

                if (affectedRows == 0)
                {
                    transaction.Rollback();
                    return (false, "該当する受入検査データが見つかりませんでした");
                }

                // 検査情報集計・ヘッダー側（SNT_UKEIREKENSA_S）の更新処理 (画像3枚目・下部参照)
                string updateHeaderQuery = @"
                    UPDATE ""COMPLEMENTARY"".""SNT_UKEIREKENSA_S""
                    SET 
                        ""GOUKAKURUKEISU"" = (SELECT COALESCE(SUM(""GOUKAKUSUSU""), 0) FROM ""COMPLEMENTARY"".""SNT_UKEIREKENSA_M"" WHERE ""CHUUMONNO"" = @ChuumonNo),
                        ""FUGOUKAKURUKEISU"" = (SELECT COALESCE(SUM(""FUGOUKAKUSUSU""), 0) FROM ""COMPLEMENTARY"".""SNT_UKEIREKENSA_M"" WHERE ""CHUUMONNO"" = @ChuumonNo),
                        ""NOUNYUURUikeisu"" = (SELECT COALESCE(SUM(""NOUNYUUSUSU""), 0) FROM ""COMPLEMENTARY"".""SNT_UKEIREKENSA_M"" WHERE ""CHUUMONNO"" = @ChuumonNo)
                    WHERE ""CHUUMONNO"" = @ChuumonNo;";

                await connection.ExecuteAsync(updateHeaderQuery, new { ChuumonNo = request.ChuumonNo }, transaction);

                transaction.Commit();
                Log.Information("=== UpdateZenkanAsync Commit 成功 ===");
                return (true, null);
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                Log.Error(ex, "UpdateZenkanAsync エラー: {Message}", ex.Message);
                throw;
            }
        }

        // 10. 受入書き込み / 未検査・不合格あり & 検査情報集計・UPDATE処理 (8.)
        public async Task<(bool IsSuccess, string? Message)> UpdateUkeireAsync(AcceptanceWriteRequestModel request)
        {
            Log.Information("=== UpdateUkeireAsync 処理開始 ===");
            using var connection = _connectionFactory.CreatePostgresConnection();
            if (connection.State != ConnectionState.Open)
                connection.Open();

            using var transaction = connection.BeginTransaction();
            try
            {
                string updateQuery = @"
                    UPDATE ""COMPLEMENTARY"".""SNT_UKEIREKENSA_M""
                    SET 
                        ""UKEIREBI"" = CURRENT_DATE,
                        ""NOUNYUUSUSU"" = @Nounyuusu,
                        ""UKEIRESHA"" = @LoginUserCd,
                        ""UKEIRESHACD"" = @LoginUserCd,
                        ""KENSABI"" = CURRENT_DATE,
                        ""GOUKAKUSUSU"" = @Goukakusu,
                        ""FUGOUKAKUSUSU"" = @Fugoukakusu,
                        ""KOSHINDTM"" = CURRENT_TIMESTAMP,
                        ""KOSHINSHACD"" = @LoginUserCd
                    WHERE ""CHUUMONNO"" = @ChuumonNo
                      AND (""BUNDENNO"" = @BunnoDenpyoNo OR (@BunnoDenpyoNo IS NULL AND ""BUNDENNO"" IS NULL));";

                int affectedRows = await connection.ExecuteAsync(updateQuery, new {
                    Nounyuusu = request.Nounyuusu,
                    Goukakusu = request.Goukakusu,
                    Fugoukakusu = request.Fugoukakusu,
                    LoginUserCd = request.LoginUserCd,
                    ChuumonNo = request.ChuumonNo,
                    BunnoDenpyoNo = request.BunnoDenpyoNo
                }, transaction);

                if (affectedRows == 0)
                {
                    transaction.Rollback();
                    return (false, "該当する受入検査データが見つかりませんでした");
                }

                // 検査情報集計・ヘッダー側（SNT_UKEIREKENSA_S）の更新処理
                string updateHeaderQuery = @"
                    UPDATE ""COMPLEMENTARY"".""SNT_UKEIREKENSA_S""
                    SET 
                        ""GOUKAKURUKEISU"" = (SELECT COALESCE(SUM(""GOUKAKUSUSU""), 0) FROM ""COMPLEMENTARY"".""SNT_UKEIREKENSA_M"" WHERE ""CHUUMONNO"" = @ChuumonNo),
                        ""FUGOUKAKURUKEISU"" = (SELECT COALESCE(SUM(""FUGOUKAKUSUSU""), 0) FROM ""COMPLEMENTARY"".""SNT_UKEIREKENSA_M"" WHERE ""CHUUMONNO"" = @ChuumonNo),
                        ""NOUNYUURUikeisu"" = (SELECT COALESCE(SUM(""NOUNYUUSUSU""), 0) FROM ""COMPLEMENTARY"".""SNT_UKEIREKENSA_M"" WHERE ""CHUUMONNO"" = @ChuumonNo)
                    WHERE ""CHUUMONNO"" = @ChuumonNo;";

                await connection.ExecuteAsync(updateHeaderQuery, new { ChuumonNo = request.ChuumonNo }, transaction);

                transaction.Commit();
                Log.Information("=== UpdateUkeireAsync Commit 成功 ===");
                return (true, null);
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                Log.Error(ex, "UpdateUkeireAsync エラー: {Message}", ex.Message);
                throw;
            }
        }

        // 11. 終了時ログイン削除処理 (10.)
        public async Task<int> DeleteLoginAsync(string userCd)
        {
            Log.Information("DeleteLoginAsync 処理開始: userCd={UserCd}", userCd);
            using var connection = _connectionFactory.CreatePostgresConnection();
            if (connection.State != ConnectionState.Open)
                connection.Open();

            string deleteQuery = @"DELETE FROM ""COMPLEMENTARY"".""SLT_LOGIN"" WHERE ""USERCD"" = @UserCd;";
            int rows = await connection.ExecuteAsync(deleteQuery, new { UserCd = userCd });
            return rows;
        }
    }
}