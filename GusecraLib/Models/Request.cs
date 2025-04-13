using System.Buffers.Text;
using System.ComponentModel.DataAnnotations;
using System.Text;
using Gusecra.Security;
using System.IO.Compression;
using Microsoft.VisualBasic;
using SimpleBase;

namespace Gusecra.Models;

public static class Constants {
    public static readonly string Start = "<START>";
    public static readonly string Stop = "<STOP>";
}

public struct AttachedFile(string filename, byte[] content) {
    public string FileName = filename;
    public byte[] Content = content;
}

public struct Request(bool status=false, byte[]? message = null, string callsign="Anonymous", AttachedFile? file=null, DateTime? timestamp=null) {
    // File attachment
    public readonly AttachedFile? File = file;

    // False = malformed & True = complete 
    public readonly bool Status = status;

    // Timestamp
    public readonly DateTime TimeStamp = timestamp == null ? DateTime.Now : timestamp.GetValueOrDefault();

    // Hash (for detecting repeated messages)
    public readonly string Hash { 
        get {
            string status = this.Status.ToString();
            string timestamp = this.TimeStamp.Ticks.ToString();
            byte[] message = Message;
            List<byte> allData = [];
            allData.AddRange(Encoding.UTF8.GetBytes(status));
            allData.AddRange(Encoding.UTF8.GetBytes(timestamp));
            allData.AddRange(Encoding.UTF8.GetBytes(Callsign));
            allData.AddRange(message);
            byte[] hash = Security.Hash.Sha256Hash(allData.ToArray());
            return Convert.ToHexString(hash).ToLower();
        } 
    }

    // Content (decrypted)
    public readonly byte[] Message = message == null ? [] : message;

    // Callsign/alias (decrypted)
    public readonly string Callsign = string.IsNullOrEmpty(callsign) ? "Anonymous" : callsign;
}

public static class RequestUtils {
    /// <summary>
    /// Creates a string representation of the request. Encryption takes place here.
    /// </summary>
    /// <param name="request"></param>
    /// <param name="key"></param>
    /// <param name="iv"></param>
    /// <returns></returns>
    public static byte[] CreateRequest(Request request, byte[] key, byte[] iv) {
        var parts = new List<string>();
        if (request.File != null) {
            var file = request.File.GetValueOrDefault();
            parts.Add($"FILE {prepData(Encoding.UTF8.GetBytes(file.FileName), key, iv)}");
            parts.Add($"FILEC{prepData(file.Content, key, iv)}");
        }

        parts.Add($"TIME {prepData(Encoding.UTF8.GetBytes(DateTime.Now.Ticks.ToString()), key, iv)}");
        parts.Add($"MSG  {prepData(request.Message, key, iv)}");
        parts.Add($"CALL {prepData(Encoding.UTF8.GetBytes(request.Callsign), key, iv)}");

        string template = @$"{"\n\n"}
{Constants.Start}
{string.Join('\n', parts.ToArray())}
{Constants.Stop}{"\n\n"}";

        return Encoding.UTF8.GetBytes(template);
    }

    /// <summary>
    /// Returns the decoded request. Throws error if malformed.
    /// </summary>
    /// <param name="requestContent"></param>
    /// <param name="key"></param>
    /// <param name="iv"></param>
    /// <returns></returns>
    public static Request ParseRequest(string requestContent, byte[] key, byte[] iv) {
        string filename = "";
        string callsign = "";
        bool status = true;
        byte[] message = [];
        byte[] filecontent = [];
        DateTime timestamp = DateTime.Now;

        foreach(string line in requestContent.Split("\n")) {
            // Check if we even want to parse the line
            if (line.Length < 4) 
                continue;
            if (new string[]{Constants.Start, Constants.Stop}.Aggregate(false, (a,b) => line.Contains(b) || a))
                continue;

            // Parse the line (eg. MSG blablablac+olbase64==)
            string command = line[..5].Trim();
            string content = line.Substring(5).Trim();
            byte[] decrypted = readData(content, key, iv);
            switch(command) {
                case "FILEC":
                    filecontent = decrypted;
                    break;
                case "FILE":
                    filename = Encoding.UTF8.GetString(decrypted);
                    break;
                case "TIME":
                    timestamp = new DateTime(long.Parse(Encoding.UTF8.GetString(decrypted)));
                    break;
                case "CALL":
                    callsign = Encoding.UTF8.GetString(decrypted);
                    break;
                case "MSG":
                    message = decrypted;
                    break;
            }
        }

        AttachedFile? file = null;
        if (filename != "")  
            file = new AttachedFile(filename, filecontent);

        return new Request(status: status, 
                        message: message, 
                        callsign: callsign, 
                        timestamp: timestamp,
                        file: file);
    }

    #region helpers
    /// <summary>
    /// Converts from raw bytes to a format it will be transmitted in.
    /// </summary>
    /// <param name="data"></param>
    /// <param name="key"></param>
    /// <param name="iv"></param>
    /// <returns></returns>
    private static string prepData(byte[] data, byte[] key, byte[] iv)
        => encode(compress(AES256.Encrypt(data, key, iv)));

    /// <summary>
    /// Converts encrypted payload back to plaintext.
    /// </summary>
    /// <param name="data"></param>
    /// <param name="key"></param>
    /// <param name="iv"></param>
    /// <returns></returns>
    private static byte[] readData(string data, byte[] key, byte[] iv)
        => AES256.Decrypt(decompress(decode(data)), key, iv);


    /// <summary>
    /// Compresses the data.
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    private static byte[] compress(byte[] data) {
        MemoryStream input = new MemoryStream(data);
        using(MemoryStream compressStream = new MemoryStream())
        using(DeflateStream compressor = new DeflateStream(compressStream, CompressionMode.Compress))
        {
            input.CopyTo(compressor);
            compressor.Close();
            return compressStream.ToArray();
        }
    }

    /// <summary>
    /// Decompresses the data
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    private static byte[] decompress(byte[] input)
    {
        MemoryStream output = new MemoryStream();
        using (MemoryStream compressStream = new MemoryStream(input))
        using (DeflateStream decompressor = new DeflateStream(compressStream, CompressionMode.Decompress))
        decompressor.CopyTo(output);
        output.Position = 0;
        return output.ToArray();
    }

    /// <summary>
    /// Encodes it into a form it can be transmitted.
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    private static byte[] decode(string data) =>
        Base85.Ascii85.Decode(data);
    
    /// <summary>
    /// Decodes to bytes.
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    private static string encode(byte[] data) =>
        Base85.Ascii85.Encode(data);
    
    #endregion
}