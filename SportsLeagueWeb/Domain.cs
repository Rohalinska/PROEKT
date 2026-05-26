using System;
using System.Collections.Generic;

namespace SportsLeague.Domain
{
    public class LibraryLeagueException : Exception
    {
        public LibraryLeagueException(string message) : base(message) { }
    }

    public class LeagueValidationException : LibraryLeagueException
    {
        public LeagueValidationException(string message) : base($"[Помилка валідації]: {message}") { }
    }

    public class MatchException : LibraryLeagueException
    {
        public MatchException(string message) : base($"[Помилка матчу]: {message}") { }
    }

    public abstract class Person
    {
        private string _name = string.Empty;
        public string Name
        {
            get => _name;
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new LeagueValidationException("Ім'я чи назва не може бути порожньою!");
                _name = value;
            }
        }
        protected Person(string name) => Name = name;
        public virtual string GetInfo() => $"Ім'я: {Name}";
    }

    public class Player : Person
    {
        public int Goals { get; set; }
        public string Position { get; set; }

        public Player(string name, string position, int goals = 0) : base(name)
        {
            Position = position;
            Goals = goals;
        }
        public override string GetInfo() => $"Гравець: {Name}, Позиція: {Position}, Голи: {Goals}";
    }

    public class Team : IDisposable
    {
        private string _name = string.Empty;
        public string Name
        {
            get => _name;
            set => _name = string.IsNullOrWhiteSpace(value) ? throw new LeagueValidationException("Назва команди невалідна!") : value;
        }

        public int Points { get; set; }
        public List<Player> Players { get; set; } = new();

        public Team(string name) => Name = name;

        public Team(Team other)
        {
            Name = other.Name + " (Копія/Дубль)";
            Points = 0;
            foreach (var p in other.Players)
            {
                Players.Add(new Player(p.Name, p.Position, p.Goals));
            }
        }

        public Player this[int index]
        {
            get => Players[index];
            set => Players[index] = value;
        }

        public static Team operator +(Team team, Player player)
        {
            if (player == null) throw new LeagueValidationException("Гравець не існує!");
            team.Players.Add(player);
            return team;
        }

        public static bool operator ==(Team? left, Team? right)
        {
            if (ReferenceEquals(left, right)) return true;
            if (left is null || right is null) return false;
            return left.Name.Equals(right.Name, StringComparison.OrdinalIgnoreCase);
        }

        public static bool operator !=(Team? left, Team? right) => !(left == right);
        public override bool Equals(object? obj) => obj is Team team && this == team;
        public override int GetHashCode() => Name.GetHashCode();

        public void Dispose()
        {
            Players.Clear();
            GC.SuppressFinalize(this);
        }
    }

    public class Match
    {
        public int Id { get; set; }
        public Team HomeTeam { get; set; }
        public Team AwayTeam { get; set; }
        public int HomeGoals { get; set; }
        public int AwayGoals { get; set; }
        public bool IsFinished { get; private set; }

        public Match(int id, Team home, Team away, int hGoals, int aGoals)
        {
            Id = id;
            HomeTeam = home;
            AwayTeam = away;
            HomeGoals = hGoals;
            AwayGoals = aGoals;
        }

        public void Play(IScoreStrategy strategy)
        {
            if (IsFinished) throw new MatchException("Цей матч уже зіграно!");
            var (hPoints, aPoints) = strategy.CalculatePoints(HomeGoals, AwayGoals);
            HomeTeam.Points += hPoints;
            AwayTeam.Points += aPoints;
            IsFinished = true;
        }
    }

    public interface IScoreStrategy
    {
        string StrategyName { get; }
        (int home, int away) CalculatePoints(int homeGoals, int awayGoals);
    }

    public class FootballClassicStrategy : IScoreStrategy
    {
        public string StrategyName => "Стандартна футбольна (3 за перемогу, 1 за нічию)";
        public (int home, int away) CalculatePoints(int hG, int aG) => hG > aG ? (3, 0) : hG < aG ? (0, 3) : (1, 1);
    }

    public class AggressiveStrategy : IScoreStrategy
    {
        public string StrategyName => "Агресивна ліга (2 за перемогу, нічия = 0 очок обом)";
        public (int home, int away) CalculatePoints(int hG, int aG) => hG > aG ? (2, 0) : hG < aG ? (0, 2) : (0, 0);
    }

    public interface ISeasonState
    {
        string StageName { get; }
        void NextStage(SeasonContext context);
    }

    public class GroupStage : ISeasonState
    {
        public string StageName => "Груповий етап";
        public void NextStage(SeasonContext context) => context.CurrentState = new PlayoffsStage();
    }

    public class PlayoffsStage : ISeasonState
    {
        public string StageName => "Плей-оф (1/2 фіналу)";
        public void NextStage(SeasonContext context) => context.CurrentState = new FinalStage();
    }

    public class FinalStage : ISeasonState
    {
        public string StageName => "Гранд-Фінал сезону";
        public void NextStage(SeasonContext context) => context.CurrentState = new GroupStage();
    }

    public class SeasonContext
    {
        public ISeasonState CurrentState { get; set; } = new GroupStage();
    }
}