namespace client.Models;

using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Protocol;
using System.Text;

public class Heater
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
            .WithClientId("Heater_" + Guid.NewGuid())
            .Build();

        await _client.ConnectAsync(options);
    }

    // 히터 켜기  "a" 전송
    public Task TurnOnAsync() => PublishAsync("a");

    // 히터 끄기  "b" 전송
    public Task TurnOffAsync() => PublishAsync("b");

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
