namespace client.Models;

using MQTTnet;
using MQTTnet.Client;
using System.Text;

public class WaterPump1
{
    private readonly string _broker = "210.119.12.68";
    private readonly int _port = 1883;
    private readonly string _topic = "myfish_tank/control";

    private IMqttClient? _client;

    public async Task ConnectAsync()
    {
        var factory = new MqttFactory();
        _client = factory.CreateMqttClient();

        var options = new MqttClientOptionsBuilder()
            .WithTcpServer(_broker, _port)
            .WithClientId("WaterPump1_" + Guid.NewGuid())
            .Build();

        await _client.ConnectAsync(options);
    }

    public Task TurnOnAsync() => PublishAsync("e"); // e=ON
    public Task TurnOffAsync() => PublishAsync("f"); // f=OFF

    private async Task PublishAsync(string cmd)
    {
        if (_client == null || !_client.IsConnected)
            throw new InvalidOperationException("MQTT 연결이 안 되어 있습니다.");

        var msg = new MqttApplicationMessageBuilder()
            .WithTopic(_topic)
            .WithPayload(Encoding.UTF8.GetBytes(cmd))
            .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
            .Build();

        await _client.PublishAsync(msg);
    }
}
