using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tzaar.Shared;
using Newtonsoft.Json;


namespace Tzaar.Server.Hubs
{
    public class GameHub : Hub
    {
        public static Dictionary<string,Game> _games = new Dictionary<string,Game>();
        public static string _waitingPlayer = String.Empty;

        object LockObj = new object();

        public override async Task OnConnectedAsync()
        {
            await this.Clients.All.SendAsync("ClientLog", $"Player joined");

            Players players = null;
            Game game = null;

            lock (LockObj)
            {
                if(String.IsNullOrEmpty(_waitingPlayer))
                {
                    _waitingPlayer = Context.ConnectionId;
                }
                else
                {
                    //start game
                    game = new Game();
                    game.GameId = Guid.NewGuid().ToString();
                    _games.Add(game.GameId,game);
                    Groups.AddToGroupAsync(_waitingPlayer, game.GameId);
                    Groups.AddToGroupAsync(Context.ConnectionId, game.GameId);

                    players = game.StartGame(_waitingPlayer, Context.ConnectionId, false, false);
                    _waitingPlayer = String.Empty;
                }
            }

            if(players != null)
            {
                await Clients.Client(players.White.Id).SendAsync("SetPlayer", JsonConvert.SerializeObject(players.White));
                await Clients.Client(players.Black.Id).SendAsync("SetPlayer", JsonConvert.SerializeObject(players.Black));
                await Clients.Groups(game.GameId).SendAsync("RefreshGame", SerializeGame(game));
                
            }
        }

        public async override Task OnDisconnectedAsync(Exception exception)
        {
            //clear if waiting for a game
            if(_waitingPlayer == Context.ConnectionId)
            {
                _waitingPlayer = string.Empty;
            }

            var game = _games.Where(x => x.Value.PlayerBlack.Id == Context.ConnectionId || x.Value.PlayerWhite.Id == Context.ConnectionId).Select(x => x.Value).FirstOrDefault();

            //if in game
            if(game != null)
            {
                var otherPlayer = game.PlayerBlack.Id == Context.ConnectionId ? game.PlayerWhite : game.PlayerBlack;
                await Clients.Client(otherPlayer.Id).SendAsync("SetMessage", $"{otherPlayer} disconnected");
                await Clients.Client(otherPlayer.Id).SendAsync("ClientLog", $"{otherPlayer} left");
                await Clients.Client(otherPlayer.Id).SendAsync("RefreshGame", SerializeGame(game));
                _games.Remove(game.GameId);
            }                
        }

        public async Task<bool> SelectPiece(string gameId, string nodeId)
        {
            Game game = _games[gameId];
            Node node = GetNode(game, nodeId);
            bool res = game.SelectPiece(node);
            await Clients.Groups(gameId).SendAsync("RefreshGame", SerializeGame(game));
            return res;
        }

        public async Task<bool> MovePiece(string gameId, string nodeId)
        {
            Game game = _games[gameId];
            Node node = GetNode(game, nodeId);
            bool res = game.MovePiece(node);

            //if this move results in a win remove game
            if(game.WinningPlayer != null)
            {
                _games.Remove(game.GameId);
            }

            await Clients.Groups(gameId).SendAsync("RefreshGame", SerializeGame(game));
            return res;
        }

        public async Task<bool> Pass(string gameId)
        {
            Game game = _games[gameId];
            bool res = game.Pass();
            await Clients.Groups(gameId).SendAsync("RefreshGame", SerializeGame(game));
            return res;
        }

        private string SerializeGame(Game game)
        {
            return JsonConvert.SerializeObject(game, new JsonSerializerSettings { PreserveReferencesHandling = PreserveReferencesHandling.Objects });
        }

        private Node GetNode(Game game, string id)
        {
            return game.Board.Nodes.Where(n => n.Id == id).FirstOrDefault();
        }
    }
}
