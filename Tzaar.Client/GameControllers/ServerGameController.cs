using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tzaar.Shared;
using Blazor.Extensions;
using Newtonsoft.Json;

namespace Tzaar.Client.GameControllers
{
    public class ServerGameController : IGameController
    {
        public event EventHandler BoardUpdated;
        private HubConnection _connection;
        public Game Game { get; set; }
        public ClientPlayerType ClientPlayer { get; set; }
        public string Message { get; set; }

        public void StartGame()
        {
            this._connection = new HubConnectionBuilder()
                   .WithUrl("/gamehub",
                   opt =>
                   {
                       opt.LogLevel = SignalRLogLevel.None;
                       opt.Transport = HttpTransportType.WebSockets;
                       opt.SkipNegotiation = true;
                   })
                   .AddMessagePackProtocol()
                   .Build();

            _connection.On<string>("ClientLog", ClientLog);
            _connection.On<string>("SetMessage", SetMessage);
            _connection.On<string>("RefreshGame", RefreshGame);
            _connection.On<string>("SetPlayer", SetPlayer);
            _connection.OnClose(exc =>
            {
                return Task.CompletedTask;
            });

            

            _connection.StartAsync();
            ClientPlayer = ClientPlayerType.Waiting;
        }

        public async Task<bool> MovePiece(Node moveToPiece)
        {
            return await _connection.InvokeAsync<bool>("MovePiece", Game.GameId, moveToPiece.Id);
        }

        public void NextAction()
        {
            return;
        }

        public async Task<bool> Pass()
        {
            return await _connection.InvokeAsync<bool>("Pass", Game.GameId);
        }

        public async Task<bool> SelectPiece(Node selectedPiece)
        {
            return await _connection.InvokeAsync<bool>("SelectPiece",Game.GameId, selectedPiece.Id);
        }

        private Task  ClientLog(string msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }


        private Task SetMessage(string msg)
        {
            Message = msg;
            return Task.CompletedTask;
        }

       
        private Task RefreshGame(string game)
        {
           
            Game = JsonConvert.DeserializeObject<Game>(game);

            //serialization reverses stack
            foreach(var n in Game.Board.Nodes)
            {
                n.Pieces = new Stack<Piece>(n.Pieces);
            }

            BoardUpdated?.Invoke(this, new EventArgs());
            return Task.CompletedTask;
        }

        private Task SetPlayer(string player)
        {
            Player p = JsonConvert.DeserializeObject<Player>(player);
            ClientPlayer = p.Color == PlayerColor.White ? ClientPlayerType.White : ClientPlayerType.Black;
            return Task.CompletedTask;
        }


    }
}
