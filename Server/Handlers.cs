using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Server;

public static class Handlers
{
    public static async Task UploadCommand(Socket client, byte[] request, int requestLength)
    {
        uint fileSize = (uint)IPAddress.NetworkToHostOrder(BitConverter.ToInt32(request.AsSpan(1, 4)));
        if (fileSize == 0)
        {
            await client.SendAsync((byte[])[(byte)ResponseCode.ZeroFileSize]);
            return;
        }

        string? fileName = null;
        byte fileNameLength = request[5];
        if (fileNameLength > 0)
            fileName = Encoding.ASCII.GetString(request.AsSpan(6, fileNameLength));

        var buffer = new byte[Config.BufferSize];
        string uuidString = Guid.NewGuid().ToString();
        byte[] uuidBytes = Encoding.ASCII.GetBytes(uuidString);

        buffer[0] = 0x00;
        Array.ConstrainedCopy(uuidBytes, 0, buffer, 1, uuidBytes.Length);

        // Send response
        int responseLength = 1 + uuidBytes.Length;
        await client.SendAsync(buffer.AsMemory(0, responseLength));

        // Create an empty file
        string filePath = Path.Join(Config.RootDirectory, uuidString);
        uint nwritten = await IO.CopyFromSocketToFile(client, filePath, fileSize);
        if (nwritten == 0 || nwritten > fileSize)
        {
            // If client sent more bytes than expected or no data has been read,
            // delete the file
            try
            {
                File.Delete(filePath);
            }
            catch { /* ignore the exception. nothing we can do here */ }

            return;
        }

        using var db = new Models.ServerDbContext();
        var uploadedFile = new Models.File(uuidString, fileSize, nwritten, fileName);

        await db.Files.AddAsync(uploadedFile);
        await db.SaveChangesAsync();
    }

    public static async Task ResumeCommand(Socket client, byte[] request, int requestLength)
    {
        if (requestLength != 37)
            return;

        string uuid = Encoding.ASCII.GetString(request.AsSpan(1, 36));

        int responseLength;
        var response = new byte[Config.BufferSize];

        using var db = new Models.ServerDbContext();
        var uploadedFile = await db.Files.FindAsync(uuid);
        if (uploadedFile is null)
        {
            response[0] = (byte)ResponseCode.InvalidUUID;
            responseLength = 1;
        }
        else if (uploadedFile.Size == uploadedFile.Written)
        {
            response[0] = (byte)ResponseCode.UploadAlreadyFinished;
            responseLength = 1;
        }
        else
        {
            response[0] = (byte)ResponseCode.OK;
            Array.ConstrainedCopy(
                BitConverter.GetBytes(IPAddress.HostToNetworkOrder((int)uploadedFile.Written)),
                0,
                response,
                1,
                4);
            responseLength = 5;
        }

        await client.SendAsync(response.AsMemory(0, responseLength));
        if (responseLength == 1)
            return;

        var filePath = Path.Join(Config.RootDirectory, uuid);
        uint nwritten = await IO.CopyFromSocketToFile(client, filePath, uploadedFile!.Size);
        if (nwritten > uploadedFile.Size)
        {
            try
            {
                File.Delete(filePath);
                db.Files.Remove(uploadedFile);
            }
            catch { /* ignore the exception. nothing we can do here */ }
        }
        else
        {
            uploadedFile.Written = nwritten;
            db.Files.Update(uploadedFile);
        }

        await db.SaveChangesAsync();
    }
}