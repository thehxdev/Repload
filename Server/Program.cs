using System.Net;
using System.Net.Sockets;

namespace Server;

public static class Program
{
    public static async Task Main()
    {
        if (!Directory.Exists(Config.RootDirectory))
            Directory.CreateDirectory(Config.RootDirectory);
        using var serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
        {
            Blocking = false
        };
        serverSocket.Bind(new IPEndPoint(IPAddress.Parse(Config.ListenAddress), Config.ListenPort));
        serverSocket.Listen(Config.ListenBacklog);

        Console.WriteLine($"{DateTime.Now} Server listening on {Config.ListenAddress}:{Config.ListenPort}");
        while (true)
        {
            var client = await serverSocket.AcceptAsync();
            client.Blocking = false;

            _ = Task.Run(async () =>
            {
                var requestBuffer = new byte[256];
                // Read client request
                var requestLength = await client.ReceiveAsync(requestBuffer);
                switch ((Command)requestBuffer[0])
                {
                    case Command.Upload:
                        await Handlers.UploadCommand(client, requestBuffer, requestLength);
                        break;

                    case Command.Resume:
                        await Handlers.ResumeCommand(client, requestBuffer, requestLength);
                        break;
                }
                client.Close();
            });

            await Task.Yield();
        }
    }
}