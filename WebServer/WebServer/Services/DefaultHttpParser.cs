using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using WebServer.Models;
using WebServer.Models;

namespace WebServer.Services;

public class DefaultHttpParser : IHttpRequestParser
{
    private static string ExtractValue(string[] lines, string key)
    {
        foreach (var line in lines)
        {
            if (line.Trim().StartsWith(key, StringComparison.OrdinalIgnoreCase))
            {
                return line.Trim().Substring(key.Length + 1).Trim();
            }
        }

        return string.Empty;
    }

    public HttpRequestModel ParseHttpRequest(string input)
    {
        string[] sections =
            input.Split(new string[] { "\r\n\r\n" },
                StringSplitOptions.RemoveEmptyEntries); //0 header, 1 and so on is body values
        var model = new HttpRequestModel();
        //Splitting all logged data into array lines
        string[] lines = sections[0].Split(new[] { Environment.NewLine }, StringSplitOptions.None);

        //Extracting logged data and placing them into HTTPRequestModel
        model.Host = ExtractValue(lines, "Host");

        string[] lineOneParts = lines[0].Split(" "); //splitting 1st line into parts EG. "GET[0], /path[1], HTTP1.1[2]" 
        model.RequestType = lineOneParts[0]; //Request type GET PUT POST DELETE


        model.RequestedPort = int.TryParse(model.Host.Split(':').LastOrDefault(), out int port) ? port : 0;

        model.Path = lineOneParts[1]; // /path/to/file
        model.Connection = ExtractValue(lines, "Connection");
        model.ContentType = ExtractValue(lines, "Content-Type");
        model.ContentLength = int.Parse(ExtractValue(lines,"Content-Length")); 

        foreach (var line in lines)
        {
            string[] parts = line.Split(':');
            if (parts.Length == 2)
            {
                model.Headers.Add(new KeyValuePair<string, string>(parts[0].Trim(), parts[1].Trim()));
            }
            //some headers have more : so i just added the first part[0] as the key and the rest as the values.
            else if (parts.Length > 2)
            {
                string key = parts[0].Trim();
                string value = string.Join(":", parts.Skip(1)).Trim();
                model.Headers.Add(new KeyValuePair<string, string>(key, value));
            }
        }

        return model;
    }

    // string zipPath = @"/Users/danieljr/Desktop/Projects/Assingment5CS230.zip";
    // string extractPath = @"/Users/danieljr/RiderProjects/TESTING/TESTING/FILE";
    //     
    // System.IO.Compression.ZipFile.ExtractToDirectory(zipPath, extractPath);
}