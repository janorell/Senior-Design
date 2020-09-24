/*
 * Static Speech class for analyzing audio buffers.
 */


using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Numerics;
using System.Linq;

public static class Speech
{
    #region Utility
    //make a mono clip and then save to assets folder as a wav
    public static void saveArrayToWav(float[] array, string name, int frequency)
    {
       AudioClip clipToSave = AudioClip.Create(name, array.Length, 1, frequency, false);
       clipToSave.SetData(array, 0);
       SavWav.Save(name, clipToSave);
    }
    //trim an Envelope Object using rms envelope
    public static void trim(ref Envelope envelope, int chunkSize, int samplingRate, float threshold, float extra, bool end)
    {
        //get extra envelope samples
        int extraEnvelope = Mathf.CeilToInt(extra * (samplingRate / chunkSize));
        int cutoff = -1;
        //trim end
        if (end)
        {
            //find first index to hit threshold
            for (int i = envelope.envelope.Length - 1; i > -1; --i)
            {
                if (envelope.envelope[i] > threshold)
                {
                    cutoff = i;
                    break;
                }
            }
            if (cutoff == -1)
                Debug.LogError("Tried to trim a silent clip.");
            if (cutoff+extraEnvelope <envelope.envelope.Length)
            {
                //get trimmed envelope
                int length = cutoff + extraEnvelope+1;
                System.Array.Resize(ref envelope.envelope, length);
                //get trimmed samples
                length *= chunkSize;
                System.Array.Resize(ref envelope.samples, length);
            }
            return;
        }
        //trim beginnning
        else
        { 
            for (int i = 1; i < envelope.envelope.Length; ++i)
            {
                if (envelope.envelope[i] > threshold)
                {
                    cutoff = i;
                    break;
                }
            }
            if (cutoff == -1)
                Debug.LogError("Tried to trim a silent clip.");
            if (cutoff-extraEnvelope > 0)
            {
                //get trimmed envelope
                int length = cutoff - extraEnvelope;
                envelope.envelope = envelope.envelope.Skip(length).ToArray();
                //get trimmed samples
                length *= chunkSize;
                envelope.samples = envelope.samples.Skip(length).ToArray();
            }
            return;
        }
    }
    #endregion
    #region Conversions
    //convert between decibel and linear scales (power)
    public static float gainTodB(float gain)
    {
        return 10 * Mathf.Log10(gain);
    }

    //use for plotter fitting
    public static float dbLerp(float a, float b, float db, float nINF)
    {
        return Mathf.Lerp(a, b, db / nINF);
    }

    //Gets accuracy using minimum error. Accuracy normalized to a max in range [0.0f,1.0f].
    public static float getScore(float[] error, float max)
    {
        return Mathf.Clamp(1 - Mathf.Min(error), float.NegativeInfinity, max)/max*100.0f;
    }
    #endregion
    #region Metrics
    //get psudeo-expected value using a mass function (buffer). Function needs not have an area of 1.
    public static float getExpected (float[] buffer)
    {
        float res = 0.0f;
        for (int i = 0; i<buffer.Length; ++i)
        {
            res += i * buffer[i];
        }
        return res;
    }
    //normalize a buffer (for audio samples)
    public static void normalize(ref float[] buffer)
    {
        float max = Mathf.Max(buffer);
        float min = Mathf.Min(buffer);
        float scale = (Mathf.Abs(min) < max) ? 1.0f / max : 1.0f / Mathf.Abs(min);
        for (int i = 0; i < buffer.Length; ++i)
        {
            buffer[i] *= scale;
        }
    }
    //normalize positives only
    public static void normalizeMax(ref float[] buffer)
    {
        float scale = 1.0f / Mathf.Max(buffer);
        for (int i = 0; i < buffer.Length; ++i)
        {
            buffer[i] *= scale;
        }
    }
    //MinMax normalization for analytical signals (range = [0,1])
    public static void normalizeMinMax(ref float[] buffer)
    {
        float max = Mathf.Max(buffer);
        float min = Mathf.Min(buffer);
        float scale = 1 / (max - min);
        for (int i = 0; i < buffer.Length; ++i)
        {
            buffer[i] -= min;
            buffer[i] *= scale;
        }
    }
    //Differential Envelope Technology transient detector. Takes in an envelope and times in miliseconds of the fast and slow RC time constants.
    //https://spl.audio/wp-content/uploads/transient_designer_4_9842_tech_talk_E.pdf
    //Intended use: Get normalized transients for syllable detection.
    public static float[] DET(float[] envelope, float fastMS, float slowMS)
    {
        //error check
        if (Debug.isDebugBuild)
        {
            if (fastMS <= 0 || slowMS <= 0)
            {
                Debug.LogError("Error: One or more time constants in DET is <= 0.");
                return new float[1];
            }
            if (slowMS < fastMS)
            {
                Debug.LogError("Error: fastMS must be less than slowMS");
                return new float[1];
            }
        }
        //DET
        float fastCoeff = Mathf.Exp(-1000.0f / (fastMS * Mic.chunkFrequency)); //controls RC time constant of the single pole iir step response
        float slowCoeff = Mathf.Exp(-1000.0f / (slowMS * Mic.chunkFrequency)); //same as fastCoeff but noticablly larger
        float fastSample = 0, slowSample = 0; //calculate both smoothed envelopes sample-by-sample
        float[] res = new float[envelope.Length];
        for (int i = 0; i < envelope.Length; ++i)
        {
            fastSample = fastCoeff * fastSample + (1 - fastCoeff) * envelope[i];
            slowSample = slowCoeff * slowSample + (1 - slowCoeff) * envelope[i];
            res[i] = fastSample - slowSample;
        }
        //normalize
        normalizeMax(ref res);
        return res;
    }
    //get syllables from DET output
    //TODO threshold can be a multiple of average??
    public static int getSyllables(float[] DET, float threshold)
    {
        int res = 0;
        bool last = false;
        for (int i = 0; i <DET.Length; ++i)
        {
            if (DET[i] < threshold)
            {
                last = false;
            }
            else
            {
                if (!last) { ++res; }
                last = true;
            }
        }
        return res;
    }
    //error metric based on normalized minimum absolute error raised to power 'p' between the kernel and a kernel-sized chunk of the buffer
    public static float[] error(float[] kernel, float[] buffer, float p)
    {
        //normalize to error of zero input
        float max = 0.0f;
        for (int i = 0; i < kernel.Length; ++i)
        {
            max += Mathf.Pow(Mathf.Abs(kernel[i]),p);
        }
        //get sliding window error
        float[] res = new float[buffer.Length + 2 * kernel.Length - 2];
        System.Array.Copy(buffer, 0, res, kernel.Length - 1, buffer.Length);
        for (int i = 0; i < buffer.Length + kernel.Length - 1; ++i) //get error in place
        {
            for (int j = 0; j < kernel.Length; ++j)
            {
                res[i] += Mathf.Pow(Mathf.Abs(kernel[j] - res[i + j]),p);
            }
            res[i] /= max; //normalize
        }
        //output
        System.Array.Resize(ref res, buffer.Length + kernel.Length - 1); //trim trailing zeros used for in place calculation
        return res;
    }
    public static float[] errorNoRamp(float[] kernel, float[] buffer, float p, float overshoot)
    {
        //normalize to error of zero input
       /* float max = 0.0f;
        for (int i = 0; i < kernel.Length; ++i)
        {
            max += Mathf.Pow(Mathf.Abs(kernel[i]), p);
        }*/
        //normalize to error of dc input of 1
        float max = 0.0f;
        for (int i = 0; i < kernel.Length; ++i)
        {
            max += Mathf.Pow(Mathf.Abs(1-kernel[i]), p);
        }
        //get sliding window error
        float[] res = new float[buffer.Length - kernel.Length];
        for (int i = 0; i < buffer.Length - kernel.Length; ++i) //get error
        {
            for (int j = 0; j < kernel.Length; ++j)
            {
                if (buffer[i + j] > kernel[j])
                    res[i] += Mathf.Pow(buffer[i + j]-kernel[j], p) *overshoot;
                else
                    res[i] += Mathf.Pow(kernel[j] - buffer[i + j], p);
            }
            res[i] /= max; //normalize
        }
        //output
        return res;
    }
    public static float[] getDiffLowHigh(Spectrogram spectrogram)
    {
        float[] res = new float[2];
        for (int i = 0; i < spectrogram.high.Length/2; ++i)
        {
            res[1] -= spectrogram.high[i];
            res[0] -= spectrogram.low[i];
        }
        for (int i = spectrogram.high.Length/2;  i < spectrogram.high.Length; ++i)
        {
            res[1] += spectrogram.high[i];
            res[0] += spectrogram.low[i];
        }
        return res;
    }
    public static int getArgMax(float[] buffer)
    {
        float max = 0.0f;
        int res = -1;
        for (int i = 0; i < buffer.Length; ++i)
        {
            if (buffer[i] > max)
            {
                max = buffer[i];
                res = i;
            }
        }
        return res;
    }
    public static float getVar(float[] buffer)
    {
        float res = 0;
        float mean = buffer.Average();
        for (int i = 0; i< buffer.Length; ++i)
        {
            res += Mathf.Pow(buffer[i] - mean, 2.0f);
        }
        return res / buffer.Length;
    }
    //gets discrete derivative of a buffer (clamps negative values to 0)
    public static float[] getDerivative(float[] buffer)
    {
        float[] res = new float[buffer.Length - 1];
        for (int  i =1; i<buffer.Length; ++i)
        {
            res[i-1] = ((buffer[i] - buffer[i - 1]) > 0)? buffer[i]-buffer[i-1]: 0;
        }
        return res;
    }
    //modifies a derivative array so that small values are set as the last value with magnitude greater than the threshold
    public static void getThresholdedDerivative(ref float[] buffer, float threshold)
    {
        float prev = 0.0f;
        for (int i = 0; i < buffer.Length; ++i)
        {
            if (Mathf.Abs(buffer[i]) < threshold)
                buffer[i] = prev;
            else
                prev = buffer[i];
        }
    }
    #endregion
    #region Frequency Domain
    public static void applyHannWindow(ref float[] buffer)
    {
        for (int i = 0; i < buffer.Length; ++i)
        {
            buffer[i] *= 0.5f - (0.5f * Mathf.Cos((2.0f * Mathf.PI * i) / (buffer.Length - 1)));
        }
    }

    //returns normalized envelope of buffer using set chunkSize (multiple of 2) and threhold (between 0 and 1)
    public static Complex[] getComplexBuffer(float[] buffer)
    {
        Complex[] res = new Complex[buffer.Length];
        for (int i = 0; i < res.Length; ++i)
        {
            res[i] = buffer[i];
        }
        return res;
    }

    //FFT returns raw micSpectrum data
    public static Complex[] FFT(Complex[] buffer)
    {
        //even-odd sort
        int N = buffer.Length;
        if (N == 1) { return buffer; }
        int M = N / 2;
        Complex[] xEven = new Complex[M];
        Complex[] xOdd = new Complex[M];
        for (int i = 0; i < M; ++i)
        {
            xEven[i] = buffer[2 * i];
            xOdd[i] = buffer[2 * i + 1];
        }
        Complex[] fEven = new Complex[M];
        fEven = FFT(xEven);
        Complex[] fOdd = new Complex[M];
        fOdd = FFT(xOdd);
        //FFT
        Complex[] micSpectrum = new Complex[N];
        for (int i = 0; i < N / 2; ++i)
        {
            Complex cmplxOdd = Complex.FromPolarCoordinates(1, -2 * Mathf.PI * i / N) * fOdd[i];
            micSpectrum[i] = fEven[i] + cmplxOdd;
            micSpectrum[i + N / 2] = fEven[i] - cmplxOdd;
        }
        return micSpectrum;
    }

    //gets normalized Fourier coefficients
    public static float[] getFFTmag(Complex[] coeff) //PSD
    {
        int N = coeff.Length;
        int Ndiv2 = N / 2;
        float[] res = new float[Ndiv2 + 1];
        res[0] = Mathf.Pow((float)coeff[0].Magnitude, 2) / N;
        res[res.Length - 1] = Mathf.Pow((float)coeff[Ndiv2].Magnitude, 2) / N;
        for (int i = 1; i < res.Length - 1; ++i)
        {
            res[i] = Mathf.Pow((float)coeff[i].Magnitude, 2) / Ndiv2;
        }
        return res;
    }
    #endregion
    #region AV Playground
    public static float getRMSFromFT(int freq, int samplingRate, int fftLen, float[] oneBufferPower)
    {//calculate RMS in dB above the frequency freq;
        float s = 0;
        int indexFreq=1;
        if(freq>0) indexFreq=(freq*fftLen) / samplingRate;//convert freq to indexFreq in freq domain: freq=sampleRate/2 corresponds to spectrumAmpOut.Length;
        for (int i = indexFreq; i < Mic.magnLen; i++) {//Speech.FFT(Speech.FFT(Speech.getComplexBuffer(buffer))
            s += oneBufferPower[i];
        }
        return Mathf.Sqrt(s);
    }
    public static int getIndexOfPeakFreq(int freqLeft, int freqRight, int sampleRate, int fftLen, float[] oneBufferPower)
    {//AV freqLeft is absolute value of freq
        double maxAmpl=0; int indexMax=0;
        int indexFreqLeft=1, indexFreqRight=1;
        if(freqLeft>=0)  indexFreqLeft =(freqLeft* fftLen) / sampleRate;//convert freq to indexFreq in freq domain: freq=sampleRate/2 corresponds to spectrumAmpOut.Length;
        if(freqRight>=0) indexFreqRight=(freqRight*fftLen) / sampleRate;
        if(freqRight>Mic.magnLen) freqRight=Mic.magnLen;
        for (int i=indexFreqLeft; i<indexFreqRight; i++) {
            if(maxAmpl < oneBufferPower[i]){ maxAmpl=oneBufferPower[i]; indexMax=i;}
        }
        return( (indexMax*sampleRate) / fftLen );
    }
    public static float ArrayAvgFreq(int freqCenter, int deltaLeft, int deltaRight, int sampleRate, int fftLen, float[] oneBufferPower)
    {//deltaLeft is delta, relative to freqCenter.
        int ni=0, i=0;
        float result=0;
        int iFreqLeft=1, iFreqRight=1;
        iFreqLeft =((deltaLeft +freqCenter)*fftLen) / sampleRate;//convert freq to indexFreq in freq domain: freq=sampleRate/2 corresponds to spectrumAmpOut.Length;
        iFreqRight=((deltaRight+freqCenter)*fftLen) / sampleRate;

        for(i=iFreqLeft; i<=iFreqRight; i++)
        {
            if(i>=0 && i<Mic.magnLen)
            {//watch to avoid going beyond the array range
                result += oneBufferPower[i];
                ni++;
            }
        }
        if(ni<1) return 999.9f;
        else return(result/ni);//index 104 in the PSD corresponds to 809Hz
    }
    public static float getPeakToMeanRatio(int freqOfPeak, int maxDeltaFreq, int sampleRate, int fftLen, float[] oneBufferPower)
    {//
        float ratio=0, maxRatio=0, max=0, mean=0;
        for(int freq=10; freq<=maxDeltaFreq; freq+=5)
        {//scan for peak thickness that generates the highest ratio against background
            max =  ArrayAvgFreq(freqOfPeak, -freq, freq, sampleRate, fftLen, oneBufferPower);
            mean=( ArrayAvgFreq(freqOfPeak, -Mathf.RoundToInt(freq*2.4f), -Mathf.RoundToInt(freq*1.1f), sampleRate, fftLen, oneBufferPower) + 
                   ArrayAvgFreq(freqOfPeak,  Mathf.RoundToInt(freq*1.1f),  Mathf.RoundToInt(freq*2.4f), sampleRate, fftLen, oneBufferPower) )/2;//this must be away from harmonics
            if( mean!=0 ) {
                ratio = max / mean;
                if (ratio > (maxRatio * 1.2f)) {
                    maxRatio = ratio;
                }//by multiplying by 1.2 we favor narrow peaks over broad peaks.
            }
        }
        return maxRatio;
    }
    public static float getMeanPSD_dB(int freqLeft, int freqRight, int sampleRate, int fftLen, float[] oneBufferPower)
    {//AV freqLeft is absolute value of freq
        float meanAmp=0; int ni=0;
        int indexFreqLeft=1, indexFreqRight=1;
        if(freqLeft>=0)  indexFreqLeft =(freqLeft* fftLen) / sampleRate;//convert freq to indexFreq in freq domain: freq=sampleRate/2 corresponds to spectrumAmpOut.Length;
        if(freqRight>=0) indexFreqRight=(freqRight*fftLen) / sampleRate;
        if(freqRight>Mic.magnLen) freqRight=Mic.magnLen;
        for (int i=indexFreqLeft; i<indexFreqRight; i++) {
            meanAmp += oneBufferPower[i];
            ni++;
        }
        return( meanAmp/ni );
    }
    public static float getPeakAmpl_dB(int freqOfPeak, int sampleRate, int fftLen, float[] oneBufferPower) {
        int indexFreq=1;
        if(freqOfPeak>=0)  indexFreq =(freqOfPeak* fftLen) / sampleRate;//convert freq to indexFreq in freq domain: freq=sampleRate/2 corresponds to spectrumAmpOut.Length;
        float x1 = oneBufferPower[indexFreq-1];
        float x2 = oneBufferPower[indexFreq];
        float x3 = oneBufferPower[indexFreq+1];
        return (x1+x2+x3)/3;
    }
    #endregion
    #region Deprecated
   
    //use for threshold conversion (field) 
    public static float dBToGain(float db)
    {
        return Mathf.Pow(10, db / 20);
    }

    //cross correlate two float[], normalize to percent of max value from kernel autocorrelation, and set accuracy
    public static float[] crossCorrelate(float[] kernel, float[] buffer, ref float accuracy)
    {
        //autocorrelate kernel
        float max = 0;
        for (int i = 0; i < kernel.Length; ++i)
        {
            max += kernel[i] * kernel[i];
        }
        //get cross correlation while ignoring first kernel.Length - 1 terms, normalize to kernel autocorrelation, and set accuracy as maxRes
        float[] res = new float[buffer.Length + kernel.Length - 1];
        System.Array.Copy(buffer, 0, res, 0, buffer.Length);
        float maxRes = 0;
        for (int i = 0; i < buffer.Length; ++i) //cross correlate in place
        {
            for (int j = 0; j < kernel.Length; ++j)
            {
                res[i] += kernel[j] * res[i + j];
            }
            res[i] /= max; //normalize
            if (res[i] > maxRes) { maxRes = res[i]; } //update accuracy value
        }
        //output
        System.Array.Resize(ref res, buffer.Length); //trim trailing zeros used for in place calculation
        accuracy = maxRes; //set accuracy
        return res;
    }
        public static float[] squaredError(float[] kernel, float[] buffer, ref float minError)
    {
        //normalize to squared error of zero input
        float max = 0;
        for (int i = 0; i < kernel.Length; ++i)
        {
            max += kernel[i] * kernel[i];
        }
        //get running squared error
        float[] res = new float[buffer.Length + kernel.Length - 1];
        System.Array.Copy(buffer, 0, res, 0, buffer.Length);
        float minRes = 1.0f;
        for (int i = 0; i < buffer.Length; ++i) //cross correlate in place
        {
            for (int j = 0; j < kernel.Length; ++j)
            {
                res[i] += Mathf.Pow(kernel[j] - res[i + j], 2);
            }
            res[i] /= max; //normalize
            if (res[i] < minRes) { minRes = res[i]; }
        }
        //output
        System.Array.Resize(ref res, buffer.Length); //trim trailing zeros used for in place calculation
        minError = minRes;
        return res;
    }
       //first order HPF assuming 50Hz envelope sampling rate
    public static void highPass(ref float[] envelope, int cutoff)
    {
        //2Hz default fc
        float a = 0.775679511049613f, b = 0.887839755524807f;
        //apply coefficients for desired fc
        switch (cutoff)
        {
            case 1: //5Hz
                a = 0.509525449494429f;
                b = 0.754762724747214f;
                break;
            case 2: //10Hz
                a = 0.158384440324536f;
                b = 0.579192220162268f;
                break;
            case 3: //20Hz
                a = -0.509525449494429f;
                b = 0.245237275252786f;
                break;
            default: //2Hz
                break;
        }
        //first order butterworth
        envelope[0] *= b;
        for (int i = 1; i < envelope.Length; ++i)
        {
            envelope[i] = (a * envelope[i - 1]) + (b * (envelope[i] - envelope[i - 1]));
        }
    }
    //remove leading and trailing zeros from an envelope and its samples
    public static void trimZeros(ref float[] envelope, ref float[] samples, int chunkSize)
    {
        int start = -1;
        int end = -1;
        //get start
        for (int i = 0; i < envelope.Length; ++i)
        {
            if (envelope[i] != 0)
            {
                start = i;
                break;
            }
        }
        if (start != -1)
        {
            //get end
            for (int i = envelope.Length - 1; i > -1; --i)
            {
                if (envelope[i] != 0)
                {
                    end = i;
                    break;
                }
            }
            //get trimmed envelope
            int length = end - start + 1;
            float[] res = new float[length];
            System.Array.Copy(envelope, start, res, 0, length);
            envelope = res;
            //get trimmed samples
            end *= chunkSize;
            start *= chunkSize;
            end += chunkSize - 1;
            length = end - start + 1;
            res = new float[length];
            System.Array.Copy(samples, start, res, 0, length);
            samples = res;
            return;
        }
        return;
    }
    public static void subAvg(ref float[] envelope)
    {
        float avg = 0;
        for (int i = 0; i < envelope.Length; ++i)
        {
            avg += envelope[i];
        }
        avg /= envelope.Length;
        for (int i = 0; i < envelope.Length; ++i)
        {
            envelope[i] = envelope[i] - avg;
        }
    }
    #endregion
}

