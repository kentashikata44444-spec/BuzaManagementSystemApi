using System;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Logging;
using BuzaiManagementApi.Models;

namespace BuzaiManagementApi.Repositories
{
    /// <summary>
    /// SKL0101G01（仕分け：保管BOX・保管資材確定）画面に関連するデータアクセスを提供するリポジトリ
    /// </summary>
    public class SKL0101G01Repository
    {
        private readonly IDbConnectionFactory _connectionFactory;
        private readonly ILogger<SKL0101G01Repository> _logger;

        public SKL0101G01Repository(IDbConnectionFactory connectionFactory, ILogger<SKL0101G01Repository> logger)
        {
            _connectionFactory = connectionFactory;
            _logger = logger;
        }

        /// <summary>
        /// 1. ロケーション中分類マスタを検索します。
        /// </summary>
        /// <param name="chubunruiCd">中分類コード</param>
        /// <returns>中分類マスタエンティティ（存在しない場合はnull）</returns>
        public async Task<SltRokeshonchuM?> GetChuMasterAsync(string chubunruiCd)
        {
            _logger.LogInformation("GetChuMasterAsync 開始: chubunruiCd={ChubunruiCd}", chubunruiCd);

            try
            {
                using var connection = _connectionFactory.CreatePostgresConnection();
                connection.Open();
                const string sql = @"
                    SELECT ""CHUBUNRUIMEISHO"" AS ChubunruiMeisho
                    FROM ""COMPLEMENTARY"".""SLT_ROKESHONCHU_M"" 
                    WHERE ""CHUBUNRUICD"" = @ChubunruiCd 
                      AND ""JOTAIKBN"" = '1'";
                
                var result = await connection.QuerySingleOrDefaultAsync<SltRokeshonchuM>(sql, new { ChubunruiCd = chubunruiCd });
                
                if (result == null)
                {
                    _logger.LogWarning("GetChuMasterAsync 警告: 該当する有効な中分類マスタが見つかりません (chubunruiCd={ChubunruiCd})", chubunruiCd);
                }
                else
                {
                    _logger.LogInformation("GetChuMasterAsync 成功: chubunruiCd={ChubunruiCd}, 名称={Meisho}", chubunruiCd, result.ChubunruiMeisho);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetChuMasterAsync エラー: chubunruiCd={ChubunruiCd}", chubunruiCd);
                throw;
            }
        }

        /// <summary>
        /// 2. ロケーション小分類マスタを検索します。
        /// </summary>
        /// <param name="shobunruiCd">小分類コード</param>
        /// <returns>小分類マスタエンティティ（存在しない場合はnull）</returns>
        public async Task<SltRokeshonshoM?> GetShoMasterAsync(string shobunruiCd)
        {
            _logger.LogInformation("GetShoMasterAsync 開始: shobunruiCd={ShobunruiCd}", shobunruiCd);

            try
            {
                using var connection = _connectionFactory.CreatePostgresConnection();
                connection.Open();
                const string sql = @"
                    SELECT ""SHOBUNRUIMEISHO"" AS ShobunruiMeisho
                    FROM ""COMPLEMENTARY"".""SLT_ROKESHONSHO_M"" 
                    WHERE ""SHOBUNRUICD"" = @ShobunruiCd 
                      AND ""JOTAIKBN"" = '1'";
                
                var result = await connection.QuerySingleOrDefaultAsync<SltRokeshonshoM>(sql, new { ShobunruiCd = shobunruiCd });

                if (result == null)
                {
                    _logger.LogWarning("GetShoMasterAsync 警告: 該当する有効な小分類マスタが見つかりません (shobunruiCd={ShobunruiCd})", shobunruiCd);
                }
                else
                {
                    _logger.LogInformation("GetShoMasterAsync 成功: shobunruiCd={ShobunruiCd}, 名称={Meisho}", shobunruiCd, result.ShobunruiMeisho);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetShoMasterAsync エラー: shobunruiCd={ShobunruiCd}", shobunruiCd);
                throw;
            }
        }

        /// <summary>
        /// 3. 保管BOX使用情報を検索します。
        /// </summary>
        /// <param name="hokanBoxCd">保管BOXコード</param>
        /// <returns>保管BOX使用情報エンティティ（存在しない場合はnull）</returns>
        public async Task<SltHokanBoxSiyoJoho?> GetHokanBoxSiyoJohoAsync(string hokanBoxCd)
        {
            _logger.LogInformation("GetHokanBoxSiyoJohoAsync 開始: hokanBoxCd={HokanBoxCd}", hokanBoxCd);

            try
            {
                using var connection = _connectionFactory.CreatePostgresConnection();
                connection.Open();
                const string sql = @"
                    SELECT ""HOKANBOXCD"" AS HokanBoxCd,
                           ""SEIHINBANGO"" AS SeihinBango,
                           ""HOKANBOXMEISHO"" AS HokanShizaiMeisho,
                           ""JOTAIKBN"" AS JotaiKbn
                    FROM ""COMPLEMENTARY"".""SLT_HOKANBOXSIYOJOHO"" 
                    WHERE ""HOKANBOXCD"" = @HokanBoxCd 
                      AND ""JOTAIKBN"" = '1'";
                
                var result = await connection.QuerySingleOrDefaultAsync<SltHokanBoxSiyoJoho>(sql, new { HokanBoxCd = hokanBoxCd });

                if (result == null)
                {
                    _logger.LogInformation("GetHokanBoxSiyoJohoAsync 情報: 該当データなし（新規登録・確定対象） (hokanBoxCd={HokanBoxCd})", hokanBoxCd);
                }
                else
                {
                    _logger.LogInformation("GetHokanBoxSiyoJohoAsync 成功: hokanBoxCd={HokanBoxCd}, 製品番号={SeihinBango}", hokanBoxCd, result.SeihinBango);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetHokanBoxSiyoJohoAsync エラー: hokanBoxCd={HokanBoxCd}", hokanBoxCd);
                throw;
            }
        }

        /// <summary>
        /// 4. 保管資材使用情報を検索します。
        /// </summary>
        /// <param name="hokanShizaiCd">保管資材コード</param>
        /// <returns>保管資材使用情報エンティティ（存在しない場合はnull）</returns>
        public async Task<SltHokanShizaiSiyoJoho?> GetHokanShizaiSiyoJohoAsync(string hokanShizaiCd)
        {
            _logger.LogInformation("GetHokanShizaiSiyoJohoAsync 開始: hokanShizaiCd={HokanShizaiCd}", hokanShizaiCd);

            try
            {
                using var connection = _connectionFactory.CreatePostgresConnection();
                connection.Open();
                const string sql = @"
                    SELECT ""HOKANSIZAICD"" AS HokanShizaiCd,
                           ""SEIHINBANGO"" AS SeihinBango,
                           ""HOKANSIZAIMEISHO"" AS HokanShizaiMeisho,
                           ""JOTAIKBN"" AS JotaiKbn
                    FROM ""COMPLEMENTARY"".""SLT_HOKANSIZAISIYOJOHO"" 
                    WHERE ""HOKANSIZAICD"" = @HokanShizaiCd 
                      AND ""JOTAIKBN"" = '1'";
                
                var result = await connection.QuerySingleOrDefaultAsync<SltHokanShizaiSiyoJoho>(sql, new { HokanShizaiCd = hokanShizaiCd });

                if (result == null)
                {
                    _logger.LogInformation("GetHokanShizaiSiyoJohoAsync 情報: 該当データなし (hokanShizaiCd={HokanShizaiCd})", hokanShizaiCd);
                }
                else
                {
                    _logger.LogInformation("GetHokanShizaiSiyoJohoAsync 成功: hokanShizaiCd={HokanShizaiCd}, 製品番号={SeihinBango}", hokanShizaiCd, result.SeihinBango);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetHokanShizaiSiyoJohoAsync エラー: hokanShizaiCd={HokanShizaiCd}", hokanShizaiCd);
                throw;
            }
        }
    }
}