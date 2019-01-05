using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tzaar.Shared;

namespace Tzaar.Client.GameControllers
{
    public class BotOnlyGameClientController : BotGameClientController
    {
        public override void StartGame()
        {
            Game = new Game();
            var players = Game.StartGame("", "", true, true);
            NextAction();
            ClientPlayer = ClientPlayerType.None;
        }
    }
}
