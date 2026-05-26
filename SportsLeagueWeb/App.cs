using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using SportsLeague.Domain;

namespace SportsLeague.BusinessLogic
{
    public class Repository<T> where T : class
    {
        protected readonly List<T> _items = new();
        public void Add(T item) => _items.Add(item);
        public IEnumerable<T> GetAll() => _items;
        public T? Find(Predicate<T> match) => _items.Find(match);
    }

    public static class GenericAlgorithms
    {
        public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
        {
            foreach (var item in source) action(item);
        }

        public static IEnumerable<TResult> Map<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, TResult> selector)
        {
            foreach (var item in source) yield return selector(item);
        }
    }

    public static class LeagueExtensions
    {
        public static int GetTotalGoalsInLeague(this IEnumerable<Match> matches)
        {
            return matches.Aggregate(0, (total, next) => total + next.HomeGoals + next.AwayGoals);
        }

        public static Dictionary<string, int> GetGoalsByTeams(this IEnumerable<Team> teams, IEnumerable<Match> matches)
        {
            return teams
                .Join(teams, t => t.Name, t2 => t2.Name, (t, t2) => t)
                .GroupBy(t => t.Name)
                .ToDictionary(g => g.Key, g => g.First().Players.Sum(p => p.Goals));
        }
    }

    public abstract class TournamentReglement
    {
        public void RunReglementWorkflow(List<string> logs)
        {
            LogStep(logs, "Ініціалізація регламенту ліги...");
            SetupSecurityRules(logs);
            AllocateBudgets(logs);
        }
        protected abstract void SetupSecurityRules(List<string> logs);
        protected virtual void AllocateBudgets(List<string> logs) => logs.Add("Template Method: Виділено стандартний бюджет.");
        private void LogStep(List<string> logs, string message) => logs.Add($"[Крок регламенту]: {message}");
    }

    public class UkrainianLeagueReglement : TournamentReglement
    {
        protected override void SetupSecurityRules(List<string> logs) => logs.Add("Template Method: Додано регламент безпеки проведення матчів під час повітряних тривог.");
    }

    public class TeamDto
    {
        public string TeamName { get; set; } = string.Empty;
        public int CurrentPoints { get; set; }
    }

    public class LeagueManagerFacade
    {
        public Repository<Team> TeamRepository { get; } = new();
        public Repository<Match> MatchRepository { get; } = new();

        public event EventHandler<string>? OnLeagueNotification;

        public IScoreStrategy CreateStrategy(string type)
        {
            return type switch
            {
                "Aggressive" => new AggressiveStrategy(),
                _ => new FootballClassicStrategy()
            };
        }

        public void PlayAllMatches(IScoreStrategy strategy)
        {
            foreach (var match in MatchRepository.GetAll())
            {
                if (!match.IsFinished)
                {
                    match.Play(strategy);
                    OnLeagueNotification?.Invoke(this, $"Матч №{match.Id} закінчився: {match.HomeTeam.Name} {match.HomeGoals}:{match.AwayGoals} {match.AwayTeam.Name}");
                }
            }
        }

        public bool SaveConfigurationWithRetryPolicy(string jsonData, List<string> logs)
        {
            int maxAttempts = 3;
            int attempt = 0;

            while (attempt < maxAttempts)
            {
                try
                {
                    attempt++;
                    if (attempt < 2) 
                        throw new TimeoutException("Сервер синхронізації бази даних тимчасово недоступний.");

                    logs.Add($"[Retry Policy]: Конфігурацію успішно синхронізовано на спробі №{attempt}.");
                    return true;
                }
                catch (TimeoutException ex)
                {
                    logs.Add($"[Retry Policy - Спроба {attempt} ПРОВАЛЕНА]: {ex.Message}");
                    if (attempt >= maxAttempts)
                    {
                        logs.Add("[Retry Policy]: Критична помилка. Система перейшла в автономний режим.");
                        return false;
                    }
                    Thread.Sleep(attempt * 100);
                }
            }
            return false;
        }

        public string ExportDataToFormatJson()
        {
            var dtos = TeamRepository.GetAll().Map(t => new TeamDto { TeamName = t.Name, CurrentPoints = t.Points });
            return JsonSerializer.Serialize(dtos, new JsonSerializerOptions { WriteIndented = true });
        }
    }
}