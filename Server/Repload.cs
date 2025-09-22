namespace Server;

public enum Command : byte
{
    Upload,
    Resume,
}

public enum ResponseCode : byte
{
    OK,
    InvalidUUID,
    UploadAlreadyFinished,
    ZeroFileSize,
    GeneralError = 0xff,
}