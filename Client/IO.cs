using System;
using System.Net;
using System.Net.Sockets;
namespace Client;

public delegate void ProgressCallback(uint total, uint completed);

public static class IO
{
    /// <summary>
    /// sends content of a <see cref="FileStream"/> to a <see cref="Socket"/> starting from
    /// file's current position. Calls <paramref name="pcb"/> callback each time sends new
    /// data to socket.
    /// </summary>
    /// <param name="socket"></param>
    /// <param name="file"></param>
    /// <param name="pcb"></param>
    public static async Task
        CopyFromFileToSocket(Socket socket, FileStream file, ProgressCallback pcb)
    {
        //var buffer = new byte[64 * 1024];
        var buffer = new byte[256];
        uint nsent = (uint)file.Position;
        while (nsent < file.Length)
        {
            int nread = await file.ReadAsync(buffer.AsMemory(0, buffer.Length));
            nsent += (uint)await socket.SendAsync(buffer.AsMemory(0, nread));
            pcb((uint)file.Length, nsent);
            // simulate slow network connection
            Thread.Sleep(500);
        }
    }

    public static async Task
        CopyFromFileToSocket(Socket socket, string path, uint position, ProgressCallback pcb)
    {
        using var file = File.OpenRead(path);
        file.Seek(position, SeekOrigin.Begin);
        Console.WriteLine($"resuming from {file.Position}");
        await CopyFromFileToSocket(socket, file, pcb);
    }

    public static async Task<Socket> ConnectToServer(string address, int port)
    {
        var s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
        {
            Blocking = false,
        };
        await s.ConnectAsync(IPAddress.Parse(address), port);
        return s;
    }
}