using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tzaar.Shared;

namespace Tzaar.Client.GameControllers
{
    public interface IGameController
    {
        Game Game { get; set; }
        string Message { get; set; }
        ClientPlayerType ClientPlayer { get; set; }
        event EventHandler BoardUpdated;
        void StartGame();
        Task<bool> SelectPiece(Node selectedPiece);
        Task<bool> MovePiece(Node moveToPiece);
        void NextAction();
        Task<bool> Pass();
        
    }

    public enum ClientPlayerType
    {
        White,
        Black,
        Both,
        None,
        NotSet,
        Waiting
    }
}
