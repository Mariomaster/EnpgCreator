using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using NSMBe4;
using System.IO;

namespace EnpgCreator
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Image to ENPG Batch Encoder - v1.0");
            Console.WriteLine("By Mariomaster using NSMBe's Image Indexer and LZ77 compressor");
            Console.WriteLine();

            if (args.Length == 0)
            {
                Console.WriteLine("Drag 256x256 Image files onto this application to convert them to NSMB's ENPG format.");
                Console.WriteLine();
                Console.Write("Press any key to exit...");
                Console.ReadKey();
                return;
            }

            bool compress = false;
            bool forCredits = false;

            checkCompress:
            Console.Write("Do you want to LZ77 compress the ENPGs? (y/n): ");
            char rc = Console.ReadKey().KeyChar;
            Console.WriteLine();

            if (rc == 'y' || rc == 'Y')
                compress = true;
            else if (rc == 'n' || rc == 'N')
                compress = false;
            else
                goto checkCompress;


            checkForCredits:
            Console.Write("Are the Images intended as Credit Images (Reserve first 5 Palette slots)? (y/n): ");
            char rcr = Console.ReadKey().KeyChar;
            Console.WriteLine();

            if (rcr == 'y' || rcr == 'Y')
                forCredits = true;
            else if (rcr == 'n' || rcr == 'N')
                forCredits = false;
            else
                goto checkForCredits;

            Console.WriteLine();
            Console.WriteLine("Encoding " + args.Length + " images.");
            Console.WriteLine();

            int successCount = 0;

            foreach (string path in args)
            {
                if (!File.Exists(path))
                {
                    Console.WriteLine("The file " + path + " does not exist!");
                    continue;
                }

                if (!System.Web.MimeMapping.GetMimeMapping(path).StartsWith("image/"))
                {
                    Console.WriteLine("The file " + path + " is not an Image!");
                    continue;
                }

                Bitmap bmp = (Bitmap)Image.FromFile(path);

                if (bmp.Width != 256 || bmp.Height != 256)
                {
                    Console.WriteLine("The Image " + path + " is not 256x256!");
                    continue;
                }

                string folder = Path.GetDirectoryName(path);
                string name = Path.GetFileNameWithoutExtension(path);

                Console.Write("Encoding " + path + "...");

                bmp.RotateFlip(RotateFlipType.Rotate90FlipX);

                int cCount = 256;
                if (forCredits)
                    cCount = 251;

                Color[] pal = ImageIndexer.createPaletteForImage(bmp, cCount);

                ByteArrayOutputStream b = new ByteArrayOutputStream();

                for (int x = 0; x < bmp.Width; x++)
                    for (int y = 0; y < bmp.Height; y++)
                    {
                        Color c = bmp.GetPixel(x, y);
                        int i = getClosestColor(pal, c);

                        int writeColor = i;

                        if (forCredits)
                        {
                            if (writeColor > 0)
                                writeColor += 5;
                        }

                        b.writeByte((byte)writeColor);
                    }

                b.writeUShort(toRGB15(pal[0]));

                if (forCredits)
                    for (int i = 0; i < 5; i++)
                        b.writeUShort(0x0);

                for (int i = 1; i < pal.Length; i++)
                    b.writeUShort(toRGB15(pal[i]));

                if (!Directory.Exists(folder + "\\convert"))
                    Directory.CreateDirectory(folder + "\\convert");

                var bw = new BinaryWriter(File.Open(folder + "\\convert\\" + name + ".enpg", FileMode.OpenOrCreate));

                if (compress)
                    bw.Write(lz77.LZ77_Compress(b.getArray()));
                else
                    bw.Write(b.getArray());

                bw.Flush();
                bw.Close();

                successCount++;
                Console.WriteLine("\rEncoding " + path + " done.");
            }

            Console.WriteLine();
            Console.Write("Successfully encoded " + successCount + " of " + args.Length + " images. Press any key to exit...");
            Console.ReadKey();
        }

        static private int getClosestColor(Color[] pal, Color c)
        {
            if (c.A == 0)
                return 0;

            int bestInd = 1;
            float bestDif = ImageIndexer.colorDifferenceWithoutAlpha(pal[1], c);

            for (int i = 1; i < pal.Length; i++)
            {
                float d = ImageIndexer.colorDifferenceWithoutAlpha(pal[i], c);
                if (d < bestDif)
                {
                    bestDif = d;
                    bestInd = i;
                }
            }

            return bestInd;
        }

        private static ushort toRGB15(Color c)
        {
            byte r = (byte)(c.R >> 3);
            byte g = (byte)(c.G >> 3);
            byte b = (byte)(c.B >> 3);

            ushort val = 0;

            val |= r;
            val |= (ushort)(g << 5);
            val |= (ushort)(b << 10);
            return val;
        }
    }
}
