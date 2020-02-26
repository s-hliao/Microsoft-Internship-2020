using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;

namespace splitPhoto
{
    class Program
    {
        static void Main(string[] args) {
            string path = "";
            Bitmap bmp = new Bitmap(path);
            List<Image> l = new List<Image>();
            Graphics g = Graphics.FromImage(bmp);            
            for(int i = 0; i<8; i++) {
                for(int j = 0; j<8; j++) {
                    Rectangle r = new Rectangle(i, j, bmp.Width/8, bmp.Height/8);
                    l.Add(cropImage(bmp, r));
                }
            }
            for (int i = 0; i<l.Count; i++) {
                String output = "" + i;
                l[i].Save(output, ImageFormat.Jpeg) ;
            }
            
        }

        static Image cropImage(Image img, Rectangle cropArea) {
            Bitmap bmpImage = new Bitmap(img);
            Bitmap bmpCrop = bmpImage.Clone(cropArea, System.Drawing.Imaging.PixelFormat.DontCare);
            return (Image)(bmpCrop);
        }


    }
}
