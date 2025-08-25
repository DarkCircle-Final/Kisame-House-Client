using MySqlConnector;


namespace client.Services
{
    public class DeviceLogService
    {
        private readonly string _connectionString = "Server=localhost;Database=kisame;Uid=root;Pwd=12345;Charset=utf8;";


        public async Task SaveLogAsync(Dictionary<string, string> logs)
        {
            using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();


            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
INSERT INTO devicelogs (heater, fan, O2, filtering, pump1, pump2, feed, led)
VALUES (@heater, @fan, @O2, @filtering, @pump1, @pump2, @feed, @led);";


            cmd.Parameters.AddWithValue("@heater", logs.GetValueOrDefault("heater", "OFF"));
            cmd.Parameters.AddWithValue("@fan", logs.GetValueOrDefault("fan", "OFF"));
            cmd.Parameters.AddWithValue("@O2", logs.GetValueOrDefault("O2", "OFF"));
            cmd.Parameters.AddWithValue("@filtering", logs.GetValueOrDefault("filtering", "OFF"));
            cmd.Parameters.AddWithValue("@pump1", logs.GetValueOrDefault("PUMP1", "OFF"));
            cmd.Parameters.AddWithValue("@pump2", logs.GetValueOrDefault("PUMP2", "OFF"));
            cmd.Parameters.AddWithValue("@feed", logs.GetValueOrDefault("Feed", "OFF"));
            cmd.Parameters.AddWithValue("@led", logs.GetValueOrDefault("LED", "OFF"));


            await cmd.ExecuteNonQueryAsync();
        }
    }
}