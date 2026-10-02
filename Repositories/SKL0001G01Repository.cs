using BuzaiManagementApi.Models;
using Microsoft.Extensions.Logging;
using Npgsql;
using System;

namespace BuzaiManagementApi.Repositories
{
    /// <summary>
    /// SKL0001G01（ログイン・起動条件チェック）画面に関連するデータアクセスを提供するリポジトリ
    /// </summary>
    public class SKL0001G01Repository
    {
        private readonly IDbConnectionFactory _connectionFactory;
        private readonly ILogger<SKL0001G01Repository> _logger;

        public SKL0001G01Repository(IDbConnectionFactory connectionFactory, ILogger<SKL0001G01Repository> logger)
        {
            _connectionFactory = connectionFactory;
            _logger = logger;
        }

        private NpgsqlConnection CreateConnection()
        {
            return (NpgsqlConnection)_connectionFactory.CreatePostgresConnection();
        }

        /// <summary>
        /// 稼働フラグを取得します ("SL部材稼働フラグ")。
        /// </summary>
        public string GetOperationFlag()
        {
            _logger.LogInformation("GetOperationFlag 開始");

            try
            {
                string sql = "SELECT \"MOJI1\" FROM \"COMPLEMENTARY\".\"SAT_UNYOJOKEN_MB\" WHERE \"UNYOCD\" = 'SL部材稼働フラグ' AND \"JOTAIKBN\" = '1'";

                using var conn = CreateConnection();
                using var cmd = new NpgsqlCommand(sql, conn);
                conn.Open();
                var result = cmd.ExecuteScalar();
                var flag = result?.ToString()?.Trim() ?? "";

                _logger.LogInformation("GetOperationFlag 成功: flag={Flag}", flag);
                return flag;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetOperationFlag エラー: {Message}", ex.Message);
                throw;
            }
        }

        /// <summary>
        /// 稼働時間を取得します ("SL部材稼働時間")。
        /// SUJI1: 開始時刻, SUJI2: 終了時刻
        /// </summary>
        public SKL0001G01ConditionDto? GetOperationTime()
        {
            _logger.LogInformation("GetOperationTime 開始");

            try
            {
                string sql = "SELECT \"SUJI1\", \"SUJI2\" FROM \"COMPLEMENTARY\".\"SAT_UNYOJOKEN_MB\" WHERE \"UNYOCD\" = 'SL部材稼働時間' AND \"JOTAIKBN\" = '1'";

                using var conn = CreateConnection();
                using var cmd = new NpgsqlCommand(sql, conn);
                conn.Open();
                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    var dto = new SKL0001G01ConditionDto
                    {
                        Suji1 = reader["SUJI1"]?.ToString()?.Trim(),
                        Suji2 = reader["SUJI2"]?.ToString()?.Trim()
                    };
                    _logger.LogInformation("GetOperationTime 成功: Suji1={Suji1}, Suji2={Suji2}", dto.Suji1, dto.Suji2);
                    return dto;
                }

                _logger.LogWarning("GetOperationTime 警告: 稼働時間の該当データが見つかりません。");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetOperationTime エラー: {Message}", ex.Message);
                throw;
            }
        }

        /// <summary>
        /// ユーザー情報・部署情報・業務担当情報を取得します。
        /// </summary>
        public SKL0001G01UserDto? GetUserInfo(string userCd)
        {
            _logger.LogInformation("GetUserInfo 開始: userCd={UserCd}", userCd);

            try
            {
                string sql = "SELECT u.\"USERCD\", u.\"USERMEISHO\", u.\"PSW\", u.\"BUSHOCD\", b.\"BUSHONAME\", " +
                             "t.\"GYMTNTCD\", t.\"GYMTNTMEI\" " +
                             "FROM \"COMPLEMENTARY\".\"SAT_USER_M\" u " +
                             "LEFT JOIN \"COMPLEMENTARY\".\"SAT_BUSHO_M\" b ON u.\"BUSHOCD\" = b.\"BUSHOCD\" AND b.\"JOTAIKBN\" = '1' " +
                             "LEFT JOIN \"COMPLEMENTARY\".\"SAT_GYOMTNT_MB\" t ON SUBSTRING(t.\"GYMTNTCD\" FROM 4) = u.\"USERCD\" AND t.\"JOTAIKBN\" = '1' " +
                             "WHERE u.\"USERCD\" = @userCd AND u.\"JOTAIKBN\" = '1'";

                using var conn = CreateConnection();
                using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@userCd", userCd);
                conn.Open();
                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    var dto = new SKL0001G01UserDto
                    {
                        UserCd = reader["USERCD"]?.ToString()?.Trim() ?? "",
                        UserName = reader["USERMEISHO"]?.ToString()?.Trim() ?? "",
                        Password = reader["PSW"]?.ToString()?.Trim() ?? "",
                        BushoCd = reader["BUSHOCD"]?.ToString()?.Trim() ?? "",
                        BushoName = reader["BUSHONAME"]?.ToString()?.Trim() ?? "",
                        GymTntCd = reader["GYMTNTCD"]?.ToString()?.Trim() ?? "",
                        GymTntMei = reader["GYMTNTMEI"]?.ToString()?.Trim() ?? ""
                    };
                    _logger.LogInformation("GetUserInfo 成功: userCd={UserCd}, userName={UserName}", dto.UserCd, dto.UserName);
                    return dto;
                }

                _logger.LogWarning("GetUserInfo 警告: 指定されたユーザーが見つかりません (userCd={UserCd})", userCd);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetUserInfo エラー: userCd={UserCd}", userCd);
                throw;
            }
        }

        /// <summary>
        /// 多重ログインチェックを行います。
        /// </summary>
        public SKL0001G01LoginStatusDto GetLoginStatus(string userCd)
        {
            _logger.LogInformation("GetLoginStatus 開始: userCd={UserCd}", userCd);

            try
            {
                string sql = "SELECT \"SYSNAME\" FROM \"COMPLEMENTARY\".\"SAT_LOGINJOHO\" WHERE \"USERCD\" = @userCd AND \"JOTAIKBN\" = '1'";

                using var conn = CreateConnection();
                using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@userCd", userCd);
                conn.Open();
                var obj = cmd.ExecuteScalar();
                if (obj != null && obj != DBNull.Value)
                {
                    var sysName = obj.ToString()?.Trim() ?? "";
                    _logger.LogInformation("GetLoginStatus 情報: すでにログイン中です (userCd={UserCd}, sysName={SysName})", userCd, sysName);
                    return new SKL0001G01LoginStatusDto
                    {
                        IsLogined = true,
                        SysName = sysName
                    };
                }

                _logger.LogInformation("GetLoginStatus 情報: ログインしていません (userCd={UserCd})", userCd);
                return new SKL0001G01LoginStatusDto { IsLogined = false };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetLoginStatus エラー: userCd={UserCd}", userCd);
                throw;
            }
        }

        /// <summary>
        /// ログイン情報を登録します。
        /// </summary>
        public bool InsertLoginInfo(SKL0001G01LoginRequest request)
        {
            // request が null の場合のガード節を追加
            if (request == null)
            {
                _logger.LogWarning("InsertLoginInfo 警告: request が null です。");
                return false;
            }

            _logger.LogInformation("InsertLoginInfo 開始: userCd={UserCd}", request.UserCd);

            string sql = "INSERT INTO \"COMPLEMENTARY\".\"SAT_LOGINJOHO\" " +
                        "(\"SYSTEMCD\", \"SYSNAME\", \"USERCD\", \"USERNAME\", \"BUSHOCD\", \"BUSHONAME\", \"TOROKUSHACD\", \"JOTAIKBN\") " +
                        "VALUES " +
                        "('10', '部材管理システム', @UserCd, @UserName, @BushoCd, @BushoName, @UserCd, '1')";

            try
            {
                _connectionFactory.ExecuteInPostgresTransaction((conn, tx) =>
                {
                    using var cmd = new NpgsqlCommand(sql, (NpgsqlConnection)conn, (NpgsqlTransaction)tx);
                    cmd.Parameters.AddWithValue("@UserCd", request.UserCd ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@UserName", request.UserName ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@BushoCd", request.BushoCd ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@BushoName", request.BushoName ?? (object)DBNull.Value);
                    
                    cmd.ExecuteNonQuery();
                }, _logger);

                _logger.LogInformation("InsertLoginInfo 成功: userCd={UserCd}", request.UserCd);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "InsertLoginInfo エラー: userCd={UserCd}", request.UserCd);
                return false;
            }
        }
    }
}