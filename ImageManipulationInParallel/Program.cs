/*
 * Bill Nicholson
 * nicholdw@ucmail.uc.edu
 * https://docs.microsoft.com/en-us/dotnet/api/system.drawing.image.fromfile?view=dotnet-plat-ext-3.1
 * 
 * Need to add the reference to System.Drawing.dll to the project!
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Globalization;
using System.Diagnostics;
using System.Threading;

namespace ImageManipulationInParallel
{
    class Program
    {
        static void Main(string[] args)
        {
            // There are 10,000 ticks in a millisecond.
            Console.WriteLine("Serially: " + DoSerially() / 10_000 + " milliseconds.");
            Console.WriteLine("Parallel: " + DoInParallel() / 10_000 + " milliseconds.");
        }
        private static long DoSerially()
        {
            Image image = Image.FromFile("..\\..\\Images\\violet under chair.jpg");
            Bitmap bitmap = (Bitmap)image;
            Size size = bitmap.Size;
            // Take out the red, pixel by pixel
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            for (int i = 0; i < size.Width; i++)
            {
                for (int k = 0; k < size.Height; k++)
                {
                    Color color = bitmap.GetPixel(i, k);
                    Color newColor = Color.FromArgb(0, color.G, color.B);
                    bitmap.SetPixel(i, k, newColor);                // Take out the red
                }
            }
            stopwatch.Stop();
            bitmap.Save("..\\..\\Images\\violet under chair modified.jpg");
            return stopwatch.ElapsedTicks;
        }
        private static long DoInParallel()
        {
            // https://docs.microsoft.com/en-us/dotnet/standard/parallel-programming/how-to-use-parallel-invoke-to-execute-parallel-operations
            Image image = Image.FromFile("..\\..\\Images\\violet under chair.jpg");
            Bitmap bitmap = (Bitmap)image;
            Size size = bitmap.Size;
            // Take out the red, pixel by pixel
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            // Perform three tasks in parallel on the matrix of pixels
            int width = size.Width;
            int widthOneThird = width / 3;
            int height = size.Height; int heightOneThird = height / 3;
            Object myLock = new Object();
            Rectangle rectangle01 = new Rectangle(0, 0               , bitmap.Width, heightOneThird);
            Rectangle rectangle02 = new Rectangle(0, heightOneThird  , bitmap.Width, heightOneThird);
            Rectangle rectangle03 = new Rectangle(0, heightOneThird*2, bitmap.Width, heightOneThird);
            Bitmap bitmap01 = new Bitmap(width, heightOneThird);
            Bitmap bitmap02 = new Bitmap(width, heightOneThird);
            Bitmap bitmap03 = new Bitmap(width, heightOneThird);
            bitmap01 = bitmap.Clone(rectangle01, bitmap.PixelFormat);
            bitmap02 = bitmap.Clone(rectangle02, bitmap.PixelFormat);
            bitmap03 = bitmap.Clone(rectangle03, bitmap.PixelFormat);
            Parallel.Invoke(
                () =>
                {
                    //                    Console.WriteLine("Begin first task...");
                    Color color, newColor;
                    for (int i = 0; i < bitmap01.Width; i++) {
                        for (int k = 0; k < bitmap01.Height; k++) {
                            //lock (myLock) 
                            { color = bitmap01.GetPixel(i, k); }
                            newColor = Color.FromArgb(0, color.G, color.B);
                            //lock (myLock)
                            {bitmap01.SetPixel(i, k, newColor);}               // Take out the red
                        }
                    }
                },  // close first Action

                () =>
                {
                    //                    Console.WriteLine("Begin second task...");
                    Color color, newColor;
                    for (int i = 0; i < bitmap02.Width; i++) {
                        for (int k = 0; k < bitmap02.Height; k++) {
                            //lock (myLock)
                            { color = bitmap02.GetPixel(i, k); }
                            newColor = Color.FromArgb(0, color.G, color.B);
                            //lock (myLock)
                            {bitmap02.SetPixel(i, k, newColor);}               // Take out the red
                        }
                    }
                }, //close second Action

                () =>
                {
                    //                    Console.WriteLine("Begin third task...");
                    Color color, newColor;
                    for (int i = 0; i < bitmap03.Width; i++) {
                        for (int k = 0; k < bitmap03.Height; k++) {
                            //lock (myLock)
                            { color = bitmap03.GetPixel(i, k); }
                            newColor = Color.FromArgb(0, color.G, color.B);
                            //lock (myLock)
                            {bitmap03.SetPixel(i, k, newColor);}               // Take out the red
                        }
                    }
                } //close third Action
            ); //close parallel.invoke
            // All parallel tasks are done when we get here.
            Rectangle sourceRectangle =   new Rectangle(0, 0,                 bitmap01.Width, bitmap01.Height);
            Rectangle targetRectangle01 = new Rectangle(0, 0,                 bitmap01.Width, bitmap01.Height);
            Rectangle targetRectangle02 = new Rectangle(0, heightOneThird,    bitmap01.Width, bitmap01.Height);
            Rectangle targetRectangle03 = new Rectangle(0, heightOneThird*2,  bitmap01.Width, bitmap01.Height);
            CopyRegionIntoImage(bitmap01, sourceRectangle, ref bitmap, targetRectangle01);
            CopyRegionIntoImage(bitmap02, sourceRectangle, ref bitmap, targetRectangle02);
            CopyRegionIntoImage(bitmap03, sourceRectangle, ref bitmap, targetRectangle03);
            Console.WriteLine("Returned from Parallel.Invoke"); stopwatch.Stop();

            // For debugging, save the three image chunks
            bitmap01.Save("..\\..\\Images\\violet under chair modified in parallel01.jpg");
            bitmap02.Save("..\\..\\Images\\violet under chair modified in parallel02.jpg");
            bitmap03.Save("..\\..\\Images\\violet under chair modified in parallel03.jpg");

            // Save the final image after rebuilding it
            bitmap.Save("..\\..\\Images\\violet under chair modified in parallel.jpg");
            return stopwatch.ElapsedTicks;
        }
        /// <summary>
        /// https://stackoverflow.com/questions/9616617/c-sharp-copy-paste-an-image-region-into-another-image
        /// </summary>
        /// <param name="srcBitmap"></param>
        /// <param name="srcRegion"></param>
        /// <param name="destBitmap"></param>
        /// <param name="destRegion"></param>
        public static void CopyRegionIntoImage(Bitmap srcBitmap, Rectangle srcRegion, ref Bitmap destBitmap, Rectangle destRegion) {
            using (Graphics grD = Graphics.FromImage(destBitmap)) {
                grD.DrawImage(srcBitmap, destRegion, srcRegion, GraphicsUnit.Pixel);
            }
        }
    }
}
