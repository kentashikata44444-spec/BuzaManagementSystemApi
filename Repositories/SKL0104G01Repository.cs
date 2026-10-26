using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using Serilog;
using BuzaiManagementApi.Models;

namespace BuzaiManagementApi.Repositories
{
    public class SKL0104G01Repository
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public SKL0104G01Repository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        // 1. 緊急品チェック
        public async Task<string?> CheckEmergencyAsync(string seihinBango)
        {
            Log.Information("CheckEmergencyAsync 処理開始: seihinBango={SeihinBango}", seihinBango);
            try
            {
                using var connection = _connectionFactory.CreatePostgresConnection();
                if (connection.State != ConnectionState.Open)
                    connection.Open();

                string query = @"
                    SELECT COALESCE(""KINKYUKBN"", '') AS 緊急
                    FROM ""COMPLEMENTARY"".""SLT_BUZAISHOZAI_B""
                    WHERE ""SEIHINBANGO"" = @SeihinBango
                      AND ""SAGYOSJOTAICD"" > '02'
                      AND ""JOTAIKBN"" = '1'
                    LIMIT 1;";

                Log.Information("CheckEmergencyAsync 発行クエリパラメータ: SeihinBango={SeihinBango}", seihinBango);
                var result = await connection.QueryFirstOrDefaultAsync<string>(query, new { SeihinBango = seihinBango });
                Log.Information("CheckEmergencyAsync 完了: seihinBango={SeihinBango}, 判定結果={Result}", seihinBango, result);
                return result;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "CheckEmergencyAsync エラー発生: seihinBango={SeihinBango}, Message={Message}", seihinBango, ex.Message);
                throw;
            }
        }

        // 2. 保管BOX使用情報検索
        public async Task<string?> GetHokanBoxSiyoJohoAsync(string hokanBoxCd)
        {
            Log.Information("GetHokanBoxSiyoJohoAsync 処理開始: hokanBoxCd={HokanBoxCd}", hokanBoxCd);
            try
            {
                using var connection = _connectionFactory.CreatePostgresConnection();
                if (connection.State != ConnectionState.Open)
                    connection.Open();

                string query = @"
                    SELECT ""SEIHINBANGO""
                    FROM ""COMPLEMENTARY"".""SLT_HOKANBOXSIYOJOHO""
                    WHERE ""HOKANBOXCD"" = @HokanBoxCd
                      AND ""JOTAIKBN"" = '1'";

                Log.Information("GetHokanBoxSiyoJohoAsync 発行クエリパラメータ: HokanBoxCd={HokanBoxCd}", hokanBoxCd);
                var result = await connection.QueryFirstOrDefaultAsync<string>(query, new { HokanBoxCd = hokanBoxCd });
                Log.Information("GetHokanBoxSiyoJohoAsync 完了: hokanBoxCd={HokanBoxCd}, 取得製品No={SeihinBango}", hokanBoxCd, result);
                return result;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "GetHokanBoxSiyoJohoAsync エラー発生: hokanBoxCd={HokanBoxCd}, Message={Message}", hokanBoxCd, ex.Message);
                throw;
            }
        }

        // 3. 保管資材使用情報検索
        public async Task<string?> GetHokanShizaiSiyoJohoAsync(string hokanShizaiCd, string seihinBango)
        {
            Log.Information("GetHokanShizaiSiyoJohoAsync 処理開始: hokanShizaiCd={HokanShizaiCd}, seihinBango={SeihinBango}", hokanShizaiCd, seihinBango);
            try
            {
                using var connection = _connectionFactory.CreatePostgresConnection();
                if (connection.State != ConnectionState.Open)
                    connection.Open();

                string query = @"
                    SELECT ""SEIHINBANGO""
                    FROM ""COMPLEMENTARY"".""SLT_HOKANSIZAISIYOJOHO""
                    WHERE ""HOKANSIZAICD"" = @HokanShizaiCd
                      AND ""SEIHINBANGO"" = @SeihinBango
                      AND ""JOTAIKBN"" = '1'";

                Log.Information("GetHokanShizaiSiyoJohoAsync 発行クエリパラメータ: HokanShizaiCd={HokanShizaiCd}, SeihinBango={SeihinBango}", hokanShizaiCd, seihinBango);
                var result = await connection.QueryFirstOrDefaultAsync<string>(query, new { HokanShizaiCd = hokanShizaiCd, SeihinBango = seihinBango });
                Log.Information("GetHokanShizaiSiyoJohoAsync 完了: hokanShizaiCd={HokanShizaiCd}, 取得製品No={SeihinBango}", hokanShizaiCd, result);
                return result;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "GetHokanShizaiSiyoJohoAsync エラー発生: hokanShizaiCd={HokanShizaiCd}, seihinBango={SeihinBango}, Message={Message}", hokanShizaiCd, seihinBango, ex.Message);
                throw;
            }
        }

        // 4. 部品表情報検索 (SQLServer/Techs接続)
        public async Task<BuhinDataModel?> GetBuhinHyoInfoAsync(string? seihinBango, string? buhinKanriNo, string? hinban)
        {
            Log.Information("GetBuhinHyoInfoAsync 処理開始: seihinBango={SeihinBango}, buhinKanriNo={BuhinKanriNo}, hinban={Hinban}", seihinBango, buhinKanriNo, hinban);

            try
            {
                using var connection = _connectionFactory.CreateSqlServerConnection();
                if (connection.State != ConnectionState.Open)
                    connection.Open();
                
                string query = @"
                    SELECT 
                        [品番ｸﾞﾙｰﾌﾟｺｰﾄﾞ] AS HinbanGroupCode,
                        [品番ｸﾞﾙｰﾌﾟ名] AS HinbanGroupName,
                        [品番] AS Hinban,
                        [図番] AS Zuban,
                        [品名] AS Hinmei,
                        [形式寸法] AS KoshikiZuhyo,
                        [部品管理No] AS BuhinKanriNo,
                        [得意先ｺｰﾄﾞ] AS TokuisakiCode,
                        [得意先正式名] AS TokuisakiSeishikiMei,
                        [製番] AS Seiban,
                        [受注No] AS JuchuNo,
                        [製品数] AS SeihinSu,
                        [製品分類ｺｰﾄﾞ] AS SeihinBunruiCode,
                        [製品分類名] AS SeihinBunruiMei,
                        [納入先ｺｰﾄﾞ] AS NonyusakiCode,
                        [納入先正式名] AS NonyusakiSeishikiMei,
                        [手配数] AS TehaiSu,
                        [手配単位名] AS TehaiTaniMei,
                        [製品No] AS SeihinBango
                    FROM [040101_部品表情報]
                    WHERE 1 = 1";

                if (!string.IsNullOrEmpty(buhinKanriNo))
                {
                    query += " AND [部品管理No] = @BuhinKanriNo";
                }
                else if (!string.IsNullOrEmpty(seihinBango))
                {
                    query += " AND [製品No] LIKE @SeihinBango";
                    if (!string.IsNullOrEmpty(hinban))
                    {
                        query += " AND [品番] LIKE @Hinban";
                    }
                }
                else
                {
                    Log.Warning("GetBuhinHyoInfoAsync 警告: 検索条件（seihinBango, buhinKanriNo）が両方とも空です");
                    return null;
                }

                query += " ORDER BY (SELECT NULL) OFFSET 0 ROWS FETCH NEXT 1 ROWS ONLY";

                decimal? dbBuhinKanriNo = decimal.TryParse(buhinKanriNo, out var parsedBuhinNo) ? parsedBuhinNo : (decimal?)null;

                Log.Information("GetBuhinHyoInfoAsync 発行クエリパラメータ: SeihinBango={SqlSeihinBango}, BuhinKanriNo={SqlBuhinKanriNo}, Hinban={SqlHinban}", 
                    $"%{seihinBango}%", dbBuhinKanriNo, $"%{hinban}%");

                var result = await connection.QueryFirstOrDefaultAsync<BuhinDataModel>(
                    query, 
                    new { SeihinBango = $"%{seihinBango}%", BuhinKanriNo = dbBuhinKanriNo, Hinban = $"%{hinban}%" }
                );

                if (result != null)
                {
                    Log.Information("GetBuhinHyoInfoAsync データ取得成功: 製品No={SeihinBango}, 部品管理No={BuhinKanriNo}, 手配数={TehaiSu}", 
                        result.SeihinBango, result.BuhinKanriNo, result.TehaiSu);
                }
                else
                {
                    Log.Warning("GetBuhinHyoInfoAsync 警告: 該当する部品表情報が見つかりませんでした（結果がnullです）");
                }

                return result;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "GetBuhinHyoInfoAsync クエリ実行中にエラーが発生しました: {Message}", ex.Message);
                throw new InvalidOperationException("Techs接続エラー \nシステム管理者に問合せ願います", ex);
            }
        }

        // 5. 部材所在明細存在チェック
        public async Task<bool> CheckBuzaishozaiDetailExistsAsync(string denpyoShubetsuCd, string denpyoBango)
        {
            Log.Information("CheckBuzaishozaiDetailExistsAsync 処理開始: 伝票種別コード={DenpyoShubetsuCd}, 伝票No={DenpyoBango}", denpyoShubetsuCd, denpyoBango);
            try
            {
                using var connection = _connectionFactory.CreatePostgresConnection();
                if (connection.State != ConnectionState.Open)
                    connection.Open();

                string query = @"
                    SELECT COUNT(1) 
                    FROM ""COMPLEMENTARY"".""SLT_BUZAISHOZAI_B""
                    WHERE ""DENPSHUBETUCD"" = @DenpyoShubetsuCd
                      AND ""DENPBANGO"" = @DenpyoBango
                      AND ""JOTAIKBN"" = '1'";
                
                int count = await connection.ExecuteScalarAsync<int>(query, new { DenpyoShubetsuCd = denpyoShubetsuCd, DenpyoBango = denpyoBango });
                Log.Information("CheckBuzaishozaiDetailExistsAsync 完了: 伝票種別コード={DenpyoShubetsuCd}, 伝票No={DenpyoBango}, 存在件数={Count}", denpyoShubetsuCd, denpyoBango, count);
                return count > 0;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "CheckBuzaishozaiDetailExistsAsync エラー発生: 伝票種別={DenpyoShubetsuCd}, 伝票No={DenpyoBango}, Message={Message}", denpyoShubetsuCd, denpyoBango, ex.Message);
                throw;
            }
        }

        // 6. 部材所在明細 支給数合計取得
        public async Task<decimal> GetBuzaishozaiSuryoSumAsync(string buhinKanriNo)
        {
            Log.Information("GetBuzaishozaiSuryoSumAsync 処理開始: buhinKanriNo={BuhinKanriNo}", buhinKanriNo);
            try
            {
                using var connection = _connectionFactory.CreatePostgresConnection();
                if (connection.State != ConnectionState.Open)
                    connection.Open();

                string query = @"
                    SELECT COALESCE(SUM(""SURYO""), 0) 
                    FROM ""COMPLEMENTARY"".""SLT_BUZAISHOZAI_B""
                    WHERE ""BUHINKANRIBANGO"" = @BuhinKanriNo
                      AND ""JOTAIKBN"" = '1'";
                
                decimal sum = await connection.ExecuteScalarAsync<decimal>(query, new { BuhinKanriNo = buhinKanriNo });
                Log.Information("GetBuzaishozaiSuryoSumAsync 完了: buhinKanriNo={BuhinKanriNo}, 支給数合計={Sum}", buhinKanriNo, sum);
                return sum;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "GetBuzaishozaiSuryoSumAsync エラー発生: buhinKanriNo={BuhinKanriNo}, Message={Message}", buhinKanriNo, ex.Message);
                throw;
            }
        }

        // 7. 共通採番処理
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
                
                if (status != 0 || string.IsNullOrEmpty(code))
                {
                    Log.Warning("GetSaibanCode 警告: ステータスが異常またはコードが空です (status={Status})", status);
                    return (null, status);
                }

                return (code, status);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "GetSaibanCodeAsync エラー発生: saibanBunruiCd={SaibanBunruiCd}, Message={Message}", saibanBunruiCd, ex.Message);
                throw;
            }
        }

        // 8. 登録トランザクション実行
        public async Task<(bool IsSuccess, string? Message)> RegisterDataAsync(
            string denpyoShubetsuCd, string denpyoBango, string seihinBango, 
            string buhinKanriNo, string hokanBoxCd, string hokanBoxMeisho,
            string hokanShizaiCd, string hokanShizaiMeisho,
            decimal konkaiShikyuSu, string userCd, string genpkanribango,
            string feKojoSeiban, string feSeq, string hinban, string hinmei, 
            string katashiki, string tehaiTaniMei, decimal tehaiSu, decimal shikyuruiRuisekiSu,
            string hinbanGrpCd, string hinbanGrpNm, string zuban, 
            string seiban, string juchuNo, decimal seihinSu, 
            string seihinBunCd, string seihinBunNm, 
            string nonyuCd, string nonyuNm, string tokuiCd, string tokuiNm,
            string seihinHinmei, 
            string kinkyuKbn)
        {

            // ★ここで空文字や空白だけのものを強制的に null に変換する
            string? dbHokanBoxCd = string.IsNullOrWhiteSpace(hokanBoxCd) ? null : hokanBoxCd;
            string? dbHokanBoxMeisho = string.IsNullOrWhiteSpace(hokanBoxMeisho) ? null : hokanBoxMeisho;
            string? dbHokanShizaiCd = string.IsNullOrWhiteSpace(hokanShizaiCd) ? null : hokanShizaiCd;
            string? dbHokanShizaiMeisho = string.IsNullOrWhiteSpace(hokanShizaiMeisho) ? null : hokanShizaiMeisho;
            
            Log.Information("=== RegisterDataAsync トランザクション処理開始 ===");
            Log.Information("登録パラメータ -> seihinBango={SeihinBango}, denpyoBango={DenpyoBango}, genpkanribango={Genpkanribango}, konkaiShikyuSu={KonkaiShikyuSu}, kinkyuKbn={KinkyuKbn}", 
                seihinBango, denpyoBango, genpkanribango, konkaiShikyuSu, kinkyuKbn);

            decimal dbBuhinKanriNo = decimal.TryParse(buhinKanriNo, out var parsedBuhinNo) ? parsedBuhinNo : 0m;

            using var connection = _connectionFactory.CreatePostgresConnection();
            if (connection.State != ConnectionState.Open)
                connection.Open();

            using var transaction = connection.BeginTransaction();
            try
            {
                // ② 部材所在ヘッダ情報の存在チェック＆登録
                Log.Information("【登録Step 1】部材所在ヘッダ情報チェック: seihinBango={SeihinBango}", seihinBango);
                string checkHeader = @"
                    SELECT COUNT(1) 
                    FROM ""COMPLEMENTARY"".""SLT_BUZAISHOZAI_H""
                    WHERE ""SEIHINBANGO"" = @SeihinBango 
                      AND ""JOTAIKBN"" = '1'";
                int headerCount = await connection.ExecuteScalarAsync<int>(checkHeader, new { SeihinBango = seihinBango }, transaction);
                Log.Information("部材所在ヘッダ存在件数={HeaderCount}", headerCount);

                if (headerCount == 0)
                {
                    Log.Information("部材所在ヘッダが存在しないため、新規INSERTを実行します");
                    string insertHeader = @"
                        INSERT INTO ""COMPLEMENTARY"".""SLT_BUZAISHOZAI_H""
                        (
                            ""SEIHINBANGO"", ""SEIBAN"", ""JUCHUBANGO"", ""MENSU"", 
                            ""KISHU"", ""KISHUMEI"", ""SEIHINHINMEI"", 
                            ""KOJOCD"", ""KOJOMEI"", ""KOKYAKUCD"", ""KOKYAKUMEI"", 
                            ""TOROKUDTM"", ""TOROKUSHACD"", ""JOTAIKBN""
                        )
                        VALUES 
                        (
                            @SeihinBango, @Seiban, @JuchuNo, @SeihinSu, 
                            @SeihinBunCd, @SeihinBunNm, @SeihinHinmei, 
                            @TokuiCd, @TokuiNm, @NonyuCd, @NonyuNm,  
                            CURRENT_TIMESTAMP, @UserCd, '1'
                        );";
                    
                    await connection.ExecuteAsync(insertHeader, new { 
                        SeihinBango = seihinBango, Seiban = seiban, JuchuNo = juchuNo, SeihinSu = seihinSu, 
                        SeihinBunCd = seihinBunCd, SeihinBunNm = seihinBunNm, SeihinHinmei = seihinHinmei, 
                        NonyuCd = nonyuCd, NonyuNm = nonyuNm, TokuiCd = tokuiCd, TokuiNm = tokuiNm, UserCd = userCd 
                    }, transaction);
                    Log.Information("部材所在ヘッダINSERT成功");
                }

                // ③ 明細INSERT前に、Fe支給品情報から累計数を取得して手配数と比較する
                Log.Information("【登録Step 2】Fe支給品情報から累計数を取得: FeKojoSeiban={FeKojoSeiban}, FeSeq={FeSeq}", feKojoSeiban, feSeq);
                string getSuryoSumQuery = @"
                    SELECT COALESCE(SUM(""SHIKYUSU""), 0) 
                    FROM ""COMPLEMENTARY"".""SLT_FESHIKYUHINJOHO""
                    WHERE ""FEKOJOSEIBAN"" = @FeKojoSeiban 
                      AND ""FESEQ"" = @FeSeq 
                      AND ""JOTAIKBN"" = '1'";
                
                decimal currentSum = await connection.ExecuteScalarAsync<decimal>(
                    getSuryoSumQuery, 
                    new { FeKojoSeiban = feKojoSeiban, FeSeq = feSeq }, 
                    transaction
                );
                
                decimal totalSuryo = currentSum + konkaiShikyuSu;
                Log.Information("支給累計数チェック -> 既存累計数={CurrentSum}, 今回支給数={KonkaiShikyuSu}, 合計累計数={TotalSuryo}, 手配数={TehaiSu}", 
                    currentSum, konkaiShikyuSu, totalSuryo, tehaiSu);

                if (tehaiSu < totalSuryo)
                {
                    Log.Warning("RegisterDataAsync 登録エラー: 支給累計数が手配数を超えています (手配数: {TehaiSu}, 累計数: {TotalSuryo})", tehaiSu, totalSuryo);
                    return (false, "支給累計数が手配数を超えています");
                }

                // ④ 部材所在明細情報登録
                Log.Information("【登録Step 3】部材所在明細情報(SLT_BUZAISHOZAI_B) INSERT実行");

                Log.Information("リポジトリ登録直前確認 -> HokanBoxCd='{BoxCd}', HokanBoxMeisho='{BoxMei}', HokanShizaiCd='{ShizaiCd}', HokanShizaiMeisho='{ShizaiMei}'", 
                hokanBoxCd, hokanBoxMeisho, hokanShizaiCd, hokanShizaiMeisho);
                string insertDetail = @"
                    INSERT INTO ""COMPLEMENTARY"".""SLT_BUZAISHOZAI_B""
                    (
                        ""GENPKANRIBANGO"", ""DETASHUBETUCD"", ""SAGYOSJOTAICD"", ""SEIHINBANGO"",
                        ""HIBANGURUPUCD"", ""HIBANGURUPUMEI"", ""HINBAN"", ""ZUBAN"", ""HINMEI"",
                        ""KATASIKISPO"", ""BUHINKANRIBANGO"", ""SURYO"", ""TANI"",
                        ""SHIIRESAKICD"", ""SHIIRESAKIMEI"", ""DENPSHUBETUCD"", ""DENPBANGO"",
                        ""FEKOJOSEIBAN"", ""FETEHAISEQ"", ""KYOTENCD"", ""KYOTENMEISHO"",
                        ""SOKOCD"", ""SOKOMEISHO"", ""HOKANERIACD"", ""HOKANERIAMEISHO"",
                        ""HOKANSIZAICD"", ""HOKANSIZAIMEISHO"", ""HOKANBOXCD"", ""HOKANBOXMEISHO"",
                        ""HNKDENPBANGO"", ""KENSAIRIKBN"", ""KINKYUKBN"",
                        ""SHIWAKETNTCD"", ""SHIWAKEBI"",
                        ""TOROKUDTM"", ""TOROKUSHACD"", ""JOTAIKBN""
                    )
                    VALUES
                    (
                        @Genpkanribango, '02', '01', @SeihinBango,
                        @HinbanGrpCd, @HinbanGrpNm, @Hinban, @Zuban, @Hinmei,
                        @Katashiki, @BuhinKanriNo, @KonkaiShikyuSu, @TehaiTaniMei,
                        @TokuiCd, @TokuiNm, @DenpyoShubetsuCd, @DenpyoBango,
                        @FeKojoSeiban, @FeSeq, '00', '勝英産業',
                        '', '', '', '',
                        @HokanShizaiCd, @HokanShizaiMeisho, @HokanBoxCd, @HokanBoxMeisho,
                        0, '3', @KinkyuKbn,
                        @UserCd, CURRENT_DATE,
                        CURRENT_TIMESTAMP, @UserCd, '1'
                    );";

                await connection.ExecuteAsync(insertDetail, new { 
                    Genpkanribango = genpkanribango, 
                    SeihinBango = seihinBango, 
                    HinbanGrpCd = hinbanGrpCd,
                    HinbanGrpNm = hinbanGrpNm,
                    Hinban = hinban,
                    Zuban = zuban,
                    Hinmei = hinmei,
                    Katashiki = katashiki,
                    BuhinKanriNo = dbBuhinKanriNo,
                    KonkaiShikyuSu = konkaiShikyuSu,
                    TehaiTaniMei = tehaiTaniMei,
                    TokuiCd = tokuiCd,
                    TokuiNm = tokuiNm,
                    DenpyoShubetsuCd = denpyoShubetsuCd, 
                    DenpyoBango = denpyoBango, 
                    FeKojoSeiban = feKojoSeiban,
                    FeSeq = feSeq,
                    // ★ここを修正（nullならデータベース側でNULLとして登録されます）
                    HokanShizaiCd = dbHokanShizaiCd,
                    HokanShizaiMeisho = dbHokanShizaiMeisho,
                    HokanBoxCd = dbHokanBoxCd,
                    HokanBoxMeisho = dbHokanBoxMeisho,
                    KinkyuKbn = kinkyuKbn,
                    UserCd = userCd 
                }, transaction);
                Log.Information("部材所在明細INSERT成功");

                // ⑤ 納入SEQの取得
                Log.Information("【登録Step 4】納入SEQ取得処理");
                string getMaxSeqQuery = @"
                    SELECT COALESCE(MAX(""NONYUSEQ""), 0) 
                    FROM ""COMPLEMENTARY"".""SLT_FESHIKYUHINJOHO""
                    WHERE ""FEKOJOSEIBAN"" = @FeKojoSeiban 
                      AND ""FESEQ"" = @FeSeq 
                      AND ""JOTAIKBN"" = '1'";
                int maxSeq = await connection.ExecuteScalarAsync<int>(getMaxSeqQuery, new { FeKojoSeiban = feKojoSeiban, FeSeq = feSeq }, transaction);
                int nonyuSeq = maxSeq + 1;
                Log.Information("取得した最大納入SEQ={MaxSeq} -> 新規納入SEQ={NonyuSeq}", maxSeq, nonyuSeq);

                decimal totalRuisekiSu = currentSum + konkaiShikyuSu;
                string nonyuJokyoKbn = (totalRuisekiSu >= tehaiSu) ? "2" : "1";
                string nonyuJokyo = (totalRuisekiSu >= tehaiSu) ? "完納" : "分納";
                Log.Information("納入状況判定 -> 区分={NonyuJokyoKbn}, 状態={NonyuJokyo}", nonyuJokyoKbn, nonyuJokyo);

                // ⑥ Fe支給品情報登録
                Log.Information("【登録Step 5】Fe支給品情報(SLT_FESHIKYUHINJOHO) INSERT実行");
                string insertFeSikyu = @"
                    INSERT INTO ""COMPLEMENTARY"".""SLT_FESHIKYUHINJOHO""
                    (
                        ""FEKOJOSEIBAN"", ""FESEQ"", ""NONYUSEQ"", ""SEIHINBANGO"", ""SEQ"", 
                        ""SHIKYUYOTEISU"", ""SHIKYUSU"", ""TANICD"", ""TANIMEI"", 
                        ""NONYUJOKYOKBN"", ""NONYUJOKYO"", ""HINMEI"", ""KATASHIKI"", 
                        ""BUHINKANRIBANGO"", ""SHIWAKEBI"", ""DENPYONO"", 
                        ""TOROKUDTM"", ""TOROKUSHACD"", 
                        ""KOSHINDTM"", ""KOSHINSHACD"", ""JOTAIKBN""
                    )
                    VALUES 
                    (
                        @FeKojoSeiban, @FeSeq, @NonyuSeq, @SeihinBango, @FeSeq, 
                        @TehaiSu, @KonkaiShikyuSu, 
                        CASE WHEN @TehaiTaniMei = '面' THEN '01'
                            WHEN @TehaiTaniMei = '個' THEN '02'
                            WHEN @TehaiTaniMei = '式' THEN '03'
                            WHEN @TehaiTaniMei = 'ST' THEN '04'
                            WHEN @TehaiTaniMei = '台' THEN '05'
                            WHEN @TehaiTaniMei = 'm'  THEN '06'
                            WHEN @TehaiTaniMei = '枚' THEN '07'
                            WHEN @TehaiTaniMei = '本' THEN '08'
                            WHEN @TehaiTaniMei = '箱' THEN '09'
                            WHEN @TehaiTaniMei = '巻' THEN '10'
                            WHEN @TehaiTaniMei = '缶' THEN '11'
                            WHEN @TehaiTaniMei = '双' THEN '12'
                            WHEN @TehaiTaniMei = '袋' THEN '13'
                            ELSE '' END,
                        @TehaiTaniMei,
                        @NonyuJokyoKbn, @NonyuJokyo, @Hinmei, @Katashiki, 
                        @BuhinKanriNo, CURRENT_DATE, @DenpyoBango, 
                        CURRENT_TIMESTAMP, @UserCd, 
                        @KoshinDtm, @KoshinShaCd, '1'
                    );";

                await connection.ExecuteAsync(insertFeSikyu, new { 
                    FeKojoSeiban = feKojoSeiban, 
                    FeSeq = feSeq, 
                    NonyuSeq = nonyuSeq, 
                    SeihinBango = seihinBango, 
                    TehaiSu = tehaiSu, 
                    KonkaiShikyuSu = konkaiShikyuSu, 
                    TehaiTaniMei = tehaiTaniMei,
                    NonyuJokyoKbn = nonyuJokyoKbn,
                    NonyuJokyo = nonyuJokyo,
                    Hinmei = hinmei, 
                    Katashiki = katashiki, 
                    BuhinKanriNo = dbBuhinKanriNo, 
                    DenpyoBango = denpyoBango, 
                    UserCd = userCd,
                    KoshinDtm = (DateTime?)null, // 新規登録時はNULLを渡す
                    KoshinShaCd = (string?)null   // 新規登録時はNULLを渡す
                }, transaction);
                Log.Information("Fe支給品情報INSERT成功");

                // ⑦ 保管BOX使用情報の登録
                if (!string.IsNullOrEmpty(hokanBoxCd))
                {
                    Log.Information("【登録Step 6】保管BOX使用情報チェック: hokanBoxCd={HokanBoxCd}", hokanBoxCd);
                    string checkHokanBox = @"
                        SELECT COUNT(1) 
                        FROM ""COMPLEMENTARY"".""SLT_HOKANBOXSIYOJOHO""
                        WHERE ""HOKANBOXCD"" = @HokanBoxCd 
                          AND ""JOTAIKBN"" = '1'";
                    int boxExistsCount = await connection.ExecuteScalarAsync<int>(checkHokanBox, new { HokanBoxCd = hokanBoxCd }, transaction);
                    Log.Information("保管BOX使用情報存在件数={BoxExistsCount}", boxExistsCount);

                    if (boxExistsCount == 0)
                    {
                        Log.Information("保管BOX使用情報が存在しないため新規INSERT実行");
                        string insertBox = @"
                            INSERT INTO ""COMPLEMENTARY"".""SLT_HOKANBOXSIYOJOHO""
                            (""HOKANBOXCD"", ""SEIHINBANGO"", ""HOKANBOXMEISHO"", ""TOROKUDTM"", ""TOROKUSHACD"", ""JOTAIKBN"")
                            VALUES (@HokanBoxCd, @SeihinBango, @HokanBoxMeisho, CURRENT_TIMESTAMP, @UserCd, '1');";
                        
                        await connection.ExecuteAsync(insertBox, new { HokanBoxCd = hokanBoxCd, SeihinBango = seihinBango, HokanBoxMeisho = hokanBoxMeisho, UserCd = userCd }, transaction);
                        Log.Information("保管BOX使用情報INSERT成功");
                    }
                }

                // ⑧ 保管資材使用情報の登録
                if (!string.IsNullOrEmpty(hokanShizaiCd))
                {
                    Log.Information("【登録Step 7】保管資材使用情報チェック: hokanShizaiCd={HokanShizaiCd}", hokanShizaiCd);
                    string checkHokanShizai = @"
                        SELECT COUNT(1) 
                        FROM ""COMPLEMENTARY"".""SLT_HOKANSIZAISIYOJOHO""
                        WHERE ""HOKANSIZAICD"" = @HokanShizaiCd 
                          AND ""SEIHINBANGO"" = @SeihinBango 
                          AND ""JOTAIKBN"" = '1'";
                    int shizaiExistsCount = await connection.ExecuteScalarAsync<int>(checkHokanShizai, new { HokanShizaiCd = hokanShizaiCd, SeihinBango = seihinBango }, transaction);
                    Log.Information("保管資材使用情報存在件数={ShizaiExistsCount}", shizaiExistsCount);

                    if (shizaiExistsCount == 0)
                    {
                        Log.Information("保管資材使用情報が存在しないため新規INSERT実行");
                        string insertShizai = @"
                            INSERT INTO ""COMPLEMENTARY"".""SLT_HOKANSIZAISIYOJOHO""
                            (""HOKANSIZAICD"", ""SEIHINBANGO"", ""HOKANSIZAIMEISHO"", ""TOROKUDTM"", ""TOROKUSHACD"", ""JOTAIKBN"")
                            VALUES (@HokanShizaiCd, @SeihinBango, @HokanShizaiMeisho, CURRENT_TIMESTAMP, @UserCd, '1');";
                        
                        await connection.ExecuteAsync(insertShizai, new { HokanShizaiCd = hokanShizaiCd, SeihinBango = seihinBango, HokanShizaiMeisho = hokanShizaiMeisho, UserCd = userCd }, transaction);
                        Log.Information("保管資材使用情報INSERT成功");
                    }
                }
                
                transaction.Commit();
                Log.Information("=== RegisterDataAsync トランザクション Commit 成功 ===");
                return (true, null);
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                Log.Error(ex, "RegisterDataAsync トランザクションエラー (Rollback実行): {Message}", ex.Message);
                throw;
            }
        }

        // 9. 削除処理
        public async Task<bool> DeleteDataAsync(string seihinBango, string denpyoBango, string? hokanBoxCd, string? hokanShizaiCd)
        {
            Log.Information("=== DeleteDataAsync 処理開始 ===");
            Log.Information("削除パラメータ -> seihinBango={SeihinBango}, denpyoBango={DenpyoBango}, hokanBoxCd={HokanBoxCd}, hokanShizaiCd={HokanShizaiCd}", seihinBango, denpyoBango, hokanBoxCd, hokanShizaiCd);

            using var connection = _connectionFactory.CreatePostgresConnection();
            if (connection.State != ConnectionState.Open)
                connection.Open();

            using var transaction = connection.BeginTransaction();
            try
            {
                // 1. 画面の伝票番号(denpyoBango)を品番(HINBAN)に紐づけて現品管理番号を特定し、1件削除
                Log.Information("【削除Step 1】部材所在明細(SLT_BUZAISHOZAI_B)のDELETE実行");
                string deleteDetailQuery = @"
                    DELETE FROM ""COMPLEMENTARY"".""SLT_BUZAISHOZAI_B"" 
                    WHERE ""GENPKANRIBANGO"" IN (
                        SELECT ""GENPKANRIBANGO"" 
                        FROM ""COMPLEMENTARY"".""SLT_BUZAISHOZAI_B"" 
                        WHERE ""SEIHINBANGO"" = @SeihinBango 
                          AND ""HINBAN"" = @DenpyoBango 
                          AND ""JOTAIKBN"" = '1'
                        LIMIT 1
                    )";
                await connection.ExecuteAsync(deleteDetailQuery, new { SeihinBango = seihinBango, DenpyoBango = denpyoBango }, transaction);

                // 2. 残りの明細件数を取得
                Log.Information("【削除Step 2】残りの明細件数確認");
                string countDetailQuery = @"
                    SELECT COUNT(COALESCE(""GENPKANRIBANGO"", '0')) 
                    FROM ""COMPLEMENTARY"".""SLT_BUZAISHOZAI_B"" 
                    WHERE ""SEIHINBANGO"" = @SeihinBango 
                      AND ""JOTAIKBN"" = '1'";
                int remainingDetailCount = await connection.ExecuteScalarAsync<int>(countDetailQuery, new { SeihinBango = seihinBango }, transaction);
                Log.Information("削除後の残り明細件数={RemainingDetailCount}", remainingDetailCount);

                if (remainingDetailCount == 0)
                {
                    Log.Information("【削除Step 3-A】残り明細が0件のため、ヘッダーおよび使用情報もすべて削除します");
                    string deleteHeaderQuery = @"
                        DELETE FROM ""COMPLEMENTARY"".""SLT_BUZAISHOZAI_H"" 
                        WHERE ""SEIHINBANGO"" = @SeihinBango 
                          AND ""JOTAIKBN"" = '1'";
                    await connection.ExecuteAsync(deleteHeaderQuery, new { SeihinBango = seihinBango }, transaction);

                    string deleteBoxQuery = @"
                        DELETE FROM ""COMPLEMENTARY"".""SLT_HOKANBOXSIYOJOHO"" 
                        WHERE ""SEIHINBANGO"" = @SeihinBango 
                          AND ""JOTAIKBN"" = '1'";
                    await connection.ExecuteAsync(deleteBoxQuery, new { SeihinBango = seihinBango }, transaction);

                    string deleteShizaiQuery = @"
                        DELETE FROM ""COMPLEMENTARY"".""SLT_HOKANSIZAISIYOJOHO"" 
                        WHERE ""SEIHINBANGO"" = @SeihinBango 
                          AND ""JOTAIKBN"" = '1'";
                    await connection.ExecuteAsync(deleteShizaiQuery, new { SeihinBango = seihinBango }, transaction);
                    Log.Information("ヘッダーおよび使用情報の完全削除完了");
                }
                else
                {
                    Log.Information("【削除Step 3-B】残り明細が存在するため、個別BOX/資材の使用状況を確認します");
                    if (!string.IsNullOrEmpty(hokanBoxCd))
                    {
                        string checkBoxQuery = @"
                            SELECT COUNT(""HOKANBOXCD"") 
                            FROM ""COMPLEMENTARY"".""SLT_BUZAISHOZAI_B"" 
                            WHERE ""HOKANBOXCD"" = @HokanBoxCd 
                              AND ""SEIHINBANGO"" = @SeihinBango 
                              AND ""JOTAIKBN"" = '1'";
                        int boxCount = await connection.ExecuteScalarAsync<int>(checkBoxQuery, new { HokanBoxCd = hokanBoxCd, SeihinBango = seihinBango }, transaction);
                        Log.Information("指定BOX使用中の明細残数={BoxCount}", boxCount);

                        if (boxCount == 0)
                        {
                            string deleteBoxQuery = @"
                                DELETE FROM ""COMPLEMENTARY"".""SLT_HOKANBOXSIYOJOHO"" 
                                WHERE ""SEIHINBANGO"" = @SeihinBango 
                                  AND ""HOKANBOXCD"" = @HokanBoxCd 
                                  AND ""JOTAIKBN"" = '1'";
                            await connection.ExecuteAsync(deleteBoxQuery, new { SeihinBango = seihinBango, HokanBoxCd = hokanBoxCd }, transaction);
                            Log.Information("他の明細で使用されていないため保管BOX使用情報を削除しました");
                        }
                    }

                    if (!string.IsNullOrEmpty(hokanShizaiCd))
                    {
                        string checkShizaiQuery = @"
                            SELECT COUNT(""HOKANBOXCD"") 
                            FROM ""COMPLEMENTARY"".""SLT_BUZAISHOZAI_B"" 
                            WHERE ""HOKANSIZAICD"" = @HokanShizaiCd 
                              AND ""SEIHINBANGO"" = @SeihinBango 
                              AND ""JOTAIKBN"" = '1'";
                        int shizaiCount = await connection.ExecuteScalarAsync<int>(checkShizaiQuery, new { HokanShizaiCd = hokanShizaiCd, SeihinBango = seihinBango }, transaction);
                        Log.Information("指定資材使用中の明細残数={ShizaiCount}", shizaiCount);

                        if (shizaiCount == 0)
                        {
                            string deleteShizaiQuery = @"
                                DELETE FROM ""COMPLEMENTARY"".""SLT_HOKANSIZAISIYOJOHO"" 
                                WHERE ""SEIHINBANGO"" = @SeihinBango 
                                  AND ""HOKANSIZAICD"" = @HokanShizaiCd 
                                  AND ""JOTAIKBN"" = '1'";
                            await connection.ExecuteAsync(deleteShizaiQuery, new { SeihinBango = seihinBango, HokanShizaiCd = hokanShizaiCd }, transaction);
                            Log.Information("他の明細で使用されていないため保管資材使用情報を削除しました");
                        }
                    }
                }

                transaction.Commit();
                Log.Information("=== DeleteDataAsync トランザクション Commit 成功 ===");
                return true;
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                Log.Error(ex, "DeleteDataAsync トランザクションエラー (Rollback実行): {Message}", ex.Message);
                throw;
            }
        }
    }
}