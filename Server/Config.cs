namespace Server;

public static class Config
{
    public static readonly string
        ListenAddress = "127.0.0.1",
        RootDirectory = Environment.ExpandEnvironmentVariables(@"%TEMP%\_ReploadUploades");

    public static readonly int
        ListenPort = 8080,
        ListenBacklog = 128, 
        BufferSize = 64 * 1024;
}