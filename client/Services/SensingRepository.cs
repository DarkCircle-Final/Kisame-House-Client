// log랑 센서 수치 같이 넣음
using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using MySqlConnector;
using client.Models;


namespace client.Services
{
    public class SensingRepository
    {
        private readonly string _connectionString;

        public SensingRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task InsertAsync(SensingData s, CancellationToken ct = default)
        {
            const string sql = @"
INSERT INTO sensingdatas
(gas, humidity, temp, tdsValue, water_temp, ph)
VALUES (@gas, @humidity, @temp, @tdsValue, @water_temp, @ph);";

            await using var conn = new MySqlConnection(_connectionString);
            try
            {
                await conn.OpenAsync(ct);
                await using var cmd = new MySqlCommand(sql, conn);

                cmd.Parameters.Add("@gas", MySqlDbType.Float).Value = (object?)s.gas ?? DBNull.Value;
                cmd.Parameters.Add("@humidity", MySqlDbType.Float).Value = (object?)s.humidity ?? DBNull.Value;
                cmd.Parameters.Add("@temp", MySqlDbType.Float).Value = (object?)s.temp ?? DBNull.Value;
                cmd.Parameters.Add("@tdsValue", MySqlDbType.Float).Value = (object?)s.tdsValue ?? DBNull.Value;
                cmd.Parameters.Add("@water_temp", MySqlDbType.Float).Value = (object?)s.water_temp ?? DBNull.Value;
                cmd.Parameters.Add("@ph", MySqlDbType.Float).Value = (object?)s.ph ?? DBNull.Value;

                var n = await cmd.ExecuteNonQueryAsync(ct);
                Console.WriteLine($"[DB] sensingdatas INSERT affected={n}");
            }
            catch (Exception ex)
            {
                Console.WriteLine("[DB] INSERT error:\n" + ex.ToString());
                throw; // 상위에서 한 번 더 로깅
            }
        }


        public async Task InsertDeviceLogAsync(DeviceLog log, CancellationToken ct = default)
        {
            const string sql = @"
INSERT INTO devicelogs
(heater, fan, O2, filtering, pump1, pump2, feed, led)
VALUES (@heater, @fan, @O2, @filtering, @pump1, @pump2, @feed, @led);";

            await using var conn = new MySqlConnection(_connectionString);
            try
            {
                await conn.OpenAsync(ct);
                await using var cmd = new MySqlCommand(sql, conn);

                cmd.Parameters.AddWithValue("@heater", log.heater);
                cmd.Parameters.AddWithValue("@fan", log.fan);
                cmd.Parameters.AddWithValue("@O2", log.O2);
                cmd.Parameters.AddWithValue("@filtering", log.filtering);
                cmd.Parameters.AddWithValue("@pump1", log.pump1);
                cmd.Parameters.AddWithValue("@pump2", log.pump2);
                cmd.Parameters.AddWithValue("@feed", log.feed);
                cmd.Parameters.AddWithValue("@led", log.led);

                var n = await cmd.ExecuteNonQueryAsync(ct);
                Console.WriteLine($"[DB] devicelogs INSERT affected={n}");
            }
            catch (Exception ex)
            {
                Console.WriteLine("[DB] LOG INSERT error:\n" + ex.ToString());
                throw;
            }
        }


        public async Task<List<(DateTime Timestamp, SensingData Sensor, DeviceLog Log)>> GetMergedDataAsync()
        {
            const string sql = @"
SELECT s.sensingdate, s.gas, s.humidity, s.temp, s.tdsValue, s.water_temp, s.ph,
       d.logdate, d.heater, d.fan, d.O2, d.filtering, d.pump1, d.pump2, d.feed, d.led
FROM sensingdatas s
JOIN devicelogs d ON DATE_FORMAT(s.sensingdate, '%Y-%m-%d %H:%i:%s') = DATE_FORMAT(d.logdate, '%Y-%m-%d %H:%i:%s')
ORDER BY s.sensingdate DESC
LIMIT 1000;";

            var result = new List<(DateTime, SensingData, DeviceLog)>();

            await using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();
            await using var cmd = new MySqlCommand(sql, conn);
            await using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var timestamp = reader.GetDateTime(0);
                var sensor = new SensingData
                {
                    gas = reader.IsDBNull(1) ? null : reader.GetFloat(1),
                    humidity = reader.IsDBNull(2) ? null : reader.GetFloat(2),
                    temp = reader.IsDBNull(3) ? null : reader.GetFloat(3),
                    tdsValue = reader.IsDBNull(4) ? null : reader.GetFloat(4),
                    water_temp = reader.IsDBNull(5) ? null : reader.GetFloat(5),
                    ph = reader.IsDBNull(6) ? null : reader.GetFloat(6)
                };
                var log = new DeviceLog
                {
                    heater = reader.GetString(8),
                    fan = reader.GetString(9),
                    O2 = reader.GetString(10),
                    filtering = reader.GetString(11),
                    pump1 = reader.GetString(12),
                    pump2 = reader.GetString(13),
                    feed = reader.GetString(14),
                    led = reader.GetString(15)
                };
                result.Add((timestamp, sensor, log));
            }

            return result;
        }
    }
}
