using System.Collections.Concurrent;
using System.Diagnostics.Eventing.Reader;
using System.Net.Sockets;
using System.Net;
using System.Net.WebSockets;
using System.Reflection.Emit;
using System.Text;
using Microsoft.Extensions.FileSystemGlobbing.Internal.Patterns;
using Microsoft.Extensions.Options;
using Microsoft.VisualBasic.CompilerServices;
using WebServer.Models;
using WebServer.Services;

namespace WebServer;

public class WorkerService : BackgroundService
{
    private readonly ServerConfigModel _config;
    private readonly ILogger<WorkerService> _logger;
    private readonly WebsiteListModel _websiteConfigModels;
    private Thread _thread = null!;

    private readonly ConcurrentQueue<HttpRequestModel> _requestsQueue = new();

    private readonly IHttpRequestParser _parser;

    public WorkerService(ILogger<WorkerService> logger, IHttpRequestParser parser, IOptions<ServerConfigModel> config, IOptions<WebsiteListModel> webConfig)
    {
        _config = config.Value;
        //_serverPort = _config.Port;
        _logger = logger;
        _parser = parser;
        _websiteConfigModels = webConfig.Value;
    }


    private void StartServer(CancellationToken stoppingToken)
    {
        var websites = _websiteConfigModels.WebsiteConfigList;

        foreach (var website in websites)
        {
            _thread = new Thread(() => ConnectionThreadMethod(website,stoppingToken));
            _thread.Start();
        }
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();
        StartServer(stoppingToken);
        while (!stoppingToken.IsCancellationRequested)
        {
            
            if (_requestsQueue.TryDequeue(out var requestModel) && requestModel.Client != null)
            {
                var handler = requestModel.Client;
                var hostParts = requestModel.Host.Split(":"); //hostParts = localhost:8085 (trying to find port e.g. "8085")
                int port = IntegerType.FromString(hostParts[1]);
                await handler.SendToAsync(GetResponse(requestModel,_websiteConfigModels.WebsiteConfigList.First((x) => x.WebsitePort == port)), handler.RemoteEndPoint!, stoppingToken);
                handler.Close();
            }

            Thread.Sleep(100);
        }
    }

    private void ConnectionThreadMethod(WebsiteConfigModel website, CancellationToken token)
    {
        try
        {
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, website.WebsitePort);
            var httpServer = new Socket(SocketType.Stream, ProtocolType.Tcp);
            httpServer.Bind(endPoint);
            httpServer.Listen(100);
            _ = StartListeningForData(httpServer, token);
        }
        catch(Exception ex)
        {
            Console.WriteLine($"{website.Path} Server could not start: {ex.Message}");
        }
    }

    private async Task StartListeningForData(Socket httpServer, CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            var data = "";
            var bytes = new byte[1_024];
            var handler = await httpServer.AcceptAsync(token);

            while (!token.IsCancellationRequested)
            {
                var received = await handler.ReceiveAsync(bytes, token);
                var partialData = Encoding.ASCII.GetString(bytes, 0, received);
                data += partialData;

                if (data.Contains("\r\n"))
                {
                    break;
                }
            }
            
            
            LogRequestData(data);
            var request = _parser.ParseHttpRequest(data);
            request.Client = handler;
            _requestsQueue.Enqueue(request);


            data = string.Empty;
        }
    }
    
    private byte[] GetResponse(HttpRequestModel requestModel, WebsiteConfigModel website)
    {
        string statusCode = "200 OK";
        string fileName = requestModel.Path; // File path e.g. "/styles-XHU57CVJ.css"
        string methodType = requestModel.RequestType; // Request type e.g. GET, PUT, POST, DELETE
        string webSite = website.Path;

        //Re-routing to default page in website
        if (string.IsNullOrEmpty(fileName) || fileName.Equals("/"))
        {
            fileName = website.DefaultPage; // fileName = "index.html"
        }
        //Otherwise gets filename client wants 
        else if (fileName.StartsWith($"/"))
        {
            fileName = fileName.Substring(1);
        }
        
        
        var rootFolder = _config.RootFolder;

        var requestedFile = Path.Combine(rootFolder, webSite, fileName);
        
        if (methodType.Equals("GET")) // Do we sort methods in here like this? one after the other?
        {
            // put logic in here
        }

        if (methodType.Equals("POST"))
        {
            //put logic in here
        }
        
        
        // File doesn't exist, return 404 Not Found
        if (!File.Exists(requestedFile))
        {
            Console.WriteLine($"File not found: {requestedFile}");
            return NotFound404(website,statusCode);
        }
        
        
        var file = File.ReadAllBytes(requestedFile);
        
        
        //TODO: is there better way to detect??
        string contentType = FindContentType(requestedFile);
        
        String resHeader =
            $"HTTP/1.1 {statusCode}\r\n" +
            "Server: Microsoft_web_server\r\n" +
            $"Content-Type: {contentType}; charset=UTF-8\r\n" +
            $"Access-Control-Allow-Origin: {website.AllowedHosts}\r\n\r\n";
        
       var resData = Encoding.ASCII.GetBytes(resHeader).Concat(file);
        return resData.ToArray();

    }
    public static string FindContentType(string requestedFile) =>
        requestedFile.EndsWith(".js") ? "text/javascript" :
        requestedFile.EndsWith(".css") ? "text/css" :
        "text/html";
    



    public byte[] NotFound404(WebsiteConfigModel website,string statusCode)
    {
        statusCode = "404 Not found";
        String responseHeader =
            $"HTTP/1.1 {statusCode}\r\n" +
            "Server: Microsoft_web_server\r\n" +
            $"Access-Control-Allow-Origin: {website.AllowedHosts}\r\n\r\n";
        
        var responseData = Encoding.ASCII.GetBytes(responseHeader);
        return responseData.ToArray();
    }
   

    private void LogRequestData(string requestData)
    { 
        _logger.LogInformation($"\n{requestData}");
    }
}