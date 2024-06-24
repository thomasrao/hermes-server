using System.Net.WebSockets;

namespace HermesSocketServer.Socket.Handlers
{
  public interface ISocketHandler
    {
        int OpCode { get; }
        Task Execute<T>(WebSocketUser sender, T message, HermesSocketManager sockets);
    }
}