using System.Net.Sockets;

namespace Server;

public static class IO
{
    public static async Task<uint>
        CopyFromSocketToFile(Socket socket, FileStream file, uint fileSize)
    {
        int nread;
        var buffer = new byte[Config.BufferSize];
        while (file.Position < fileSize)
        {
            try
            {
                nread = await socket.ReceiveAsync(buffer.AsMemory(0, buffer.Length));
                if (nread == 0)
                    break;
            }
            catch (Exception)
            {
                break;
            }
            await file.WriteAsync(buffer.AsMemory(0, nread));
        }
        return (uint)file.Position;
    }

    public static async Task<uint>
        CopyFromSocketToFile(Socket socket, string filePath, uint fileSize)
    {
        using var file = File.OpenWrite(filePath);
        file.Seek(0, SeekOrigin.End);
        return await CopyFromSocketToFile(socket, file, fileSize);
    }
}