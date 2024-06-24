using System.Text.Json;
using CommonSocketLibrary.Abstract;
using HermesSocketServer.Socket.Handlers;
using ILogger = Serilog.ILogger;

namespace HermesSocketServer.Socket
{
    public class SocketHandlerManager : HandlerManager<WebSocketUser, ISocketHandler>
    {
        private readonly HermesSocketManager _sockets;
        private readonly IServiceProvider _serviceProvider;


        public SocketHandlerManager(HermesSocketManager sockets, IServiceProvider serviceProvider, ILogger logger)
        : base(logger)
        {
            _sockets = sockets;
            _serviceProvider = serviceProvider;

            Add(0, _serviceProvider.GetRequiredKeyedService<ISocketHandler>("hermes-heartbeat"));
            Add(1, _serviceProvider.GetRequiredKeyedService<ISocketHandler>("hermes-hermeslogin"));
            Add(3, _serviceProvider.GetRequiredKeyedService<ISocketHandler>("hermes-request"));
            Add(5, _serviceProvider.GetRequiredKeyedService<ISocketHandler>("hermes-error"));
            Add(6, _serviceProvider.GetRequiredKeyedService<ISocketHandler>("hermes-chatter"));
            Add(7, _serviceProvider.GetRequiredKeyedService<ISocketHandler>("hermes-emotedetails"));
            Add(8, _serviceProvider.GetRequiredKeyedService<ISocketHandler>("hermes-emoteusage"));
        }

        protected override async Task Execute<T>(WebSocketUser sender, ISocketHandler handler, T value)
        {
            await handler.Execute(sender, value, _sockets);
        }
    }
}