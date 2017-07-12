using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media.Imaging;

namespace WatermarkAlgrithm
{
    class ArnoldClass
    {
        public const int IMGSize = 40;
        /// Arnold置乱和恢复算法
        //只收集 400*400 的黑白图像 ， 执行了八次arnold变换
        public static WriteableBitmap ArnoldIMG(WriteableBitmap sourceimage)
        {
            try
            {
                if (sourceimage == null) return null;
                //if (sourceimage == null || sourceimage.PixelWidth != IMGSize || sourceimage.PixelHeight != IMGSize) return null;
                WriteableBitmap temp = new WriteableBitmap(IMGSize, IMGSize);
                int locx2 = 0, locy2 = 0;
                int curline = 0;
                for (int i = 0; i < IMGSize; ++i)
                {
                    for (int j = 0; j < IMGSize; ++j)
                    {
                        //一次
                        //locx2 = (i + j + 1) % 400;
                        //locy2 = (i + j + j + 2) % 400;
                        //temp.Pixels[locx2 * 400 + locy2] = wb.Pixels[curline + j];
                        /// *8
                        //locx2 = (610 * i + 987 * j + 610 + 987 - 1) % 400;
                        //locy2 = (987 * i + 1597 * j + 987 + 1597 - 1) % 400;
                        //简化计算后
                        locx2 = (610 * i + 987 * j + 1596) % IMGSize;
                        locy2 = (987 * i + 1597 * j + 2583) % IMGSize;
                        //locx2 = (610 * i + 987 * j + 396) % 400;
                        //locy2 = (987 * i + 1597 * j + 183) % 400;
                        temp.Pixels[locx2 * IMGSize + locy2] = sourceimage.Pixels[curline + j];
                    }
                    curline += IMGSize ;
                }
                return temp;
            }
            catch (Exception) { return null; }
        }

        //只收集 400*400 的经过Arnold变换的黑白图像，转换为原始图像
        public static WriteableBitmap ReArnoldIMG(WriteableBitmap sourceimage)
        {
            try
            {
                //if (sourceimage == null || sourceimage.PixelWidth != IMGSize || sourceimage.PixelHeight != IMGSize) return null;
                if (sourceimage == null) return null;
                WriteableBitmap temp = new WriteableBitmap(IMGSize, IMGSize);
                int locx2 = 0, locy2 = 0;
                int curline = 0;
                int blancey = IMGSize - 378; //保证locy是正值需将所有负的加数-378变成 N*IMGSize-397
                while (blancey < 0) 
                    blancey += IMGSize;
                for (int i = 0; i < IMGSize; ++i)
                {
                    for (int j = 0; j < IMGSize; ++j)
                    {
                        //一次
                        //locx2 = (i + i - j + 399) % 400;
                        //locy2 = (j - i + 399) % 400;
                        //temp.Pixels[locx2 * 400 + locy2] = wb.Pixels[curline + j];

                        /// *8
                        //locx2 = (1597 * i + 987 * (400 - j) + 1597 - 987 - 1) % 400;
                        //locy2 = (610 * j + 987 * (400 - i) + 610 - 987 - 1) % 400;
                        //简化计算后
                        locx2 = (1597 * i + 987 * (IMGSize - j) + 609) % IMGSize;
                        locy2 = (610 * j + 987 * (IMGSize - i) + blancey) % IMGSize;
                        temp.Pixels[locx2 * IMGSize + locy2] = sourceimage.Pixels[curline + j];
                    }
                    curline += IMGSize;
                }
                return temp;
            }
            catch (Exception) { return null; }
        }

        //输入原始水印照片，获得变化后水印的一维灰度的数组
        public static int[] GetGreyArrayAfterArnold(WriteableBitmap wb)
        {
            int temp = 0,r,g,b;
            WriteableBitmap awb = ReArnoldIMG(wb);
            int size = awb.Pixels.Length;
            int[] arr = new int[size];
            for (int i = 0; i < size; i++)
            {
                temp = awb.Pixels[i];
                r = (temp & 0xFF0000) >> 16;
                g = (temp & 0xFF00) >> 8;
                b = temp & 0xFF;
                temp = (int)(r * 0.299 + g * 0.587 + b * 0.114);
                if (temp < 0) temp = 0;
                else if (temp > 255) temp = 255;
                arr[i] = temp;
            }
            return arr;
        }

        //输入原始照片，获得归一的变换后的一维数组，取值范围[-1,1]
        public static double[] GetGreyArrayAfterArnoldFactor(WriteableBitmap wb)
        {
            int temp = 0, r, g, b;
            WriteableBitmap awb = ReArnoldIMG(wb);
            int size = awb.Pixels.Length;
            double[] arr = new double[size];
            double factorTemp = 0.0;
            for (int i = 0; i < size; i++)
            {
                temp = awb.Pixels[i];
                r = (temp & 0xFF0000) >> 16;
                g = (temp & 0xFF00) >> 8;
                b = temp & 0xFF;
                factorTemp = (r * 0.299 + g * 0.587 + b * 0.114 - 128) / 128;//归一运算
                if (factorTemp < -1) factorTemp = -1;
                else if (factorTemp > 1) factorTemp = 1;
                arr[i] = factorTemp;
            }
            return arr;
        }

        //输入原始照片，获得二值（0,1）的序列
        public static int[] GetGreyArrayAfterArnoldZeroOne(WriteableBitmap wb)
        {
            int temp = 0, r, g, b;
            WriteableBitmap awb = ArnoldIMG(wb);
            int size = awb.Pixels.Length;
            int[] arr = new int[size];
            double factorTemp = 0.0;
            for (int i = 0; i < size; i++)
            {
                temp = awb.Pixels[i];
                r = (temp & 0xFF0000) >> 16;
                g = (temp & 0xFF00) >> 8;
                b = temp & 0xFF;
                //取灰度值
                factorTemp = r * 0.299 + g * 0.587 + b * 0.114;
                //二值化，大于128的是白色1，小于等于128的是0
                if (factorTemp < 128.1) arr[i] = 0;
                else arr[i] = 1;
            }
            return arr;
        }
    }
}
