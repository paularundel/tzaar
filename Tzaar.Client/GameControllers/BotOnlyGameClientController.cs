using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tzaar.Shared;
using Tzaar.Shared.AI;

namespace Tzaar.Client.GameControllers
{
    public class BotOnlyGameClientController : BotGameClientController
    {
        public override void StartGame()
        {
            Game = new Game();
            Game.StartGame(new AlwaysStackBot(), new SFBot());
            NextAction();
            ClientPlayer = ClientPlayerType.None;
        }
    }
}
