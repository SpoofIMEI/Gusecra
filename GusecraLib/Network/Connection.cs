using System.Data.SqlTypes;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Unicode;
using Gusecra.Models;
using Gusecra.Security;
using Microsoft.Extensions.Logging;

namespace Gusecra;
public class GusecraConnection {
    readonly static private string defaultKey = "none1";
    readonly static private string defaultIv = "none2";

    private bool closed = false;
    private Socket socket;
    private string requestParts = "";
    private string cache = "";
    private bool readingRequest = false;
    private Dictionary<string, bool> messageList = [];
    private byte[] key = Encoding.UTF32.GetBytes(defaultKey);
    private byte[] iv = Encoding.UTF8.GetBytes(defaultIv);
    private ILogger? logger;

    /// <summary>
    /// Connects to the fldigi server. If password and IV are not 
    /// defined, a predefined value will be used making the encryption
    /// useless.
    /// </summary>
    /// <param name="host"></param>
    /// <param name="port"></param>
    /// <param name="password"></param>
    /// <param name="iv"></param>
    public GusecraConnection(string host="127.0.0.1", int port=7322, string password="none1", string iv="") {
        SetPassword(password, iv);

        IPHostEntry ipHost = Dns.GetHostEntry(host);
        IPAddress ip = ipHost.AddressList[0];
        IPEndPoint ipEndpoint = new(ip, port);
        this.socket = new(ipEndpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        this.socket.Connect(ipEndpoint);
    }

    /// <summary>
    /// Can change the password on the fly.
    /// </summary>
    /// <param name="password"></param>
    /// <param name="iv"></param>
    public void SetPassword(string password, string iv="") {
        byte[] finalIv = [];
        if (iv == "") 
            finalIv = Security.Hash.Sha256Hash(Encoding.UTF8.GetBytes(password))[..^3];

        this.key = Encoding.UTF8.GetBytes(password);
        this.iv = finalIv;
    }

    /// <summary>
    /// Adds a logger that will be used to communicate all sorts
    /// of information.
    /// </summary>
    /// <param name="logger"></param>
    /// <returns></returns>
    public GusecraConnection AddLogger(ILogger? logger) {
        this.logger = logger;
        return this;
    }

    public void Close() {
        socket.Close();
        this.messageList.Clear();
        this.key = [];
        this.iv = [];
        closed = true;
    }

    /// <summary>
    /// Writes a request to the fldigi server listener.
    /// Note, actually transmitting the signal takes
    /// longer than the time it takes to send the data 
    /// to fldigi. Repeat is the amount of time the message
    /// is repeated. The more the more reliable and slower it
    /// becomes.
    /// </summary>
    /// <param name="request"></param>
    /// <param name="repeat"></param>
    public void Write(Request request, int repeat) {
        var req = RequestUtils.CreateRequest(request, key, iv);
        for (int count = 0; count < repeat; count++) 
            this.socket.Send(req);
    }

    /// <summary>
    /// Waits for a request and then returns it.
    /// </summary>
    /// <returns></returns>
    public Request Read() {
        // Wait for a request
        Request request = new(false);
        while (!request.Status && !closed) {
            // Read fldigi
            var buffer = new byte[1024];
            int received = socket.Receive(buffer);
            buffer = buffer.ToList().GetRange(0, received).ToArray();

            request = processChunk(buffer);
            if (request.Status == false)
                continue;
                
            if (messageList.ContainsKey(request.Hash)) {
                request = new(false);
                continue;
            }
            messageList.Add(request.Hash, true);
        }
        
        return request;
    }


    /// <summary>
    /// Processes a chunk of bytes and returns a request
    /// if one if found, otherwise it will return a
    /// request with the status of 'false'.
    /// </summary>
    /// <param name="buffer"></param>
    /// <returns></returns>
    private Request processChunk(byte[] buffer) {
        string decoded = Encoding.UTF8.GetString(buffer);

        // Fixed sized cache of 1000 characters
        cache += decoded;
        if (cache.Length > 1000)
            cache = cache.Substring(cache.Length - 1000);

        // Some intial variables
        Request? request = null;
        bool hasStart = cache.Contains(Constants.Start);
        bool hasEnd = cache.Contains(Constants.Stop);

        // If request end is detected, add all the decoded bytes
        // to the "final request". Extra bytes after the stop 
        // will be trimmed out in the parser.
        if ((hasEnd || (!hasEnd && !hasStart)) && readingRequest)
            requestParts += decoded;
        

        // If there's a message and it is already in the middle
        // of reading another request, mark the request as failed.
        // Otherwise, acknowledge the starting of a request.
        if (hasStart) {
            if (readingRequest && !hasEnd) {
                request = new(false);
            }else {
                logger?.LogDebug(">>NEW MESSAGE");
                readingRequest = true;
                int loc = cache.IndexOf(Constants.Start);
                requestParts += cache.Substring(loc);
                cache = "";
            }
        }

        // If message is ending, but Gusecra is not reading a message
        // to begin with, mark it as malformed. Otherwise acknowledge
        // the ending.
        if (hasEnd) {
            if (!readingRequest) {
                request = new(false);
                cache = "";
                requestParts = "";
            }else if (!hasStart) {
                cache = "";
            }
            readingRequest = false;
        }
        
        // If request is not null, it means that means the request 
        // has already failed conditions above (malformed).
        if (request != null) {
            readingRequest = false;
            requestParts = "";
            return request.GetValueOrDefault();
        }

        // If not reading a message anymore and the full request 
        // in memory contains both a starting and an end, decode it.
        // Otherwise mark as malformed.
        if (!readingRequest) {
            if (!requestParts.Contains(Constants.Start) || !requestParts.Contains(Constants.Stop)) {
                request = new(false);
            }else {
                string requestContent = requestParts.Split(Constants.Start)[1].Split(Constants.Stop)[0];
                try {
                    request = RequestUtils.ParseRequest(requestContent, key, iv);
                }catch(CryptographicException e) {
                    logger?.LogError($"error decrypting request (make sure the decryption key is correct): {e.Message}");
                }catch(Exception e) {
                    logger?.LogError($"error decoding request (corrupt?): {e.Message}");
                }
                cache = "";
                requestParts = "";
            }
        }
        return request.GetValueOrDefault();
    }
}