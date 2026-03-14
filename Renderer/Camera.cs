using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BOIDSimulator.Renderer
{
    internal static class Camera
    {
        public const float speed = 50f;
        public static float currentSpeed = speed;
        public const float zoomSpeed = 1.1f;
        public const float zoomMax = 100;
        public static float zoom = 4;
        // position of the top left of the camera
        public static float x;
        public static float y;
    }
}
