using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FieryGunHand.Render
{
    public struct BoundingBox
    {
        public float X0, X1;
        public float Y0, Y1;
        public float Z0, Z1;

        public BoundingBox(float x0, float y0, float z0, float x1, float y1, float z1)
        {
            if (x0 > x1)
            {
                X1 = x0;
                X0 = x1;
            }
            else
            {
                X0 = x0;
                X1 = x1;
            }

            if (y0 > y1)
            {
                Y1 = y0;
                Y0 = y1;
            }
            else
            {
                Y0 = y0;
                Y1 = y1;
            }

            if (z0 > z1)
            {
                Z1 = z0;
                Z0 = z1;
            }
            else
            {
                Z0 = z0;
                Z1 = z1;
            }
        }

        public void Extend(float x, float y, float z)
        {
            if (x < X0)
            {
                X0 = x;
            }
            else if (x > X1)
            {
                X1 = x;
            }

            if (y < Y0)
            {
                Y0 = y;
            }
            else if (y > Y1)
            {
                Y1 = y;
            }

            if (z < Z0)
            {
                Z0 = z;
            }
            else if (z > Z1)
            {
                Z1 = z;
            }
        }
    }
}
