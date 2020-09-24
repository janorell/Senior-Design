/*
 * Audio processing classes that implement the 'Iprocessor' interface.
 * Intended use: Processing and Storing metric history block by block.
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Interface for audio processing
interface Iprocessor
{
    //Set Private Variables using memory in samples and an optional context specific parameter
    void update(int memoryInSamples, int param1 = 0);
    //Process context specific data block
    void process(float[] data1);
    //Get Public Output Variables
    void get();
}

//Processes envelope and sample history
public class Envelope : Iprocessor
{
    /*=================Public Output Variables=================*/
    public float[] envelope = new float[1], samples = new float[1];
    /*=================Private Variables=======================*/
    //Parameters
    int chunkSize = 0;
    //Circular Buffers
    float[] cEnvelope = new float[1], cSamples = new float[1];
    //State Variables
    int envelopeWriteIdx = 0, samplesWriteIdx = 0, chunkIdx = 0;
    float sumSquares = 0.0f, peak = 0.0f;
    float[] currentChunk = new float[1];
    /*=================Iprocessor Implementation===============*/
    public void update(int memoryInSamples, int sizeOfChunks)
    {
        //Set Parameters
        chunkSize = sizeOfChunks;
        //Allocate chunk-aligned Circular Buffers to store Output
        cEnvelope = new float[memoryInSamples / chunkSize];
        cSamples = new float[memoryInSamples - (memoryInSamples % chunkSize)];
        //Allocate Output
        envelope = new float[cEnvelope.Length];
        samples = new float[cSamples.Length];
        //Allocate/reset State Variables
        currentChunk = new float[chunkSize];
        samplesWriteIdx = 0;
        envelopeWriteIdx = 0;
        chunkIdx = 0;
        sumSquares = 0.0f;
    }
    public void process(float[] buffer)
    {
        for (int i = 0; i < buffer.Length; ++i)
        {
            //process sample
            sumSquares += buffer[i] * buffer[i];
            currentChunk[chunkIdx] = buffer[i];
            //handle full chunks
            if (++chunkIdx == chunkSize)
            {
                //save corresponding samples
                System.Array.Copy(currentChunk, 0, cSamples, samplesWriteIdx, chunkSize);
                //go to next chunk
                samplesWriteIdx += chunkSize;
                //save envelope
                cEnvelope[envelopeWriteIdx] = Mathf.Sqrt(sumSquares / chunkSize); // get RMS sample
                //go to next envelope sample
                if (++envelopeWriteIdx == cEnvelope.Length)
                {
                    //circular buffer wrap around
                    envelopeWriteIdx = 0;
                    samplesWriteIdx = 0;
                }
                //reset chunk state
                sumSquares = 0;
                peak = 0;
                chunkIdx = 0;
            }
        }
    }
    public void get()
    {
        //Get Output
        System.Array.Copy(cEnvelope, envelopeWriteIdx, envelope, 0, cEnvelope.Length - envelopeWriteIdx);
        System.Array.Copy(cEnvelope, 0, envelope, cEnvelope.Length - envelopeWriteIdx, envelopeWriteIdx);
        System.Array.Copy(cSamples, samplesWriteIdx, samples, 0, cSamples.Length - samplesWriteIdx);
        System.Array.Copy(cSamples, 0, samples, cSamples.Length - samplesWriteIdx, samplesWriteIdx);
        //Normalize Output
        Speech.normalizeMinMax(ref envelope);
        Speech.normalize(ref samples);
    }
}

//Processes pseudo-expected PSD values and energy in high/low halves
public class Spectrogram : Iprocessor
{
    /*=================Public Output Variables=================*/
    public float[] high = new float[1], low = new float[1], expected = new float[1];
    /*=================Private Variables=======================*/
    //Circular Buffers
    private float[] cHigh = new float[1], cLow = new float[1], cExpected = new float[1];
    //State Variables
    int spectrogramIdx = 0; //current index in all circuluar buffers
    /*=================Iprocessor Implementation===============*/
    public void update(int memoryInSamples, int bufferSize)
    {
        //Allocate buffer-aligned Circular Buffers to store Output
        cHigh = new float[memoryInSamples / bufferSize];
        cLow = new float[cHigh.Length];
        cExpected = new float[cHigh.Length];
        //Allocate Output
        high = new float[cHigh.Length];
        low = new float[cLow.Length];
        expected = new float[cExpected.Length];
        //Reset State Variables
        spectrogramIdx = 0;
    }
    public void process(float[] PSD)
    {
        for (int i = 0; i < PSD.Length/2; ++i)
        {
            cHigh[spectrogramIdx] += PSD[i + PSD.Length/2]; 
            cLow[spectrogramIdx] += PSD[i];
        }
        cExpected[spectrogramIdx] = Speech.getExpected(PSD);
        if (++spectrogramIdx == cHigh.Length)
            spectrogramIdx = 0;
    }
    public void get()
    {
        //Get Output
        System.Array.Copy(cHigh, spectrogramIdx, high, 0, cHigh.Length - spectrogramIdx);
        System.Array.Copy(cHigh, 0, high, cHigh.Length - spectrogramIdx, spectrogramIdx);
        System.Array.Copy(cLow, spectrogramIdx, low, 0, cLow.Length - spectrogramIdx);
        System.Array.Copy(cLow, 0, low, cLow.Length - spectrogramIdx, spectrogramIdx);
        System.Array.Copy(cExpected, spectrogramIdx, expected, 0, cExpected.Length - spectrogramIdx);
        System.Array.Copy(cExpected, 0, expected, cExpected.Length - spectrogramIdx, spectrogramIdx);
        //Normalize Output
        Speech.normalizeMinMax(ref expected);
    }
}