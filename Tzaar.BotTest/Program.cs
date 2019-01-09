using System;
using Tzaar.Shared;
using Tzaar.Shared.AI;

namespace Tzaar.BotTest
{
    class Program
    {
        static void Main(string[] args)
        {
            RunGames(1000, new SFBot(), new AlwaysStackBot());
            Console.ReadLine();
        }

        static void RunGames(int noGames, Player pl1, Player pl2)
        {
            int whiteWins = 0;
            int pl1Wins = 0;
            int i = 0;
            while (i++ < noGames)
            {
                Game g = new Game();
                g.StartGame(pl1, pl2);

                while (g.WinningPlayer == null)
                {
                    IBot bot = (IBot)g.CurrentPlayer;
                    bot.Select(g);
                    bot.Move(g);
                }

                if (g.WinningPlayer == g.PlayerWhite)
                {
                    whiteWins++;
                }


                if (g.WinningPlayer == pl1)
                {
                    pl1Wins++;
                }

                
            }

            Console.WriteLine($"{pl1.GetType()} wins: {pl1Wins}");
            Console.WriteLine($"{pl2.GetType()} wins: {noGames - pl1Wins}");
            Console.WriteLine($"white wins: {whiteWins}");
            Console.WriteLine($"black wins: {noGames - whiteWins}");


        }
    }
}
