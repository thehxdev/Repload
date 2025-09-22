# Repload specification

## Problem Description
A resumable file upload protocol.
### Success criteria
Be able to upload a file from client to server. Once the upload process has finished, both files on the client and server must have same SHA256 hash. Client can disconnect any time in the upload process then resume from where it's left.
### User stories
What users might want or expect when they upload a file to a server?
- As a user I want to be able to upload a file, then pause the process and after a while resume from where it's left.
- As a user I want to be able to resume the upload process after reconnecting to the internet.
---
## Terminology
- A byte or 1 byte is a unit of information that has 8 bits.
- `read` and `write` calls refer to operating system's way of receiving and sending data from a network connection respectively.
- In this document, UUID is a UUID version 4.
- Hexadecimal values are prefixed with `0x`.
## Protocol
At first, client sends a request with a command to the server. The command must be the first byte and it specifies how rest of the request must be parsed. A request packet must be sent with **JUST ONE** `write` call and server must receive that with **JUST ONE** `read` call. Server sends a response to each request and client must act depending on server response. Any variable sized data in the request and response phase must be length prefixed.
List of commands that the server must implement:
- `0x00` Upload
- `0x01` Resume

Generally a request looks like this. First byte is the command and the rest is command's data.
```
Command (1 byte)  |  Command data...
```

Server responds to client's request by sending a response. The first byte in the response contains the response code. Ok responses' structure vary on request type but error responses contain just the error code and nothing else. Server must close the connection after sending error response.
List of response codes that the server may return:
- `0x00` OK
- `0x01` Invalid UUID
- `0x02` Upload already finished
- `0x03` Zero file size
- `0xff` General error

### Upload request
The upload command informs the server that user wants to upload a new file. The client sends upload command with a 4 byte big endian unsigned integer value indicating the file's size, followed by a length-prefixed file name. The file name must not contain any path traversal character like `/` and `\`.

Structure of an upload request:
```
Command (0x00) | File size (4 byte unsigned big endian)  |  File name length (1 byte)  |  File name (varibale)
```
#### Server response
Server must assign each file a unique identifier. In this document I'll use a UUID but this is implementation specific. Any time a client wants to interact with a file on the server, it must use it's unique identifier. As part of the response to the upload request, server sends back the UUID it assigned to the corresponding file.
```
OK (0x00) | UUID (36 bytes)
```
Then the client can proceed and start uploading. Each payload must not be more than `65536` bytes. Server must create an empty file with the same name as it's UUID on the persistent storage and write bytes as it receives them. Server must not receive more than `File size` bytes.

### Resume request
To resume upload process client must send a Resume request with the corresponding file's UUID.

Structure of a Resume request:
```
Command (0x01) | UUID (36 bytes)
```
#### Server response
First, server must check for the UUID provided by the client request. In case of any error, server must send corresponding response code and close the connection.
Possible error response codes for Resume request:
- Invalid UUID
- Upload already finished
##### Ok response
```
OK (0x00) | Position (4 byte unsigned big endian)
```
Client must send bytes from `Position` byte (the byte in `Position` position itself is included). Server must not receive more than remaining bytes since it already knows the file's size.

---
## Implementation and entities
This section is implementation specific. You may skip this.
### File entity
A `File` describes a file within the system.

Properties of a `File` structure:
- **Name**: Original name of the file.
- **UUID**: Identifies a unique file in the system. Must be indexed in a database. The UUID is also the file name in the persistent storage.
- **Size**: Size of the file in bytes. Server must not read more bytes than `Size` value from stream.
- **Written**: Bytes written to persistent storage. This field must be equal to `Size` field if the upload process has been completed.

Optional properties:
- **Hash**: SHA256 sum of the file. Must be indexed in a database.
- **SameAs**: UUID of a file that has same `Hash` as current file. Using this field, server can keep the oldest copy of that file and remove others from persistent storage to save space.
- **UploadStartTime**: When the upload process started. Upload process starts when the server sends Ok response back to client. Server may use this field to cleanup old, dead and unfinished uploads.
- **UploadFinishTime**: When the upload process finished. Server may use this field to cleanup old files.
