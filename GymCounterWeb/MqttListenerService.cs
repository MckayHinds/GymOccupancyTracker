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
        var topicOut = _config["MQTT_TOPIC_OUT"] ?? "esp32DataChannel";

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

        //Correct receive hook for your MQTTnet API surface
        mqttClient.ApplicationMessageReceivedAsync += e =>
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
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing MQTT message");
            }

            return Task.CompletedTask;
        };

        // Correct TLS builder usage
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

                    //Subscribe using topic filter builder
                    var filter = new MqttTopicFilterBuilder()
                        .WithTopic(topicOut)
                        .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
                        .Build();

                    await mqttClient.SubscribeAsync(filter, stoppingToken);
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
