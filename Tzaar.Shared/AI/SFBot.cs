using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace Tzaar.Shared.AI
{
    public class SFBot : Player, IBot
    {
        Random Rng = new Random();
        bool _willPass = false;
        NodePair _selection;

        public bool Select(Game game)
        {
            _selection = null;

            //get list of bot nodes
            var botNodes = game.Board.Nodes.Where(n => !n.IsVacant && n.TopPiece.PieceColor == Color).ToList();

            //get opposing players nodes
            var opposingNodes = game.Board.Nodes.Where(n => !n.IsVacant && n.TopPiece.PieceColor != Color).ToList();

            //get list of capturable nodes
            var captures = botNodes.Join(opposingNodes, s => true, n => true,
                                    (s, n) => new NodePair { Select = s, Target = n })
                                    .Where(a => game.IsValidMove(a.Select, a.Target) && a.Select.StackHeight >= a.Target.StackHeight)
                                    .ToList();

            //ensure random piece is chosen all other things being equal
            Board.Shuffle(captures);
            
            //TODO can win with two captures
            if (game.TurnStage == TurnStage.Capture)
            {
                //get the least common opponent type
                SelectForCapture(captures);
                //to selection should be random from the least common type?
            }
            else if (game.TurnStage == TurnStage.CaptureStackOrPass)
            {
                //first see if a capture will win the game
                SelectForWinningCapture(captures);

                //otherwise always stack
                if (_selection == null)
                {
                    //get list of stackable nodes
                    var stackable = botNodes.Join(botNodes, s => true, n => true,
                                            (s, n) => new NodePair { Select = s, Target = n })
                                            .Where(a => a.Select != a.Target && game.IsValidMove(a.Select, a.Target)).ToList();
                    
                    //ensure random piece is chosen all other things being equal
                    Board.Shuffle(stackable);

                    //find bots tallest stack
                    var botMaxHeight = botNodes.OrderByDescending(n => n.StackHeight).FirstOrDefault().StackHeight;
                    var oppMaxHeight = opposingNodes.OrderByDescending(n => n.StackHeight).FirstOrDefault().StackHeight;

                    PieceType stackType = botMaxHeight > oppMaxHeight + 2 ? PieceType.Tzaaras : PieceType.Tzaars;

                    //piece type we have the most of
                    var typeCounts = botNodes.GroupBy(n => n.TopPiece.Type)
                                                .OrderByDescending(grp => grp.Count());

                    PieceType mostPieceType = typeCounts.ElementAt(0).Key;
                    PieceType secondMostPieceType = typeCounts.ElementAt(1).Key;

                    SelectForStack(stackable, mostPieceType, stackType);

                    //bigstack cannot stack on most
                    if (_selection is null)
                    {
                        SelectForStack(stackable, secondMostPieceType, stackType == PieceType.Tzaars ? PieceType.Tzaaras : PieceType.Tzaars);
                    }

                    //if still no option capture
                    if (_selection is null && captures.Count() > 0)
                    {
                        SelectForCapture(captures);
                    }

                    //if can't capture
                    if (_selection is null)
                    {
                        _willPass = true;
                        return true;
                    }
                }
            }
           
            return game.SelectPiece(_selection.Select);
        }

        private void SelectForCapture(IEnumerable<NodePair> captures)
        {
            var leastType = captures.GroupBy(np => np.Target.TopPiece.Type)
                                                .OrderBy(grp => grp.Count())
                                                .FirstOrDefault();
            _selection = leastType.ElementAt(Rng.Next(leastType.Count()));
        }

        private void SelectForWinningCapture(IEnumerable<NodePair> captures)
        {
            var winningCapture = captures.GroupBy(np => np.Target.TopPiece.Type)
                                    .Where(grp => grp.Count() == 1)
                                    .FirstOrDefault();

            if(winningCapture != null)
            {
                _selection = winningCapture.FirstOrDefault();
            }
        }

        private void SelectForStack(IEnumerable<NodePair> stackable, PieceType mostPieceType, PieceType stackType)
        {
            //get tallest stack for this type
            var bigStack = stackable.Where(s => s.Select.TopPiece.Type == stackType)
                            .OrderByDescending(o => o.Select.StackHeight)
                            .FirstOrDefault();

            //tallest cannot stack
            if(bigStack == null)
            {
                _selection = null;
                return;
            }
            
            //can stack on most pieces type
            _selection = stackable.Where(s => s.Select == bigStack.Select && s.Target.TopPiece.Type == mostPieceType)
                                                    .FirstOrDefault();
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
