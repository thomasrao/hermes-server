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
using Microsoft.AspNetCore.Connections;
using HermesSocketServer.Validators;
using HermesSocketServer.Store;
using HermesSocketServer.Services;


var yamlDeserializer = new DeserializerBuilder()
    .WithNamingConvention(HyphenatedNamingConvention.Instance)
    .Build();

var configFileName = "server.config.yml";
var environment = Environment.GetEnvironmentVariable("TTS_ENV")!.ToLower();
if (File.Exists("server.config." + environment + ".yml"))
    configFileName = "server.config." + environment + ".yml";
var configContent = File.ReadAllText(configFileName);
var configuration = yamlDeserializer.Deserialize<ServerConfiguration>(configContent);

if (configuration.Environment.ToUpper() != "QA" && configuration.Environment.ToUpper() != "PROD")
    throw new Exception("Invalid environment set.");

var builder = WebApplication.CreateBuilder();
builder.Logging.ClearProviders();

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
});

builder.WebHost.UseUrls($"http://{configuration.WebsocketServer.Host}:{configuration.WebsocketServer.Port}");
var loggerConfiguration = new LoggerConfiguration();
if (configuration.Environment.ToUpper() == "QA")
    loggerConfiguration.MinimumLevel.Verbose();
else
    loggerConfiguration.MinimumLevel.Debug();

loggerConfiguration.Enrich.FromLogContext()
    .WriteTo.File($"logs/{configuration.Environment.ToUpper()}/serverlog-.log", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 7);
if (configuration.Environment.ToUpper() == "QA")
    loggerConfiguration.WriteTo.Console(restrictedToMinimumLevel: LogEventLevel.Debug);
else
    loggerConfiguration.WriteTo.Console(restrictedToMinimumLevel: LogEventLevel.Information);

var logger = loggerConfiguration.CreateLogger();

builder.Host.UseSerilog(logger);
builder.Logging.AddSerilog(logger);
var s = builder.Services;

s.AddSerilog(logger);

s.AddSingleton<ServerConfiguration>(configuration);
s.AddSingleton<Database>();

// Socket message handlers
s.AddSingleton<Serilog.ILogger>(logger);
s.AddSingleton<ISocketHandler, HeartbeatHandler>();
s.AddSingleton<ISocketHandler, HermesLoginHandler>();
s.AddSingleton<ISocketHandler, RequestHandler>();
s.AddSingleton<ISocketHandler, LoggingHandler>();
s.AddSingleton<ISocketHandler, ChatterHandler>();
s.AddSingleton<ISocketHandler, EmoteDetailsHandler>();
s.AddSingleton<ISocketHandler, EmoteUsageHandler>();

// Validators
s.AddSingleton<VoiceIdValidator>();
s.AddSingleton<VoiceNameValidator>();

// Stores
s.AddSingleton<VoiceStore>();
s.AddSingleton<ChatterStore>();

// Request handlers
s.AddSingleton<IRequest, GetTTSUsers>();
s.AddSingleton<IRequest, GetTTSVoices>();
s.AddSingleton<IRequest, GetTTSWordFilters>();
s.AddSingleton<IRequest, CreateTTSUser>();
s.AddSingleton<IRequest, CreateTTSVoice>();
s.AddSingleton<IRequest, DeleteTTSVoice>();
s.AddSingleton<IRequest, UpdateTTSUser>();
s.AddSingleton<IRequest, UpdateTTSVoice>();
s.AddSingleton<IRequest, GetChatterIds>();
s.AddSingleton<IRequest, GetConnections>();
s.AddSingleton<IRequest, GetDefaultTTSVoice>();
s.AddSingleton<IRequest, GetEmotes>();
s.AddSingleton<IRequest, GetEnabledTTSVoices>();
s.AddSingleton<IRequest, GetPermissions>();
s.AddSingleton<IRequest, GetRedemptions>();
s.AddSingleton<IRequest, GetRedeemableActions>();
s.AddSingleton<IRequest, UpdateTTSVoiceState>();
s.AddSingleton<IRequest, UpdateDefaultTTSVoice>();

s.AddSingleton<HermesSocketManager>();
s.AddSingleton<SocketHandlerManager>();
s.AddSingleton<IRequestManager, RequestManager>();
s.AddSingleton(new JsonSerializerOptions()
{
    PropertyNameCaseInsensitive = false,
    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
});
s.AddSingleton<Server>();


s.AddHostedService<DatabaseService>();

var app = builder.Build();
app.UseForwardedHeaders();
app.UseSerilogRequestLogging();

var wsOptions = new WebSocketOptions()
{
    KeepAliveInterval = TimeSpan.FromSeconds(30)
};
// wsOptions.AllowedOrigins.Add("wss://tomtospeech.com");
//wsOptions.AllowedOrigins.Add("ws.tomtospeech.com");
//wsOptions.AllowedOrigins.Add("hermes-ws.goblincaves.com");
app.UseWebSockets(wsOptions);

var options = app.Services.GetRequiredService<JsonSerializerOptions>();
var server = app.Services.GetRequiredService<Server>();

app.Use(async (HttpContext context, RequestDelegate next) =>
{
    if (context.Request.Path != "/")
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
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
    await next(context);
});

await app.RunAsync();