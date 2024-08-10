namespace HermesSocketServer.Socket.Handlers
{
  public interface ISocketHandler
    {
        int OperationCode { get; }
        Task Execute<T>(WebSocketUser sender, T message, HermesSocketManager sockets);
    }
}