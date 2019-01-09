using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace Tzaar.Shared.AI
{
    public class AlwaysStackBot : Player, IBot 
    {
        Random Rng = new Random();
        bool _willPass = false;
        NodePair _selection;

        public bool Select(Game game)
        {
            //get list of player nodes
            var playerNodes = game.Board.Nodes.Where(n => !n.IsVacant && n.TopPiece.PieceColor == Color).ToList();

            //get opposing players nodes
            var opposingNodes = game.Board.Nodes.Where(n => !n.IsVacant && n.TopPiece.PieceColor != Color).ToList();
            
            //get list of capturable nodes
            var captures = playerNodes.Join(opposingNodes, s => true, n => true,
                                    (s, n) => new NodePair { Select = s, Target = n })
                                    .Where(a => game.IsValidMove(a.Select, a.Target) && a.Select.StackHeight >= a.Target.StackHeight);
            
            //get list of stackable nodes
            var stackable = playerNodes.Join(playerNodes, s => true, n => true,
                                    (s, n) => new NodePair { Select = s, Target = n })
                                    .Where(a => a.Select != a.Target && game.IsValidMove(a.Select, a.Target));

            
            //todo random is slow
            if (game.TurnStage == TurnStage.CaptureStackOrPass && stackable.Count() > 0)
            {
                //Console.WriteLine($"BOT: stacking");
                _selection = stackable.ElementAt(Rng.Next(stackable.Count()));
            }
            else if(captures.Count() < 1 && game.TurnStage == TurnStage.CaptureStackOrPass)
            {
                 //Console.WriteLine($"BOT: passing");
                _willPass = true;
            }
            else
            {
                //Console.WriteLine($"BOT: capturing");
                _selection = captures.ElementAt(Rng.Next(captures.Count()));
            }
            
            //Console.WriteLine($"BOT: selected {_selection.Select.Id}");
            //Console.WriteLine($"BOT: target {_selection.Target.Id}");
            return game.SelectPiece(_selection.Select);
        }

        public bool Move(Game game)
        {
            if (_willPass)
            {
                _willPass = false;
                return game.Pass();
            }
            
            return game.MovePiece(_selection.Target);
        }

        public class NodePair
        {
            public Node Select { get; set; }
            public Node Target { get; set; }
        }
    }
}
