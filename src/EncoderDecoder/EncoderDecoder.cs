using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;

namespace FourIF
{
    public class EncoderDecoder
    {
        const int FourIF_Version = 2;

        public int MinimumResolution { get; set; }
        public int MaximumResolution { get; set; }

        public EncoderDecoder()
        {
            this.MinimumResolution = 10;
            this.MaximumResolution = 10000;
        }

        public byte[] EncodeFile(byte[] data, string fileextension)
        {
            int ideal_size = Convert.ToInt32(Math.Sqrt(data.Length / 4)) + 4;
            int needed_res = 0;

            if (ideal_size > MaximumResolution - 1)
            {
                throw new Exception("File is too big. Either compress the file or set a higer maximum image resoltion");
            }

            if (ideal_size <= MinimumResolution)
            {
                needed_res = MinimumResolution;
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

            //[0]: FourIF version
            //[1]: File Extension
            //[2]: Completed 4
            //[3]: Helper

            //Data start at x = 4
            int x = 4;
            int y = 0;

            fb.SetPixel(0, 0, IntToColor(FourIF_Version));
            fb.SetPixel(1, 0, StringToColor(fileextension));

            List<byte> buffer = new List<byte>(4);

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

            //if there is 3 or less bytes (non 4bytes chunks)
            //add each one as a seperate pixel
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

            //Set the number of 4bytes chunks
            fb.SetPixel(2, 0, IntToColor(completed_four));

            //How many individual bytes pixels
            Color helper = Color.FromArgb(rem, 0, 0, 0);

            fb.SetPixel(3, 0, helper);

            fb.UnlockImage();

            MemoryStream final = new MemoryStream();

            bi.Save(final, System.Drawing.Imaging.ImageFormat.Png);

            byte[] result = final.ToArray();
            final.Dispose();

            return result;
        }

        public KeyValuePair<string, byte[]> DecodeFile(byte[] data)
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

            //get saved data

            int FourIFEncoderVersion = ColorToInt(fb.GetPixel(0, 0));

            string FileExtension = ColorToString(fb.GetPixel(1, 0));

            FileExtension = FileExtension.TrimEnd();

            int Completed4 = ColorToInt(fb.GetPixel(2, 0));

            int RemainingIndiviual = Convert.ToInt32(fb.GetPixel(3, 0).A);

            int prog_c = 0;

            int y = 0;
            int x = 4;

            for (prog_c = 0; prog_c != Completed4; prog_c++)
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

            for (; RemainingIndiviual > 0; RemainingIndiviual--)
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

            return new KeyValuePair<string, byte[]>(FileExtension, result);
        }

        private Color IntToColor(int i)
        {
            byte[] b = BitConverter.GetBytes(i);

            return Color.FromArgb(b[0], b[1], b[2], b[3]);
        }

        private int ColorToInt(Color c)
        {
            byte[] b = new byte[4];
            b[0] = c.A;
            b[1] = c.R;
            b[2] = c.G;
            b[3] = c.B;
            return BitConverter.ToInt32(b, 0);
        }

        private Color StringToColor(string s)
        {
            if (s.Length > 4)
            {
                s = s.Substring(0, 4);
            }
            else if (s.Length < 4)
            {
                while (s.Length < 4)
                {
                    s = s + " ";
                }
            }
            byte[] b = Encoding.UTF8.GetBytes(s);
            return Color.FromArgb(b[0], b[1], b[2], b[3]);
        }

        private string ColorToString(Color c)
        {
            byte[] b = new byte[4];
            b[0] = c.A;
            b[1] = c.R;
            b[2] = c.G;
            b[3] = c.B;
            return Encoding.UTF8.GetString(b);
        }
    }
}
