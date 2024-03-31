namespace WebServer.Models;

public class ServerConfigModel
{
    public int Port { get; set; }
    public string RootFolder { get; set; } = String.Empty;

    public IList<WebsiteConfigModel> Websites { get; set; } = new List<WebsiteConfigModel>();
}