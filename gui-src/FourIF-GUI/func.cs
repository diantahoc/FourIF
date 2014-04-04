using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;

namespace FourIF_GUI
{
    public static class Func
    {
        public static int max_res = 10000;

        public static int min_res = 1000;

        public static byte[] decode_file(byte[] data)
        {
            MemoryStream image_stream = new MemoryStream();

            MemoryStream temp_s = new MemoryStream();

            image_stream.Write(data, 0, data.Length - 1);

            Image im = null;
            try
            {
                im = Image.FromStream(image_stream);
            }
            catch (Exception)
            {
                throw new Exception("Bad image data");
            }


            FastBitmap fb = new FastBitmap((Bitmap)im);

            fb.LockImage();

            //get saved pixel count

            int completed_f = color_to_number(fb.GetPixel(0, 0));

            int prog_c = 0;

            int y = 0;
            int x = 2;


            for (prog_c = 0; prog_c != completed_f; prog_c++)
            {
                Color a = fb.GetPixel(x, y);

                temp_s.WriteByte(a.A);
                temp_s.WriteByte(a.R);
                temp_s.WriteByte(a.G);
                temp_s.WriteByte(a.B);

                x++;
                if (x >= im.Width)
                {
                    y++;
                    x = 0;
                }
            }

            int rem = Convert.ToInt32(fb.GetPixel(1, 0).A);

            for (; rem > 0; rem--)
            {
                temp_s.WriteByte(fb.GetPixel(x, y).B);
                x++;
            }

            fb.UnlockImage();

            im.Dispose();

            image_stream.Close();
            image_stream.Dispose();

            byte[] result = temp_s.ToArray();
            temp_s.Dispose();

            return result;
        }

        public static byte[] encode_file(byte[] data)
        {

            int ideal_size = Convert.ToInt32(Math.Sqrt(data.Length)) + 4;
            int needed_res = 0;

            if (ideal_size > max_res - 1) { throw new Exception("File is too big. Either compress the file or set a higer maximum image resoltion"); }

            if (ideal_size <= min_res)
            {
                needed_res = min_res;
            }
            else
            {
                needed_res = ideal_size;
            }


            Bitmap bi = new Bitmap(needed_res, needed_res);

            FastBitmap fb = new FastBitmap(bi);

            fb.LockImage();

            int completed_four = 0;
            int rem = 0;

            int x = 2;
            int y = 0;

            List<byte> buffer = new List<byte>();

            foreach (byte b in data)
            {
                buffer.Add(b);

                if (buffer.Count == 4)
                {
                    Color colo = Color.FromArgb(buffer[0], buffer[1], buffer[2], buffer[3]);

                    fb.SetPixel(x, y, colo);
                    x++;
                    if (x >= bi.Width)
                    {
                        x = 0;
                        y++;
                    }
                    completed_four++;
                    buffer.Clear();
                }
            }

            if (buffer.Count != 0)
            {
                rem = buffer.Count;
                foreach (byte b in buffer)
                {
                    Color c = Color.FromArgb(0, 0, 0, b);
                    fb.SetPixel(x, y, c);
                    x++;
                    if (x >= bi.Width)
                    {
                        x = 0;
                        y++;
                    }
                }
            }

            fb.SetPixel(0, 0, number_to_color(completed_four));

            Color helper = Color.FromArgb(rem, 0, 0, 0);

            fb.SetPixel(1, 0, helper);

            fb.UnlockImage();

            MemoryStream final = new MemoryStream();

            bi.Save(final, System.Drawing.Imaging.ImageFormat.Png);


            byte[] result = final.ToArray();
            final.Dispose();

            return result;
        }

        private static Color number_to_color(int w)
        {
            string s = w.ToString();

            while (s.Length < 8)
            {
                s = "0" + s;
            }

            int _1 = Convert.ToInt32(s.Substring(0, 2));
            int _2 = Convert.ToInt32(s.Substring(2, 2));
            int _3 = Convert.ToInt32(s.Substring(4, 2));
            int _4 = Convert.ToInt32(s.Substring(6, 2));

            return Color.FromArgb(_1, _2, _3, _4);
        }

        private static int color_to_number(Color c)
        {
            int _1 = Convert.ToInt32(c.A);
            int _2 = Convert.ToInt32(c.R);
            int _3 = Convert.ToInt32(c.G);
            int _4 = Convert.ToInt32(c.B);
            return (_4 + _3 * 100 + _2 * 10000 + _1 * 1000000);
        }

        private static string prompt(string message)
        {
            Console.WriteLine(message);
            return Console.ReadLine();
        }

        public static string FormatSize(double size)
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

    unsafe public class FastBitmap
    {
        private struct PixelData
        {
            public byte blue;
            public byte green;
            public byte red;
            public byte alpha;

            public override string ToString()
            {
                return "(" + alpha.ToString() + ", " + red.ToString() + ", " + green.ToString() + ", " + blue.ToString() + ")";
            }
        }

        private Bitmap workingBitmap = null;
        private int width = 0;
        private BitmapData bitmapData = null;
        private Byte* pBase = null;

        public FastBitmap(Bitmap inputBitmap)
        {
            workingBitmap = inputBitmap;
        }

        public void LockImage()
        {
            Rectangle bounds = new Rectangle(Point.Empty, workingBitmap.Size);

            width = (int)(bounds.Width * sizeof(PixelData));
            if (width % 4 != 0) width = 4 * (width / 4 + 1);

            //Lock Image
            bitmapData = workingBitmap.LockBits(bounds, ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            pBase = (Byte*)bitmapData.Scan0.ToPointer();
        }

        private PixelData* pixelData = null;

        public Color GetPixel(int x, int y)
        {
            pixelData = (PixelData*)(pBase + y * width + x * sizeof(PixelData));
            return Color.FromArgb(pixelData->alpha, pixelData->red, pixelData->green, pixelData->blue);
        }

        public Color GetPixelNext()
        {
            pixelData++;
            return Color.FromArgb(pixelData->alpha, pixelData->red, pixelData->green, pixelData->blue);
        }

        public void SetPixel(int x, int y, Color color)
        {
            PixelData* data = (PixelData*)(pBase + y * width + x * sizeof(PixelData));
            data->alpha = color.A;
            data->red = color.R;
            data->green = color.G;
            data->blue = color.B;
        }

        public void UnlockImage()
        {
            workingBitmap.UnlockBits(bitmapData);
            bitmapData = null;
            pBase = null;
        }
    }
}
