using System.Net;
using System.Text.Json;
using HermesSocketLibrary;
using HermesSocketLibrary.db;
using HermesSocketLibrary.Requests;
using HermesSocketServer;
using HermesSocketServer.Requests;
using HermesSocketServer.Socket;
using HermesSocketServer.Socket.Handlers;
using Microsoft.AspNetCore.HttpOverrides;
using Serilog;
using Serilog.Events;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;


var deserializer = new DeserializerBuilder()
    .WithNamingConvention(HyphenatedNamingConvention.Instance)
    .Build();

var configFileName = "server.config.yml";
if (File.Exists("server.config." + Environment.GetEnvironmentVariable("TTS_ENV").ToLower() + ".yml"))
    configFileName = "server.config." + Environment.GetEnvironmentVariable("TTS_ENV").ToLower() + ".yml";
var configContent = File.ReadAllText(configFileName);
var configuration = deserializer.Deserialize<ServerConfiguration>(configContent);

if (configuration.Environment.ToUpper() != "QA" && configuration.Environment.ToUpper() != "PROD")
    throw new Exception("Invalid environment set.");

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
});

builder.WebHost.UseUrls($"http://{configuration.WebsocketServer.Host}:{configuration.WebsocketServer.Port}");
var logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .Enrich.FromLogContext()
    .WriteTo.File("logs/log.log", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 7)
    .WriteTo.Console(restrictedToMinimumLevel: LogEventLevel.Information)
    .CreateLogger();

builder.Host.UseSerilog(logger);
builder.Logging.AddSerilog(logger);
var s = builder.Services;

s.AddSerilog();

s.AddSingleton<ServerConfiguration>(configuration);
s.AddSingleton<Database>();

// Socket message handlers
s.AddSingleton<Serilog.ILogger>(logger);
s.AddKeyedSingleton<ISocketHandler, HeartbeatHandler>("hermes-heartbeat");
s.AddKeyedSingleton<ISocketHandler, HermesLoginHandler>("hermes-hermeslogin");
s.AddKeyedSingleton<ISocketHandler, RequestHandler>("hermes-request");
s.AddKeyedSingleton<ISocketHandler, ErrorHandler>("hermes-error");
s.AddKeyedSingleton<ISocketHandler, ChatterHandler>("hermes-chatter");
s.AddKeyedSingleton<ISocketHandler, EmoteDetailsHandler>("hermes-emotedetails");
s.AddKeyedSingleton<ISocketHandler, EmoteUsageHandler>("hermes-emoteusage");

// Request handlers
s.AddKeyedSingleton<IRequest, GetTTSUsers>("BanTTSUser");
s.AddKeyedSingleton<IRequest, GetTTSUsers>("GetTTSUsers");
s.AddKeyedSingleton<IRequest, GetTTSVoices>("GetTTSVoices");
s.AddKeyedSingleton<IRequest, GetTTSWordFilters>("GetTTSWordFilters");
s.AddKeyedSingleton<IRequest, CreateTTSUser>("CreateTTSUser");
s.AddKeyedSingleton<IRequest, CreateTTSVoice>("CreateTTSVoice");
s.AddKeyedSingleton<IRequest, DeleteTTSVoice>("DeleteTTSVoice");
s.AddKeyedSingleton<IRequest, UpdateTTSUser>("UpdateTTSUser");
s.AddKeyedSingleton<IRequest, UpdateTTSVoice>("UpdateTTSVoice");
s.AddKeyedSingleton<IRequest, GetChatterIds>("GetChatterIds");
s.AddKeyedSingleton<IRequest, GetEmotes>("GetEmotes");
s.AddKeyedSingleton<IRequest, UpdateTTSVoiceState>("UpdateTTSVoiceState");

s.AddSingleton<HermesSocketManager>();
s.AddSingleton<SocketHandlerManager>();
s.AddSingleton<RequestManager, ServerRequestManager>();
s.AddSingleton<JsonSerializerOptions>(new JsonSerializerOptions()
{
    PropertyNameCaseInsensitive = false,
    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
});
s.AddSingleton<Server>();

var app = builder.Build();
app.UseForwardedHeaders();
app.UseSerilogRequestLogging();
app.UseWebSockets(new WebSocketOptions()
{
    KeepAliveInterval = TimeSpan.FromSeconds(30)
});

var options = app.Services.GetRequiredService<JsonSerializerOptions>();
var server = app.Services.GetRequiredService<Server>();

app.Use(async (HttpContext context, RequestDelegate next) =>
{
    if (context.Request.Path != "/")
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        return;
    }

    if (context.WebSockets.IsWebSocketRequest)
    {
        using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
        await server.Handle(new WebSocketUser(webSocket, IPAddress.Parse(context.Request.Headers["X-Forwarded-For"].ToString()), options, logger), context);
    }
    else
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
    }
});

await app.RunAsync();