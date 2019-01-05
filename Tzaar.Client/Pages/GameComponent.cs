using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Blazor.Components;
using Blazor.Extensions;
using Microsoft.JSInterop;
using Tzaar.Shared;
using Microsoft.AspNetCore.Blazor;
using Microsoft.AspNetCore.Blazor.RenderTree;
using System.Drawing;
using Tzaar.Client.GameControllers;


namespace Tzaar.Client.Pages
{
    [RouteAttribute("/game/{GameType}")]
    public class GameComponent : BlazorComponent
    {
        //game type 
       
        [Parameter]
        private string GameType { get; set; }

        private IGameController _gameController;
        private List<PiecePosition> _piecePositions = new List<PiecePosition>();

        //screen size vars
        private double _width = 0;
        private double _height = 0;
        private double _vertexLength;
        private double _rowHeight;
        private double _colWidth;
        //left column
        private double _offsetX;
        //bottom row
        private double _offsetY;

        //event handlers
        public static event EventHandler<Rectangle> Initialized;
        public static event EventHandler<Rectangle> SizeChanged;

        //todo css
        public PieceColor WhiteColor = new PieceColor() { Base = "#f7f9f9", Ring = "#f7dc6f", Outer = "#212f3c" };
        public PieceColor BlackColor = new PieceColor() { Base = "#212f3c", Ring = "#b2babb", Outer = "#f7f9f9" };

        public GameComponent() {}

        protected override void OnInit()
        {
            Initialized += GameComponent_Initialized;
            SizeChanged += GameComponent_SizeChanged;

            switch (GameType)
            {
                case "clientlocalbot":  _gameController = new BotGameClientController();
                                        break;
                case "botbattle":       _gameController = new BotOnlyGameClientController();
                                        break;
                case "online":          _gameController = new ServerGameController();
                                        break;
                default:                _gameController = new PassAndPlayGameClientController();
                                        break;
            }

            _gameController.BoardUpdated += GameController_BoardUpdated;
            _gameController.StartGame();
            base.OnInit();
        }

        object lockObj = new object();
        private void GameController_BoardUpdated(object sender, EventArgs e)
        {
            lock(lockObj){
                DrawBoard();
            }
        }

        #region window size handlers

        private void GameComponent_SizeChanged(object sender, Rectangle e)
        {
            Console.WriteLine("GameComponent_SizeChanged");
            SetSize(e.Width, e.Height);
            StateHasChanged();
        }

        private void GameComponent_Initialized(object sender, Rectangle e)
        {
            Console.WriteLine("GameComponent_Initialized");
            SetSize(e.Width, e.Height);
            StateHasChanged();
        }

        private void SetSize(double width, double height)
        {
            _width = width;
            _height = height;
            _vertexLength = (_height < _width ?  _height : width) * .1;
            _rowHeight = _vertexLength / 2;
            _colWidth = Math.Sqrt((_vertexLength * _vertexLength) - (_rowHeight * _rowHeight));
            _offsetX = _width / 2 - _colWidth * 4;
            _offsetY = 9 * _vertexLength;
        }

        bool initSize = true;

        protected override async Task OnInitAsync()
        {
            if (initSize)
            {
                initSize = false;
                await JSRuntime.Current.InvokeAsync<string>("JsInteropWindow.initialize");
            }
        }

        [JSInvokable]
        public static Task<bool> WindowInitialized(int width, int height)
        {
            Initialized.Invoke(null, new Rectangle() { Height = height, Width = width });
            return Task.FromResult(true);
        }

        [JSInvokable]
        public static Task<bool> UpdateWindowSize(int width, int height)
        {
            SizeChanged?.Invoke(null, new Rectangle(0, 0, width, height));
            return Task.FromResult(true);
        }

        #endregion

        #region input handlers

        public void HandleUndo()
        {
            //TGame = TGame.GetLastState();

            StateHasChanged();
        }

        public void HandlePass()
        {
            _gameController.Pass();
            StateHasChanged();
        }

        public async void HandleClick(UIMouseEventArgs e)
        {
            //not client players turn?
            if (_gameController.Game.WinningPlayer != null || !ClientPlayersTurn())
            {
                return;
            }

            var y = e.ClientY - (.1 * _height);
            var p = _piecePositions.Where(n => e.ClientX > n.X - (n.Size / 2) && y > n.Y - (n.Size / 2) && e.ClientX < n.X + (n.Size / 2) && y < n.Y + (n.Size / 2)).Select(n => n.Node).FirstOrDefault();

            bool valid;

            if (p != null)
            {
                if (_gameController.Game.IsPieceSelected)
                {
                    valid = await _gameController.MovePiece(p);
                    //if (valid) await JSRuntime.Current.InvokeAsync<bool>("playSound", "sound_move");
                }
                else
                {
                    valid = await _gameController.SelectPiece(p);
                    //if (valid) await JSRuntime.Current.InvokeAsync<bool>("playSound", "sound_select");
                }

                if (!valid)
                {
                    Console.WriteLine("Invalid selection");
                    //await JSRuntime.Current.InvokeAsync<bool>("playSound", "sound_invalid");
                }

                _gameController.NextAction();
             
            }
            //pass
            else if(_gameController.Game.TurnStage == TurnStage.CaptureStackOrPass && e.ClientX > _offsetX && e.ClientX < _offsetX + _colWidth && y > _offsetY - _rowHeight && y < _offsetY)
            {
                await _gameController.Pass();
                _gameController.NextAction();
            }
            else
            {
                Console.WriteLine("No piece selected");
            }
        }

        private bool ClientPlayersTurn()
        {
            return _gameController.Game.CurrentPlayer.Color.ToString() == _gameController.ClientPlayer.ToString() || _gameController.ClientPlayer == ClientPlayerType.Both;
        }

        #endregion

        #region draw game

        public void DrawBoard()
        {
            StateHasChanged();
        }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            int ct = 0;

            if (_gameController.ClientPlayer == ClientPlayerType.Waiting)
            {
                builder.OpenElement(ct++, "text");
                builder.AddAttribute(ct++, "x", _offsetX - _colWidth / 2);
                builder.AddAttribute(ct++, "y", _vertexLength * 5);

                builder.AddAttribute(ct++, "style", $"fill:black;font-size:{GetFontSize()}px;");
                builder.AddContent(ct++, "Waiting for oponent....");
                builder.CloseElement();
            }
            else
            {
                Console.WriteLine($"Player={_gameController.Game.CurrentPlayer.Color} Stage={_gameController.Game.TurnStage} TurnNo={_gameController.Game.TurnNo}");
                
                builder.OpenElement(ct++, "svg");
                builder.AddAttribute(ct++, "height", _height);
                builder.AddAttribute(ct++, "width", _width);
                builder.AddAttribute(ct++, "style", "text-align:center;");
                builder.AddAttribute(ct++, "onclick", BindMethods.GetEventHandlerValue<UIMouseEventArgs>(HandleClick));
                DrawBoard(builder, ref ct);
                builder.CloseElement();
            }
            base.BuildRenderTree(builder);
        }

        private void DrawBoard(RenderTreeBuilder builder, ref int ct)
        {
            

            ct = DrawLines(builder, ct);
            ct = DrawPieces(builder, ct);
            ct = DrawMessage(builder, ct);
            ct = DrawPass(builder, ct);
            ct = DrawPlayerColor(builder, ct);

        }

        private int DrawPlayerColor(RenderTreeBuilder builder, int ct)
        {
            if(_gameController.ClientPlayer == ClientPlayerType.Both || _gameController.ClientPlayer == ClientPlayerType.None)
            {
                return ct;
            }

            DrawPieceBase(builder, _offsetX + 7 * _vertexLength, _offsetY,ref ct, _gameController.ClientPlayer == ClientPlayerType.White ? WhiteColor : BlackColor, "");
            return ct;    

        }

        private int DrawMessage(RenderTreeBuilder builder, int ct)
        {
            //game state message
            builder.OpenElement(ct++, "text");
            builder.AddAttribute(ct++, "x", _offsetX - _colWidth/2);
            builder.AddAttribute(ct++, "y", _vertexLength);
            builder.AddAttribute(ct++, "style", $"fill:black;font-size:{GetFontSize()}px;");
            builder.AddContent(ct++, _gameController.Game.GetPlayerActionMessage());
            builder.CloseElement();

            builder.OpenElement(ct++, "text");
            builder.AddAttribute(ct++, "x", _offsetX - _colWidth / 2);
            builder.AddAttribute(ct++, "y", _vertexLength + _vertexLength/3);
            builder.AddAttribute(ct++, "style", $"fill:black;font-size:{GetFontSize()}px;");
            builder.AddContent(ct++, _gameController.Game.TurnStageText());
            builder.CloseElement();

            //controller message
            builder.OpenElement(ct++, "text");
            builder.AddAttribute(ct++, "x", _offsetX + _colWidth * 6);
            builder.AddAttribute(ct++, "y", _vertexLength);
            builder.AddAttribute(ct++, "style", $"fill:black;font-size:{GetFontSize()}px;");
            builder.AddContent(ct++, _gameController.Message);
            builder.CloseElement();


            return ct;
        }

        private int GetFontSize()
        {
            return (int)(_vertexLength / 5);
    }

        private int DrawPass(RenderTreeBuilder builder, int ct)
        {
            if(_gameController.Game.TurnStage == TurnStage.CaptureStackOrPass && ClientPlayersTurn())
            {
                //offsetY = offsetY - _vertexLength * .5;
                builder.OpenElement(ct++, "rect");
                builder.AddAttribute(ct++, "x", _offsetX);
                builder.AddAttribute(ct++, "y", _offsetY - _rowHeight);
                builder.AddAttribute(ct++, "width", _colWidth);
                builder.AddAttribute(ct++, "height", _rowHeight);
                builder.AddAttribute(ct++, "class", "pass_button");
                builder.CloseElement();

                builder.OpenElement(ct++, "text");
                builder.AddAttribute(ct++, "x", _offsetX + _vertexLength * .15);
                builder.AddAttribute(ct++, "y", _offsetY - _rowHeight * .3 );

                builder.AddAttribute(ct++, "style", $"fill:white;font-size:{GetFontSize()}px;");
                builder.AddContent(ct++, "Pass");
                builder.CloseElement();
            }
            

            return ct;
        }

        private int DrawPieces(RenderTreeBuilder builder, int ct)
        {
            _piecePositions = new List<PiecePosition>();
            foreach (Node n in _gameController.Game.Board.Nodes)
            {
                var x = _offsetX + (n.Col * _colWidth);
                var y = _offsetY - (n.Row * _rowHeight);

                _piecePositions.Add(new PiecePosition() { Node = n, X = x, Y = y, Size = _vertexLength });
              
                if (!n.IsVacant)
                {
                    DrawPiece(builder, x, y, ref ct, n.TopPiece, n.Id);
                    if (n == _gameController.Game.SelectedNode)
                    {
                        DrawPieceSelected(builder, x, y, ref ct, n.Id, "piece_selected selected");
                    }

                    if(n == _gameController.Game.LastMovedToNode)
                    {
                        DrawPieceSelected(builder, x, y, ref ct, n.Id, "move_to_selected selected");
                    }

                    if (n.Pieces.Count > 1)
                    {
                        DrawStackCount(builder, x, y, ref ct, n.Id, n.Pieces.Count,GetPieceColor(n.TopPiece.PieceColor));
                    }
                }

            }

            return ct;
        }

        private void DrawStackCount(RenderTreeBuilder builder, double x, double y, ref int ct, string id, int stackHeight, PieceColor color)
        {
            var x1 = x - 4;
            var y1 = y + 4;

            builder.OpenElement(ct++, "text");
            builder.AddAttribute(ct++, "name", id);
            builder.AddAttribute(ct++, "x", x1);
            builder.AddAttribute(ct++, "y", y1);

            builder.AddAttribute(ct++, "style", $"fill:{color.Outer};font-size:{GetFontSize()}px");
            builder.AddContent(ct++, stackHeight);
            builder.CloseElement();
        }

        private PieceColor GetPieceColor(PlayerColor p)
        {
            return p == PlayerColor.White ? WhiteColor : BlackColor;
        }

        private void DrawPiece(RenderTreeBuilder builder, double x, double y, ref int ct, Piece p, string id)
        {
            PieceColor color = GetPieceColor(p.PieceColor);
            DrawPieceBase(builder, x, y, ref ct, color, id);

            if (p.Type == PieceType.Tzaaras)
            {
                DrawPieceRing(builder, x, y, ref ct, color, id);
            }

            if (p.Type == PieceType.Tzaaras || p.Type == PieceType.Tzaars)
            {
                DrawPieceCentre(builder, x, y, ref ct, color, id);
            }
        }

        private void DrawPieceBase(RenderTreeBuilder builder, double x, double y, ref int ct, PieceColor color, string id)
        {
            builder.OpenElement(ct++, "circle");
            builder.AddAttribute(ct++, "name", id);
            builder.AddAttribute(ct++, "cx", x);
            builder.AddAttribute(ct++, "cy", y);
            builder.AddAttribute(ct++, "r", _vertexLength * .4);
            builder.AddAttribute(ct++, "fill", color.Base);
            builder.AddAttribute(ct++, "stroke", color.Outer);
            builder.AddAttribute(ct++, "stroke-width", 1);
            builder.CloseElement();
        }

        private void DrawPieceCentre(RenderTreeBuilder builder, double x, double y, ref int ct, PieceColor color, string id)
        {
            builder.OpenElement(ct++, "circle");
            builder.AddAttribute(ct++, "name", id);
            builder.AddAttribute(ct++, "cx", x);
            builder.AddAttribute(ct++, "cy", y);
            builder.AddAttribute(ct++, "r", _vertexLength * .2);
            builder.AddAttribute(ct++, "fill", color.Ring);
            //adding this ring has stopped an error on render 'can't remove addtribute from non element'
            builder.AddAttribute(ct++, "stroke", color.Ring);
            builder.AddAttribute(ct++, "stroke-width", 1);

            builder.CloseElement();
        }

        private void DrawPieceRing(RenderTreeBuilder builder, double x, double y, ref int ct, PieceColor color, string id)
        {
            builder.OpenElement(ct++, "circle");
            builder.AddAttribute(ct++, "name", id);
            builder.AddAttribute(ct++, "cx", x);
            builder.AddAttribute(ct++, "cy", y);
            builder.AddAttribute(ct++, "r", _vertexLength * .3);
            builder.AddAttribute(ct++, "fill", color.Base);
            builder.AddAttribute(ct++, "stroke", color.Ring);
            builder.AddAttribute(ct++, "stroke-width", 3);
            builder.CloseElement();
        }


        private void DrawPieceSelected(RenderTreeBuilder builder, double x, double y, ref int ct, string id, string cl)
        {
            builder.OpenElement(ct++, "rect");
            builder.AddAttribute(ct++, "name", id);
            builder.AddAttribute(ct++, "x", x - _vertexLength / 2);
            builder.AddAttribute(ct++, "y", y - _vertexLength / 2);
            builder.AddAttribute(ct++, "width", _vertexLength);
            builder.AddAttribute(ct++, "height", _vertexLength);
            //builder.AddAttribute(ct++, "style", $"fill:gray;stroke:{color};stroke-width:4;fill-opacity:0.1;stroke-opacity:0.7");
            builder.AddAttribute(ct++, "class", cl);
            builder.CloseElement();
        }

        private int DrawLines(RenderTreeBuilder builder, int ct)
        {
            foreach (Node n in _gameController.Game.Board.Nodes)
            {
                var x = _offsetX + (n.Col * _colWidth);
                var y = _offsetY - (n.Row * _rowHeight);

                foreach (Link l in _gameController.Game.Board.Links.Where(lk => lk.From == n))
                {

                    var lx = x;
                    var ly = y;

                    if (l.Direction == LinkDirection.Up)
                    {
                        ly = ly - _vertexLength;
                    }

                    if (l.Direction == LinkDirection.Down)
                    {
                        ly = ly + _vertexLength;
                    }

                    if (l.Direction == LinkDirection.UpRight)
                    {
                        ly = ly - _rowHeight;
                        lx = lx + _colWidth;
                    }

                    if (l.Direction == LinkDirection.DownRight)
                    {
                        ly = ly + _rowHeight;
                        lx = lx + _colWidth;
                    }

                    if (l.Direction == LinkDirection.UpLeft)
                    {
                        ly = ly - _rowHeight;
                        lx = lx - _colWidth;
                    }

                    if (l.Direction == LinkDirection.DownLeft)
                    {
                        ly = ly + _rowHeight;
                        lx = lx - _colWidth;
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

        #endregion
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

