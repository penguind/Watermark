//DWT2.cs
using System;
using System.Collections.Generic;
using System.Text;

namespace WatermarkAlgrithm
{
    class DWT2
    {
        //低通滤波系数
        public static double[] DWTH = { 0.23037781330889, 0.71484657055291, 0.63088076792986, -0.02798376941686, -0.18703481171909, 0.03084138183556, 0.03288301166689, -0.01059740178507 };
        //高通滤波系数
        public static double[] DWTG = { 0.23037781330889, -0.71484657055291, 0.63088076792986, 0.02798376941686, -0.18703481171909, -0.03084138183556, 0.03288301166689, 0.01059740178507 };

        int iw, ih;

        //初始化要用于转换的图像的宽度和高度
        public DWT2(int w, int h)
        {
            iw = w;
            ih = h;
        }

        private void wavelet1D(double[] scl0, double[] p, double[] q,
                               out double[] scl1, out double[] wvl1)
        {
            int temp;
            int sclLen = scl0.Length;
            int pLen = p.Length;
            int half_sclLen = sclLen / 2;
            scl1 = new double[half_sclLen];
            wvl1 = new double[half_sclLen];

            for (int i = 0; i < half_sclLen; i++)
            {
                scl1[i] = 0.0;
                wvl1[i] = 0.0;
                for (int j = 0; j < pLen; j++)
                {
                    temp = (j + i * 2) % sclLen;
                    scl1[i] += p[j] * scl0[temp];
                    wvl1[i] += q[j] * scl0[temp];
                }
            }
        }

        private void iwavelet1D(out double[] scl0, double[] p, double[] q,
                                    double[] scl1, double[] wvl1)
        {
            int temp;
            int tempIndex = 0;
            int sclLen = scl1.Length;
            int pLen = p.Length;
            scl0 = new double[sclLen * 2];
            int half_pLen = pLen / 2;
            for (int i = 0; i < sclLen; i++)
            {
                scl0[2 * i + 1] = 0.0;
                scl0[2 * i] = 0.0;
                for (int j = 0; j < half_pLen; j++)
                {
                    temp = (i - j + sclLen) % sclLen;
                    tempIndex = 2 * j + 1;
                    scl0[2 * i + 1] += p[tempIndex] * scl1[temp] + q[tempIndex] * wvl1[temp];
                    scl0[2 * i] += p[tempIndex - 1] * scl1[temp] + q[tempIndex - 1] * wvl1[temp];
                }
            }
        }

        public void wavelet2D(ref double[] dataImage, double[] p, double[] q, int series)
        {
            int longE = (iw >= ih) ? iw : ih;
            int shortE = (iw < ih) ? iw : ih;
            int longES = longE / series;
            int shortES = shortE / series;
            int longES_half = longES / 2;
            int shortES_half = shortES / 2;
            //int arrsize = iw + ih;
            double[] s = new double[longES];
            double[] s1 = new double[longES_half];
            double[] w1 = new double[longES_half];
            int temp1;
            for (int i = 0; i < shortES; i++)
            {
                temp1 = i * longES;
                for (int j = 0; j < longES; j++)
                    s[j] = dataImage[temp1 + j];

                wavelet1D(s, p, q, out s1, out w1);

                for (int j = 0; j < longES; j++)
                {
                    if (j < longES_half)
                        dataImage[i * longES + j] = s1[j];
                    else
                        dataImage[i * longES + j] = w1[j - longES_half];
                }
            }

            for (int i = 0; i < longES; i++)
            {
                for (int j = 0; j < shortES; j++)
                    s[j] = dataImage[j * longES + i];

                wavelet1D(s, p, q, out s1, out w1);
                for (int j = 0; j < shortES; j++)
                {
                    if (j < shortES / 2)
                        dataImage[j * longES + i] = s1[j];
                    else
                        dataImage[j * longES + i] = w1[j - shortES_half];
                }
            }
        }

        public void iwavelet2D(ref double[] dataImage, double[] p,
                                    double[] q, int series)
        {
            int longE = (iw >= ih) ? iw : ih;
            int shortE = (iw < ih) ? iw : ih;
            int longES = longE / series;
            int shortES = shortE / series;
            int longES_half = longES / 2;
            int shortES_half = shortES / 2;
            //int arrsize = iw + ih;
            double[] s = new double[longES];
            double[] s1 = new double[longES_half];
            double[] w1 = new double[longES_half];
            int temp1;
            for (int i = 0; i < longES; i++)
            {
                for (int j = 0; j < shortES; j++)
                {
                    if (j < shortES_half)
                        s1[j] = dataImage[j * longES + i];
                    else
                        w1[j - shortES_half] = dataImage[j * longES + i];
                }
                iwavelet1D(out s, p, q, s1, w1);
                for (int j = 0; j < shortES; j++)
                    dataImage[j * longES + i] = s[j];
            }
            for (int i = 0; i < shortES; i++)
            {
                temp1 = i * longES;
                for (int j = 0; j < longES; j++)
                {
                    if (j < longES_half)
                        s1[j] = dataImage[temp1 + j];
                    else
                        w1[j - longES_half] = dataImage[temp1 + j];
                }
                iwavelet1D(out s, p, q, s1, w1);
                for (int j = 0; j < longES; j++)
                    dataImage[temp1 + j] = s[j];
            }
        }
    }
}
