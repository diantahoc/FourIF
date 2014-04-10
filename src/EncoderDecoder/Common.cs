using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FourIF
{
    public static class Common
    {
        public static string FormatSize(int size)
        {
            double kb = 1024.0;
            double mb = 1048576.0;
            double gb = 1073741824.0;

            if (size < kb)
            {
                return size.ToString() + " Bytes";
            }
            else if (size > kb && size < mb)
            {
                double a = size / kb;
                return Math.Round(a, 2).ToString() + " KB";
            }
            else if (size > mb && size < gb)
            {
                double a = size / mb;
                return Math.Round(a, 2).ToString() + " MB";
            }
            else if (size > gb)
            {
                double a = size / gb;
                return Math.Round(a, 2).ToString() + " GB";
            }
            else
            {
                return size.ToString();
            }
        }
    }
}
