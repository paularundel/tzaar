using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace Tzaar.Shared
{
    public class Board
    {
        public List<Node> Nodes = new List<Node>();
        public List<Link> Links = new List<Link>();

        public int[] ColHeights = new int[] { 8, 10, 12, 14, 16, 14, 12, 10, 8 };
        public string ColNames = "ABCDEFGHI";

        public void InitBoard()
        {
            AddNodes(0, (16 - ColHeights[0]) / 2);
            LinkNodes();
            InitPieces();
        }

        public void InitPieces()
        {
            List<Piece> pieces = new List<Piece>();
            pieces.AddRange(GetPieces(6, PieceType.Tzaars));
            pieces.AddRange(GetPieces(9, PieceType.Tzaaras));
            pieces.AddRange(GetPieces(15, PieceType.Totts));
            Shuffle<Piece>(pieces);
            int i = 0;
            foreach(Node n in Nodes)
            {
                n.Pieces.Push(pieces[i++]);
            }
        }

        List<Piece> GetPieces(int n, PieceType type)
        {
            List<Piece> pieces = new List<Piece>();
            for (int i = 0; i < n; i++)
            {
                pieces.Add(new Piece() { Type = type, PieceColor = PlayerColor.Black });
                pieces.Add(new Piece() { Type = type, PieceColor = PlayerColor.White });
            }

            return pieces;
        }

        public string NodeName(int col, int row)
        {
            return $"{ColNames[col]}{row}";
        }

        public void LinkNodes()
        {
            for (int i = 0; i < Nodes.Count; i++)
            {
                Node node = Nodes[i];
                LinkNode(x => x.Col == node.Col - 1 && x.Row == node.Row + 1, node, LinkDirection.UpLeft); 
                LinkNode(x => x.Col == node.Col - 1 && x.Row == node.Row - 1, node, LinkDirection.DownLeft);
                LinkNode(x => x.Col == node.Col && x.Row == node.Row - 2, node, LinkDirection.Down); 
                LinkNode(x => x.Col == node.Col + 1 && x.Row == node.Row - 1, node, LinkDirection.DownRight); 
                LinkNode(x => x.Col == node.Col + 1 && x.Row == node.Row + 1, node, LinkDirection.UpRight);
                LinkNode(x => x.Col == node.Col && x.Row == node.Row + 2, node, LinkDirection.Up); 
            }
        }

        public void LinkNode(Func<Node,bool> f, Node n, LinkDirection ld)
        {
            var t = Nodes.Where(f).FirstOrDefault();
            if(t != null)
            {
                Links.Add(new Link() { From = n, To = t, Direction = ld });
            }
        }

        public void AddNodes(int col, int height)
        {
            //centre node
            if (col == 4 && height == 8)
            {
                AddNodes(col, height + 2);
                return;
            }

            Node p = new Node(NodeName(col, height), col, height);
            Nodes.Add(p);

            //if node above
            if (height < ColHeights[col] + ((16 - ColHeights[col]) / 2))
            {
                AddNodes(col, height + 2);
            }
            else if (col < ColNames.Length - 1)
            {
                //next column
                AddNodes(col + 1, (16 - ColHeights[col + 1])/2);
            }
            else
            {
                //at last node 
                return;
            }
        }

        public static void Shuffle<T>(List<T> l)
        {
            Random rng = new Random();
            int n = l.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                T value = l[k];
                l[k] = l[n];
                l[n] = value;
            }
        }
    }

    public class Node
    {
        public string Id { get; set; }
        public int Col { get; set; }
        public int Row { get; set; }
        public Stack<Piece> Pieces = new Stack<Piece>();
        //public bool IsSelected { get; set; } = false;
        //public bool IsLastMovedToNode { get; set; } = false;

        //public List<Link> Links { get; set; }

        public bool IsVacant { get { return Pieces.Count < 1; } }
        public Piece TopPiece { get { return IsVacant ? null : Pieces.Peek(); } }
                                 
        public int StackHeight { get { return Pieces.Count; } }

        //for serialization
        public Node() { }

        public Node(string id, int col, int row)
        {
            Id = id;
            Col = col;
            Row = row;
            //Links = new List<Link>();
        }

        public void RemovePieces()
        {
            Pieces.Clear();
        }

        public void AddPieces(Node n)
        {
            n.Pieces.Reverse().ToList().ForEach(i => Pieces.Push(i));
        }
    }

    public class Link
    {
        public LinkDirection Direction { get; set; }
        public Node From { get; set; }
        public Node To { get; set; }
    }

    public enum LinkDirection
    {
        Up,
        UpRight,
        UpLeft,
        Down,
        DownRight,
        DownLeft,
        None
    }

    

    public class Piece
    {
        public PieceType Type { get; set; }
        public PlayerColor PieceColor { get; set; }
    }

}
