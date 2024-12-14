using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Security.Authentication;
using MQTTnet;
using MQTTnet.Client;
//using MQTTnet.Client.Options;

class TcpToMqttBridge
{
    private static async Task Main(string[] args)
    {
        // Setup TCP server to listen on port 5000
        int tcpPort = 5000;
        TcpListener tcpServer = new TcpListener(IPAddress.Any, tcpPort);
        tcpServer.Start();
        Console.WriteLine($"TCP server started, listening on port {tcpPort}...");

        // Setup MQTT client with TLS
        var mqttFactory = new MqttFactory();
        var mqttClient = mqttFactory.CreateMqttClient();

        var mqttOptions = new MqttClientOptionsBuilder()
            .WithTcpServer("broker_url", 8883)
            .WithTls(new MqttClientOptionsBuilderTlsParameters
            {
                UseTls = true,
                SslProtocol = SslProtocols.Tls12
            })
            .WithCredentials("your_username", "password") // Add your HiveMQ Cloud credentials
            .WithClientId($"TcpToMqttClient_{Guid.NewGuid()}") // Unique client ID
            .Build();


        try
        {
            // Connect to the MQTT broker
            await mqttClient.ConnectAsync(mqttOptions);
            Console.WriteLine("Connected to HiveMQ Cloud broker.");

            // Accept incoming TCP client connection
            while (true)
            {
                TcpClient tcpClient = await tcpServer.AcceptTcpClientAsync();
                Console.WriteLine("TCP client connected.");

                // Handle each client in a separate task
                _ = HandleTcpClientAsync(tcpClient, mqttClient);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            Console.WriteLine($"StackTrace: {ex.StackTrace}");
        }
        finally
        {
            await mqttClient.DisconnectAsync();
            tcpServer.Stop();
            Console.WriteLine("TCP server stopped.");
        }
    }

    private static async Task HandleTcpClientAsync(TcpClient tcpClient, IMqttClient mqttClient)
    {
        try
        {
            using (tcpClient)
            using (NetworkStream tcpStream = tcpClient.GetStream())
            {
                byte[] buffer = new byte[1024];
                int bytesRead;

                while ((bytesRead = await tcpStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    string receivedData = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();
                    Console.WriteLine($"Received TCP Data: {receivedData}");

                    // Publish data to MQTT broker
                    var message = new MqttApplicationMessageBuilder()
                        .WithTopic("tcp/data") // You can change this topic
                        .WithPayload(receivedData)
                        .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
                        .WithRetainFlag(false)
                        .Build();

                    await mqttClient.PublishAsync(message);
                    Console.WriteLine($"Data published to MQTT topic 'tcp/data'");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error handling TCP client: {ex.Message}");
        }
    }
}
