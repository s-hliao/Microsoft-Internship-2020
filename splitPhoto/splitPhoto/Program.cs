using System;
using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;

namespace splitPhoto
{
    class Program
    {
        static void Main(string[] args) {
            
            string path = Path.Combine(AppContext.BaseDirectory, "source");
            string[] allPaths = Directory.GetFiles(path);
            List<Bitmap> l = new List<Bitmap>();

            for (int f = 0; f < allPaths.Length; f++)
            {
                using (Stream BitmapStream = System.IO.File.Open(allPaths[f], System.IO.FileMode.Open))
                {
                    Console.WriteLine("found file");
                    Image img = Image.FromStream(BitmapStream);
                    Bitmap bmp = new Bitmap(img);ur

                    Graphics g = Graphics.FromImage(bmp);
                    for (int i = 0; i < 8; i++)
                    {
                        for (int j = 0; j < 8; j++)
                        {
                            Rectangle r = new Rectangle(i * bmp.Width / 8, j * bmp.Height / 8, bmp.Width / 8, bmp.Height / 8);
                            l.Add(cropImage(bmp, r));
                            Console.WriteLine("Cut" + i + "x" + j);
                        }
                    }

                }
            }
            string folder = Path.Combine(AppContext.BaseDirectory, "boardSplit");
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
            for (int i = 0; i < l.Count; i++)
            {
                string output = "chessBoard" + i + ".jpg";
                l[i].Save(Path.Combine(folder, output), ImageFormat.Jpeg);
                Console.WriteLine("Saved" + i);
            }

        }

        static Bitmap cropImage(Bitmap bmpImage, Rectangle cropArea) {
            Bitmap bmpCrop = bmpImage.Clone(cropArea, System.Drawing.Imaging.PixelFormat.DontCare);
            return bmpCrop;
        }


    }
}
