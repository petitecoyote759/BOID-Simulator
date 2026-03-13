using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static SDL2.SDL;

namespace BOIDSimulator.Renderer
{
    internal static class Handler
    {
        public static void HandleEvents()
        {
            while (SDL_PollEvent(out SDL_Event e) == 1)
            {
                switch (e.type)
                {
                    case SDL_EventType.SDL_QUIT: // ensures that quitting works and runs cleanup code
                        RendererTools.Stop();
                        break;

                    default:

                        break;
                }
            }
        }
    }
}
