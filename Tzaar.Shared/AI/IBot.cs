using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace Tzaar.Shared.AI
{
    public interface IBot
    {
        PlayerColor Color { get; set; }
        bool Select(Game game);
        bool Move(Game game);
    }
}
