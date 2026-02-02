using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Options;
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
        var topicOut = _config["MQTT_TOPIC_OUT"] ?? "esp32DataChannel";

        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(portStr))
        {
            _logger.LogWarning("MQTT not configured (MQTT_HOST/MQTT_PORT missing). Skipping MQTT listener.");
            return;
        }

        if (!int.TryParse(portStr, out var port))
        {
            _logger.LogError("MQTT_PORT is not a valid int: {PortStr}", portStr);
            return;
        }

        var factory = new MqttFactory();
        using var mqttClient = factory.CreateMqttClient();

        mqttClient.ApplicationMessageReceivedAsync += e =>
        {
            try
            {
                var topic = e.ApplicationMessage.Topic;
                var payloadBytes = e.ApplicationMessage.PayloadSegment.Array ?? Array.Empty<byte>();
                var payload = Encoding.UTF8.GetString(payloadBytes, e.ApplicationMessage.PayloadSegment.Offset, e.ApplicationMessage.PayloadSegment.Count);

                // Record it in occupancy store
                var accepted = _store.RecordEntry(source: $"mqtt:{topic}", rawMessage: payload, out var active);
                _logger.LogInformation("MQTT message topic={Topic} accepted={Accepted} activeLastHour={Active} payload={Payload}",
                    topic, accepted, active, payload);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing MQTT message");
            }

            return Task.CompletedTask;
        };

        // Build TLS options (broker uses 8883)
        var options = new MqttClientOptionsBuilder()
            .WithClientId($"backend-{Guid.NewGuid():N}")
            .WithTcpServer(host, port)
            .WithCredentials(user, pass)
            .WithTls(new MqttClientOptionsBuilderTlsParameters
            {
                UseTls = true,
                AllowUntrustedCertificates = true,
                IgnoreCertificateChainErrors = true,
                IgnoreCertificateRevocationErrors = true 
            })
            .WithCleanSession()
            .Build();

        // Simple reconnect loop
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (!mqttClient.IsConnected)
                {
                    _logger.LogInformation("Connecting to MQTT broker {Host}:{Port} ...", host, port);
                    await mqttClient.ConnectAsync(options, stoppingToken);

                    _logger.LogInformation("Connected. Subscribing to {TopicOut}", topicOut);
                    await mqttClient.SubscribeAsync(topicOut, MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce, stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "MQTT connect/subscribe failed. Retrying in 3s...");
                await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
            }

            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
        }

        // disconnect
        if (mqttClient.IsConnected)
        {
            await mqttClient.DisconnectAsync(cancellationToken: CancellationToken.None);
        }
    }
}
