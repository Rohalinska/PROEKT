using System;
using System.Linq;
using System.Collections.Generic;
using Xunit;
using Moq;
using SportsLeague.Domain; 
using Match = SportsLeague.Domain.Match; 

namespace FinalTests
{
    public class SportsLeagueTests
    {
        // БЛОК 1: ТЕСТУВАННЯ СУТНОСТІ "ГРАВЕЦЬ" (PLAYER)

        [Fact]
        public void Player_Creation_SetsPropertiesCorrectly()
        {
            var player = new Player("Мудрик", "Півзахисник");

            Assert.Equal("Мудрик", player.Name); 
            Assert.Equal("Півзахисник", player.Position);
        }

        [Fact]
        public void Player_GetInfo_ReturnsFormattedString()
        {
            var player = new Player("Зінченко", "Захисник");
            var result = player.GetInfo(); 
            
            Assert.Contains("Зінченко", result);
            Assert.Contains("Захисник", result);
        }

        [Fact]
        public void Player_EmptyName_ThrowsLeagueValidationException()
        {
            Assert.Throws<LeagueValidationException>(() => new Player("  ", "Нападник"));
        }


        // БЛОК 2: ТЕСТУВАННЯ СУТНОСТІ "КОМАНДА" (TEAM)

        [Fact]
        public void Team_Creation_InitializesEmptyPlayerList()
        {
            var team = new Team("Динамо");
            
            Assert.Equal("Динамо", team.Name);
            Assert.Empty(team.Players);
            Assert.Equal(0, team.Points);
        }

        [Fact]
        public void Team_PlusOperator_AddsPlayerToTeam()
        {
            var team = new Team("Шахтар");
            var player = new Player("Судаков", "Півзахисник");
            
            team = team + player; 

            Assert.Single(team.Players);
            Assert.Equal("Судаков", team.Players[0].Name);
        }

        [Fact]
        public void Team_EqualityOperator_SameName_ReturnsTrue()
        {
            var team1 = new Team("Ворскла");
            var team2 = new Team("Ворскла");
            
            Assert.True(team1 == team2); 
        }

        [Fact]
        public void Team_InequalityOperator_DifferentName_ReturnsTrue()
        {
            var team1 = new Team("Ворскла");
            var team2 = new Team("Дніпро-1");
            
            Assert.True(team1 != team2);
        }

        [Theory]
        [InlineData(3)]
        [InlineData(10)]
        [InlineData(0)]
        public void Team_Points_CanBeSetCorrectly(int pointsToSet)
        {
            var team = new Team("Зоря");
            team.Points = pointsToSet;
            Assert.Equal(pointsToSet, team.Points);
        }


        // БЛОК 3: ТЕСТУВАННЯ СТРАТЕГІЙ (STRATEGY PATTERN)

        [Fact]
        public void ClassicStrategy_HomeWin_Returns3And0()
        {
            var strategy = new FootballClassicStrategy();
            var (homePts, awayPts) = strategy.CalculatePoints(2, 0); 
            
            Assert.Equal(3, homePts);
            Assert.Equal(0, awayPts);
        }

        [Fact]
        public void ClassicStrategy_Draw_Returns1And1()
        {
            var strategy = new FootballClassicStrategy();
            var (homePts, awayPts) = strategy.CalculatePoints(1, 1); 
            
            Assert.Equal(1, homePts);
            Assert.Equal(1, awayPts);
        }

        [Fact]
        public void AggressiveStrategy_HomeWin_Returns2And0()
        {
            var strategy = new AggressiveStrategy(); 
            var (homePts, awayPts) = strategy.CalculatePoints(3, 1); 
            
            Assert.Equal(2, homePts);
            Assert.Equal(0, awayPts);
        }

        [Fact]
        public void Strategies_ReturnCorrectNames()
        {
            var classic = new FootballClassicStrategy();
            var aggressive = new AggressiveStrategy();

            Assert.False(string.IsNullOrEmpty(classic.StrategyName));
            Assert.False(string.IsNullOrEmpty(aggressive.StrategyName));
            Assert.NotEqual(classic.StrategyName, aggressive.StrategyName);
        }


        // БЛОК 4: ТЕСТУВАННЯ МАТЧУ ТА MOQ

        [Fact]
        public void Match_Creation_AssignsTeamsAndGoals()
        {
            var home = new Team("Динамо");
            var away = new Team("Шахтар");
            var match = new Match(1, home, away, 2, 1); 

            Assert.Equal(2, match.HomeGoals);
            Assert.Equal(1, match.AwayGoals);
            Assert.Equal(home, match.HomeTeam);
            Assert.Equal(away, match.AwayTeam);
        }

        [Fact]
        public void Match_Play_WithMockedStrategy_CallsCalculatePoints()
        {
            var home = new Team("Кривбас");
            var away = new Team("Рух");
            var match = new Match(2, home, away, 1, 0);

            var mockStrategy = new Mock<IScoreStrategy>();
            mockStrategy.Setup(s => s.CalculatePoints(1, 0)).Returns((3, 0));

            match.Play(mockStrategy.Object);

            mockStrategy.Verify(s => s.CalculatePoints(1, 0), Times.Once);
            Assert.Equal(3, home.Points);
            Assert.Equal(0, away.Points);
        }


        // БЛОК 5: ТЕСТУВАННЯ REPOSITORY (GENERICS)

        [Fact]
        public void Repository_Add_IncreasesItemsCount()
        {
            var repo = new Repository<Team>();
            repo.Add(new Team("Чорноморець"));
            
            Assert.Single(repo.GetAll());
        }

        [Fact]
        public void Repository_GetAll_ReturnsCorrectTypes()
        {
            var repo = new Repository<Player>();
            repo.Add(new Player("Гравець 1", "Захисник"));
            repo.Add(new Player("Гравець 2", "Воротар"));

            var all = repo.GetAll().ToList();
            Assert.Equal(2, all.Count);
            Assert.IsType<Player>(all[0]);
        }


        // БЛОК 6: ТЕСТУВАННЯ LINQ ТА БІЗНЕС-ЛОГІКИ

        [Fact]
        public void LINQ_OrderByDescending_SortsTeamsCorrectly()
        {
            var teams = new List<Team>
            {
                new Team("Team A") { Points = 10 },
                new Team("Team B") { Points = 15 },
                new Team("Team C") { Points = 5 }
            };

            var sorted = teams.OrderByDescending(t => t.Points).ToList();

            Assert.Equal("Team B", sorted[0].Name); 
            Assert.Equal("Team A", sorted[1].Name); 
            Assert.Equal("Team C", sorted[2].Name); 
        }

        [Fact]
        public void LINQ_TotalGoals_CalculatesCorrectSum()
        {
            var matches = new List<Match>
            {
                new Match(1, new Team("A"), new Team("B"), 2, 1), 
                new Match(2, new Team("C"), new Team("D"), 0, 0), 
                new Match(3, new Team("E"), new Team("F"), 1, 4)  
            };

            int totalGoals = matches.Sum(m => m.HomeGoals + m.AwayGoals);

            Assert.Equal(8, totalGoals);
        }


        // БЛОК 7: ТЕСТУВАННЯ ОБРОБКИ ВИНЯТКІВ (EXCEPTIONS)

        [Fact]
        public void Team_EmptyName_ThrowsLeagueValidationException()
        {
            Assert.Throws<LeagueValidationException>(() => new Team(""));
        }

        [Fact]
        public void Match_PlayAlreadyFinishedMatch_ThrowsMatchException()
        {
            var match = new Match(1, new Team("A"), new Team("B"), 1, 0);
            match.Play(new FootballClassicStrategy()); 
            
            // Додали фігурні дужки { }, щоб компілятор не плутав це з асинхронним Task
            Assert.Throws<MatchException>(() => { match.Play(new FootballClassicStrategy()); });
        }
    }

    // Generic-клас Repository для Блоку 5
    public class Repository<T>
    {
        private readonly List<T> _items = new();
        public void Add(T item) => _items.Add(item);
        public IEnumerable<T> GetAll() => _items;
    }
}