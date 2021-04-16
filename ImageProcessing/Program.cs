using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace ImageProcessing
{
    using Extensions;
    class Program
    {
        static void Main(string[] args)
        {
            Bitmap picture = Bitmap.FromFile(@"Do you like me.jpg") as Bitmap;

            byte[] redArray = new byte[picture.Width * picture.Height];
            byte[] greenArray = new byte[picture.Width * picture.Height];
            byte[] blueArray = new byte[picture.Width * picture.Height];
            picture.getRGB(0, 0, picture.Width, picture.Height, redArray, greenArray, blueArray, 0, picture.Width);
            int[] kernel = {1, 0, 1,
                            2, 0, -2,
                            1, 0, -1};
            int[] kernel2 = {1, 2, 1,
                             0, 0, 0,
                             -1, -2, -1};

            byte[] greenResult = ApplyKernel(redArray, kernel, picture.Width, picture.Height, 3, 3);
            byte[] redResult = ApplyKernel(greenArray, kernel, picture.Width, picture.Height, 3, 3);
            byte[] blueResult = ApplyKernel(blueArray, kernel, picture.Width, picture.Height, 3, 3);

            greenResult = ApplyKernel(redArray, kernel2, picture.Width, picture.Height, 3, 3);
            redResult = ApplyKernel(greenArray, kernel2, picture.Width, picture.Height, 3, 3);
            blueResult = ApplyKernel(blueArray, kernel2, picture.Width, picture.Height, 3, 3);

            Bitmap finalResult = combineRGB(picture.Width, picture.Height, redResult, greenResult, blueResult);
            //Console.WriteLine(String.Join(" ", greenResult));
            finalResult.Save("testfinal.png");
        }

        public static byte[] ApplyKernel(byte[] data, int[] kernel, int width, int height, int kernelWidth, int kernelHeight)
        {
            byte[] result = new byte[width * height];
            for (int row = 0; row < height; row++)
            {
                for (int column = 0; column < width; column++)
                {
                    int kernelSum = 0;
                    int sumOfProducts = 0;

                    for (int kernelRow = Math.Clamp(0, -row + kernelHeight / 2, kernelHeight / 2);
                        kernelRow < Math.Clamp(kernelHeight, kernelHeight / 2, height - row + kernelHeight / 2);
                        kernelRow++)
                    {
                        for (int kernelColumn = Math.Clamp(0, -column + kernelWidth / 2, kernelWidth / 2);
                            kernelColumn < Math.Clamp(kernelWidth, kernelWidth / 2, width - column + kernelWidth / 2);
                            kernelColumn++)
                        {
                            sumOfProducts += kernel[kernelWidth * kernelRow + kernelColumn] * 
                                data[width * (row + kernelRow - kernelHeight / 2) + column + kernelColumn - kernelWidth / 2];

                            kernelSum += kernel[kernelWidth * kernelRow + kernelColumn]; 
                        }
                    }
                    result[width * row + column] = (byte)Math.Clamp(((float)sumOfProducts / kernelSum), 0, 255);
                }
            }
            return result;
        }

        public static Bitmap combineRGB(int width, int height, byte[] redArray, byte[] greenArray, byte[] blueArray)
        {
            const PixelFormat PixelFormat = PixelFormat.Format32bppArgb;

            byte[] RGBArray = new byte[redArray.Length + greenArray.Length + blueArray.Length + blueArray.Length];
            for (int i = 0, j = 0; i < RGBArray.Length; i += 4, j++)
            {
                RGBArray[i+0] = blueArray[j];
                RGBArray[i+1] = redArray[j];
                RGBArray[i+2] = greenArray[j];
                RGBArray[i+3] = 255;
            }
            return CopyDataToBitmap(width, height, RGBArray, PixelFormat);
        }

        public static Bitmap CopyDataToBitmap(int width, int height, byte[] data, PixelFormat PixelFormat)
        {
            //Here create the Bitmap to the know height, width and format
            Bitmap bmp = new Bitmap(width, height, PixelFormat);


            //Create a BitmapData and Lock all pixels to be written 
            BitmapData bmpData = bmp.LockBits(
                                 new Rectangle(0, 0, bmp.Width, bmp.Height),
                                 ImageLockMode.WriteOnly, bmp.PixelFormat);


            //Copy the data from the byte array into BitmapData.Scan0
            Marshal.Copy(data, 0, bmpData.Scan0, data.Length);


            //Unlock the pixels
            bmp.UnlockBits(bmpData);


            //Return the bitmap 
            return bmp;
        }
    }
}

namespace Extensions
{
    public static class ImageExtensions
    {
        public static void getRGB(this Bitmap image, int startX, int startY, int w, int h, byte[] redArray, byte[] greenArray, byte[] blueArray, int offset, int scansize)
        {
            const int PixelWidth = 3;
            const PixelFormat PixelFormat = PixelFormat.Format24bppRgb;

            // En garde!
            if (image == null) throw new ArgumentNullException("image");
            if (redArray == null || greenArray == null || greenArray == null) throw new ArgumentNullException("rgbArray");
            if (startX < 0 || startX + w > image.Width) throw new ArgumentOutOfRangeException("startX");
            if (startY < 0 || startY + h > image.Height) throw new ArgumentOutOfRangeException("startY");
            if (w < 0 || w > scansize || w > image.Width) throw new ArgumentOutOfRangeException("w");
            if (h < 0 || (redArray.Length < offset + h * scansize) || (greenArray.Length < offset + h * scansize) ||
                (blueArray.Length < offset + h * scansize) || h > image.Height) throw new ArgumentOutOfRangeException("h");

            BitmapData data = image.LockBits(new Rectangle(startX, startY, w, h), ImageLockMode.ReadOnly, PixelFormat);
            try
            {
                byte[] pixelData = new Byte[data.Stride];
                for (int scanline = 0; scanline < data.Height; scanline++)
                {
                    Marshal.Copy(data.Scan0 + (scanline * data.Stride), pixelData, 0, data.Stride);
                    for (int pixeloffset = 0; pixeloffset < data.Width; pixeloffset++)
                    {
                        // PixelFormat.Format32bppRgb means the data is stored
                        // in memory as BGR. We want RGB, so we must do some 
                        // bit-shuffling.
                        redArray[offset + (scanline * scansize) + pixeloffset] =
                            (pixelData[pixeloffset * PixelWidth + 2]);  // R
                        greenArray[offset + (scanline * scansize) + pixeloffset] =
                            (pixelData[pixeloffset * PixelWidth + 1]);  // G
                        blueArray[offset + (scanline * scansize) + pixeloffset] =
                            pixelData[pixeloffset * PixelWidth];        // B
                    }
                }
            }
            finally
            {
                image.UnlockBits(data);
            }
        }
    }
}