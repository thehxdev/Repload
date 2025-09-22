using System.Net;
using System.Text;

namespace Client;

public static class Program
{
    private static readonly string serverAddress = "127.0.0.1";
    private static readonly int serverPort = 8080;

    public static async Task Main(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine($"Usage: client ...\n\tupload <path>\n\tresume <uuid> <path>");
            return;
        }

        switch (args[0])
        {
            case "upload":
                await UploadHandler(args[1..]);
                break;

            case "resume":
                await ResumeHandler(args[1..]);
                break;

            default:
                throw new Exception("invalid command");
        }
    }

    private static async Task UploadHandler(string[] args)
    {
        var filePath = args[0];
        var fileName = Path.GetFileName(filePath);
        if (fileName is null)
            throw new Exception("provide a path to an actual file");
        else if (fileName.Length > 255)
            throw new Exception("file name must be less than 255 characters");

        uint fileSize = (uint)new FileInfo(filePath).Length;
        using var sock = await IO.ConnectToServer(serverAddress, serverPort);

        int requestLength = 6 + fileName.Length;
        byte[] buffer = new byte[6 + 255];

        buffer[0] = 0x00;
        Array.ConstrainedCopy(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((int)fileSize)), 0, buffer, 1, 4);
        buffer[6] = (byte)fileName.Length;
        Array.ConstrainedCopy(Encoding.ASCII.GetBytes(fileName), 0, buffer, 7, fileName.Length);
        await sock.SendAsync(buffer.AsMemory(0, requestLength));

        await sock.ReceiveAsync(buffer);
        if (buffer[0] != 0)
            throw new Exception($"server sent {buffer[0]} response code");

        var uuid = Encoding.ASCII.GetString(buffer.AsSpan(1, 37));
        Console.WriteLine($"UUID: {uuid}");

        await IO.CopyFromFileToSocket(sock, filePath, 0, UploadProgressCallback);
    }

    private static async Task ResumeHandler(string[] args)
    {
        string uuid = args[0];
        byte[] uuidBytes = Encoding.ASCII.GetBytes(uuid);

        string filePath = args[1];
        using var sock = await IO.ConnectToServer(serverAddress, serverPort);

        var buffer = new byte[64];
        buffer[0] = 0x01;

        Array.ConstrainedCopy(uuidBytes, 0, buffer, 1, uuidBytes.Length);
        await sock.SendAsync(buffer.AsMemory(0, 37));

        await sock.ReceiveAsync(buffer);
        if (buffer[0] != 0)
            throw new Exception($"server sent {buffer[0]} response code");

        var position = (uint)IPAddress.NetworkToHostOrder(BitConverter.ToInt32(buffer.AsSpan(1, 4)));
        Console.WriteLine($"Position: {position}");

        await IO.CopyFromFileToSocket(sock, filePath, position, UploadProgressCallback);
    }

    private static void UploadProgressCallback(uint total, uint completed)
    {
        // not a nice progress bar on windows because \r also goes to next line :(
        Console.WriteLine($"\rsent {completed} of {total} bytes to socket ({(float)completed / total * 100,5:F1}%)");
    }
}