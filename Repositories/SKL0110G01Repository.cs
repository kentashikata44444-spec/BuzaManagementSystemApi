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
    /// SKL0110G01（仕分け：保管資材確定）画面に関連するデータアクセスを提供するリポジトリ[cite: 3]
    /// </summary>
    public class SKL0110G01Repository
    {
        private readonly IDbConnectionFactory _connectionFactory;
        private readonly ILogger<SKL0110G01Repository> _logger;

        public SKL0110G01Repository(IDbConnectionFactory connectionFactory, ILogger<SKL0110G01Repository> logger)
        {
            _connectionFactory = connectionFactory;
            _logger = logger;
        }

        /// <summary>
        /// 1. 保管資材確定対象検索（製品Noに紐づく保管BOX情報を取得）[cite: 3]
        /// </summary>
        public async Task<IEnumerable<HokanZaimuTargetModel>> GetHokanZaimuTargetAsync(string seihinBango)
        {
            _logger.LogInformation("GetHokanZaimuTargetAsync 開始: seihinBango={SeihinBango}", seihinBango);
            try
            {
                using var connection = _connectionFactory.CreatePostgresConnection();
                if (connection.State != ConnectionState.Open)
                    connection.Open();

                const string sql = @"
                    SELECT ""HOKANBOXCD"" AS HokanBoxCd,
                           MIN(""HOKANBOXMEISHO"") AS HokanBoxMeisho
                    FROM ""COMPLEMENTARY"".""SLT_BUZAISHOZAI_B""
                    WHERE ""SEIHINBANGO"" = @SeihinBango
                      AND ""HOKANBOXCD"" IS NOT NULL
                      AND ""HOKANSIZAICD"" IS NULL
                      AND ""JOTAIKBN"" = '1'
                    GROUP BY ""HOKANBOXCD""
                    ORDER BY ""HOKANBOXCD"";";

                var result = await connection.QueryAsync<HokanZaimuTargetModel>(sql, new { SeihinBango = seihinBango });
                _logger.LogInformation("GetHokanZaimuTargetAsync 完了: seihinBango={SeihinBango}", seihinBango);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetHokanZaimuTargetAsync エラー: seihinBango={SeihinBango}", seihinBango);
                throw;
            }
        }

        /// <summary>
        /// 2. ロケーション中分類マスタ検索（保管資材名称の取得）[cite: 2, 3]
        /// </summary>
        public async Task<string?> GetChuMasterMeishoAsync(string chubunruiCd)
        {
            _logger.LogInformation("GetChuMasterMeishoAsync 開始: chubunruiCd={ChubunruiCd}", chubunruiCd);
            try
            {
                using var connection = _connectionFactory.CreatePostgresConnection();
                if (connection.State != ConnectionState.Open)
                    connection.Open();

                const string sql = @"
                    SELECT ""CHUBUNRUIMEISHO""
                    FROM ""COMPLEMENTARY"".""SLT_ROKESHONCHU_M""
                    WHERE ""CHUBUNRUICD"" = @ChubunruiCd
                      AND ""JOTAIKBN"" = '1'";

                var result = await connection.QueryFirstOrDefaultAsync<string>(sql, new { ChubunruiCd = chubunruiCd });
                _logger.LogInformation("GetChuMasterMeishoAsync 完了: chubunruiCd={ChubunruiCd}, 名称={Meisho}", chubunruiCd, result);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetChuMasterMeishoAsync エラー: chubunruiCd={ChubunruiCd}", chubunruiCd);
                throw;
            }
        }

        /// <summary>
        /// 保管BOX名称取得・存在チェック（ロケーション小分類マスタ等からの取得）
        /// </summary>
        public async Task<string?> GetBoxMeishoAsync(string ShobunruiCd)
        {
            // 引数の ShobunruiCd を正しく参照するように修正
            _logger.LogInformation("GetBoxMeishoAsync 開始: ShobunruiCd={ShobunruiCd}", ShobunruiCd);
            try
            {
                using var connection = _connectionFactory.CreatePostgresConnection();
                if (connection.State != ConnectionState.Open)
                    connection.Open();

                const string sql = @"
                    SELECT ""SHOBUNRUIMEISHO""
                    FROM ""COMPLEMENTARY"".""SLT_ROKESHONSHO_M""
                    WHERE ""SHOBUNRUICD"" = @ShobunruiCd
                      AND ""JOTAIKBN"" = '1'";

                var result = await connection.QueryFirstOrDefaultAsync<string>(sql, new { ShobunruiCd = ShobunruiCd });
                _logger.LogInformation("GetBoxMeishoAsync 完了: ShobunruiCd={ShobunruiCd}, 名称={Meisho}", ShobunruiCd, result);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetBoxMeishoAsync エラー: ShobunruiCd={ShobunruiCd}", ShobunruiCd);
                throw;
            }
        }

        /// <summary>
        /// 3. 保管BOX使用情報検索[cite: 3]
        /// </summary>
        public async Task<string?> GetHokanBoxSiyoJohoAsync(string hokanBoxCd)
        {
            _logger.LogInformation("GetHokanBoxSiyoJohoAsync 開始: hokanBoxCd={HokanBoxCd}", hokanBoxCd);
            try
            {
                using var connection = _connectionFactory.CreatePostgresConnection();
                if (connection.State != ConnectionState.Open)
                    connection.Open();

                const string sql = @"
                    SELECT ""SEIHINBANGO""
                    FROM ""COMPLEMENTARY"".""SLT_HOKANBOXSIYOJOHO""
                    WHERE ""HOKANBOXCD"" = @HokanBoxCd
                      AND ""JOTAIKBN"" = '1'";

                var result = await connection.QueryFirstOrDefaultAsync<string>(sql, new { HokanBoxCd = hokanBoxCd });
                _logger.LogInformation("GetHokanBoxSiyoJohoAsync 完了: hokanBoxCd={HokanBoxCd}, 製品No={SeihinBango}", hokanBoxCd, result);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetHokanBoxSiyoJohoAsync エラー: hokanBoxCd={HokanBoxCd}", hokanBoxCd);
                throw;
            }
        }

        /// <summary>
        /// 4. 保管資材使用情報検索（重複チェック）[cite: 3]
        /// </summary>
        public async Task<int> GetHokanShizaiSiyoCountAsync(string hokanShizaiCd, string seihinBango)
        {
            _logger.LogInformation("GetHokanShizaiSiyoCountAsync 開始: hokanShizaiCd={HokanShizaiCd}, seihinBango={SeihinBango}", hokanShizaiCd, seihinBango);
            try
            {
                using var connection = _connectionFactory.CreatePostgresConnection();
                if (connection.State != ConnectionState.Open)
                    connection.Open();

                const string sql = @"
                    SELECT COUNT(1)
                    FROM ""COMPLEMENTARY"".""SLT_HOKANSIZAISIYOJOHO""
                    WHERE ""HOKANSIZAICD"" = @HokanShizaiCd
                      AND ""SEIHINBANGO"" = @SeihinBango
                      AND ""JOTAIKBN"" = '1'";

                int count = await connection.ExecuteScalarAsync<int>(sql, new { HokanShizaiCd = hokanShizaiCd, SeihinBango = seihinBango });
                _logger.LogInformation("GetHokanShizaiSiyoCountAsync 完了: 件数={Count}", count);
                return count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetHokanShizaiSiyoCountAsync エラー");
                throw;
            }
        }

        /// <summary>
        /// 5. メイン処理トランザクション（部材所在明細更新 ＆ 保管資材使用情報登録）[cite: 4]
        /// </summary>
        public async Task<(bool IsSuccess, string? Message)> UpdateAndRegisterHokanAreaAsync(
            string seihinBango, string hokanBoxCd, string hokanShizaiCd, string hokanShizaiMeisho, string userCd)
        {
            _logger.LogInformation("=== UpdateAndRegisterHokanAreaAsync トランザクション処理開始 ===");
            _logger.LogInformation("パラメータ -> seihinBango={SeihinBango}, hokanBoxCd={HokanBoxCd}, hokanShizaiCd={HokanShizaiCd}, hokanShizaiMeisho={HokanShizaiMeisho}", 
                seihinBango, hokanBoxCd, hokanShizaiCd, hokanShizaiMeisho);

            using var connection = _connectionFactory.CreatePostgresConnection();
            if (connection.State != ConnectionState.Open)
                connection.Open();

            using var transaction = connection.BeginTransaction();
            try
            {
                // ① 部材所在明細の更新 (SLT_BUZAISHOZAI_B)
                const string updateDetailSql = @"
                    UPDATE ""COMPLEMENTARY"".""SLT_BUZAISHOZAI_B""
                    SET ""HOKANSIZAICD"" = @HokanShizaiCd,
                        ""HOKANSIZAIMEISHO"" = @HokanShizaiMeisho,
                        ""KOSHINDTM"" = CURRENT_TIMESTAMP,
                        ""KOSHINSHACD"" = @UserCd
                    WHERE ""SEIHINBANGO"" = @SeihinBango
                      AND ""HOKANBOXCD"" = @HokanBoxCd
                      AND ""HOKANSIZAICD"" IS NULL
                      AND ""JOTAIKBN"" = '1';";

                int affectedRows = await connection.ExecuteAsync(updateDetailSql, new {
                    HokanShizaiCd = hokanShizaiCd,
                    HokanShizaiMeisho = hokanShizaiMeisho,
                    SeihinBango = seihinBango,
                    HokanBoxCd = hokanBoxCd,
                    UserCd = userCd
                }, transaction);

                _logger.LogInformation("部材所在明細更新 成功: 更新行数={AffectedRows}", affectedRows);

                // ② 保管資材使用情報の存在チェック（仕様書フロー準拠）[cite: 4]
                const string checkExistSql = @"
                    SELECT COUNT(1) 
                    FROM ""COMPLEMENTARY"".""SLT_HOKANSIZAISIYOJOHO""
                    WHERE ""HOKANSIZAICD"" = @HokanShizaiCd 
                      AND ""SEIHINBANGO"" = @SeihinBango;";

                int existingCount = await connection.ExecuteScalarAsync<int>(checkExistSql, new {
                    HokanShizaiCd = hokanShizaiCd,
                    SeihinBango = seihinBango
                }, transaction);

                // ③ 存在しない場合のみ、保管資材使用情報に登録する (SLT_HOKANSIZAISIYOJOHO)[cite: 4]
                if (existingCount == 0)
                {
                    const string insertShizaiSql = @"
                        INSERT INTO ""COMPLEMENTARY"".""SLT_HOKANSIZAISIYOJOHO""
                        (
                            ""HOKANSIZAICD"", ""SEIHINBANGO"", ""HOKANSIZAIMEISHO"", 
                            ""TOROKUDTM"", ""TOROKUSHACD"", ""JOTAIKBN""
                        )
                        VALUES 
                        (
                            @HokanShizaiCd, @SeihinBango, @HokanShizaiMeisho, 
                            CURRENT_TIMESTAMP, @UserCd, '1'
                        );";

                    await connection.ExecuteAsync(insertShizaiSql, new {
                        HokanShizaiCd = hokanShizaiCd,
                        SeihinBango = seihinBango,
                        HokanShizaiMeisho = hokanShizaiMeisho,
                        UserCd = userCd
                    }, transaction);

                    _logger.LogInformation("保管資材使用情報登録 成功");
                }
                else
                {
                    _logger.LogInformation("保管資材使用情報は既に存在するため登録をスキップしました");
                }

                transaction.Commit();
                _logger.LogInformation("=== UpdateAndRegisterHokanAreaAsync コミット成功 ===");
                return (true, null);
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                _logger.LogError(ex, "UpdateAndRegisterHokanAreaAsync トランザクションエラー (ロールバック実行): {Message}", ex.Message);
                return (false, "部材所在明細の更新に失敗しました\nシステム管理者に連絡してください");
            }
        }
    }
}