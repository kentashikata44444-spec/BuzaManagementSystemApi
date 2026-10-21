using BuzaiManagementApi.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using Dapper;

namespace BuzaiManagementApi.Repositories
{
    /// <summary>
    /// バーコードリーダーマスタ（SAT_BCREADER_M）に関するデータアクセスリポジトリ
    /// </summary>
    public class BarcodeReaderMasterRepository
    {
        private readonly IDbConnectionFactory _dbConnectionFactory;
        private readonly ILogger<BarcodeReaderMasterRepository> _logger;

        public BarcodeReaderMasterRepository(IDbConnectionFactory dbConnectionFactory, ILogger<BarcodeReaderMasterRepository> logger)
        {
            _dbConnectionFactory = dbConnectionFactory;
            _logger = logger;
        }

        /// <summary>
        /// バーコードリーダーマスタ（SAT_BCREADER_M）から一覧を取得する
        /// </summary>
        public IEnumerable<BarcodeReaderMasterDto> GetBarcodeReaderList()
        {
            _logger.LogInformation("GetBarcodeReaderList: SAT_BCREADER_M から一覧の取得を開始します");

            // 1行の通常の文字列として定義し、ASで大文字プロパティへ確実にマッピングする
            string sql = "SELECT \"HOZON_NM\" AS HOZON_NM, \"MAC_ADS\" AS MAC_ADS, \"MEISHO\" AS MEISHO, \"SEIZOMOTO\" AS SEIZOMOTO, \"MODERU_NO\" AS MODERU_NO, \"SHIRIARU_NO\" AS SHIRIARU_NO FROM \"COMPLEMENTARY\".\"SAT_BCREADER_M\"";

            try
            {
                using var connection = _dbConnectionFactory.CreatePostgresConnection();
                connection.Open();

                return connection.Query<BarcodeReaderMasterDto>(sql);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetBarcodeReaderList エラー: {Message}", ex.Message);
                throw;
            }
        }
    }
}