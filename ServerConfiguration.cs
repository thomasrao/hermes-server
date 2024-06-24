namespace HermesSocketServer
{
    public class ServerConfiguration
    {
        public string Environment;
        public WebsocketServerConfiguration WebsocketServer;
        public DatabaseConfiguration Database;
    }

    public class WebsocketServerConfiguration
    {
        public string Host;
        public string Port;
    }

    public class DatabaseConfiguration
    {
        public string ConnectionString;
    }
}