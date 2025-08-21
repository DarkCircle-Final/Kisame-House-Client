using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Protocol;
using System.Text;
using System.Text.Json;
using client.Models;
using System.Globalization;

namespace client.Services
{
    /// <summary>
    /// MQTT 구독/발행 + 센서 DB 저장 통합 서비스
    /// - 구독: aquabox/sensors, aquabox/logs, aquabox/control
    /// - 발행: aquabox/control
    /// - UI 이벤트: SensorsReceived / LogsReceived / ControlReceived
    /// - DB: aquabox/sensors 수신 시 sensingdatas INSERT
    /// </summary>
    public sealed class MqttService
    {
        private IMqttClient? _client;

        public event Action<Dictionary<string, string>>? SensorsReceived;
        public event Action<Dictionary<string, string>>? LogsReceived;
        public event Action<string>? ControlReceived;

        private readonly string _host;
        private readonly int _port;

        private readonly SensingRepository _repo;

        public bool IsConnected => _client?.IsConnected == true;

        /// <param name="mysqlConnStr">
        /// 예: "Server=127.0.0.1;Port=3306;Database=kisame;Uid=root;Pwd=12345;SslMode=None;"
        /// </param>
        public MqttService(
            string host = "210.119.12.68",
            int port = 1883,
            string mysqlConnStr = "Server=127.0.0.1;;Database=kisame;Uid=root;Pwd=12345;Charset=utf8;")
        {
            _host = host;
            _port = port;
            _repo = new SensingRepository(mysqlConnStr);
            Console.WriteLine($"[MQTT] using MySQL CS: {mysqlConnStr}");
        }

        public async Task ConnectAsync(CancellationToken ct = default)
        {
            var factory = new MqttFactory();
            _client = factory.CreateMqttClient();

            _client.ApplicationMessageReceivedAsync += async e =>
            {
                var topic = e.ApplicationMessage.Topic ?? "";
                var payloadBytes = e.ApplicationMessage.Payload ?? Array.Empty<byte>();
                var payload = Encoding.UTF8.GetString(payloadBytes);

                Console.WriteLine($"[MQTT] < {topic}  payload={payload}");

                try
                {
                    switch (topic)
                    {
                        case "aquabox/sensors":
                            {
                                // 두 형식 모두 지원:
                                // 1) {"gas":..., "humidity":..., ...}
                                // 2) {"sensors": { "gas":..., "humidity":..., ... }}
                                var dict = ParseSensorsToFlatMap(payload);
                                if (dict.Count == 0)
                                {
                                    Console.WriteLine("[MQTT] sensors payload could not be parsed.");
                                    break;
                                }

                                SensorsReceived?.Invoke(dict); // UI

                                var row = new SensingData
                                {
                                    gas = TryToFloat(dict, "gas"),
                                    humidity = TryToFloat(dict, "humidity"),
                                    temp = TryToFloat(dict, "temp"),
                                    tdsValue = TryToFloat(dict, "tdsValue"),
                                    water_temp = TryToFloat(dict, "water_temp"),
                                    ph = TryToFloat(dict, "ph"),
                                };

                                Console.WriteLine($"[DB] ready to insert: gas={row.gas}, hum={row.humidity}, temp={row.temp}, tds={row.tdsValue}, w_temp={row.water_temp}, ph={row.ph}");
                                await _repo.InsertAsync(row, ct);
                                break;
                            }

                        case "aquabox/logs":
                            {
                                var logs = ParseLogsToFlatMap(payload);
                                if (logs.Count > 0)
                                {
                                    LogsReceived?.Invoke(logs); // UI 전달

                                    var deviceLog = new DeviceLog
                                    {
                                        heater = logs.GetValueOrDefault("heater", "OFF"),
                                        fan = logs.GetValueOrDefault("fan", "OFF"),
                                        O2 = logs.GetValueOrDefault("O2", "OFF"),
                                        filtering = logs.GetValueOrDefault("filtering", "OFF"),
                                        pump1 = logs.GetValueOrDefault("pump1", "OFF"),
                                        pump2 = logs.GetValueOrDefault("pump2", "OFF"),
                                        feed = logs.GetValueOrDefault("feed", "OFF"),
                                        led = logs.GetValueOrDefault("led", "OFF")
                                    };

                                    await _repo.InsertDeviceLogAsync(deviceLog, ct);
                                }
                                break;
                            }

                        case "aquabox/control":
                            {
                                // "a"  또는 {"control":"a"}
                                string value = ParseControl(payload);
                                ControlReceived?.Invoke(value);
                                break;
                            }

                        default:
                            // 무시하거나 필요시 로그
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[MQTT] handler error:\n{ex.ToString()}");
                }
            };

            _client.ConnectedAsync += async _ =>
            {
                var subs = new MqttClientSubscribeOptionsBuilder()
                    .WithTopicFilter("aquabox/sensors")
                    .WithTopicFilter("aquabox/logs")
                    .WithTopicFilter("aquabox/control")
                    .Build();

                await _client!.SubscribeAsync(subs, ct);
                Console.WriteLine("[MQTT] Subscribed to topics.");
            };

            var options = new MqttClientOptionsBuilder()
                .WithTcpServer(_host, _port)
                .WithClientId($"maui-{Environment.MachineName}-{Guid.NewGuid():N}")
                .WithCleanSession()
                .Build();

            await _client.ConnectAsync(options, ct);
            Console.WriteLine("[MQTT] Connected.");
        }

        // ---------- Publish ----------
        public async Task PublishControlAsync(string control, CancellationToken ct = default)
        {
            if (_client is null || !_client.IsConnected)
            {
                Console.WriteLine("[MQTT] publish skipped: not connected.");
                return;
            }

            var msg = new MqttApplicationMessageBuilder()
                .WithTopic("aquabox/control")
                .WithPayload(Encoding.UTF8.GetBytes(control))
                .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
                .Build();

            await _client.PublishAsync(msg, ct);
            Console.WriteLine($"[MQTT] > aquabox/control  payload={control}");
        }

        public async Task PublishControlJsonAsync(string control, CancellationToken ct = default)
        {
            if (_client is null || !_client.IsConnected)
            {
                Console.WriteLine("[MQTT] publish skipped: not connected.");
                return;
            }

            var payload = $"{{\"control\":\"{control}\"}}";
            var msg = new MqttApplicationMessageBuilder()
                .WithTopic("aquabox/control")
                .WithPayload(payload)
                .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
                .Build();

            await _client.PublishAsync(msg, ct);
            Console.WriteLine($"[MQTT] > aquabox/control  payload={payload}");
        }

        // ---------- Parsing Utils ----------
        private static Dictionary<string, string> ParseSensorsToFlatMap(string json)
        {
            var map = new Dictionary<string, string>();
            using var doc = JsonDocument.Parse(json);

            JsonElement root = doc.RootElement;
            if (root.TryGetProperty("sensors", out var sensorsObj) && sensorsObj.ValueKind == JsonValueKind.Object)
            {
                root = sensorsObj; // nested 케이스
            }

            if (root.ValueKind != JsonValueKind.Object) return map;

            foreach (var p in root.EnumerateObject())
            {
                map[p.Name] = p.Value.ValueKind switch
                {
                    JsonValueKind.String => p.Value.GetString() ?? "",
                    JsonValueKind.Number => p.Value.ToString(),
                    _ => p.Value.ToString()
                };
            }
            return map;
        }

        private static Dictionary<string, string> ParseLogsToFlatMap(string json)
        {
            var map = new Dictionary<string, string>();
            using var doc = JsonDocument.Parse(json);

            if (doc.RootElement.ValueKind == JsonValueKind.Object)
            {
                if (doc.RootElement.TryGetProperty("logs", out var logsObj) && logsObj.ValueKind == JsonValueKind.Object)
                {
                    foreach (var p in logsObj.EnumerateObject())
                        map[p.Name] = p.Value.GetString() ?? "OFF";
                }
                else
                {
                    // flat logs
                    foreach (var p in doc.RootElement.EnumerateObject())
                        map[p.Name] = p.Value.GetString() ?? "OFF";
                }
            }
            return map;
        }

        private static string ParseControl(string jsonOrText)
        {
            try
            {
                using var doc = JsonDocument.Parse(jsonOrText);
                if (doc.RootElement.ValueKind == JsonValueKind.Object &&
                    doc.RootElement.TryGetProperty("control", out var cEl))
                {
                    return (cEl.GetString() ?? "").Trim();
                }
            }
            catch { /* plain text */ }
            return jsonOrText.Trim();
        }

        private static float? TryToFloat(Dictionary<string, string> map, string key)
        {
            if (!map.TryGetValue(key, out var s) || string.IsNullOrWhiteSpace(s))
                return null;

            s = s.Replace(',', '.');
            if (float.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var f))
                return f;

            if (double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var d))
                return (float)d;

            return null;
        }
    }
}
