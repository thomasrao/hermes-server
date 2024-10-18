using HermesSocketServer.Store;

namespace HermesSocketServer.Services
{
    public class DatabaseService : BackgroundService
    {
        private readonly VoiceStore _voices;
        private readonly ChatterStore _chatters;
        private readonly ServerConfiguration _configuration;
        private readonly Serilog.ILogger _logger;

        public DatabaseService(VoiceStore voices, ChatterStore chatters, ServerConfiguration configuration, Serilog.ILogger logger) {
            _voices = voices;
            _chatters = chatters;
            _configuration = configuration;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            _logger.Information("Loading TTS voices...");
            await _voices.Load();
            _logger.Information("Loading TTS chatters' voice.");
            await _chatters.Load();

            await Task.Run(async () =>
            {
                while (true)
                {
                    await Task.Delay(TimeSpan.FromSeconds(_configuration.Database.SaveDelayInSeconds));
                    await _voices.Save();
                    await _chatters.Save();
                }
            });
        }
    }
}