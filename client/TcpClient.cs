using System;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

class TcpClientExample
{
    static async Task Main()
    {
        string serverIp = "127.0.0.1";   // TCP server IP address
        int serverPort = 5000;
        int i = 1;       
        try
        {
            // Initialize HttpClient to fetch data from API
            using HttpClient httpClient = new HttpClient();

            // Create TCP client and connect to the server
            TcpClient client = new TcpClient(serverIp, serverPort);
            NetworkStream stream = client.GetStream();
            Console.WriteLine("Connected to the server.");

            while (true) // Infinite loop for real-time data transfer
            {
                try
                {
                    // Fetch data from the API
                    string apiUrl = $"https://dummyjson.com/quotes/{i}";

                    HttpResponseMessage response = await httpClient.GetAsync(apiUrl);
                    response.EnsureSuccessStatusCode();

                    string apiData = await response.Content.ReadAsStringAsync();
                    i++;
                    // Send API data to the TCP server
                    byte[] data = Encoding.UTF8.GetBytes(apiData);
                    stream.Write(data, 0, data.Length);

                    Console.WriteLine($"Sent to server: {apiData}");

                    // Delay for a few seconds before fetching again
                    await Task.Delay(500); // 2-second delay

                }
                catch (Exception apiEx)
                {
                    Console.WriteLine("Error fetching API data: " + apiEx.Message);
                }
            }

            // Close the connection (this won't be reached unless the loop exits)
            stream.Close();
            client.Close();
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error: " + ex.Message);
        }
    }
}
