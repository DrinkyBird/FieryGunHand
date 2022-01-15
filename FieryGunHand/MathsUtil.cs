using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;

namespace FieryGunHand
{
    public static class MathsUtil
    {
        public static float[] MatrixToArray(ref Matrix4 mat)
        {
            var f = new float[16];
            MatrixToArray(ref mat, f);
            return f;
        }

        public static void MatrixToArray(ref Matrix4 mat, float[] f)
        {
            f[0] = mat.M11;
            f[1] = mat.M12;
            f[2] = mat.M13;
            f[3] = mat.M14;
            f[4] = mat.M21;
            f[5] = mat.M22;
            f[6] = mat.M23;
            f[7] = mat.M24;
            f[8] = mat.M31;
            f[9] = mat.M32;
            f[10] = mat.M33;
            f[11] = mat.M34;
            f[12] = mat.M41;
            f[13] = mat.M42;
            f[14] = mat.M43;
            f[15] = mat.M44;
        }
    }
}
