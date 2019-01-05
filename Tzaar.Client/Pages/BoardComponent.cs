using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Blazor.Components;
using Microsoft.AspNetCore.Blazor.RenderTree;
using Tzaar.Shared;
using Microsoft.AspNetCore.Blazor;
using Microsoft.JSInterop;

namespace Tzaar.Client.Pages
{
    public class BoardComponent : BlazorComponent
    {
        int Width = 1000;
        int Height = 500;

        [Parameter]
        Board Board { get; set; }

        [Parameter]
        protected Game Game { get; set; }


        //TODO CSS
        public PieceColor WhiteColor = new PieceColor() { Base = "#FFFFFF", Ring = "#D4AF37", Outer = "#000000" };
        public PieceColor BlackColor = new PieceColor() { Base = "#000000", Ring = "#C0C0C0", Outer = "#FFFFFF" };


        List<PiecePosition> PiecePositions = new List<PiecePosition>();


        public void HandleClick(UIMouseEventArgs e)
        {
            Console.WriteLine($"Client: {e.ClientX}/{e.ClientY} : Screen: {e.ScreenX}/{e.ScreenY}");
            var p = PiecePositions.Where(n => e.ClientX > n.X - (n.Size / 2) && e.ClientY > n.Y - (n.Size / 2) && e.ClientX < n.X + (n.Size / 2) && e.ClientY < n.Y + (n.Size / 2)).Select(n => n.Node).FirstOrDefault();
            //TODO should be in gamecomponent ?

            bool valid;

            if (p != null)
            {
                if (Game.IsPieceSelected)
                {
                    valid = Game.MovePiece(p);
                   // if(valid) JSRuntime.Current.InvokeAsync<bool>("playSound","sound_move");
                }
                else
                {
                    //TODO handle pass
                    valid = Game.SelectPiece(false, p);
                    //if(valid) JSRuntime.Current.InvokeAsync<bool>("playSound","sound_select");
                }

                if (!valid)
                {
                    Console.WriteLine("Invalid selection");
                }
            }
            else
            {
                Console.WriteLine("No piece selected");
            }

            Console.WriteLine("Game Updated");

            DrawBoard();
        }

        
       
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            Console.WriteLine($"Player={Game.CurrentPlayer.Color} Stage={Game.TurnStage} TurnNo={Game.TurnNo}");
            Console.WriteLine("Rendering Board");
           
            int ct = 0;
            builder.OpenElement(ct++, "svg");
            builder.AddAttribute(ct++, "height", Height);
            builder.AddAttribute(ct++, "width", Width);
            builder.AddAttribute(ct++, "onclick", BindMethods.GetEventHandlerValue<UIMouseEventArgs>(HandleClick));
            DrawBoard(builder, ref ct);    
            builder.CloseElement();
            base.BuildRenderTree(builder);
        }

        public void DrawBoard()
        {
            StateHasChanged();
        }

        private void DrawBoard(RenderTreeBuilder builder, ref int ct)
        {
            var offsetX = Width * .05;
            var offsetY = Height * .9;
            var e = Height * .10;
            var b = e / 2;
            var a = Math.Sqrt((e * e) - (b * b));

            ct = DrawLines(builder, ct, offsetX, offsetY, e, b);
            ct = DrawPieces(builder, ct, offsetX, offsetY,e,b);

        }

        private int DrawPieces(RenderTreeBuilder builder, int ct, double offsetX, double offsetY, double e, double b)
        {
            PiecePositions = new List<PiecePosition>();

            foreach(Node n in Board.Nodes)
            {
                var x = offsetX + (n.Col * e);
                var y = offsetY - (n.Row * b);

                //TODO include piece size?
                PiecePositions.Add(new PiecePosition() { Node = n, X = x, Y = y, Size = e });

                if (!n.IsVacant)
                {
                    DrawPiece(builder, x, y, e, ref ct, n.TopPiece, n.Id);
                    if (n.IsSelected)
                    {
                        DrawPieceSelected(builder, x, y, e, ref ct, n.Id);
                    }

                    if(n.Pieces.Count > 1)
                    {
                        DrawStackCount(builder, x, y, e, ref ct, n.Id, n.Pieces.Count);
                    }
                }
               
            }

            return ct;
        }

        private void DrawStackCount(RenderTreeBuilder builder, double x, double y, double e, ref int ct, string id, int stackHeight)
        {
            /*
            builder.OpenElement(ct++, "rect");
            builder.AddAttribute(ct++, "name", id);
            builder.AddAttribute(ct++, "x", x + 2);
            builder.AddAttribute(ct++, "y", y);
            builder.AddAttribute(ct++, "width", 10);
            builder.AddAttribute(ct++, "height", 10);
            builder.AddAttribute(ct++, "style", "fill:red;");
            builder.CloseElement();
            */

            builder.OpenElement(ct++, "text");
            builder.AddAttribute(ct++, "name", id + "text");
            builder.AddAttribute(ct++, "x", x -2);
            builder.AddAttribute(ct++, "y", y);
            
            builder.AddAttribute(ct++, "style", "fill:red;10px Arial bold");
            builder.AddContent(ct++, stackHeight);
            builder.CloseElement();
        }

        private PieceColor GetPieceColor(PlayerType p)
        {
            return p == PlayerType.White ? WhiteColor : BlackColor;
        }

        private void DrawPiece(RenderTreeBuilder builder, double x, double y, double e, ref int ct, Piece p, string id)
        {
            PieceColor color = GetPieceColor(p.Player);
            DrawPieceBase(builder, x, y, e, ref ct, color,id);
            
            if (p.Type == PieceType.Tzaaras)
            {
                DrawPieceRing(builder, x, y, e, ref ct, color, id);
            }

            if (p.Type == PieceType.Tzaaras || p.Type == PieceType.Tzaars)
            {
                DrawPieceCentre(builder, x, y, e, ref ct, color,id);
            }
        }

        private void DrawPieceBase(RenderTreeBuilder builder, double x, double y, double e, ref int ct, PieceColor color, string id)
        {
            builder.OpenElement(ct++, "circle");
            builder.AddAttribute(ct++, "name", id);
            builder.AddAttribute(ct++, "cx", x);
            builder.AddAttribute(ct++, "cy", y);
            builder.AddAttribute(ct++, "r", e * .4);
            builder.AddAttribute(ct++, "fill", color.Base);
            builder.AddAttribute(ct++, "stroke", color.Outer);
            builder.AddAttribute(ct++, "stroke-width", 1);
            builder.CloseElement();
        }

        private void DrawPieceCentre(RenderTreeBuilder builder, double x, double y, double e, ref int ct, PieceColor color, string id)
        {
            builder.OpenElement(ct++, "circle");
            builder.AddAttribute(ct++, "name", id);
            builder.AddAttribute(ct++, "cx", x);
            builder.AddAttribute(ct++, "cy", y);
            builder.AddAttribute(ct++, "r", e * .2);
            builder.AddAttribute(ct++, "fill", color.Ring);
            //adding this ring has stopped an error on render 'can't remove addtribute from non element'
            builder.AddAttribute(ct++, "stroke", color.Outer);
            builder.AddAttribute(ct++, "stroke-width", 1);

            builder.CloseElement();
        }

        private void DrawPieceRing(RenderTreeBuilder builder, double x, double y, double e, ref int ct, PieceColor color, string id)
        {
            builder.OpenElement(ct++, "circle");
            builder.AddAttribute(ct++, "name", id);
            builder.AddAttribute(ct++, "cx", x);
            builder.AddAttribute(ct++, "cy", y);
            builder.AddAttribute(ct++, "r", e * .3);
            builder.AddAttribute(ct++, "fill", color.Base);
            builder.AddAttribute(ct++, "stroke", color.Ring);
            builder.AddAttribute(ct++, "stroke-width", 2);
            builder.CloseElement();
        }


        private void DrawPieceSelected(RenderTreeBuilder builder, double x, double y, double e, ref int ct, string id)
        {
            builder.OpenElement(ct++, "rect");
            builder.AddAttribute(ct++, "name", id);
            builder.AddAttribute(ct++, "x", x - e/2);
            builder.AddAttribute(ct++, "y", y - e/2);
            builder.AddAttribute(ct++, "width", e);
            builder.AddAttribute(ct++, "height", e);
            builder.AddAttribute(ct++, "style", "fill:gray;stroke:pink;stroke-width:5;fill-opacity:0.1;stroke-opacity:0.9");
            builder.CloseElement();
        }

        private int DrawLines(RenderTreeBuilder builder, int ct, double offsetX, double offsetY, double e, double b)
        {
            foreach (Node n in Board.Nodes)
            {
                var x = offsetX + (n.Col * e);
                var y = offsetY - (n.Row * b);

                foreach (Link l in Board.Links.Where(lk => lk.From == n))
                {
                   
                    var lx = x;
                    var ly = y;

                    if (l.Direction == LinkDirection.Up)
                    {
                        ly = ly - e;
                    }

                    if (l.Direction == LinkDirection.Down)
                    {
                        ly = ly + e;
                    }

                    if (l.Direction == LinkDirection.UpRight)
                    {
                        ly = ly - b;
                        lx = lx + e;
                    }

                    if (l.Direction == LinkDirection.DownRight)
                    {
                        ly = ly + b;
                        lx = lx + e;
                    }

                    if (l.Direction == LinkDirection.UpLeft)
                    {
                        ly = ly - b;
                        lx = lx - e;
                    }

                    if (l.Direction == LinkDirection.DownLeft)
                    {
                        ly = ly + b;
                        lx = lx - e;
                    }


                    DrawLine(builder, x, y, lx, ly, ref ct);
                }

                
            }

            return ct;
        }

        private void DrawLine(RenderTreeBuilder builder, double x1, double y1, double x2, double y2, ref int ct)
        {
           
            builder.OpenElement(ct++, "line");
            builder.AddAttribute(ct++, "x1", x1);
            builder.AddAttribute(ct++, "y1", y1);
            builder.AddAttribute(ct++, "x2", x2);
            builder.AddAttribute(ct++, "y2", y2);
            builder.AddAttribute(ct++, "stroke", "black");
            builder.CloseElement();

        }
    }

    public class PieceColor
    {
        public string Base { get; set; }
        public string Ring { get; set; }
        public string Outer { get; set; }
    }

    public class PiecePosition
    {
        public double X { get; set; }
        public double Y { get; set; }
        public Node Node { get; set; }
        public double Size { get; set; }
    }
}
