using System;
using System.Data;
using System.Threading.Tasks;
using Dapper;

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
            using var connection = _connectionFactory.CreatePostgresConnection();
            string query = @"
                SELECT NVL(KINKYU,'') AS Kinkyu
                FROM SLT_BUZAISHOZAI_B
                WHERE SEIHINBANGO = @SeihinBango
                  AND SAGHOSJOTAICD > '02'
                  AND JOTAIKBN = '1'";
            return await connection.QueryFirstOrDefaultAsync<string>(query, new { SeihinBango = seihinBango });
        }

        // 2. 保管BOX使用情報検索
        public async Task<string?> GetHokanBoxSiyoJohoAsync(string hokanBoxCd)
        {
            using var connection = _connectionFactory.CreatePostgresConnection();
            string query = @"
                SELECT SEIHINBANGO
                FROM SLT_HOKANBOXSIYOJOHO
                WHERE HOKANBOXCD = @HokanBoxCd
                  AND JOTAIKBN = '1'";
            return await connection.QueryFirstOrDefaultAsync<string>(query, new { HokanBoxCd = hokanBoxCd });
        }

        // 3. 保管資材使用情報検索
        public async Task<string?> GetHokanShizaiSiyoJohoAsync(string hokanShizaiCd, string seihinBango)
        {
            using var connection = _connectionFactory.CreatePostgresConnection();
            string query = @"
                SELECT SEIHINBANGO
                FROM SLT_HOKANSIZAISIYOJOHO
                WHERE HOKANSIZAICD = @HokanShizaiCd
                  AND SEIHINBANGO = @SeihinBango
                  AND JOTAIKBN = '1'";
            return await connection.QueryFirstOrDefaultAsync<string>(query, new { HokanShizaiCd = hokanShizaiCd, SeihinBango = seihinBango });
        }

        // 4. 削除処理
        public async Task<bool> DeleteDataAsync(string genpkanribango, string seihinBango, string hokanBoxCd, string hokanShizaiCd, bool hasDetailData)
        {
            using var connection = _connectionFactory.CreatePostgresConnection();
            if (connection.State != ConnectionState.Open)
                connection.Open();

            using var transaction = connection.BeginTransaction();
            try
            {
                // 部材所在ヘッダ情報削除
                string deleteHeaderQuery = @"
                    DELETE FROM SLT_BUZAISHOZAI_H
                    WHERE GENPKANRIBANGO = @Genpkanribango
                      AND JOTAIKBN = '1'";
                await connection.ExecuteAsync(deleteHeaderQuery, new { Genpkanribango = genpkanribango }, transaction);

                // 部材所在明細情報削除
                string deleteDetailQuery = @"
                    DELETE FROM SLT_BUZAISHOZAI_B
                    WHERE SEIHINBANGO = @SeihinBango
                      AND JOTAIKBN = '1'";
                await connection.ExecuteAsync(deleteDetailQuery, new { SeihinBango = seihinBango }, transaction);

                // 保管BOX使用情報削除
                string deleteBoxQuery = hasDetailData
                    ? @"DELETE FROM SLT_HOKANBOXSIYOJOHO WHERE SEIHINBANGO = @SeihinBango AND HOKANBOXCD = @HokanBoxCd AND JOTAIKBN = '1'"
                    : @"DELETE FROM SLT_HOKANBOXSIYOJOHO WHERE SEIHINBANGO = @SeihinBango AND JOTAIKBN = '1'";
                await connection.ExecuteAsync(deleteBoxQuery, new { SeihinBango = seihinBango, HokanBoxCd = hokanBoxCd }, transaction);

                // 保管資材使用情報削除
                string deleteShizaiQuery = hasDetailData
                    ? @"DELETE FROM SLT_HOKANSIZAISIYOJOHO WHERE SEIHINBANGO = @SeihinBango AND HOKANSIZAICD = @HokanShizaiCd AND JOTAIKBN = '1'"
                    : @"DELETE FROM SLT_HOKANSIZAISIYOJOHO WHERE SEIHINBANGO = @SeihinBango AND JOTAIKBN = '1'";
                await connection.ExecuteAsync(deleteShizaiQuery, new { SeihinBango = seihinBango, HokanShizaiCd = hokanShizaiCd }, transaction);

                transaction.Commit();
                return true;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
    }
}