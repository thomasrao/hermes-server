using HermesSocketServer.Store;

namespace HermesSocketServer.Services
{
    public class DatabaseService : BackgroundService
    {
        private readonly VoiceStore _voices;
        private readonly Serilog.ILogger _logger;

        public DatabaseService(VoiceStore voices, Serilog.ILogger logger) {
            _voices = voices;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            _logger.Information("Loading TTS voices...");
            await _voices.Load();
            _logger.Information("Loaded TTS voices.");

            await Task.Run(async () =>
            {
                while (true)
                {
                    await Task.Delay(TimeSpan.FromMinutes(1));
                    await _voices.Save();
                }
            });
        }
    }
}