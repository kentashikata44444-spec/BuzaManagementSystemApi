using System;
using System.Threading.Tasks;
using System.Data;

namespace BuzaiManagementApi.Repositories
{
    public class CommonRepository
    {
        private readonly IDbConnectionFactory _dbConnectionFactory;

        public CommonRepository(IDbConnectionFactory dbConnectionFactory)
        {
            _dbConnectionFactory = dbConnectionFactory;
        }

        // ログイン情報削除共通処理
        public async Task<int> DeleteLoginInfoAsync(string userCd)
        {
            // スキーマ名とダブルクォーテーションを含めた正しいSQL文に修正
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
                        return affectedRows;
                    }
                }
            }
            catch (Exception ex)
            {
                // エラー内容をデバッグ出力できるようにしておく
                System.Diagnostics.Debug.WriteLine($"削除処理エラー: {ex.Message}");
                return 0;
            }
        }
    }
}