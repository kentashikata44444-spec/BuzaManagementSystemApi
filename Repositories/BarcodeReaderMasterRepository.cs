using BuzaiManagementApi.Models;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;

namespace BuzaiManagementApi.Repositories
{
    /// <summary>
    /// Program.cs でキャッシュされた Reader.txt の内容を提供するリポジトリ
    /// </summary>
    public class BarcodeReaderMasterRepository
    {
        private readonly IReadOnlyList<DeviceMappingDto> _cachedDeviceMappings;
        private readonly ILogger<BarcodeReaderMasterRepository> _logger;

        public BarcodeReaderMasterRepository(
            IReadOnlyList<DeviceMappingDto> cachedDeviceMappings,
            ILogger<BarcodeReaderMasterRepository> logger)
        {
            _cachedDeviceMappings = cachedDeviceMappings;
            _logger = logger;
        }

        /// <summary>
        /// キャッシュされた Reader.txt のデータ一覧を DeviceMappingDto 形式で取得する
        /// </summary>
        public IEnumerable<DeviceMappingDto> GetBarcodeReaderList()
        {
            _logger.LogInformation("GetBarcodeReaderList: キャッシュされた Reader.txt データから取得します（件数: {Count}件）", _cachedDeviceMappings.Count);
            return _cachedDeviceMappings;
        }
    }
}