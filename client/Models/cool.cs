namespace client.Models;

using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Protocol;
using System.Text;

public class Cooler
{
    private readonly string _broker = "210.119.12.68";       // MQTT 브로커 IP
    private readonly int _port = 1883;                       // MQTT 포트
    private readonly string _topic = "myfish_tank/control";  // 제어 토픽

    private IMqttClient? _client;

    // 브로커 연결
    public async Task ConnectAsync()
    {
        var factory = new MqttFactory();
        _client = factory.CreateMqttClient();

        var options = new MqttClientOptionsBuilder()
            .WithTcpServer(_broker, _port)
            .WithClientId("Cooler_" + Guid.NewGuid())
            .Build();

        await _client.ConnectAsync(options);
    }

    // 냉각 ON  "c"
    public Task TurnOnAsync() => PublishAsync("c");

    // 냉각 OFF  "d"
    public Task TurnOffAsync() => PublishAsync("d");

    // 메시지 발행
    private async Task PublishAsync(string cmd)
    {
        if (_client == null || !_client.IsConnected)
            throw new InvalidOperationException("MQTT 브로커에 연결되어 있지 않습니다.");

        var msg = new MqttApplicationMessageBuilder()
            .WithTopic(_topic)
            .WithPayload(Encoding.UTF8.GetBytes(cmd))
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
            .Build();

        await _client.PublishAsync(msg);
    }
}
