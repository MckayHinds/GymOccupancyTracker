using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Protocol;
using System.Text;

public class MqttListenerService : BackgroundService
{
    private readonly IConfiguration _config;
    private readonly ILogger<MqttListenerService> _logger;
    private readonly OccupancyStore _store;

    public MqttListenerService(IConfiguration config, ILogger<MqttListenerService> logger, OccupancyStore store)
    {
        _config = config;
        _logger = logger;
        _store = store;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var host = _config["MQTT_HOST"];
        var portStr = _config["MQTT_PORT"];
        var user = _config["MQTT_USER"];
        var pass = _config["MQTT_PASS"];

        // ESP32 ->(incoming)
        var topicOut = _config["MQTT_TOPIC_OUT"] ?? "esp32DataChannel";

        // Backend ->(ACK / commands outgoing)
        var topicIn = _config["MQTT_TOPIC_IN"] ?? "esp32CommandChannel";

        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(portStr))
        {
            _logger.LogWarning("MQTT not configured (MQTT_HOST/MQTT_PORT missing). Skipping MQTT listener.");
            return;
        }

        if (!int.TryParse(portStr, out var port))
        {
            _logger.LogError("MQTT_PORT is not a valid integer: {Port}", portStr);
            return;
        }

        var factory = new MqttFactory();
        var mqttClient = factory.CreateMqttClient();

        // Receive messages from ESP32
        mqttClient.ApplicationMessageReceivedAsync += async e =>
        {
            try
            {
                var topic = e.ApplicationMessage.Topic;

                var seg = e.ApplicationMessage.PayloadSegment;
                var payload = seg.Array is null
                    ? ""
                    : Encoding.UTF8.GetString(seg.Array, seg.Offset, seg.Count);

                var accepted = _store.RecordEntry($"mqtt:{topic}", payload, out var active);

                _logger.LogInformation(
                    "MQTT msg topic={Topic} accepted={Accepted} activeLastHour={Active} payload={Payload}",
                    topic, accepted, active, payload
                );

                // ---------------- ACK BACK TO ESP32 ----------------
                // ESP32 should subscribe to MQTT_TOPIC_IN (esp32CommandChannel)
                var ackPayload = $"ACK|accepted={accepted}|activeLastHour={active}|utc={DateTime.UtcNow:O}";

                var ackMsg = new MqttApplicationMessageBuilder()
                    .WithTopic(topicIn)
                    .WithPayload(ackPayload)
                    .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
                    .Build();

                // Publish ACK
                await mqttClient.PublishAsync(ackMsg);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing MQTT message");
            }
        };

        var options = new MqttClientOptionsBuilder()
            .WithClientId($"backend-{Guid.NewGuid():N}")
            .WithTcpServer(host, port)
            .WithCredentials(user, pass)
            .WithTlsOptions(tls =>
            {
                tls.UseTls();
                tls.WithAllowUntrustedCertificates(true);
                tls.WithIgnoreCertificateChainErrors(true);
                tls.WithIgnoreCertificateRevocationErrors(true);
            })
            .WithCleanSession()
            .Build();

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (!mqttClient.IsConnected)
                {
                    _logger.LogInformation("Connecting to MQTT {Host}:{Port} ...", host, port);
                    await mqttClient.ConnectAsync(options, stoppingToken);

                    _logger.LogInformation("Connected. Subscribing to {Topic}", topicOut);

                    var filter = new MqttTopicFilterBuilder()
                        .WithTopic(topicOut)
                        .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
                        .Build();

                    await mqttClient.SubscribeAsync(filter, stoppingToken);

                    _logger.LogInformation("Subscribed to {Topic}. ACK topic is {AckTopic}", topicOut, topicIn);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "MQTT connect/subscribe failed. Retrying in 3 seconds...");
                await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
            }

            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
        }

        if (mqttClient.IsConnected)
        {
            await mqttClient.DisconnectAsync();
        }
    }
}

