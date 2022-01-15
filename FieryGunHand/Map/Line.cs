using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;

namespace FieryGunHand.Map
{
    public class Line
    {
        public Vertex Start, End;

        public float StartX => Start.X;
        public float StartY => Start.X;
        public float EndX => End.X;
        public float EndY => End.X;

        public Line(int x0, int y0, int x1, int y1)
        {
            Start = new Vertex(x0, y0);
            End = new Vertex(x1, y1);
        }

        public Line(Vertex start, Vertex end)
        {
            Start = start;
            End = end;
        }

        public Vector2? FindIntersection(Line other)
        {
            var s1 = Start;
            var e1 = End;
            var s2 = other.Start;
            var e2 = other.End;

            float a1 = e1.Y - s1.Y;
            float b1 = s1.X - e1.X;
            float c1 = a1 * s1.X + b1 * s1.Y;

            float a2 = e2.Y - s2.Y;
            float b2 = s2.X - e2.X;
            float c2 = a2 * s2.X + b2 * s2.Y;

            float delta = a1 * b2 - a2 * b1;

            if (delta == 0)
            {
                return null;
            }
            else
            {
                return new Vector2((b2 * c1 - b1 * c2) / delta, (a1 * c2 - a2 * c1) / delta);
            }
        }

        public float Distance(Vertex pt)
        {
            var a = Start.Distance(End);
            var b = Start.Distance(pt);
            var c = End.Distance(pt);
            var s = (a + b + c) / 2;
            return 2 * (float)Math.Sqrt(s * (s - a) * (s - b) * (s - c)) / a;
        }

        public float Distance(Vector2 pt)
        {
            Vector2 closest;
            float dx = End.X - Start.X;
            float dy = End.Y - Start.Y;
            if ((dx == 0) && (dy == 0))
            {
                // It's a point not a line segment.
                closest = new Vector2(Start.X, Start.Y);
                dx = pt.X - Start.X;
                dy = pt.Y - Start.Y;
                return (float)Math.Sqrt(dx * dx + dy * dy);
            }

            // Calculate the t that minimizes the distance.
            float t = ((pt.X - Start.X) * dx + (pt.Y - Start.Y) * dy) /
                      (dx * dx + dy * dy);

            // See if this represents one of the segment's
            // end points or a point in the middle.
            if (t < 0)
            {
                closest = new Vector2(Start.X, Start.Y);
                dx = pt.X - Start.X;
                dy = pt.Y - Start.Y;
            }
            else if (t > 1)
            {
                closest = new Vector2(End.X, End.Y);
                dx = pt.X - End.X;
                dy = pt.Y - End.Y;
            }
            else
            {
                closest = new Vector2(Start.X + t * dx, Start.Y + t * dy);
                dx = pt.X - closest.X;
                dy = pt.Y - closest.Y;
            }

            return (float)Math.Sqrt(dx * dx + dy * dy);
        }
    }
}
