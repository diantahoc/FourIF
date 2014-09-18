using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;

namespace FourIF
{
    public class DecodeResult
    {
        public byte[] Data;
        public string FileName;
        public string Extension;
        public string MD5;
        public byte[] MD5_Bytes;
        public bool DataValid = true;
    }

    public class EncoderDecoder
    {
        public int FourIF_Version { get { return 4; } }

        public int MinimumResolution { get; set; }
        public int MaximumResolution { get; set; }

        public EncoderDecoder()
        {
            this.MinimumResolution = 10;
            this.MaximumResolution = 10000;
        }

        //Copy constructor
        public EncoderDecoder(EncoderDecoder prev)
        {
            this.MinimumResolution = 10;
            this.MaximumResolution = 10000;
        }

        public byte[] EncodeFile(byte[] data, FileInfo fi)
        {
            int ideal_size = Convert.ToInt32(Math.Sqrt(data.Length / 4)) + 60;
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
            //[1][2]: File Extension
            //[3..66]: File name
            //[67]: Completed 4
            //[68]: Helper
            //[69,70,71,72] : MD5
            //[73]: A->Part Index, R->IsPasswordProtected (0 no, 1 yes)

            //Data start at x = 74
            int x = 74;
            int y = 0;

            //Save version info
            fb.SetPixel(0, 0, IntToColor(FourIF_Version));

            //Save file extension
            Color[] file_extension = StringToPixels(fi.Extension.Remove(0, 1), 8);
            fb.SetPixel(1, 0, file_extension[0]);
            fb.SetPixel(2, 0, file_extension[0]);

            //Save file name. Maximum file name is 256 character

            string file_name = fi.Name.Remove(fi.Name.LastIndexOf('.'));

            Color[] file_name_data = StringToPixels(file_name, 256);

            for (int i = 0; i < 64; i++)
            {
                fb.SetPixel(3 + i, 0, file_name_data[i]);
            }

            byte[] md5_hash = md5(data);

            fb.SetPixel(69, 0, Color.FromArgb(md5_hash[0], md5_hash[1], md5_hash[2], md5_hash[3]));
            fb.SetPixel(70, 0, Color.FromArgb(md5_hash[4], md5_hash[5], md5_hash[6], md5_hash[7]));
            fb.SetPixel(71, 0, Color.FromArgb(md5_hash[8], md5_hash[9], md5_hash[10], md5_hash[11]));
            fb.SetPixel(72, 0, Color.FromArgb(md5_hash[12], md5_hash[13], md5_hash[14], md5_hash[15]));
            //For future use
            fb.SetPixel(73, 0, Color.FromArgb(0, 0, 0, 0));

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
            fb.SetPixel(67, 0, IntToColor(completed_four));

            //How many individual bytes pixels
            Color helper = Color.FromArgb(rem, 0, 0, 0);

            fb.SetPixel(68, 0, helper);

            fb.UnlockImage();

            MemoryStream final = new MemoryStream();

            bi.Save(final, System.Drawing.Imaging.ImageFormat.Png);

            byte[] result = final.ToArray();
            final.Dispose();

            return result;
        }

        public DecodeResult DecodeFile(byte[] data)
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

            #region FourIF Version 2 Decoder

            if (FourIFEncoderVersion == 2)
            {
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

                return new DecodeResult() { Data = result, Extension = FileExtension };
            }

            #endregion

            #region FourIF version 3 or 4 decoder

            if (FourIFEncoderVersion == 3 || FourIFEncoderVersion == 4)
            {
                DecodeResult dr = new DecodeResult();

                dr.Extension = PixelsToString(new Color[] { fb.GetPixel(1, 0), fb.GetPixel(2, 0) });

                List<Color> file_name_buffer = new List<Color>(64);
                for (int i = 3; i < 67; i++)
                {
                    file_name_buffer.Add(fb.GetPixel(i, 0));
                }

                dr.FileName = PixelsToString(file_name_buffer.ToArray());

                int Completed4 = ColorToInt(fb.GetPixel(67, 0));

                int RemainingIndiviual = Convert.ToInt32(fb.GetPixel(68, 0).A);

                int prog_c = 0;

                int y = 0;

                int x = 69;

                if (FourIFEncoderVersion == 4)
                {
                    x = 74;
                    byte[] md5_hash = new byte[16];

                    var a = fb.GetPixel(69, 0);
                    md5_hash[0] = a.A; md5_hash[1] = a.R; md5_hash[2] = a.G; md5_hash[3] = a.B;

                    a = fb.GetPixel(70, 0);
                    md5_hash[4] = a.A; md5_hash[5] = a.R; md5_hash[6] = a.G; md5_hash[7] = a.B;

                    a = fb.GetPixel(71, 0);
                    md5_hash[8] = a.A; md5_hash[9] = a.R; md5_hash[10] = a.G; md5_hash[11] = a.B;

                    a = fb.GetPixel(72, 0);
                    md5_hash[12] = a.A; md5_hash[13] = a.R; md5_hash[14] = a.G; md5_hash[15] = a.B;

                    dr.MD5 = byte2string(md5_hash);
                    dr.MD5_Bytes = md5_hash;
                }

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

                dr.Data = temp_s.ToArray();
                temp_s.Dispose();

                if (FourIFEncoderVersion == 4) 
                {
                    //let's verify embedded hash
                    byte[] a = md5(dr.Data);
                    dr.DataValid = compare_arr(a, dr.MD5_Bytes);
                }

                return dr;
            }
            #endregion

            fb.UnlockImage();
            im.Dispose();
            image_stream.Dispose();
            temp_s.Dispose();

            return new DecodeResult();
        }

        private bool compare_arr(byte[] a1, byte[] a2) 
        {
            if (a1.Length == a2.Length) 
            {
                for (int i = 0; i < a2.Length; i++) 
                {
                    if (a1[i] != a2[i]) { return false; }
                }
                return true;
            }
            return false;
        }

        private byte[] md5(byte[] data)
        {
            using (System.Security.Cryptography.MD5CryptoServiceProvider a = new System.Security.Cryptography.MD5CryptoServiceProvider())
            {
                return a.ComputeHash(data);
            }
        }

        private string byte2string(byte[] s)
        {
            StringBuilder sb = new StringBuilder();
            foreach (byte b in s) { sb.Append(b.ToString("X2")); }
            return sb.ToString().ToLower();
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

        public Color[] StringToPixels(string s, int maxlength)
        {
            if (maxlength % 4 != 0) { throw new Exception("Maxlength must be a multiple of 4"); }

            Color[] colors = new Color[maxlength / 4];

            byte[] data = new byte[maxlength];

            for (int i = 0; i < data.Length; i++)
            {
                try
                {
                    data[i] = Convert.ToByte(s[i]);
                }
                catch (IndexOutOfRangeException) { break; }
            }

            for (int i = 0; i < colors.Length; i++)
            {
                byte a = data[0 + (i * 4)];
                byte r = data[1 + (i * 4)];
                byte g = data[2 + (i * 4)];
                byte b = data[3 + (i * 4)];

                colors[i] = Color.FromArgb(a, r, g, b);
            }

            return colors;
        }

        public string PixelsToString(Color[] colors)
        {
            List<byte> data = new List<byte>(colors.Length * 4);

            foreach (var c in colors)
            {
                data.Add(c.A);
                data.Add(c.R);
                data.Add(c.G);
                data.Add(c.B);
            }

            return Encoding.UTF8.GetString(data.ToArray(), 0, data.IndexOf(0x0));
        }

    }
}
