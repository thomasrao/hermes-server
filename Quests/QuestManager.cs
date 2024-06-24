using HermesSocketLibrary.db;
using HermesSocketLibrary.Quests;
using HermesSocketLibrary.Quests.Tasks;
using ILogger = Serilog.ILogger;

namespace HermesSocketServer.Quests
{
    public class QuestManager
    {
        private IDictionary<short, Quest> _quests;
        private IDictionary<long, IList<ChatterQuestProgression>> _progression;
        private Database _database;
        private ILogger _logger;
        private Random _random;

        public QuestManager(Database database, ILogger logger)
        {
            _database = database;
            _logger = logger;

            _quests = new Dictionary<short, Quest>();
            _progression = new Dictionary<long, IList<ChatterQuestProgression>>();
            _random = new Random();
        }

        public async Task AddNewDailyQuests(DateOnly date, int count)
        {
            var midnight = date.ToDateTime(TimeOnly.MinValue);
            var quests = _quests.Values.Select(q => q.StartTime.Date == midnight);
            for (var i = 0; i < count; i++)
            {
                IQuestTask task;
                var questType = _random.Next(2);
                if (questType == 0)
                {
                    task = new MessageQuestTask(_random.Next(21) + 10);
                }
                else
                {
                    task = new EmoteMessageQuestTask(_random.Next(11) + 5);
                }
                var temp = new DailyQuest(-1, task, date);

                string sql = "INSERT INTO \"Quest\" (type, target, start, end) VALUES (@type, @target, @start, @end)";
                await _database.Execute(sql, c =>
                {
                    c.Parameters.AddWithValue("@type", temp.Type);
                    c.Parameters.AddWithValue("@target", temp.Task.Target);
                    c.Parameters.AddWithValue("@start", temp.StartTime);
                    c.Parameters.AddWithValue("@end", temp.EndTime);
                });

                string sql2 = "SELECT id FROM \"Quest\" WHERE type = @type AND start = @start";
                int? questId = (int?)await _database.ExecuteScalar(sql, c =>
                {
                    c.Parameters.AddWithValue("@type", temp.Type);
                    c.Parameters.AddWithValue("@start", temp.StartTime);
                });

                var quest = new DailyQuest((short)questId.Value, task, date);
                _quests.Add(quest.Id, quest);
            }
        }

        public async Task AddNewWeeklyQuests(DateOnly date, int count)
        {
            for (var i = 0; i < count; i++)
            {
                IQuestTask task;
                var questType = _random.Next(2);
                if (questType == 0)
                {
                    task = new MessageQuestTask(_random.Next(21) + 10);
                }
                else
                {
                    task = new EmoteMessageQuestTask(_random.Next(11) + 5);
                }
                var temp = new WeeklyQuest(-1, task, date);

                string sql = "INSERT INTO \"Quest\" (type, target, start, end) VALUES (@type, @target, @start, @end)";
                await _database.Execute(sql, c =>
                {
                    c.Parameters.AddWithValue("@type", temp.Type);
                    c.Parameters.AddWithValue("@target", temp.Task.Target);
                    c.Parameters.AddWithValue("@start", temp.StartTime);
                    c.Parameters.AddWithValue("@end", temp.EndTime);
                });

                string sql2 = "SELECT id FROM \"Quest\" WHERE type = @type AND start = @start";
                int? questId = (int?)await _database.ExecuteScalar(sql, c =>
                {
                    c.Parameters.AddWithValue("@type", temp.Type);
                    c.Parameters.AddWithValue("@start", temp.StartTime);
                });

                var quest = new WeeklyQuest((short)questId.Value, task, date);
                _quests.Add(quest.Id, quest);
            }
        }

        public void Process(long chatterId, string message, HashSet<string> emotes)
        {
            if (!_progression.TryGetValue(chatterId, out IList<ChatterQuestProgression>? progressions) || progressions == null || !progressions.Any())
                return;

            foreach (var progression in progressions)
            {
                if (!progression.Quest.IsOngoing())
                    continue;

                _logger.Information($"Quest {progression.Quest.Task.Name} [id: {progression.Quest.Id}][progression: {progression.Counter}/{progression.Quest.Task.Target}]");
                progression.Process(message, emotes);
            }
        }

        public async Task UpdateQuests(DateOnly date, int dailies, int weeklies)
        {
            var dateMidnight = date.ToDateTime(TimeOnly.MinValue);
            var dquests = _quests.Values.Select(q => q.StartTime.Date == dateMidnight);
            var monday = date.AddDays((date.DayOfWeek - DayOfWeek.Monday + 7) % 7);
            var mondayMidnight = monday.ToDateTime(TimeOnly.MinValue);
            var wquests = _quests.Values.Select(q => q.StartTime >= mondayMidnight);

            if (dquests.Count() < dailies)
            {
                await AddNewDailyQuests(date, dailies - dquests.Count());
            }

            if (wquests.Count() < weeklies)
            {
                await AddNewWeeklyQuests(monday, dailies - wquests.Count());
            }
        }
    }
}