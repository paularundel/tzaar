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
    public abstract class GameClientController : IGameController
    {
        public Game Game { get; set; }
        public ClientPlayerType ClientPlayer { get; set; }
        public event EventHandler BoardUpdated;
        public string Message { get; set; }


        public abstract void StartGame();
        public abstract void NextAction();

        protected virtual void OnBoardUpdated(EventArgs eventArgs)
        {
            BoardUpdated?.Invoke(this, eventArgs);
        }

        public async Task<bool> MovePiece(Node moveToPiece)
        {
            return await Task.FromResult<bool>(Game.MovePiece(moveToPiece));
        }

        public async Task<bool> SelectPiece(Node selectedPiece)
        {
            return await Task.FromResult<bool>(Game.SelectPiece(selectedPiece));
        }

        public async Task<bool> Pass()
        {
            return await Task.FromResult<bool>(Game.Pass());
        }

         
    }
}
