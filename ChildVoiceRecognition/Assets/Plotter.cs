/*
 * Plotter class for displaying analysis and error between vocalizations and model word.
 * Intended use: Reading public outputs from Mic and ModifiedVideoPlayer classes.
 * NOTE: This class is for debugging only. There should be 0 references to the Plotter class.
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Plotter : MonoBehaviour 
{
    /*================Public=======================*/
    //DEPENDENCIES
    public ModifiedVideoPlayer videoPlayer;
    public Mic micro;
    public RectTransform canvasRT;
    public GameObject dbMarkers;
    public GameObject freqMarkers;
    //Parameters
    public bool envelope = true; //turn envelope, spectrogram, etc. on or off
    public bool spectrogram = false;
    public bool DET = false;
    public bool spectrum = false;
    [Range(-120.0f, -90.0f)]
    public float nINF = -120.0f; //negative INF value for spectrum
    /*===============Private=======================*/
    //fitScreen Variables
    private const float updateRate = 1.5f; //how often to call fitScreen
    private const int buffersPerSec = Mic.nRecordingHZ / Mic.bufferSize;
    private static float maxFreqLog = Mathf.Log10((Mic.nRecordingHZ / 2) + 1); //max log value for scaling
    private static Vector3[] corners; //screen corners
    private static float mMin, vMin, eMin; //lower yPosition bound for mic, video player, and error plots
    private static float vmMult; // multiplier that scales video and mic plots vertically
    private static float maxTime = 0.0f;
    //Scaled X axis
    private static float[] waveformPosX; //linear scale time
    private static float[] envelopePosX; //linear scale time
    private static float[] spectrogramPosX; //linear scale time
    private static float[] spectrumPosX; //frequency log scale
    /*===============Initialization================*/
    void Start()
    {
        //Initialize fitScreen Variables
        corners = new UnityEngine.Vector3[4];
        spectrumPosX = new float[Mic.magnLen];
        StartCoroutine(fitScreen());
    }
    /*===============fitScreen======================*/
    IEnumerator fitScreen()
    {
        while (true)
        {
            //update screen corners
            canvasRT.GetWorldCorners(corners);
            //update y bounds and scaling
            eMin = Mathf.Lerp(corners[0].y, corners[1].y, 2.0f / 3.0f);
            vMin = Mathf.Lerp(corners[0].y, corners[1].y, 1.0f / 3.0f);
            mMin = corners[0].y;
            vmMult = (vMin - mMin) / 2.0f;
            //plot 
            plotLineHoriz(eMin);
            plotLineHoriz(vMin);
            plotLineHoriz(mMin + vmMult);
            plotLineHoriz(vMin + vmMult);
            //fit time domain analysis
            //allocate new horizontal fit
            maxTime = micro.envelopeTime + videoPlayer.audioClipLength + 1;
            waveformPosX = getFitHoriz(Mic.nRecordingHZ / 10.0f, 0);
            envelopePosX = getFitHoriz((float)Mic.nRecordingHZ / micro.chunkSize, 1);
            spectrogramPosX = getFitHoriz((float)Mic.nRecordingHZ / Mic.bufferSize, 1);
            plotWaveform(micro.micEnvelope, 0);
            plotWaveform(videoPlayer.modelEnvelope, 1);
            if (envelope)
            {
                plotMetric(videoPlayer.modelEnvelope.envelope, envelopePosX, vMin + vmMult, vmMult, new Color(1.0f, 0.0f, 0.0f));
                plotMetric(micro.micEnvelope.envelope, envelopePosX, mMin + vmMult, vmMult, new Color(1.0f, 0.0f, 0.0f));
                plotMetric(micro.envelopeError, envelopePosX, eMin, 2 * vmMult, new Color(1.0f, 0.0f, 0.0f));
            }
            if (DET)
            {
                plotMetric(videoPlayer.modelDET, envelopePosX, vMin + vmMult, vmMult, new Color(1.0f, 0.0f, 1.0f));
                plotMetric(micro.micDET, envelopePosX, mMin + vmMult, vmMult, new Color(1.0f, 0.0f, 1.0f));
            }
            if (spectrogram) 
            {
                plotMetric(videoPlayer.modelSpectrogram.expected, spectrogramPosX, vMin + vmMult, vmMult, new Color(0.0f, 0.0f, 1.0f));
                plotMetric(micro.micSpectrogram.expected, spectrogramPosX, mMin + vmMult, vmMult, new Color(0.0f, 0.0f, 1.0f));
            }
            //fit spectrum
            if (spectrum)
            {
                for (int i = 0; i < spectrumPosX.Length; ++i)
                {
                    spectrumPosX[i] = Mathf.Lerp(corners[0].x, corners[3].x, Mathf.Log10((Mic.nRecordingHZ / 2 * i / (Mic.magnLen - 1)) + 1) / maxFreqLog);
                }
                //draw dB markers
                for (int i = 1; i < (-nINF + 0.01) / 12; ++i)
                {
                    float y = Mathf.Lerp(corners[1].y, corners[0].y, i * 12.0f / -nINF);
                    Transform marker = dbMarkers.transform.Find((-12 * i).ToString());
                    marker.position = new Vector3(marker.position.x, y + 0.15f, marker.position.z);
                    Debug.DrawLine(
                        new UnityEngine.Vector3(corners[0].x, y, corners[1].z),
                        new UnityEngine.Vector3(corners[3].x, y, corners[1].z),
                        Color.black,
                        updateRate
                    );
                }
                //draw freq markers
                for (int i = 0; i < 4; ++i)
                {
                    float powTen = Mathf.Pow(10.0f, i);
                    float powTenX = Mathf.Lerp(corners[0].x, corners[3].x, Mathf.Log10(powTen + 1.0f) / maxFreqLog);
                    Transform marker = freqMarkers.transform.Find(((int)powTen).ToString());
                    marker.position = new Vector3(powTenX, marker.position.y, marker.position.z);
                    Debug.DrawLine(
                            new UnityEngine.Vector3(powTenX, corners[0].y, corners[1].z),
                            new UnityEngine.Vector3(powTenX, corners[1].y, corners[1].z),
                            Color.red,
                            updateRate
                        );
                    for (int j = 2; j < 10; ++j)
                    {
                        float x = Mathf.Lerp(corners[0].x, corners[3].x, Mathf.Log10((powTen * j) + 1.0f) / maxFreqLog);
                        Debug.DrawLine(
                            new UnityEngine.Vector3(x, corners[0].y, corners[1].z),
                            new UnityEngine.Vector3(x, corners[1].y, corners[1].z),
                            Color.black,
                            updateRate
                        );
                    }
                }
                //draw marker for Nyquist freq
                Transform nyquistMarker = freqMarkers.transform.Find("Nyquist");
                nyquistMarker.position = new Vector3(corners[3].x, nyquistMarker.position.y, nyquistMarker.position.z);
                //show markers
                dbMarkers.SetActive(true);
                freqMarkers.SetActive(true);
            }
            else
            {
                freqMarkers.SetActive(false);
                dbMarkers.SetActive(false);
            }
            yield return new WaitForSeconds(updateRate);
        }
    }
    /*===============Per Frame Updates=============*/
    private void Update()
    {
        if (spectrum)
        {
            plotSpectrum();
        }
    }
    /*===============Helper Methods=================*/
    //Plots micro spectrum for a frame
    private void plotSpectrum()
    {
        for (int i = 1; i < spectrumPosX.Length; ++i)
        {
            Debug.DrawLine(
                     new UnityEngine.Vector3(spectrumPosX[i - 1],
                          Speech.dbLerp(corners[1].y, corners[0].y, micro.micMagnitude_DB[i - 1], nINF),
                         corners[1].z),
                     new UnityEngine.Vector3(spectrumPosX[i],
                          Speech.dbLerp(corners[1].y, corners[0].y, micro.micMagnitude_DB[i], nINF),
                         corners[1].z),
                     Color.magenta
             );
        }
    }

    //Use to plots videoPlayer (pos==1) and micro (pos==0) samples
    //Must be called with updated parameters every 'updateRate' seconds.
    private void plotWaveform(Envelope envelopeToPlot, int pos)
    {
        int length = Mathf.FloorToInt(((float)envelopeToPlot.envelope.Length / envelopePosX.Length) * waveformPosX.Length);
        int step = envelopeToPlot.samples.Length / length;
        float zero = (pos == 1) ? vMin+vmMult : mMin+vmMult;
        //plot waveform
        for (int i = 1, j = 0; i < length; ++i, j += step)
        {
            Debug.DrawLine(
                  new UnityEngine.Vector3(waveformPosX[i-1], zero + (vmMult * envelopeToPlot.samples[j]), corners[1].z),
                  new UnityEngine.Vector3(waveformPosX[i], zero + (vmMult * envelopeToPlot.samples[j + step]), corners[1].z),
                  Color.green,
                  updateRate
              );
        }
    }
    
    //Plots a 'metric' array to a 'fitX' array for horizontal scaling and 'posY' vertical offset. 
    //Elements are scaled vertically by 'mult' and colored with 'color'.
    //Must be called with updated parameters every 'updateRate' seconds.
    private void plotMetric(float[] metric, float[] fitX, float posY, float mult, Color color)
    {
        if (metric.Length <= fitX.Length) //make sure not to plot when 'fitX.Length' decreases 
        {
            for (int i = 1; i < metric.Length; ++i)
            {
                Debug.DrawLine(
                      new UnityEngine.Vector3(fitX[i - 1], posY + (mult * metric[i - 1]), corners[1].z),
                      new UnityEngine.Vector3(fitX[i], posY + (mult * metric[i]), corners[1].z),
                      color,
                      updateRate
                  );
            }
        }
    }
    
    //Plots a horizontal line at 'posY.'
    //Must be called with updated parameters every 'updateRate' seconds.
    private void plotLineHoriz(float posY)
    {
        Debug.DrawLine(
                  new UnityEngine.Vector3(corners[0].x, posY, corners[1].z),
                  new UnityEngine.Vector3(corners[3].x, posY, corners[1].z),
                  Color.black,
                  updateRate
              );
    }

    //Gets linear horizontal fit array given the 'samplesPerSec' of metric.
    //Offset shifts by one fit unit to the right.
    //Must be called every 'updateRate' seconds.
    private float[] getFitHoriz(float samplesPerSec, int offset)
    {
        float[] res = new float[Mathf.CeilToInt(samplesPerSec * maxTime)];
        for (int i = 0; i < res.Length; ++i)
        {
            res[i] = Mathf.Lerp(corners[0].x, corners[3].x, (float)(i + offset) / res.Length);
        }
        return res;
    }
}
