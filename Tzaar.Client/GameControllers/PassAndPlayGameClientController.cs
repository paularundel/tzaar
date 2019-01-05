using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tzaar.Shared;

namespace Tzaar.Client.GameControllers
{
    public class PassAndPlayGameClientController : GameClientController
    {
        public override void StartGame()
        {
            Game = new Game();
            Players players = Game.StartGame("", "", false, false);
            ClientPlayer= ClientPlayerType.Both;
        }

        public override void NextAction()
        {
            //return for next player input
            return;
        }
    }
}
