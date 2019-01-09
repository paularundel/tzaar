using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tzaar.Shared;
using Tzaar.Shared.AI;
using Tzaar.Client.Pages;
using System.Threading;

namespace Tzaar.Client.GameControllers
{
    public class BotGameClientController : GameClientController
    {
        private static int BotDelay = 10;

        public override void StartGame()
        {
            Game = new Game();
            Game.StartGame(new Player(), new SFBot());

            if(Game.PlayerWhite.IsBot)
            {
                ClientPlayer = ClientPlayerType.Black;
                NextAction();
                return;
            }

            ClientPlayer = ClientPlayerType.White;
        }

        public override async void NextAction()
        {
           

            //game over
            if(Game.WinningPlayer != null)
            {
                return;
            }

            //if awaiting player input return
            if (!Game.CurrentPlayer.IsBot)
            {
                return;
            }

            IBot bot = (IBot)Game.CurrentPlayer;
            OnBoardUpdated(new EventArgs());
            await Task.Delay(BotDelay);
            //otherwise next bot action
            if (!Game.IsPieceSelected)
            {
                if (!bot.Select(Game))
                {
                    throw new Exception("Invalid selection made");
                }
                await Task.Delay(BotDelay);
            }
            else
            {
                if (!bot.Move(Game))
                {
                    throw new Exception("Invalid selection made");
                }
                await Task.Delay(BotDelay);

            }

            OnBoardUpdated(new EventArgs());
            NextAction();
        }
    }
}
