using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;

namespace FieryGunHand.Map
{
    public class Vertex
    {
        public float X, Y;

        public Vertex(float x, float y)
        {
            X = x;
            Y = y;
        }

        public float Distance(Vertex pt)
        {
            float dx = pt.X - X;
            float dy = pt.Y - Y;

            return (float)Math.Sqrt(dx * dx + dy * dy);
        }

        public float Distance(Vector2 pt)
        {
            float dx = pt.X - X;
            float dy = pt.Y - Y;

            return (float)Math.Sqrt(dx * dx + dy * dy);
        }
    }
}
