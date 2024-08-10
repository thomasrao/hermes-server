using CommonSocketLibrary.Abstract;
using HermesSocketServer.Socket.Handlers;
using ILogger = Serilog.ILogger;

namespace HermesSocketServer.Socket
{
    public class SocketHandlerManager : HandlerManager<WebSocketUser, ISocketHandler>
    {
        private readonly HermesSocketManager _sockets;


        public SocketHandlerManager(HermesSocketManager sockets, IEnumerable<ISocketHandler> handlers, ILogger logger)
        : base(logger)
        {
            _sockets = sockets;

            foreach (ISocketHandler handler in handlers)
                Add(handler.OperationCode, handler);
        }

        protected override async Task Execute<T>(WebSocketUser sender, ISocketHandler handler, T value)
        {
            await handler.Execute(sender, value, _sockets);
        }
    }
}