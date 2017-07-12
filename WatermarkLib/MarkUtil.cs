using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace WatermarkAlgrithm
{
    class MarkUtil
    {
        public static double Factor = 0.1;//水印加权因子  B = B + Factor * Wf
        public static bool CanMark(WriteableBitmap bitmap, WriteableBitmap watermark)
        {
            if (bitmap == null || watermark == null) return false;
            else
            {
                //原图的大小至少是水印图像（允许0.95的尾部数据去除，暂时约定，不影响整体的情况下）的八倍
                if (bitmap.Pixels.Length / 16 >= watermark.Pixels.Length) return true;
                else return false;
            }
        }
        //水印插入
        public static WriteableBitmap MarkInsert(WriteableBitmap bitmap, WriteableBitmap watermark)
        {
            try
            {
                if (bitmap == null || watermark == null) return null;
                int bitmapSize = bitmap.Pixels.Length;
                int bitmapHeight = bitmap.PixelHeight;
                int bitmapWidth = bitmap.PixelWidth;
                //获取归一后的灰度系数
                int[] markarray = ArnoldClass.GetGreyArrayAfterArnoldZeroOne(watermark);
                int markSize = markarray.Length;
                //将原图分解成 R,G,B 三个分量
                double[] imageR = new double[bitmapSize];
                double[] imageG = new double[bitmapSize];
                double[] imageB = new double[bitmapSize];
                int temp = 0;
                for (int i = 0; i < bitmapSize; ++i)
                {
                    temp = bitmap.Pixels[i];
                    imageR[i] = (temp >> 16) & 0xFF;
                    imageG[i] = (temp >> 8) & 0xFF;
                    imageB[i] = temp & 0xFF;
                }
                //第一次小波变换
                DWT2 dwt = new DWT2(bitmap.PixelWidth, bitmap.PixelHeight);
                dwt.wavelet2D(ref imageR, DWT2.DWTH, DWT2.DWTG, 1);
                dwt.wavelet2D(ref imageG, DWT2.DWTH, DWT2.DWTG, 1);
                dwt.wavelet2D(ref imageB, DWT2.DWTH, DWT2.DWTG, 1);
                //第二次小波变换
                dwt.wavelet2D(ref imageR, DWT2.DWTH, DWT2.DWTG, 1);
                dwt.wavelet2D(ref imageG, DWT2.DWTH, DWT2.DWTG, 1);
                dwt.wavelet2D(ref imageB, DWT2.DWTH, DWT2.DWTG, 1);
                //取此时的LH2或HL2带（此时选了HL2，相当于中频）作为嵌入水印的部分
                int bitmapHeight4 = bitmapHeight / 4;
                int bitmapWidth4 = bitmapWidth / 4;
                int bitmapWidth2 = bitmapWidth / 2;
                int halfIMGSizeFrist = bitmap.Pixels.Length / 2;
                int fourIMGSizeFrist = bitmap.Pixels.Length / 4;
                //取系数最小值并将所有系数变为正数
                double minR_H = 0, minG_H = 0, minB_H = 0;
                for (int i = 0; i < bitmapSize; ++i)
                {
                    if (imageR[i] < minR_H) minR_H = imageR[i];
                    if (imageG[i] < minG_H) minG_H = imageG[i];
                    if (imageB[i] < minB_H) minB_H = imageB[i];
                }
                //系数为 1+abs（min）
                minR_H = 1 - minR_H; minG_H = 1 - minG_H; minB_H = 1 - minB_H; //取到最小值之后加一
                for (int i = 0; i < bitmapSize; ++i)
                {
                    imageR[i] = minR_H + imageR[i];
                    imageG[i] = minG_H + imageG[i];
                    imageB[i] = minB_H + imageB[i];
                }
                ////邻居矩阵
                temp = 0;// temp作为水印的一维向量的下标值  
                ///*
                // x0y0  x0y1  x0y2
                //       curX
                // x2y0  x2y1  x2y2
                // */
                for (int preline = bitmapHeight4 * bitmapWidth, curline = bitmapHeight4 * bitmapWidth + bitmapWidth, nextline = (bitmapHeight4 + 2) * bitmapWidth; nextline < halfIMGSizeFrist; )
                {
                    for (int j = 2; j < bitmapWidth4; j += 3)
                    {
                        imageR[curline + j - 1] = getNewX(imageR[curline + j - 1], markarray[temp], imageR[preline + j - 2], imageR[preline + j - 1], imageR[preline + j], imageR[nextline + j - 2], imageR[nextline + j - 1], imageR[nextline + j]);
                        imageG[curline + j - 1] = getNewX(imageG[curline + j - 1], markarray[temp], imageG[preline + j - 2], imageG[preline + j - 1], imageG[preline + j], imageG[nextline + j - 2], imageG[nextline + j - 1], imageG[nextline + j]);
                        imageB[curline + j - 1] = getNewX(imageB[curline + j - 1], markarray[temp], imageB[preline + j - 2], imageB[preline + j - 1], imageB[preline + j], imageB[nextline + j - 2], imageB[nextline + j - 1], imageB[nextline + j]);
                        ++temp;
                        if (temp >= markSize) break;
                    }
                    preline = nextline + bitmapWidth;
                    curline = preline + bitmapWidth;
                    nextline = curline + bitmapWidth;
                    if (temp >= markSize) break;
                }
                temp = 0;
                for (int preline = 0, curline = bitmapWidth, nextline = bitmapWidth + bitmapWidth; nextline < fourIMGSizeFrist; )
                {
                    for (int j = 2 +bitmapWidth4; j < bitmapWidth2; j += 3)
                    {
                        imageR[curline + j - 1] = getNewX(imageR[curline + j - 1], markarray[temp], imageR[preline + j - 2], imageR[preline + j - 1], imageR[preline + j], imageR[nextline + j - 2], imageR[nextline + j - 1], imageR[nextline + j]);
                        imageG[curline + j - 1] = getNewX(imageG[curline + j - 1], markarray[temp], imageG[preline + j - 2], imageG[preline + j - 1], imageG[preline + j], imageG[nextline + j - 2], imageG[nextline + j - 1], imageG[nextline + j]);
                        imageB[curline + j - 1] = getNewX(imageB[curline + j - 1], markarray[temp], imageB[preline + j - 2], imageB[preline + j - 1], imageB[preline + j], imageB[nextline + j - 2], imageB[nextline + j - 1], imageB[nextline + j]);
                        ++temp;
                        if (temp >= markSize) break;
                    }
                    preline = nextline + bitmapWidth;
                    curline = preline + bitmapWidth;
                    nextline = curline + bitmapWidth;
                    if (temp >= markSize) break;
                }
                //减去为了变为正值所加的系数值 minR_H
                ////for (int preline = bitmapHeight / 4 * bitmapWidth, curline = bitmapHeight / 4 * bitmapWidth + bitmapWidth, nextline = (bitmapHeight / 4 + 2) * bitmapWidth; nextline < halfIMGSizeFrist; )
                ////{
                ////    for (int j = 2; j < bitmapWidth4; j += 3)
                ////    {
                ////        imageR[curline + j - 1] -= minR_H;
                ////        imageG[curline + j - 1] -= minG_H;
                ////        imageB[curline + j - 1] -= minB_H;
                ////        ++temp;
                ////        if (temp >= markSize) break;
                ////    }
                ////    preline = nextline + bitmapWidth;
                ////    curline = preline + bitmapWidth;
                ////    nextline = curline + bitmapWidth;
                ////    ++temp;
                ////    if (temp >= markSize) break;
                ////}
                for (int i = 0; i < bitmapSize; ++i)
                {
                    imageR[i]  = imageR[i] - minR_H;
                    imageG[i]  = imageG[i] - minG_H;
                    imageB[i]  = imageB[i] - minB_H;
                }
                //对图像做两次逆小波运算，生成含水印的图片 和dwt的宽高相同故不变化
                dwt.iwavelet2D(ref imageR, DWT2.DWTH, DWT2.DWTG, 1);
                dwt.iwavelet2D(ref imageG, DWT2.DWTH, DWT2.DWTG, 1);
                dwt.iwavelet2D(ref imageB, DWT2.DWTH, DWT2.DWTG, 1);
                dwt.iwavelet2D(ref imageR, DWT2.DWTH, DWT2.DWTG, 1);
                dwt.iwavelet2D(ref imageG, DWT2.DWTH, DWT2.DWTG, 1);
                dwt.iwavelet2D(ref imageB, DWT2.DWTH, DWT2.DWTG, 1);
                //对逆小波运算后得到的三个分量做合并，生成要输出的 WriteableBitmap
                WriteableBitmap resultBitmap = new WriteableBitmap(bitmapWidth, bitmapHeight);
                for (int i = 0, r, g, b; i < bitmapSize; ++i)
                {
                     r = ((int)Math.Round(imageR[i], 1));
                     if (r > 255) r = 255;
                     else if (r < 0) r = 0;
                     g = ((int)Math.Round(imageG[i], 1));
                     if (g > 255) g = 255;
                     else if (g < 0) g = 0;
                     b = (int)Math.Round(imageB[i], 1);
                     if (b > 255) b = 255;
                     else if (b < 0) b = 0;
                     resultBitmap.Pixels[i] = (0xFF << 24) | (r << 16) | (g << 8) | b;
                }
                return resultBitmap;
            }
            catch (Exception e) { Debug.WriteLine(e.Message); return null; }
        }

        //水印检测和提取
        public static WriteableBitmap GetMark(WriteableBitmap bitmap)
        {
            try
            {
                int markSize = QRCodeUtil.QRSize * QRCodeUtil.QRSize;
                // 检测图片不能存储足够的位置给水印，即它的 1/16 的二层DWT面积内不足以除以9获得存有水印的点
                //if (bitmap == null || bitmap.Pixels.Length / 16 < markSize) 
                //    return null;
                //获取归一后的灰度系数
                int[,] markarray = new int[6,QRCodeUtil.QRSize * QRCodeUtil.QRSize];
                WriteableBitmap watermark = new WriteableBitmap(QRCodeUtil.QRSize,QRCodeUtil.QRSize);
                int bitmapSize = bitmap.Pixels.Length;
                int bitmapHeight = bitmap.PixelHeight;
                int bitmapWidth = bitmap.PixelWidth;
                //将原图分解成 R,G,B 三个分量
                double[] imageR = new double[bitmapSize];
                double[] imageG = new double[bitmapSize];
                double[] imageB = new double[bitmapSize];
                int temp = 0;
                for (int i = 0; i < bitmapSize; ++i)
                {
                    temp = bitmap.Pixels[i];
                    imageR[i] = (temp >> 16) & 0xFF;
                    imageG[i] = (temp >> 8) & 0xFF;
                    imageB[i] = temp & 0xFF;
                }
                //第一次小波变换
                DWT2 dwt = new DWT2(bitmap.PixelWidth, bitmap.PixelHeight);
                dwt.wavelet2D(ref imageR, DWT2.DWTH, DWT2.DWTG, 1);
                dwt.wavelet2D(ref imageG, DWT2.DWTH, DWT2.DWTG, 1);
                dwt.wavelet2D(ref imageB, DWT2.DWTH, DWT2.DWTG, 1);
                //第二次小波变换
                dwt.wavelet2D(ref imageR, DWT2.DWTH, DWT2.DWTG, 1);
                dwt.wavelet2D(ref imageG, DWT2.DWTH, DWT2.DWTG, 1);
                dwt.wavelet2D(ref imageB, DWT2.DWTH, DWT2.DWTG, 1);
                //取此时的LH2或HL2带（此时选了HL2，相当于中频）作为嵌入水印的部分
                int bitmapHeight4 = bitmapHeight / 4;
                int bitmapWidth4 = bitmapWidth / 4;
                int bitmapWidth2 = bitmapWidth / 2;
                int halfIMGSizeFrist = bitmap.Pixels.Length / 2;
                int fourIMGSizeFrist = bitmap.Pixels.Length / 4;
                //取系数最小值并将所有系数变为正数
                //取系数最小值并将所有系数变为正数
                double minR_H = 0, minG_H = 0, minB_H = 0;
                for (int i = 0; i < bitmapSize; ++i)
                {
                    if (imageR[i] < minR_H) minR_H = imageR[i];
                    if (imageG[i] < minG_H) minG_H = imageG[i];
                    if (imageB[i] < minB_H) minB_H = imageB[i];
                }
                //系数为 1+abs（min）
                minR_H = 1 - minR_H; minG_H = 1 - minG_H; minB_H = 1 - minB_H; //取到最小值之后加一
                for (int i = 0; i < bitmapSize; ++i)
                {
                    imageR[i] += minR_H;
                    imageG[i] += minG_H;
                    imageB[i] += minB_H;
                }
                //黑白两色
                byte[] ffw = { 0xFF, 0xFF, 0xFF, 0xFF };
                int whitecolor = BitConverter.ToInt32(ffw, 0);
                byte[] ffb = {  0x00, 0x00, 0x00,0xFF };
                int blackcolor = BitConverter.ToInt32(ffb, 0);
                //邻居矩阵,获取
                temp = 0; //temp作为颜色分量的判定值，<=1时认为是黑色0，其余认为是白色1
                int temp0 = 0;// temp0作为水印的一维向量的下标值  
                /*
                 x0y0  x0y1  x0y2
                       curX
                 x2y0  x2y1  x2y2
                 */
                for (int preline = bitmapHeight / 4 * bitmapWidth, curline = bitmapHeight / 4 * bitmapWidth + bitmapWidth, nextline = (bitmapHeight / 4 + 2) * bitmapWidth; nextline < halfIMGSizeFrist; )
                {
                    for (int j = 2; j < bitmapWidth4; j += 3)
                    {
                        markarray[0,temp0] = getWX(imageR[curline + j - 1], imageR[preline + j - 2], imageR[preline + j - 1], imageR[preline + j], imageR[nextline + j - 2], imageR[nextline + j - 1], imageR[nextline + j]);
                        markarray[1,temp0] = getWX(imageG[curline + j - 1], imageG[preline + j - 2], imageG[preline + j - 1], imageG[preline + j], imageG[nextline + j - 2], imageG[nextline + j - 1], imageG[nextline + j]);
                        markarray[2,temp0] = getWX(imageB[curline + j - 1], imageB[preline + j - 2], imageB[preline + j - 1], imageB[preline + j], imageB[nextline + j - 2], imageB[nextline + j - 1], imageB[nextline + j]);
                        //if (markarray[0, temp0] == 1)
                        //{
                        //    if (markarray[1, temp0] == 1) watermark.Pixels[temp0] = whitecolor;
                        //    else
                        //    {
                        //        if (markarray[2, temp0] == 1) watermark.Pixels[temp0] = whitecolor;
                        //        else watermark.Pixels[temp0] = blackcolor;
                        //    }
                        //}
                        //else
                        //{
                        //    if (markarray[1, temp0] == 0) watermark.Pixels[temp0] = blackcolor;
                        //    else
                        //    {
                        //        if (markarray[2, temp0] == 0) watermark.Pixels[temp0] = blackcolor;
                        //        else watermark.Pixels[temp0] = whitecolor;
                        //    }
                        //}
                        ++temp0;
                        if (temp0 >= markSize) break;
                    }
                    preline = nextline + bitmapWidth;
                    curline = preline + bitmapWidth;
                    nextline = curline + bitmapWidth;
                    if (temp0 >= markSize) break;
                }
                temp0 = 0;
                for (int preline = 0, curline = bitmapWidth, nextline = bitmapWidth + bitmapWidth; nextline < fourIMGSizeFrist; )
                {
                    for (int j = 2 + bitmapWidth4; j < bitmapWidth2; j += 3)
                    {
                        markarray[3, temp0] = getWX(imageR[curline + j - 1], imageR[preline + j - 2], imageR[preline + j - 1], imageR[preline + j], imageR[nextline + j - 2], imageR[nextline + j - 1], imageR[nextline + j]);
                        markarray[4, temp0] = getWX(imageG[curline + j - 1], imageG[preline + j - 2], imageG[preline + j - 1], imageG[preline + j], imageG[nextline + j - 2], imageG[nextline + j - 1], imageG[nextline + j]);
                        markarray[5, temp0] = getWX(imageB[curline + j - 1], imageB[preline + j - 2], imageB[preline + j - 1], imageB[preline + j], imageB[nextline + j - 2], imageB[nextline + j - 1], imageB[nextline + j]);
                        ++temp0;
                        if (temp0 >= markSize) break;
                    }
                    preline = nextline + bitmapWidth;
                    curline = preline + bitmapWidth;
                    nextline = curline + bitmapWidth;
                    if (temp0 >= markSize) break;
                }
                for (int i = 0, countw = 0; i < markSize; ++i)
                {
                    countw = 0;
                    if (markarray[0, i] == 1) ++countw;
                    if (markarray[1, i] == 1) ++countw;
                    if (markarray[2, i] == 1) ++countw;
                    if (markarray[3, i] == 1) ++countw;
                    if (markarray[4, i] == 1) ++countw;
                    if (markarray[5, i] == 1) ++countw;
                    if (countw >= 3) watermark.Pixels[i] = whitecolor; //白色认定多
                        //以下是分情况的另一种方法
                    //if (countw >= 4) watermark.Pixels[i] = whitecolor; //白色认定多
                    //else if (countw == 3) //更可能是白点
                    //{
                    //    int loc = i % QRCodeUtil.QRSize;
                    //    if (loc == 0) //该行第一个点，和它后面的点近似
                    //    {
                    //        if (i + 1 < QRCodeUtil.QRSize) watermark.Pixels[i] = watermark.Pixels[i + 1];
                    //        else watermark.Pixels[i] = whitecolor;
                    //    }
                    //    else if (loc == QRCodeUtil.QRSize - 1) //该行最后一个点
                    //    {
                    //        if (i > 0) watermark.Pixels[i] = watermark.Pixels[i - 1];
                    //        else watermark.Pixels[i] = whitecolor;
                    //    }
                    //    else watermark.Pixels[i] = whitecolor;
                    //}
                    //else if (countw == 2) //更可能是黑点
                    //{
                    //    int loc = i % QRCodeUtil.QRSize;
                    //    if (loc == 0) //该行第一个点，和它后面的点近似
                    //    {
                    //        if (i + 1 < QRCodeUtil.QRSize) watermark.Pixels[i] = watermark.Pixels[i + 1];
                    //        else watermark.Pixels[i] = blackcolor;
                    //    }
                    //    else if (loc == QRCodeUtil.QRSize - 1) //该行最后一个点
                    //    {
                    //        if (i > 0) watermark.Pixels[i] = watermark.Pixels[i - 1];
                    //        else watermark.Pixels[i] = blackcolor;
                    //    }
                    //    else watermark.Pixels[i] = blackcolor;
                    //}
                    else watermark.Pixels[i] = blackcolor;            //黑色认定多
                }
                return ArnoldClass.ReArnoldIMG( watermark );
            }
            catch (Exception) { return null; }
        }

        //水印插入
        public static WriteableBitmap MarkInsertS(WriteableBitmap bitmap, WriteableBitmap watermark)
        {
            try
            {
                if (bitmap == null || watermark == null) return null;
                int bitmapSize = bitmap.Pixels.Length;
                int bitmapHeight = bitmap.PixelHeight;
                int bitmapWidth = bitmap.PixelWidth;
                //获取归一后的灰度系数
                int[] markarray = ArnoldClass.GetGreyArrayAfterArnoldZeroOne(watermark);
                int markSize = markarray.Length;
                //将原图分解成 R,G,B 三个分量
                //double[] imageR = new double[bitmapSize];
                double[] imageG = new double[bitmapSize];
                //double[] imageB = new double[bitmapSize];
                int temp = 0;
                for (int i = 0; i < bitmapSize; ++i)
                {
                    //temp = bitmap.Pixels[i];
                    //imageR[i] = (temp >> 16) & 0xFF;
                    imageG[i] = (bitmap.Pixels[i] >> 8) & 0xFF;
                    //imageB[i] = temp & 0xFF;
                }
                //第一次小波变换
                DWT2 dwt = new DWT2(bitmap.PixelWidth, bitmap.PixelHeight);
                //dwt.wavelet2D(ref imageR, DWT2.DWTH, DWT2.DWTG, 1);
                dwt.wavelet2D(ref imageG, DWT2.DWTH, DWT2.DWTG, 1);
                //dwt.wavelet2D(ref imageB, DWT2.DWTH, DWT2.DWTG, 1);
                //第二次小波变换
                //dwt.wavelet2D(ref imageR, DWT2.DWTH, DWT2.DWTG, 1);
                dwt.wavelet2D(ref imageG, DWT2.DWTH, DWT2.DWTG, 1);
                //dwt.wavelet2D(ref imageB, DWT2.DWTH, DWT2.DWTG, 1);
                //取此时的LH2或HL2带（此时选了HL2，相当于中频）作为嵌入水印的部分
                int bitmapHeight4 = bitmapHeight / 4;
                int bitmapWidth4 = bitmapWidth / 4;
                int bitmapWidth2 = bitmapWidth / 2;
                int halfIMGSizeFrist = bitmap.Pixels.Length / 2;
                int fourIMGSizeFrist = bitmap.Pixels.Length / 4;
                //取系数最小值并将所有系数变为正数
                double minR_H = 0, minG_H = 0, minB_H = 0;
                for (int i = 0; i < bitmapSize; ++i)
                {
                    //if (imageR[i] < minR_H) minR_H = imageR[i];
                    if (imageG[i] < minG_H) minG_H = imageG[i];
                    //if (imageB[i] < minB_H) minB_H = imageB[i];
                }
                //系数为 1+abs（min）
                minR_H = 100 - minR_H; minG_H = 100 - minG_H; minB_H = 100 - minB_H; //取到最小值之后加一
                for (int i = 0; i < bitmapSize; ++i)
                {
                    //imageR[i] = minR_H + imageR[i];
                    imageG[i] = minG_H + imageG[i];
                    //imageB[i] = minB_H + imageB[i];
                }
                ////邻居矩阵
                temp = 0;// temp作为水印的一维向量的下标值  
                ///*
                // x0y0  x0y1  x0y2
                //       curX
                // x2y0  x2y1  x2y2
                // */
                for (int preline = bitmapHeight4 * bitmapWidth, curline = bitmapHeight4 * bitmapWidth + bitmapWidth, nextline = (bitmapHeight4 + 2) * bitmapWidth; nextline < halfIMGSizeFrist; )
                {
                    for (int j = 2; j < bitmapWidth4; j += 3)
                    {
                        //imageR[curline + j - 1] = getNewX(imageR[curline + j - 1], markarray[temp], imageR[preline + j - 2], imageR[preline + j - 1], imageR[preline + j], imageR[nextline + j - 2], imageR[nextline + j - 1], imageR[nextline + j]);
                        imageG[curline + j - 1] = getNewX(imageG[curline + j - 1], markarray[temp], imageG[preline + j - 2], imageG[preline + j - 1], imageG[preline + j], imageG[nextline + j - 2], imageG[nextline + j - 1], imageG[nextline + j]);
                        //imageB[curline + j - 1] = getNewX(imageB[curline + j - 1], markarray[temp], imageB[preline + j - 2], imageB[preline + j - 1], imageB[preline + j], imageB[nextline + j - 2], imageB[nextline + j - 1], imageB[nextline + j]);
                        ++temp;
                        if (temp >= markSize) break;
                    }
                    preline = nextline + bitmapWidth;
                    curline = preline + bitmapWidth;
                    nextline = curline + bitmapWidth;
                    if (temp >= markSize) break;
                }
                temp = 0;
                //for (int preline = 0, curline = bitmapWidth, nextline = bitmapWidth + bitmapWidth; nextline < fourIMGSizeFrist; )
                //{
                //    for (int j = 2 + bitmapWidth4; j < bitmapWidth2; j += 3)
                //    {
                //        //imageR[curline + j - 1] = getNewX(imageR[curline + j - 1], markarray[temp], imageR[preline + j - 2], imageR[preline + j - 1], imageR[preline + j], imageR[nextline + j - 2], imageR[nextline + j - 1], imageR[nextline + j]);
                //        imageG[curline + j - 1] = getNewX(imageG[curline + j - 1], markarray[temp], imageG[preline + j - 2], imageG[preline + j - 1], imageG[preline + j], imageG[nextline + j - 2], imageG[nextline + j - 1], imageG[nextline + j]);
                //        //imageB[curline + j - 1] = getNewX(imageB[curline + j - 1], markarray[temp], imageB[preline + j - 2], imageB[preline + j - 1], imageB[preline + j], imageB[nextline + j - 2], imageB[nextline + j - 1], imageB[nextline + j]);
                //        ++temp;
                //        if (temp >= markSize) break;
                //    }
                //    preline = nextline + bitmapWidth;
                //    curline = preline + bitmapWidth;
                //    nextline = curline + bitmapWidth;
                //    if (temp >= markSize) break;
                //}
                //减去为了变为正值所加的系数值 minR_H
                ////for (int preline = bitmapHeight / 4 * bitmapWidth, curline = bitmapHeight / 4 * bitmapWidth + bitmapWidth, nextline = (bitmapHeight / 4 + 2) * bitmapWidth; nextline < halfIMGSizeFrist; )
                ////{
                ////    for (int j = 2; j < bitmapWidth4; j += 3)
                ////    {
                ////        imageR[curline + j - 1] -= minR_H;
                ////        imageG[curline + j - 1] -= minG_H;
                ////        imageB[curline + j - 1] -= minB_H;
                ////        ++temp;
                ////        if (temp >= markSize) break;
                ////    }
                ////    preline = nextline + bitmapWidth;
                ////    curline = preline + bitmapWidth;
                ////    nextline = curline + bitmapWidth;
                ////    ++temp;
                ////    if (temp >= markSize) break;
                ////}
                for (int i = 0; i < bitmapSize; ++i)
                {
                    //imageR[i] = imageR[i] - minR_H;
                    imageG[i] = imageG[i] - minG_H;
                    //imageB[i] = imageB[i] - minB_H;
                }
                //对图像做两次逆小波运算，生成含水印的图片 和dwt的宽高相同故不变化
                //dwt.iwavelet2D(ref imageR, DWT2.DWTH, DWT2.DWTG, 1);
                dwt.iwavelet2D(ref imageG, DWT2.DWTH, DWT2.DWTG, 1);
                //dwt.iwavelet2D(ref imageB, DWT2.DWTH, DWT2.DWTG, 1);
                //dwt.iwavelet2D(ref imageR, DWT2.DWTH, DWT2.DWTG, 1);
                dwt.iwavelet2D(ref imageG, DWT2.DWTH, DWT2.DWTG, 1);
                //dwt.iwavelet2D(ref imageB, DWT2.DWTH, DWT2.DWTG, 1);
                //对逆小波运算后得到的三个分量做合并，生成要输出的 WriteableBitmap
                WriteableBitmap resultBitmap = new WriteableBitmap(bitmapWidth, bitmapHeight);
                // 使用green通道
                for (int i = 0, /*r, b, */ g; i < bitmapSize; ++i)
                {
                    //r = ((int)Math.Round(imageR[i], 1));
                    //if (r > 255) r = 255;
                    //else if (r < 0) r = 0;
                    g = ((int)Math.Round(imageG[i], 1));
                    if (g > 255) g = 255;
                    else if (g < 0) g = 0;
                    temp = bitmap.Pixels[i];
                    //b = (int)Math.Round(imageB[i], 1);
                    //if (b > 255) b = 255;
                    //else if (b < 0) b = 0;
                    resultBitmap.Pixels[i] = (0xFF << 24) | (((temp >> 16) & 0xFF) << 16) | (g << 8) | (temp & 0xFF);
                }
                return resultBitmap;
            }
            catch (Exception e) { Debug.WriteLine(e.Message); return null; }
        }

        //水印检测和提取
        public static WriteableBitmap GetMarkS(WriteableBitmap bitmap)
        {
            try
            {
                int markSize = QRCodeUtil.QRSize * QRCodeUtil.QRSize;
                // 检测图片不能存储足够的位置给水印，即它的 1/16 的二层DWT面积内不足以除以9获得存有水印的点
                //if (bitmap == null || bitmap.Pixels.Length / 16 < markSize) 
                //    return null;
                //获取归一后的灰度系数
                int[,] markarray = new int[6, QRCodeUtil.QRSize * QRCodeUtil.QRSize];
                WriteableBitmap watermark = new WriteableBitmap(QRCodeUtil.QRSize, QRCodeUtil.QRSize);
                int bitmapSize = bitmap.Pixels.Length;
                int bitmapHeight = bitmap.PixelHeight;
                int bitmapWidth = bitmap.PixelWidth;
                //将原图分解成 R,G,B 三个分量
                //double[] imageR = new double[bitmapSize];
                double[] imageG = new double[bitmapSize];
                //double[] imageB = new double[bitmapSize];
                for (int i = 0; i < bitmapSize; ++i)
                {
                    //temp = bitmap.Pixels[i];
                    //imageR[i] = (temp >> 16) & 0xFF;
                    imageG[i] = (bitmap.Pixels[i] >> 8) & 0xFF;
                    //imageB[i] = temp & 0xFF;
                }
                //第一次小波变换
                DWT2 dwt = new DWT2(bitmap.PixelWidth, bitmap.PixelHeight);
                //dwt.wavelet2D(ref imageR, DWT2.DWTH, DWT2.DWTG, 1);
                dwt.wavelet2D(ref imageG, DWT2.DWTH, DWT2.DWTG, 1);
                //dwt.wavelet2D(ref imageB, DWT2.DWTH, DWT2.DWTG, 1);
                //第二次小波变换
                //dwt.wavelet2D(ref imageR, DWT2.DWTH, DWT2.DWTG, 1);
                dwt.wavelet2D(ref imageG, DWT2.DWTH, DWT2.DWTG, 1);
                //dwt.wavelet2D(ref imageB, DWT2.DWTH, DWT2.DWTG, 1);
                //取此时的LH2或HL2带（此时选了HL2，相当于中频）作为嵌入水印的部分
                int bitmapHeight4 = bitmapHeight / 4;
                int bitmapWidth4 = bitmapWidth / 4;
                int bitmapWidth2 = bitmapWidth / 2;
                int halfIMGSizeFrist = bitmap.Pixels.Length / 2;
                int fourIMGSizeFrist = bitmap.Pixels.Length / 4;
                //取系数最小值并将所有系数变为正数
                //取系数最小值并将所有系数变为正数
                double minR_H = 0, minG_H = 0, minB_H = 0;
                for (int i = 0; i < bitmapSize; ++i)
                {
                    //if (imageR[i] < minR_H) minR_H = imageR[i];
                    if (imageG[i] < minG_H) minG_H = imageG[i];
                    //if (imageB[i] < minB_H) minB_H = imageB[i];
                }
                //系数为 1+abs（min）
                minR_H = 100 - minR_H; minG_H = 100 - minG_H; minB_H = 100 - minB_H; //取到最小值之后加一
                for (int i = 0; i < bitmapSize; ++i)
                {
                    //imageR[i] += minR_H;
                    imageG[i] += minG_H;
                    //imageB[i] += minB_H;
                }
                //黑白两色
                byte[] ffw = { 0xFF, 0xFF, 0xFF, 0xFF };
                int whitecolor = BitConverter.ToInt32(ffw, 0);
                byte[] ffb = { 0x00, 0x00, 0x00, 0xFF };
                int blackcolor = BitConverter.ToInt32(ffb, 0);
                //邻居矩阵,获取
                //temp = 0; //temp作为颜色分量的判定值，<=1时认为是黑色0，其余认为是白色1
                int temp0 = 0;// temp0作为水印的一维向量的下标值  
                /*
                 x0y0  x0y1  x0y2
                       curX
                 x2y0  x2y1  x2y2
                 */
                for (int preline = bitmapHeight / 4 * bitmapWidth, curline = bitmapHeight / 4 * bitmapWidth + bitmapWidth, nextline = (bitmapHeight / 4 + 2) * bitmapWidth; nextline < halfIMGSizeFrist; )
                {
                    for (int j = 2; j < bitmapWidth4; j += 3)
                    {
                        //markarray[0, temp0] = getWX(imageR[curline + j - 1], imageR[preline + j - 2], imageR[preline + j - 1], imageR[preline + j], imageR[nextline + j - 2], imageR[nextline + j - 1], imageR[nextline + j]);
                        markarray[1, temp0] = getWX(imageG[curline + j - 1], imageG[preline + j - 2], imageG[preline + j - 1], imageG[preline + j], imageG[nextline + j - 2], imageG[nextline + j - 1], imageG[nextline + j]);
                        //markarray[2, temp0] = getWX(imageB[curline + j - 1], imageB[preline + j - 2], imageB[preline + j - 1], imageB[preline + j], imageB[nextline + j - 2], imageB[nextline + j - 1], imageB[nextline + j]);
                        //if (markarray[0, temp0] == 1)
                        //{
                        //    if (markarray[1, temp0] == 1) watermark.Pixels[temp0] = whitecolor;
                        //    else
                        //    {
                        //        if (markarray[2, temp0] == 1) watermark.Pixels[temp0] = whitecolor;
                        //        else watermark.Pixels[temp0] = blackcolor;
                        //    }
                        //}
                        //else
                        //{
                        //    if (markarray[1, temp0] == 0) watermark.Pixels[temp0] = blackcolor;
                        //    else
                        //    {
                        //        if (markarray[2, temp0] == 0) watermark.Pixels[temp0] = blackcolor;
                        //        else watermark.Pixels[temp0] = whitecolor;
                        //    }
                        //}
                        ++temp0;
                        if (temp0 >= markSize) break;
                    }
                    preline = nextline + bitmapWidth;
                    curline = preline + bitmapWidth;
                    nextline = curline + bitmapWidth;
                    if (temp0 >= markSize) break;
                }
                //temp0 = 0;
                //for (int preline = 0, curline = bitmapWidth, nextline = bitmapWidth + bitmapWidth; nextline < fourIMGSizeFrist; )
                //{
                //    for (int j = 2 + bitmapWidth4; j < bitmapWidth2; j += 3)
                //    {
                //        //markarray[3, temp0] = getWX(imageR[curline + j - 1], imageR[preline + j - 2], imageR[preline + j - 1], imageR[preline + j], imageR[nextline + j - 2], imageR[nextline + j - 1], imageR[nextline + j]);
                //        markarray[4, temp0] = getWX(imageG[curline + j - 1], imageG[preline + j - 2], imageG[preline + j - 1], imageG[preline + j], imageG[nextline + j - 2], imageG[nextline + j - 1], imageG[nextline + j]);
                //        //markarray[5, temp0] = getWX(imageB[curline + j - 1], imageB[preline + j - 2], imageB[preline + j - 1], imageB[preline + j], imageB[nextline + j - 2], imageB[nextline + j - 1], imageB[nextline + j]);
                //        ++temp0;
                //        if (temp0 >= markSize) break;
                //    }
                //    preline = nextline + bitmapWidth;
                //    curline = preline + bitmapWidth;
                //    nextline = curline + bitmapWidth;
                //    if (temp0 >= markSize) break;
                //}
                //for (int i = 0, countw = 0; i < markSize; ++i)
                //{
                //    countw = 0;
                //    if (markarray[0, i] == 1) ++countw;
                //    if (markarray[1, i] == 1) ++countw;
                //    if (markarray[2, i] == 1) ++countw;
                //    if (markarray[3, i] == 1) ++countw;
                //    if (markarray[4, i] == 1) ++countw;
                //    if (markarray[5, i] == 1) ++countw;
                //    if (countw >= 3) watermark.Pixels[i] = whitecolor; //白色认定多
                //    //以下是分情况的另一种方法
                //    //if (countw >= 4) watermark.Pixels[i] = whitecolor; //白色认定多
                //    //else if (countw == 3) //更可能是白点
                //    //{
                //    //    int loc = i % QRCodeUtil.QRSize;
                //    //    if (loc == 0) //该行第一个点，和它后面的点近似
                //    //    {
                //    //        if (i + 1 < QRCodeUtil.QRSize) watermark.Pixels[i] = watermark.Pixels[i + 1];
                //    //        else watermark.Pixels[i] = whitecolor;
                //    //    }
                //    //    else if (loc == QRCodeUtil.QRSize - 1) //该行最后一个点
                //    //    {
                //    //        if (i > 0) watermark.Pixels[i] = watermark.Pixels[i - 1];
                //    //        else watermark.Pixels[i] = whitecolor;
                //    //    }
                //    //    else watermark.Pixels[i] = whitecolor;
                //    //}
                //    //else if (countw == 2) //更可能是黑点
                //    //{
                //    //    int loc = i % QRCodeUtil.QRSize;
                //    //    if (loc == 0) //该行第一个点，和它后面的点近似
                //    //    {
                //    //        if (i + 1 < QRCodeUtil.QRSize) watermark.Pixels[i] = watermark.Pixels[i + 1];
                //    //        else watermark.Pixels[i] = blackcolor;
                //    //    }
                //    //    else if (loc == QRCodeUtil.QRSize - 1) //该行最后一个点
                //    //    {
                //    //        if (i > 0) watermark.Pixels[i] = watermark.Pixels[i - 1];
                //    //        else watermark.Pixels[i] = blackcolor;
                //    //    }
                //    //    else watermark.Pixels[i] = blackcolor;
                //    //}
                //    else watermark.Pixels[i] = blackcolor;            //黑色认定多
                for(int i = 0 ; i < markSize; ++i){
                    if (markarray[1, i] == 1) watermark.Pixels[i] = whitecolor;
                    else watermark.Pixels[i] = blackcolor;
                }
                return ArnoldClass.ReArnoldIMG(watermark);
            }
            catch (Exception) { return null; }
        }

        //输入curx的值、二值水印点的值、curx的六个邻居的值，返回计算后的curx的值
        public static double getNewX(double curX, int markX, double x0y0, double x0y1, double x0y2, double x2y0, double x2y1, double x2y2)
        {
            double meanX = (x0y0 + x0y1 + x0y2 + x2y0 + x2y1 + x2y2) / 6; //六邻居得到的中心点的平均值，并乘以加权因子得到 m'(i,j)
            double meanXF = Factor * meanX;
            int keyX = (int)Math.Round(curX / meanXF);
            if (markX == 1) //水印值为1
            {
                if (keyX % 2 == 1)
                    return keyX * meanXF;
                else
                {
                    if (curX <= keyX * meanX)
                        return (keyX - 1) * meanXF;
                    else
                        return (keyX + 1) * meanXF;
                }
            }
            else //水印值为0
            {
                if (keyX % 2 == 0)
                    return keyX * meanXF;
                else
                {
                    if (curX <= keyX * meanX)
                        return (keyX - 1) * meanXF;
                    else
                        return (keyX + 1) * meanXF;
                }
            }
        }

        //
        public static int getWX(double curX, double x0y0, double x0y1, double x0y2, double x2y0, double x2y1, double x2y2)
        {
            double meanX = (x0y0 + x0y1 + x0y2 + x2y0 + x2y1 + x2y2) / 6; //六邻居得到的中心点的平均值，并乘以加权因子得到 m'(i,j)
            double meanXF = Factor * meanX;
            int keyX = (int)Math.Round(curX / meanXF);
            return (keyX % 2);
        }
    }
}
