using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;

namespace FourIF
{
    public static class Program
    {
        public static int max_res = 10000;

        public static int min_res = 10;

        static void Main(string[] args)
        {
            Console.Title = "4chan image filer - FourIF";

            if (args.Length == 0)
            {
                Console.WriteLine("Usage: fourif --inputfile [--decode] [--outputfile] [--minres] [--maxres] --c");
                Console.WriteLine("Example: fourif --i:somefile.zip --o:output.png --minres10 --maxres500");
                Console.WriteLine("---------");

                Console.WriteLine("Definitions:");
                Console.WriteLine("- [--inputfile] is the path of the file you which to encode. Mandatory");

                Console.WriteLine("- Specify [--decode] to decode the input file. Optional.");

                Console.WriteLine("- Specify [--c] to not overwrite existing files. Optional.");

                Console.WriteLine("- If [--outputfile] is not specified, encoded file will be placed in the\n same directory "
                                  + " as the input file such as 'inputfile_encoded.png'.");
                Console.WriteLine("- [minres] is the minimum image resolution. Optional");
                Console.WriteLine("- [maxres] is the maximum image resolution. Optional");
                Console.WriteLine("- Use [minres] and [maxres] to override the defaults,\n"
                               + "  (1000 for the [minres], and 10,000 for [maxres])");

            }
            else
            {
                if (args[0] == "--oldmode")
                {
                    old_mode();
                }
                else
                {
                    string input_file = "";
                    string output_file = "";

                    bool decode = false;
                    bool check = false;

                    foreach (string arg in args)
                    {
                        if (arg == "--decode")
                        {
                            decode = true;
                        }

                        if (arg.StartsWith("--minres"))
                        {
                            min_res = Convert.ToInt32(arg.Replace("--minres", ""));
                        }

                        if (arg.StartsWith("--maxres"))
                        {
                            min_res = Convert.ToInt32(arg.Replace("--maxres", ""));
                        }

                        if (arg.StartsWith("--i:"))
                        {
                            input_file = arg.Remove(0, 4);
                        }

                        if (arg.StartsWith("--o:"))
                        {
                            output_file = arg.Remove(0, 4);
                        }

                        if (arg == "--c") { check = true; }
                    }

                    if (!File.Exists(input_file))
                    {
                        Console.WriteLine("Input file does not exist!, exiting");
                        return;
                    }

                    if (decode)
                    {
                        byte[] input_data = File.ReadAllBytes(input_file);

                        string save_dir = Path.GetDirectoryName(input_file);
                        Directory.CreateDirectory(save_dir);

                        EncoderDecoder ed = new EncoderDecoder() { MinimumResolution = min_res, MaximumResolution = max_res };

                        DecodeResult res = ed.DecodeFile(input_data);

                        if (res.Data == null)
                        {
                            Console.WriteLine("Unable to decode file");
                            return;
                        }

                        if (string.IsNullOrEmpty(output_file))
                        {
                            if (string.IsNullOrEmpty(res.FileName))
                            {
                                output_file = input_file + "_decoded." + res.Extension;
                            }
                            else
                            {
                                output_file = Path.Combine(save_dir, res.FileName + "." + res.Extension);
                            }
                        }

                        if (File.Exists(output_file) && check)
                        {
                            Console.WriteLine("Output file already exist, exiting");
                            return;
                        }

                        File.WriteAllBytes(output_file, res.Data);
                    }
                    else
                    {
                        if (output_file == "") { output_file = input_file + "_encoded.png"; }

                        if (File.Exists(output_file) && check)
                        {
                            Console.WriteLine("Output file already exist, exiting");
                            return;
                        }

                        FileInfo fi = new FileInfo(input_file);

                        EncoderDecoder ed = new EncoderDecoder() { MinimumResolution = min_res, MaximumResolution = max_res };

                        File.WriteAllBytes(output_file, ed.EncodeFile(File.ReadAllBytes(input_file), fi));
                    }
                }
            }
        }

        private static string get_arg(string[] args, int index)
        {
            if (args.Length - 1 < index)
            {
                return "";
            }
            else
            {
                return args[index];
            }
        }

        private static void old_mode()
        {
            Console.BackgroundColor = ConsoleColor.White;
            Console.ForegroundColor = ConsoleColor.Black;
            Console.Clear();

            while (true)
            {
                Console.Clear();

                Console.WriteLine(String.Format("1 - Change minimum image resolution - currently {0}x{0}", min_res));
                Console.WriteLine(String.Format("2 - Change maximum image resolution - currently {0}x{0}", max_res));
                Console.WriteLine("3 - Encode a file");
                Console.WriteLine("4 - Decode a file");


                Console.WriteLine("------------------------------------");

                Console.WriteLine(String.Format("Maximum file size is: {0} (limited by maximum image resolution)", Common.FormatSize(max_res * max_res * 4)));


                int action = Convert.ToInt32(prompt("Select action number:"));

                switch (action)
                {
                    case 1:
                        {
                            int new_z = Convert.ToInt32(prompt("Enter new minimum image resolution:"));
                            if (new_z >= max_res) { Console.WriteLine("Minimun image resolution cannot be more or equal to maximum image resolution"); break; }
                            if (new_z <= 0) { Console.WriteLine("Resolution cannot be less than or equal to 0"); break; }

                            min_res = new_z;
                        }
                        break;
                    case 2:
                        {
                            int new_a = Convert.ToInt32(prompt("Enter new maximum image resolution:"));
                            if (new_a <= min_res) { Console.WriteLine("Maximum image resolution cannot be more or equal to minimum image resolution"); break; }
                            if (new_a > 10000) { Console.WriteLine("Resolution cannot be more than 10000"); break; }
                            max_res = new_a;
                        }
                        break;
                    case 3:
                        {
                            string file_name = prompt("Enter file path:").Replace(@"""", String.Empty);
                        sp:
                            string save_path = prompt("Enter save path (Leave empty to append '_encoded'):").Replace(@"""", String.Empty);

                            if (save_path == "") { save_path = file_name + "_encoded.png"; }

                            if (System.IO.File.Exists(save_path))
                            {
                                Console.WriteLine("The provided save path exist already");
                                string response = prompt("Overwrite ? Y/N").ToLower();
                                if (!response.StartsWith("y"))
                                {
                                    goto sp;
                                }
                            }

                            EncoderDecoder ed = new EncoderDecoder()
                            {
                                MaximumResolution = max_res,
                                MinimumResolution = min_res
                            };

                            FileInfo fi = new FileInfo(file_name);

                            byte[] encoded_data = ed.EncodeFile(File.ReadAllBytes(fi.FullName), fi);
                            System.IO.File.WriteAllBytes(save_path, encoded_data);
                        } break;
                    case 4:
                        {
                            string encoded_file_path = prompt("Enter encoded file path").Replace(@"""", "");
                            FileInfo fi = new FileInfo(encoded_file_path);
                            if (fi.Exists)
                            {
                                string save_dir = prompt("Enter save path (a directory)").Replace(@"""", "");

                                Directory.CreateDirectory(save_dir);

                                EncoderDecoder ed = new EncoderDecoder()
                                {
                                    MaximumResolution = max_res,
                                    MinimumResolution = min_res
                                };

                                DecodeResult result = ed.DecodeFile(File.ReadAllBytes(encoded_file_path));

                                if (result.Data == null)
                                {
                                    Console.WriteLine("Unable to decode file");
                                }

                                string decoded_file_path = null;

                                if (string.IsNullOrWhiteSpace(result.FileName))
                                {
                                    decoded_file_path = Path.Combine(save_dir, string.Format("{0}.{1}", fi.Name.Remove(fi.Name.LastIndexOf('.')), result.Extension));
                                }
                                else
                                {
                                    decoded_file_path = Path.Combine(save_dir, string.Format("{0}.{1}", result.FileName, result.Extension));
                                }

                                File.WriteAllBytes(decoded_file_path, result.Data);

                                break;
                            }
                            else
                            {
                                Console.WriteLine("This file does not exist!");
                                break;
                            }
                        }
                    default:
                        Console.WriteLine("Unkown command");
                        break;
                }
            }
        }

        private static string prompt(string message)
        {
            Console.WriteLine(message);
            return Console.ReadLine();
        }
    }
}
