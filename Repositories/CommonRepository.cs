using System;
using System.Threading.Tasks;
using System.Data;
using Microsoft.Extensions.Logging;

namespace BuzaiManagementApi.Repositories
{
    /// <summary>
    /// アプリケーション全体で共通して使用されるデータアクセスを提供するリポジトリ
    /// </summary>
    public class CommonRepository
    {
        private readonly IDbConnectionFactory _dbConnectionFactory;
        private readonly ILogger<CommonRepository> _logger;

        public CommonRepository(IDbConnectionFactory dbConnectionFactory, ILogger<CommonRepository> logger)
        {
            _dbConnectionFactory = dbConnectionFactory;
            _logger = logger;
        }

        /// <summary>
        /// 指定されたユーザーのログイン情報を削除します（ログイン情報削除共通処理）。
        /// </summary>
        /// <param name="userCd">ユーザーコード</param>
        /// <returns>削除された行数</returns>
        public async Task<int> DeleteLoginInfoAsync(string userCd)
        {
            _logger.LogInformation("DeleteLoginInfoAsync 開始: userCd={UserCd}", userCd);

            // スキーマ名とダブルクォーテーションを含めた正しいSQL文
            string sql = "DELETE FROM \"COMPLEMENTARY\".\"SAT_LOGINJOHO\" WHERE \"SYSTEMCD\" = '10' AND \"USERCD\" = @UserCd";

            try
            {
                using (IDbConnection db = _dbConnectionFactory.CreatePostgresConnection())
                {
                    db.Open();
                    using (IDbCommand cmd = db.CreateCommand())
                    {
                        cmd.CommandText = sql;

                        // パラメータの設定
                        IDbDataParameter param = cmd.CreateParameter();
                        param.ParameterName = "@UserCd";
                        param.Value = userCd;
                        cmd.Parameters.Add(param);

                        int affectedRows = await Task.Run(() => cmd.ExecuteNonQuery());
                        
                        _logger.LogInformation("DeleteLoginInfoAsync 成功: userCd={UserCd}, 削除件数={AffectedRows}", userCd, affectedRows);
                        return affectedRows;
                    }
                }
            }
            catch (Exception ex)
            {
                // デバッグ出力から Serilog のファイル出力へ統合
                _logger.LogError(ex, "DeleteLoginInfoAsync エラー: userCd={UserCd}, メッセージ={Message}", userCd, ex.Message);
                return 0;
            }
        }
    }
}