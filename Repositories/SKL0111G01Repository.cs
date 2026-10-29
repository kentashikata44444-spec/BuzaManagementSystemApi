using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Logging;
using BuzaiManagementApi.Models;

namespace BuzaiManagementApi.Repositories
{
    /// <summary>
    /// SKL0111G01（スマホ一時保管：エリア確定）画面に関連するデータアクセスを提供するリポジトリ
    /// </summary>
    public class SKL0111G01Repository
    {
        private readonly IDbConnectionFactory _connectionFactory;
        private readonly ILogger<SKL0111G01Repository> _logger;

        public SKL0111G01Repository(IDbConnectionFactory connectionFactory, ILogger<SKL0111G01Repository> logger)
        {
            _connectionFactory = connectionFactory;
            _logger = logger;
        }

        /// <summary>
        /// 1. 部材所在明細情報資材検索（保管エリアが未設定の仕分済資材一覧を取得）[cite: 3]
        /// </summary>
        public async Task<IEnumerable<HokanShizaiTargetModel>> GetHokanShizaiTargetAsync(string seihinBango)
        {
            _logger.LogInformation("GetHokanShizaiTargetAsync 開始: seihinBango={SeihinBango}", seihinBango);
            try
            {
                using var connection = _connectionFactory.CreatePostgresConnection();
                if (connection.State != ConnectionState.Open)
                    connection.Open();

                const string sql = @"
                    SELECT ""HOKANSIZAICD"" AS HokanShizaiCd,
                           MAX(""HOKANSIZAIMEISHO"") AS HokanShizaiMeisho
                    FROM ""COMPLEMENTARY"".""SLT_BUZAISHOZAI_B""
                    WHERE ""SEIHINBANGO"" = @SeihinBango
                      AND ""SAGYOSJOTAICD"" = '01'
                      AND ""HOKANERIACD"" IS NULL
                      AND ""HOKANSIZAICD"" IS NOT NULL
                      AND TRIM(""HOKANSIZAICD"") <> ''
                      AND ""JOTAIKBN"" = '1'
                    GROUP BY ""HOKANSIZAICD""
                    ORDER BY ""HOKANSIZAICD"";";

                var result = await connection.QueryAsync<HokanShizaiTargetModel>(sql, new { SeihinBango = seihinBango });
                _logger.LogInformation("GetHokanShizaiTargetAsync 完了: seihinBango={SeihinBango}", seihinBango);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetHokanShizaiTargetAsync エラー: seihinBango={SeihinBango}", seihinBango);
                throw;
            }
        }

        /// <summary>
        /// 2. ロケーション大分類マスタ検索（保管エリア情報の取得）[cite: 3, 4]
        /// </summary>
        public async Task<RokeshoNdaiMasterModel?> GetRokeshoNdaiMasterAsync(string daibunruiCd)
        {
            _logger.LogInformation("GetRokeshoNdaiMasterAsync 開始: daibunruiCd={DaibunruiCd}", daibunruiCd);
            try
            {
                using var connection = _connectionFactory.CreatePostgresConnection();
                if (connection.State != ConnectionState.Open)
                    connection.Open();

                const string sql = @"
                    SELECT ""SOKOCD"" AS SokoCd,
                           ""SOKOMEISHO"" AS SokoMeisho,
                           ""DAIBUNRUICD"" AS DaibunruiCd,
                           ""DAIBUNRUIMEISHO"" AS DaibunruiMeisho
                    FROM ""COMPLEMENTARY"".""SLT_ROKESHONDAI_M""
                    WHERE ""DAIBUNRUICD"" = @DaibunruiCd
                      AND ""JOTAIKBN"" = '1';";

                var result = await connection.QueryFirstOrDefaultAsync<RokeshoNdaiMasterModel>(sql, new { DaibunruiCd = daibunruiCd });
                _logger.LogInformation("GetRokeshoNdaiMasterAsync 完了: daibunruiCd={DaibunruiCd}", daibunruiCd);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetRokeshoNdaiMasterAsync エラー: daibunruiCd={DaibunruiCd}", daibunruiCd);
                throw;
            }
        }

        /// <summary>
        /// 3. 保管エリア使用情報検索[cite: 3]
        /// </summary>
        public async Task<int> GetHokanEriaSiyoCountAsync(string sokoCd, string hokanEriaCd, string seihinBango)
        {
            _logger.LogInformation("GetHokanEriaSiyoCountAsync 開始: sokoCd={SokoCd}, hokanEriaCd={HokanEriaCd}, seihinBango={SeihinBango}", sokoCd, hokanEriaCd, seihinBango);
            try
            {
                using var connection = _connectionFactory.CreatePostgresConnection();
                if (connection.State != ConnectionState.Open)
                    connection.Open();

                const string sql = @"
                    SELECT COALESCE(COUNT(""HOKANERIACD""), 0)
                    FROM ""COMPLEMENTARY"".""SLT_HOKANERIASHIYOJOHO""
                    WHERE ""SOKOCD"" = @SokoCd
                      AND ""HOKANERIACD"" = @HokanEriaCd
                      AND ""SEIHINBANGO"" = @SeihinBango
                      AND ""JOTAIKBN"" = '1';";

                int count = await connection.ExecuteScalarAsync<int>(sql, new { SokoCd = sokoCd, HokanEriaCd = hokanEriaCd, SeihinBango = seihinBango });
                _logger.LogInformation("GetHokanEriaSiyoCountAsync 完了: 件数={Count}", count);
                return count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetHokanEriaSiyoCountAsync エラー");
                throw;
            }
        }

        /// <summary>
        /// 4. 指定資材が対象製品・状態に存在するか確認（部材所在明細情報資材検索）[cite: 3]
        /// </summary>
        public async Task<bool> CheckBuzaishozaiShizaiExistsAsync(string seihinBango, string hokanShizaiCd)
        {
            _logger.LogInformation("CheckBuzaishozaiShizaiExistsAsync 開始: seihinBango={SeihinBango}, hokanShizaiCd={HokanShizaiCd}", seihinBango, hokanShizaiCd);
            try
            {
                using var connection = _connectionFactory.CreatePostgresConnection();
                if (connection.State != ConnectionState.Open)
                    connection.Open();

                const string sql = @"
                    SELECT COUNT(1)
                    FROM ""COMPLEMENTARY"".""SLT_BUZAISHOZAI_B""
                    WHERE ""SEIHINBANGO"" = @SeihinBango
                      AND ""HOKANSIZAICD"" = @HokanShizaiCd
                      AND ""JOTAIKBN"" = '1';";

                int count = await connection.ExecuteScalarAsync<int>(sql, new { SeihinBango = seihinBango, HokanShizaiCd = hokanShizaiCd });
                return count > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CheckBuzaishozaiShizaiExistsAsync エラー");
                throw;
            }
        }

        /// <summary>
        /// 5. メイン処理トランザクション（部材所在明細更新 ＆ 保管エリア使用情報登録）[cite: 4, 5]
        /// </summary>
public async Task<(bool IsSuccess, string? Message)> UpdateAreaAndRegisterHokanEriaAsync(
            string seihinBango, string hokanShizaiCd, string sokoCd, string sokoMeisho, 
            string hokanEriaCd, string hokanEriaMeisho, string userCd)
        {
            _logger.LogInformation("=== UpdateAreaAndRegisterHokanEriaAsync トランザクション処理開始 ===");
            _logger.LogInformation("パラメータ -> seihinBango={SeihinBango}, hokanShizaiCd={HokanShizaiCd}, sokoCd={SokoCd}, hokanEriaCd={HokanEriaCd}", 
                seihinBango, hokanShizaiCd, sokoCd, hokanEriaCd);

            using var connection = _connectionFactory.CreatePostgresConnection();
            if (connection.State != ConnectionState.Open)
                connection.Open();

            using var transaction = connection.BeginTransaction();
            try
            {
                // ① 部材所在明細情報の更新 (SLT_BUZAISHOZAI_B)[cite: 5]
                const string updateDetailSql = @"
                    UPDATE ""COMPLEMENTARY"".""SLT_BUZAISHOZAI_B""
                    SET ""HOKANERIACD"" = @HokanEriaCd,
                        ""HOKANERIAMEISHO"" = @HokanEriaMeisho,
                        ""KOSHINDTM"" = CURRENT_TIMESTAMP,
                        ""KOSHINSHACD"" = @UserCd
                    WHERE ""SEIHINBANGO"" = @SeihinBango
                      AND ""HOKANSIZAICD"" = @HokanShizaiCd;";

                int affectedRows = await connection.ExecuteAsync(updateDetailSql, new {
                    HokanEriaCd = hokanEriaCd,
                    HokanEriaMeisho = hokanEriaMeisho,
                    UserCd = userCd,
                    SeihinBango = seihinBango,
                    HokanShizaiCd = hokanShizaiCd
                }, transaction);

                _logger.LogInformation("部材所在明細更新 成功: 更新行数={AffectedRows}", affectedRows);

                // ② 保管エリア使用情報の存在チェック
                const string checkExistSql = @"
                    SELECT COUNT(1) 
                    FROM ""COMPLEMENTARY"".""SLT_HOKANERIASHIYOJOHO""
                    WHERE ""SOKOCD"" = @SokoCd
                      AND ""HOKANERIACD"" = @HokanEriaCd
                      AND ""SEIHINBANGO"" = @SeihinBango;";

                int existingCount = await connection.ExecuteScalarAsync<int>(checkExistSql, new {
                    SokoCd = sokoCd,
                    HokanEriaCd = hokanEriaCd,
                    SeihinBango = seihinBango
                }, transaction);

                // ③ 存在しない場合のみ、保管エリア使用情報に登録する (SLT_HOKANERIASHIYOJOHO)[cite: 4]
                if (existingCount == 0)
                {
                    const string insertEriaSql = @"
                        INSERT INTO ""COMPLEMENTARY"".""SLT_HOKANERIASHIYOJOHO""
                        (
                            ""SOKOCD"", ""HOKANERIACD"", ""SEIHINBANGO"", 
                            ""SOKOMEISHO"", ""HOKANERIAMEISHO"", 
                            ""TOROKUDTM"", ""TOROKUSHACD"", ""JOTAIKBN"",
                            ""KOSHINDTM"", ""KOSHINSHACD""
                        )
                        VALUES 
                        (
                            @SokoCd, @HokanEriaCd, @SeihinBango, 
                            @SokoMeisho, @HokanEriaMeisho, 
                            CURRENT_TIMESTAMP, @UserCd, '1',
                            NULL, NULL
                        );";

                    await connection.ExecuteAsync(insertEriaSql, new {
                        SokoCd = sokoCd,
                        HokanEriaCd = hokanEriaCd,
                        SeihinBango = seihinBango,
                        SokoMeisho = sokoMeisho,
                        HokanEriaMeisho = hokanEriaMeisho,
                        UserCd = userCd
                    }, transaction);

                    _logger.LogInformation("保管エリア使用情報登録 成功");
                }
                else
                {
                    _logger.LogInformation("保管エリア使用情報は既に存在するため登録をスキップしました");
                }

                transaction.Commit();
                _logger.LogInformation("=== UpdateAreaAndRegisterHokanEriaAsync コミット成功 ===");
                return (true, null);
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                _logger.LogError(ex, "UpdateAreaAndRegisterHokanEriaAsync トランザクションエラー (ロールバック実行): {Message}", ex.Message);
                
                // ★ どちらの処理か（または例外の発生場所に応じて）明確なメッセージを返す
                // ここでは安全のため、もし明細更新に関するエラーか登録に関するエラーかを個別に分けたい場合は
                // トランザクション内を細かくtry-catchするか、あるいは以下のように汎用的に分けることができます。
                return (false, "部材所在明細情報の更新に失敗しました\nシステム管理者に連絡してください");
            }
        }
    }
}