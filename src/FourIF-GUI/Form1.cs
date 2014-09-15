using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using FourIF;

namespace FourIF_GUI
{
    public partial class Form1 : Form
    {
        EncoderDecoder ed = new EncoderDecoder();

        public Form1()
        {
            InitializeComponent();

            set_value(ed.MinimumResolution, true);
            set_value(ed.MaximumResolution, false);
            update_size_info();
            minX.TextChanged += maxX_TextChanged;
            maxX.TextChanged += maxX_TextChanged;
        }

        private void maxX_TextChanged(object sender, EventArgs e)
        {
            validate_and_set();
        }

        private void set_value(int value, bool ismin)
        {
            if (ismin)
            {
                this.minX.Text = value.ToString();
            }
            else
            {
                this.maxX.Text = value.ToString();
            }
        }

        private void update_size_info()
        {
            this.label3.Text = string.Format("{0}x{0}", ed.MinimumResolution);
            this.label4.Text = string.Format("{0}x{0}", ed.MaximumResolution);
            this.label5.Text = string.Format("Maximum file size is {0}", Common.FormatSize(ed.MaximumResolution * ed.MaximumResolution * 4));
        }

        private void validate_and_set()
        {
            int min = Int32.Parse(minX.Text);
            int max = Int32.Parse(maxX.Text);

            if (min > max)
            {
                MessageBox.Show("Minimum resolution cannot be larger than the maximum resolution");
            }
            else if (max > 10000)
            {
                MessageBox.Show("Maximum resolution cannot be larger than 10000");
            }
            else if (min <= 0)
            {
                MessageBox.Show("Minimum resolution cannot be less or equal to 0");
            }
            else
            {
                ed.MinimumResolution = min;
                ed.MaximumResolution = max;
            }

            update_size_info();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.CheckFileExists = true;
                ofd.CheckPathExists = true;
                if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    System.IO.FileInfo fi = new System.IO.FileInfo(ofd.FileName);
                    byte[] data = System.IO.File.ReadAllBytes(ofd.FileName);
                    try
                    {
                        byte[] encoded = ed.EncodeFile(data, fi);

                        using (SaveFileDialog sfd = new SaveFileDialog())
                        {
                            sfd.FileName = ofd.FileName + "_encoded";
                            sfd.OverwritePrompt = true;
                            sfd.Filter = "PNG Files|*.png";

                            if (sfd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                            {
                                System.IO.File.WriteAllBytes(sfd.FileName, encoded);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                    }
                }
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.CheckFileExists = true;
                ofd.CheckPathExists = true;
                if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    byte[] data = System.IO.File.ReadAllBytes(ofd.FileName);
                    try
                    {
                        DecodeResult dr = ed.DecodeFile(data);

                        if (dr.Data == null)
                        {
                            MessageBox.Show("Unable to decode file");
                            return;
                        }

                        using (SaveFileDialog sfd = new SaveFileDialog())
                        {
                            sfd.OverwritePrompt = true;
                            sfd.Filter = "Decoded file|*." + dr.Extension;
                            if (!string.IsNullOrWhiteSpace(dr.FileName)) { sfd.FileName = dr.FileName; }

                            if (sfd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                            {
                                System.IO.File.WriteAllBytes(sfd.FileName, dr.Data);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                    }
                }
            }
        }
    }
}
