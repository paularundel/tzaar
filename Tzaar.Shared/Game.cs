using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Newtonsoft.Json;
using Tzaar.Shared.AI;

namespace Tzaar.Shared
{
    public class Game
    {
        //game components
        public Board Board { get; set; }
        public Player PlayerWhite { get; set; }
        public Player PlayerBlack { get; set; }

        //Game state
        public Player CurrentPlayer { get; set; }
        public Player WinningPlayer { get; set; }
        public int TurnNo { get; set; }
        public int DecisionNumber { get; set; }
        public TurnStage TurnStage { get; set; }
        public Node SelectedNode { get; set; }
        public Node LastMovedToNode { get; set; }
        public string GameId { get; set; }
       

        //undo history
        //public Dictionary<int, string> History { get; set; } = new Dictionary<int, string>();
        public string LastState { get; set; }

        public DateTime Updated { get; set; }

        private Random Rng = new Random();

        public Players StartGame(string id1, string id2, bool isBot1, bool isBot2)
        {
            Players players = new Players();
            //player 1 white
            if(Rng.Next(2) == 0)
            {
                players.White = CreatePlayer(id1, isBot1, PlayerColor.White);
                players.Black = CreatePlayer(id2, isBot2, PlayerColor.Black);
            }
            else
            {
                players.White = CreatePlayer(id2, isBot2, PlayerColor.White);
                players.Black = CreatePlayer(id1, isBot1, PlayerColor.Black);
            }

            PlayerWhite = players.White;
            PlayerBlack = players.Black;

            Board = new Board();
            Board.InitBoard();
            CurrentPlayer = PlayerWhite;
            TurnNo = 1;
            DecisionNumber = 1;
            TurnStage = TurnStage.Capture;
            Updated = DateTime.Now;

            return players;
        }

        private Player CreatePlayer(string id, bool isBot, PlayerColor color)
        {
            if(isBot)
            {
                return new Bot2() { Color = color, Id = id, IsBot =isBot };
            }

            return new Player() { Color = color, Id = id, IsBot = isBot };
        }

        public bool IsPieceSelected { get { return SelectedNode != null; } }

        public bool Pass()
        {
            if (TurnStage == TurnStage.CaptureStackOrPass)
            {
                NextStage();
                return true;
            }

            return false;
        }
        
        public bool SelectPiece(Node n)
        {
            Console.WriteLine("SelectPiece");

            if(!n.IsVacant && n.TopPiece.PieceColor == CurrentPlayer.Color)
            {
                SelectedNode = n;
                LastMovedToNode = null;
                return true;
            }

            return false;
        }


        public bool MovePiece(Node target)
        {
/*          //TODO serialization too slow
            Console.WriteLine("Saving checkpoint");
            string save = JsonConvert.SerializeObject(this);
            LastState = save;
            //History.Add(DecisionNumber, save);
            Console.WriteLine("Saving complete");
            */

            if(target.IsVacant)
            {
                return false;
            }

            if (target == SelectedNode)
            {
                SelectedNode = null;
                Console.WriteLine("Deselect");
                return true;
            }

            if (TurnStage == TurnStage.Capture
                || (TurnStage == TurnStage.CaptureStackOrPass && target.TopPiece.PieceColor != CurrentPlayer.Color))
            {
                return CapturePiece(target);
            }

            return StackPiece(target);
        }

        public Game GetLastState()
        {
            // return JsonConvert.DeserializeObject<Game>(History[DecisionNumber - 1]);
            return JsonConvert.DeserializeObject<Game>(LastState);
        }

        public void NextStage()
        {
            DecisionNumber++;
            Updated = DateTime.Now;
            SelectedNode = null;

            if ((TurnNo == 1 && CurrentPlayer.Color == PlayerColor.White && TurnStage == TurnStage.Capture)
                || TurnStage == TurnStage.CaptureStackOrPass)
            {
                CurrentPlayer = CurrentPlayer == PlayerWhite ? PlayerBlack : PlayerWhite;
                TurnStage = TurnStage.Capture;
                TurnNo++;
            }
            else
            {
                TurnStage = TurnStage.CaptureStackOrPass;
            }

            CheckForWinner();

            if (WinningPlayer != null)
            {
                TurnStage = TurnStage.GameEnd;
            }
        }

        private void CheckForWinner()
        {
            //current player cannot capture
            if(!CanCapture(CurrentPlayer) && TurnStage == TurnStage.Capture)
            {
                Console.WriteLine($"{CurrentPlayer.Color} cannot capture");
                WinningPlayer = CurrentPlayer.Color == PlayerColor.White ? PlayerBlack : PlayerWhite;
                return;
            }

            //players run out of types
            if (!HasAllTypes(PlayerWhite))
            {
                Console.WriteLine($"White does not have all types");
                WinningPlayer = PlayerBlack;
            }

            if(!HasAllTypes(PlayerBlack))
            {
                Console.WriteLine($"Black does not have all types");
                WinningPlayer = PlayerWhite;
            }

            return;
        }

        private bool HasAllTypes(Player player)
        {
            return PlayerHasNodeOfType(player, PieceType.Totts)
                    && PlayerHasNodeOfType(player, PieceType.Tzaaras)
                    && PlayerHasNodeOfType(player, PieceType.Tzaars);
        }

        private bool PlayerHasNodeOfType(Player player, PieceType pieceType)
        {
            return Board.Nodes.Exists(n => !n.IsVacant && n.TopPiece.PieceColor == player.Color && n.TopPiece.Type == pieceType);
        }

        private bool CanCapture(Player player)
        {
            return Board.Nodes.Exists(n => !n.IsVacant && n.TopPiece.PieceColor == player.Color
                                        && Board.Nodes.Exists(t => !t.IsVacant
                                                                && t.TopPiece.PieceColor != player.Color
                                                                && n.StackHeight >= t.StackHeight
                                                                && IsValidMove(n, t)));
        }

        private bool StackPiece(Node target)
        {
            Console.WriteLine("StackPiece");
            //valid stack
            if (target.TopPiece.PieceColor == CurrentPlayer.Color &&
                IsValidMove(SelectedNode, target))
            {
                target.AddPieces(SelectedNode);
                SelectedNode.RemovePieces();
                LastMovedToNode = target;
                NextStage();
                return true;
            }

            return false;
        }

        private bool CapturePiece(Node target)
        {
            Console.WriteLine("CapturePiece");
            //valid capture
            if(target.TopPiece.PieceColor != CurrentPlayer.Color &&
                IsValidMove(SelectedNode, target) &&
                IsSameOrLessHeight(SelectedNode, target))
            {
                //do capture
                target.RemovePieces();
                target.AddPieces(SelectedNode);
                SelectedNode.RemovePieces();
                LastMovedToNode = target;
                NextStage();
                return true;
            }

            return false;
        }

        public bool IsSameOrLessHeight(Node selected, Node target)
        {
            return target.Pieces.Count <= selected.Pieces.Count;
        }

        public bool IsValidMove(Node selected, Node target)
        {
            bool isAbove = target.Row > selected.Row;
            
            LinkDirection targetDirection = LinkDirection.None;

            if(target.Col > selected.Col)
            {
                targetDirection = isAbove ? LinkDirection.UpRight : LinkDirection.DownRight;
     
            } else if(target.Col < selected.Col)
            {
                targetDirection = isAbove ? LinkDirection.UpLeft : LinkDirection.DownLeft;
            } else
            {
                targetDirection = isAbove ? LinkDirection.Up : LinkDirection.Down;
            }
           
            return IsValidMove(selected, target, targetDirection);

        }

        private bool IsValidMove(Node selected, Node target, LinkDirection linkDirection)
        {
            //get link 
            Link link = Board.Links.Where(x => x.From == selected && x.Direction == linkDirection ).FirstOrDefault();

            //no node in this direction
            if(link == null)
            {
                return false;
            }

            //target node is next and in the right direction
            if (link.To == target)
            {
                return true;
            }

            //other piece in the way
            if(!link.To.IsVacant)
            {
                return false;
            }

            //otherwise try the next node
            return IsValidMove(link.To, target, linkDirection);
        }

        public string TurnStageText()
        {
            if (WinningPlayer != null)
            {
                return "";
            }

            if (TurnStage == TurnStage.CaptureStackOrPass)
            {
                return "stage capture or stack (or pass)"; 
            }

            return "stage capture";
        }

        public string GetPlayerActionMessage()
        {
            if (WinningPlayer != null)
            {
                return $"{WinningPlayer.Color} is the winner!";
            }

            return $"{CurrentPlayer.Color} {( CurrentPlayer.IsBot ? " (bot) " : "")} to { (!IsPieceSelected ? "select own piece " : "select opponent piece")} for";
        }
    }

    public class Player
    {
        public string Id { get; set; }
        public PlayerColor Color { get; set; } 
        public bool IsBot { get; set; }

        public override string ToString()
        {
            return $"Player {Color}";
        }
    }

    public class Players
    {
        public Player White { get; set; }
        public Player Black { get; set; }
    }

    public enum PlayerColor
    {
        White,
        Black
    }

    public enum PieceType
    {
        Tzaars,
        Tzaaras,
        Totts
    }

    public enum TurnStage
    {
        Capture,
        CaptureStackOrPass,
        GameEnd
    }
}
