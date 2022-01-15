using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;

namespace FieryGunHand.Render
{
    public class Frustum
    {
        private float[,] frustum = new float[6, 4];

        public Frustum()
        {

        }

        public void Update(ref Matrix4 projection, ref Matrix4 modelView)
        {
            float[] proj = MathsUtil.MatrixToArray(ref projection);
            float[] modl = MathsUtil.MatrixToArray(ref modelView);
            float[] clip = new float[16];
            float t;

            /* Combine the two matrices (multiply projection by modelview) */
            clip[0] = modl[0] * proj[0] + modl[1] * proj[4] + modl[2] * proj[8] + modl[3] * proj[12];
            clip[1] = modl[0] * proj[1] + modl[1] * proj[5] + modl[2] * proj[9] + modl[3] * proj[13];
            clip[2] = modl[0] * proj[2] + modl[1] * proj[6] + modl[2] * proj[10] + modl[3] * proj[14];
            clip[3] = modl[0] * proj[3] + modl[1] * proj[7] + modl[2] * proj[11] + modl[3] * proj[15];

            clip[4] = modl[4] * proj[0] + modl[5] * proj[4] + modl[6] * proj[8] + modl[7] * proj[12];
            clip[5] = modl[4] * proj[1] + modl[5] * proj[5] + modl[6] * proj[9] + modl[7] * proj[13];
            clip[6] = modl[4] * proj[2] + modl[5] * proj[6] + modl[6] * proj[10] + modl[7] * proj[14];
            clip[7] = modl[4] * proj[3] + modl[5] * proj[7] + modl[6] * proj[11] + modl[7] * proj[15];

            clip[8] = modl[8] * proj[0] + modl[9] * proj[4] + modl[10] * proj[8] + modl[11] * proj[12];
            clip[9] = modl[8] * proj[1] + modl[9] * proj[5] + modl[10] * proj[9] + modl[11] * proj[13];
            clip[10] = modl[8] * proj[2] + modl[9] * proj[6] + modl[10] * proj[10] + modl[11] * proj[14];
            clip[11] = modl[8] * proj[3] + modl[9] * proj[7] + modl[10] * proj[11] + modl[11] * proj[15];

            clip[12] = modl[12] * proj[0] + modl[13] * proj[4] + modl[14] * proj[8] + modl[15] * proj[12];
            clip[13] = modl[12] * proj[1] + modl[13] * proj[5] + modl[14] * proj[9] + modl[15] * proj[13];
            clip[14] = modl[12] * proj[2] + modl[13] * proj[6] + modl[14] * proj[10] + modl[15] * proj[14];
            clip[15] = modl[12] * proj[3] + modl[13] * proj[7] + modl[14] * proj[11] + modl[15] * proj[15];

            /* Extract the numbers for the RIGHT plane */
            frustum[0, 0] = clip[3] - clip[0];
            frustum[0, 1] = clip[7] - clip[4];
            frustum[0, 2] = clip[11] - clip[8];
            frustum[0, 3] = clip[15] - clip[12];

            /* Normalize the result */
            t = (float) Math.Sqrt(frustum[0, 0] * frustum[0, 0] + frustum[0, 1] * frustum[0, 1] +
                                  frustum[0, 2] * frustum[0, 2]);
            frustum[0, 0] /= t;
            frustum[0, 1] /= t;
            frustum[0, 2] /= t;
            frustum[0, 3] /= t;

            /* Extract the numbers for the LEFT plane */
            frustum[1, 0] = clip[3] + clip[0];
            frustum[1, 1] = clip[7] + clip[4];
            frustum[1, 2] = clip[11] + clip[8];
            frustum[1, 3] = clip[15] + clip[12];

            /* Normalize the result */
            t = (float) Math.Sqrt(frustum[1, 0] * frustum[1, 0] + frustum[1, 1] * frustum[1, 1] +
                                  frustum[1, 2] * frustum[1, 2]);
            frustum[1, 0] /= t;
            frustum[1, 1] /= t;
            frustum[1, 2] /= t;
            frustum[1, 3] /= t;

            /* Extract the BOTTOM plane */
            frustum[2, 0] = clip[3] + clip[1];
            frustum[2, 1] = clip[7] + clip[5];
            frustum[2, 2] = clip[11] + clip[9];
            frustum[2, 3] = clip[15] + clip[13];

            /* Normalize the result */
            t = (float) Math.Sqrt(frustum[2, 0] * frustum[2, 0] + frustum[2, 1] * frustum[2, 1] +
                                  frustum[2, 2] * frustum[2, 2]);
            frustum[2, 0] /= t;
            frustum[2, 1] /= t;
            frustum[2, 2] /= t;
            frustum[2, 3] /= t;

            /* Extract the TOP plane */
            frustum[3, 0] = clip[3] - clip[1];
            frustum[3, 1] = clip[7] - clip[5];
            frustum[3, 2] = clip[11] - clip[9];
            frustum[3, 3] = clip[15] - clip[13];

            /* Normalize the result */
            t = (float) Math.Sqrt(frustum[3, 0] * frustum[3, 0] + frustum[3, 1] * frustum[3, 1] +
                                  frustum[3, 2] * frustum[3, 2]);
            frustum[3, 0] /= t;
            frustum[3, 1] /= t;
            frustum[3, 2] /= t;
            frustum[3, 3] /= t;

            /* Extract the FAR plane */
            frustum[4, 0] = clip[3] - clip[2];
            frustum[4, 1] = clip[7] - clip[6];
            frustum[4, 2] = clip[11] - clip[10];
            frustum[4, 3] = clip[15] - clip[14];

            /* Normalize the result */
            t = (float) Math.Sqrt(frustum[4, 0] * frustum[4, 0] + frustum[4, 1] * frustum[4, 1] +
                                  frustum[4, 2] * frustum[4, 2]);
            frustum[4, 0] /= t;
            frustum[4, 1] /= t;
            frustum[4, 2] /= t;
            frustum[4, 3] /= t;

            /* Extract the NEAR plane */
            frustum[5, 0] = clip[3] + clip[2];
            frustum[5, 1] = clip[7] + clip[6];
            frustum[5, 2] = clip[11] + clip[10];
            frustum[5, 3] = clip[15] + clip[14];

            /* Normalize the result */
            t = (float) Math.Sqrt(frustum[5, 0] * frustum[5, 0] + frustum[5, 1] * frustum[5, 1] +
                                  frustum[5, 2] * frustum[5, 2]);
            frustum[5, 0] /= t;
            frustum[5, 1] /= t;
            frustum[5, 2] /= t;
            frustum[5, 3] /= t;
        }

        public bool PointInFrustum(float x, float y, float z)
        {
            int p;

            for (p = 0; p < 6; p++)
                if (frustum[p, 0] * x + frustum[p, 1] * y + frustum[p, 2] * z + frustum[p, 3] <= 0)
                    return false;
            return true;
        }

        public bool CubeInFrustum(float x, float y, float z, float size)
        {
            int p;

            for (p = 0; p < 6; p++)
            {
                if (frustum[p, 0] * (x - size) + frustum[p, 1] * (y - size) + frustum[p, 2] * (z - size) +
                    frustum[p, 3] > 0)
                    continue;
                if (frustum[p, 0] * (x + size) + frustum[p, 1] * (y - size) + frustum[p, 2] * (z - size) +
                    frustum[p, 3] > 0)
                    continue;
                if (frustum[p, 0] * (x - size) + frustum[p, 1] * (y + size) + frustum[p, 2] * (z - size) +
                    frustum[p, 3] > 0)
                    continue;
                if (frustum[p, 0] * (x + size) + frustum[p, 1] * (y + size) + frustum[p, 2] * (z - size) +
                    frustum[p, 3] > 0)
                    continue;
                if (frustum[p, 0] * (x - size) + frustum[p, 1] * (y - size) + frustum[p, 2] * (z + size) +
                    frustum[p, 3] > 0)
                    continue;
                if (frustum[p, 0] * (x + size) + frustum[p, 1] * (y - size) + frustum[p, 2] * (z + size) +
                    frustum[p, 3] > 0)
                    continue;
                if (frustum[p, 0] * (x - size) + frustum[p, 1] * (y + size) + frustum[p, 2] * (z + size) +
                    frustum[p, 3] > 0)
                    continue;
                if (frustum[p, 0] * (x + size) + frustum[p, 1] * (y + size) + frustum[p, 2] * (z + size) +
                    frustum[p, 3] > 0)
                    continue;
                return false;
            }

            return true;
        }
        public bool CuboidInFrustum(float x1, float y1, float z1, float x2, float y2, float z2)
        {
            for (int i = 0; i < 6;)
            {
                if (this.frustum[i, 0] * x1 + this.frustum[i, 1] * y1 + this.frustum[i, 2] * z1 + this.frustum[i, 3] > 0.0F ||
                    this.frustum[i, 0] * x2 + this.frustum[i, 1] * y1 + this.frustum[i, 2] * z1 + this.frustum[i, 3] > 0.0F ||
                    this.frustum[i, 0] * x1 + this.frustum[i, 1] * y2 + this.frustum[i, 2] * z1 + this.frustum[i, 3] > 0.0F ||
                    this.frustum[i, 0] * x2 + this.frustum[i, 1] * y2 + this.frustum[i, 2] * z1 + this.frustum[i, 3] > 0.0F ||
                    this.frustum[i, 0] * x1 + this.frustum[i, 1] * y1 + this.frustum[i, 2] * z2 + this.frustum[i, 3] > 0.0F ||
                    this.frustum[i, 0] * x2 + this.frustum[i, 1] * y1 + this.frustum[i, 2] * z2 + this.frustum[i, 3] > 0.0F ||
                    this.frustum[i, 0] * x1 + this.frustum[i, 1] * y2 + this.frustum[i, 2] * z2 + this.frustum[i, 3] > 0.0F ||
                    this.frustum[i, 0] * x2 + this.frustum[i, 1] * y2 + this.frustum[i, 2] * z2 + this.frustum[i, 3] > 0.0F)
                {
                    i++;
                    continue;
                }
                return false;
            }
            return true;
        }

        public bool BoundingBoxInFrustum(BoundingBox box)
        {
            return CuboidInFrustum(box.X0, box.Y0, box.Z0, box.X1, box.Y1, box.Z1);
        }
    }
}
