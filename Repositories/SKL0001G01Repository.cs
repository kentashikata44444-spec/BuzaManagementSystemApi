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
        /// ユーザー情報・部署情報・業務担当情報を個別にチェックして取得します。
        /// </summary>
        public SKL0001G01UserValidationResult GetUserInfoDetailed(string userCd)
        {
            _logger.LogInformation("GetUserInfoDetailed 開始: userCd={UserCd}", userCd);

            try
            {
                // ▼ 修正: t."GYMTNTCD" を "GYMTNTCD" として取得できるように修正
                string sql = "SELECT u.\"USERCD\", u.\"USERMEISHO\", u.\"PSW\", u.\"BUSHOCD\", " +
                             "b.\"BUSHOCD\" AS BUSHO_CHECK, b.\"BUSHONAME\", " +
                             "t.\"GYMTNTCD\", t.\"GYMTNTCD\" AS TNT_CHECK, t.\"GYMTNTMEI\" " +
                             "FROM \"COMPLEMENTARY\".\"SAT_USER_M\" u " +
                             "LEFT JOIN \"COMPLEMENTARY\".\"SAT_BUSHO_M\" b ON u.\"BUSHOCD\" = b.\"BUSHOCD\" AND b.\"JOTAIKBN\" = '1' " +
                             "LEFT JOIN \"COMPLEMENTARY\".\"SAT_GYOMTNT_MB\" t ON t.\"USERCD\" = u.\"USERCD\" AND t.\"JOTAIKBN\" = '1' " +
                             "WHERE u.\"USERCD\" = @userCd AND u.\"JOTAIKBN\" = '1'";

                using var conn = CreateConnection();
                using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@userCd", userCd);
                conn.Open();

                using var reader = cmd.ExecuteReader();
                if (!reader.Read())
                {
                    // 1. ユーザーマスタ自体が存在しない、または無効
                    _logger.LogWarning("GetUserInfoDetailed 警告: ユーザーが存在しないか無効です (userCd={UserCd})", userCd);
                    return new SKL0001G01UserValidationResult { Status = UserErrorStatus.UserNotFound, Message = "ユーザーIDが登録されていないか、無効なユーザーです。" };
                }

                // 2. 部署マスタのチェック (BUSHO_CHECK が NULL なら部署マスタに不備あり)
                var bushoCheck = reader["BUSHO_CHECK"]?.ToString();
                if (string.IsNullOrWhiteSpace(bushoCheck))
                {
                    _logger.LogWarning("GetUserInfoDetailed 警告: 部署マスタが見つからないか無効です (userCd={UserCd})", userCd);
                    return new SKL0001G01UserValidationResult { Status = UserErrorStatus.BushoNotFound, Message = "部署マスタが登録されていません \nシステム管理者に問合せ願います" };
                }

                // 3. 業務担当マスタのチェック (TNT_CHECK が NULL なら業務担当マスタに不備あり)
                var tntCheck = reader["TNT_CHECK"]?.ToString();
                if (string.IsNullOrWhiteSpace(tntCheck))
                {
                    _logger.LogWarning("GetUserInfoDetailed 警告: 業務担当マスタが見つからないか無効です (userCd={UserCd})", userCd);
                    return new SKL0001G01UserValidationResult { Status = UserErrorStatus.GymTntNotFound, Message = "業務担当マスタが登録されていません \nシステム管理者に問合せ願います" };
                }

                // すべて正常に取得できた場合
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

                _logger.LogInformation("GetUserInfoDetailed 成功: userCd={UserCd}", userCd);
                return new SKL0001G01UserValidationResult { Status = UserErrorStatus.Success, UserDto = dto };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetUserInfoDetailed エラー: userCd={UserCd}", userCd);
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
            if (request == null)
            {
                _logger.LogWarning("InsertLoginInfo 警告: request が null です。");
                return false;
            }

            _logger.LogInformation("InsertLoginInfo 開始: userCd={UserCd}", request.UserCd);

            // 古いログイン情報を削除するSQL（異常終了などで残ってしまったセッションの掃除）
            string deleteSql = "DELETE FROM \"COMPLEMENTARY\".\"SAT_LOGINJOHO\" WHERE \"USERCD\" = @UserCd";

            // 新規登録するSQL
            string insertSql = "INSERT INTO \"COMPLEMENTARY\".\"SAT_LOGINJOHO\" " +
                            "(\"SYSTEMCD\", \"SYSNAME\", \"USERCD\", \"USERNAME\", \"BUSHOCD\", \"BUSHONAME\", \"TOROKUSHACD\", \"JOTAIKBN\") " +
                            "VALUES " +
                            "('10', '部材管理システム', @UserCd, @UserName, @BushoCd, @BushoName, @UserCd, '1')";

            try
            {
                _connectionFactory.ExecuteInPostgresTransaction((conn, tx) =>
                {
                    // 1. 念のため既存のログイン情報を削除して多重ログインを防ぐ
                    using var deleteCmd = new NpgsqlCommand(deleteSql, (NpgsqlConnection)conn, (NpgsqlTransaction)tx);
                    deleteCmd.Parameters.AddWithValue("@UserCd", request.UserCd ?? (object)DBNull.Value);
                    deleteCmd.ExecuteNonQuery();

                    // 2. 新しいログイン情報を登録
                    using var insertCmd = new NpgsqlCommand(insertSql, (NpgsqlConnection)conn, (NpgsqlTransaction)tx);
                    insertCmd.Parameters.AddWithValue("@UserCd", request.UserCd ?? (object)DBNull.Value);
                    insertCmd.Parameters.AddWithValue("@UserName", request.UserName ?? (object)DBNull.Value);
                    insertCmd.Parameters.AddWithValue("@BushoCd", request.BushoCd ?? (object)DBNull.Value);
                    insertCmd.Parameters.AddWithValue("@BushoName", request.BushoName ?? (object)DBNull.Value);
                    
                    insertCmd.ExecuteNonQuery();
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