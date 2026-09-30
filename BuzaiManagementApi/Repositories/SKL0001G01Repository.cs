using BuzaiManagementApi.Models;
using Npgsql;
using System;
using System.Collections.Generic;

namespace BuzaiManagementApi.Repositories
{
    public class SKL0001G01Repository
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public SKL0001G01Repository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        private NpgsqlConnection CreateConnection()
        {
            return (NpgsqlConnection)_connectionFactory.CreatePostgresConnection();
        }

        /// <summary>
        /// 稼働フラグの取得 ("SL部材稼働フラグ")
        /// </summary>
        public string GetOperationFlag()
        {
            string sql = "SELECT \"MOJI1\" FROM \"COMPLEMENTARY\".\"SAT_UNYOJOKEN_MB\" WHERE \"UNYOCD\" = 'SL部材稼働フラグ' AND \"JOTAIKBN\" = '1'";

            using var conn = CreateConnection();
            using var cmd = new NpgsqlCommand(sql, conn);
            conn.Open();
            var result = cmd.ExecuteScalar();
            return result?.ToString()?.Trim() ?? "";
        }

        /// <summary>
        /// 稼働時間の取得 ("SL部材稼働時間")
        /// SUJI1: 開始時刻, SUJI2: 終了時刻[cite: 2]
        /// </summary>
        public SKL0001G01ConditionDto? GetOperationTime()
        {
            string sql = "SELECT \"SUJI1\", \"SUJI2\" FROM \"COMPLEMENTARY\".\"SAT_UNYOJOKEN_MB\" WHERE \"UNYOCD\" = 'SL部材稼働時間' AND \"JOTAIKBN\" = '1'";

            using var conn = CreateConnection();
            using var cmd = new NpgsqlCommand(sql, conn);
            conn.Open();
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return new SKL0001G01ConditionDto
                {
                    Suji1 = reader["SUJI1"]?.ToString()?.Trim(),
                    Suji2 = reader["SUJI2"]?.ToString()?.Trim()
                };
            }
            return null;
        }

        // ユーザー情報・部署情報・業務担当情報を取得
        public SKL0001G01UserDto? GetUserInfo(string userCd)
        {
            string sql = "SELECT u.\"USERCD\", u.\"USERMEISHO\", u.\"PSW\", u.\"BUSHOCD\", b.\"BUSHONAME\", " +
                        "t.\"GYMTNTCD\", t.\"GYMTNTMEI\" " +
                        "FROM \"COMPLEMENTARY\".\"SAT_USER_M\" u " +
                        "LEFT JOIN \"COMPLEMENTARY\".\"SAT_BUSHO_M\" b ON u.\"BUSHOCD\" = b.\"BUSHOCD\" AND b.\"JOTAIKBN\" = '1' " +
                        "LEFT JOIN \"COMPLEMENTARY\".\"SAT_GYOMTNT_MB\" t ON u.\"USERCD\" = t.\"USERCD\" AND t.\"JOTAIKBN\" = '1' " +
                        "WHERE u.\"USERCD\" = @userCd AND u.\"JOTAIKBN\" = '1'";

            using var conn = CreateConnection();
            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@userCd", userCd);
            conn.Open();
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return new SKL0001G01UserDto
                {
                    UserCd = reader["USERCD"]?.ToString()?.Trim() ?? "",
                    UserName = reader["USERMEISHO"]?.ToString()?.Trim() ?? "",
                    Password = reader["PSW"]?.ToString()?.Trim() ?? "",
                    BushoCd = reader["BUSHOCD"]?.ToString()?.Trim() ?? "",
                    BushoName = reader["BUSHONAME"]?.ToString()?.Trim() ?? "",
                    GymTntCd = reader["GYMTNTCD"]?.ToString()?.Trim() ?? "",
                    GymTntMei = reader["GYMTNTMEI"]?.ToString()?.Trim() ?? ""
                };
            }
            return null;
        }

        // 多重ログインチェック用[cite: 1]
        public SKL0001G01LoginStatusDto GetLoginStatus(string userCd)
        {
            string sql = "SELECT \"SYSNAME\" FROM \"COMPLEMENTARY\".\"SAT_LOGINJOHO\" WHERE \"USERCD\" = @userCd AND \"JOTAIKBN\" = '1'";

            using var conn = CreateConnection();
            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@userCd", userCd);
            conn.Open();
            var obj = cmd.ExecuteScalar();
            if (obj != null && obj != DBNull.Value)
            {
                return new SKL0001G01LoginStatusDto
                {
                    IsLogined = true,
                    SysName = obj.ToString()?.Trim() ?? ""
                };
            }
            return new SKL0001G01LoginStatusDto { IsLogined = false };
        }

        // ログイン情報の登録
        public bool InsertLoginInfo(SKL0001G01LoginRequest request)
        {
            string sql = "INSERT INTO \"COMPLEMENTARY\".\"SAT_LOGINJOHO\" " +
                        "(\"SYSTEMCD\", \"SYSNAME\", \"USERCD\", \"USERNAME\", \"BUSHOCD\", \"BUSHONAME\", \"TOROKUSHACD\", \"JOTAIKBN\") " +
                        "VALUES " +
                        "('10', '部材管理システム', @UserCd, @UserName, @BushoCd, @BushoName, @UserCd, '1')";

            try
            {
                // 共通のトランザクション実行拡張メソッドを使用する
                _connectionFactory.ExecuteInPostgresTransaction((conn, tx) =>
                {
                    using var cmd = new NpgsqlCommand(sql, (NpgsqlConnection)conn, (NpgsqlTransaction)tx);
                    cmd.Parameters.AddWithValue("@UserCd", request.UserCd);
                    cmd.Parameters.AddWithValue("@UserName", request.UserName);
                    cmd.Parameters.AddWithValue("@BushoCd", request.BushoCd);
                    cmd.Parameters.AddWithValue("@BushoName", request.BushoName);
                    
                    cmd.ExecuteNonQuery();
                });

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}