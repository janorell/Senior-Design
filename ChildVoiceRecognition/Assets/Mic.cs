using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Numerics;

//microphone class that fills constant-sized buffers and then gets FFT/normalize micEnvelope
public class Mic : MonoBehaviour
{
    //=================public/serializable vars=================
    //Dependencies
    public ModifiedVideoPlayer modifiedVideoPlayer;    
    //parameters
    public float startCapture = 0.0f; //how many secs after end of model word to start detecting voice, can be a negative value
    public float pValue = 0.5f; //p value to use for error metric
    public float overshootPenalty = 1.2f; //how much to multiply error by for samples that overshoot model word
    public float freqDiffPenalty = 3.0f;
    public float lengthNorm = 0.2f;
    public float fastDET = 5.0f;
    public float slowDET = 50.0f;
    public float envelopeTimeBefore = 0.5f; //time to record before detection
    public float envelopeTimeAfter = 0; //time to keep recording after recording for length of model word after detection
    //processing context
    public const float chunkFrequency = 50.0f; //frequency at which chunks are sampled
    public const int bufferSize = 2048; //FFT buffer size. MUST be multiple of 2 for FFT
    public const int nRecordingHZ = 16000;//at Sampling frequesncy 16000: bufferSize=8192=500ms; bufferSize=4096=250ms; bufferSize=2048=100ms;
    public const int magnLen = bufferSize / 2 + 1;
    //output data
    public float micLevel; //peak micLevel
    public Complex[] micSpectrum; //use indicies 0 to N/2 for real FFT coefficients
    public float[] micMagnitude, micMagnitude_DB;
    public Envelope micEnvelope;
    public Spectrogram micSpectrogram;
    public float[] micDET = new float[1];
    public float[] envelopeError = new float[1];
    public float trimExtraDETandFreq = 0.2f;
    public float[] buffer; //current audio buffer
    public float[] modelEnvelope; // current model word Envelope Output
    public float[] modelSpectrogram; //current model word Spectrogram Output
    public float modelWordTrimEnvelopeTime;
    public float modelWordTrimOtherTime;
    public float modelFrequencyScore;
    //dependent variables (do not set)
    public int chunkSize = 2048;//Anything with the word "chunk" in it is a parameter for the moving RMS in the envelope detector
    public float envelopeTime = 1; //set by modifiedVideoPlayer
    public bool blockCorrelation = true; //disables cross correlation if already processing and used for locking b/w threads
    //=================private vars=================
    //recording
    private bool nRecordingRoutine = false;
    private string sMicrophoneID = null;
    private AudioClip acRecording = null;
    //buffer data
    private int lastSample = 0;
    //time to wait after detection to cross correlate
    private float envelopeTimeAfterDetection = 5;
    /*****************************************************************************************/
    /**************AV Definitions Start*******************************************************/
    public const float SENSITIVITY2PEAKS = 8f;//range 1f to 10f: reduce to cut down number of erroneous word detections
    private const int THRESHOLD_DB = 10;//use 20 when noise level is fixed
    private const int WORDDURATION_AFTERDETECTION = 1100;//milliseconds. We will keep recording sound for this duration after the fist vocalization is detected. Please keep to the minimum to reduce delay in reward.
    private const int WORDDURATION_BEFOREDETECTION = 600;//milliseconds
    //private static final int WORDDURATION = WORDDURATION_BEFOREDETECTION + WORDDURATION_AFTERDETECTION;//milliseconds

    private int nBuffers=0;
    private const int THRESHOLDBUFFERDURATION = 2000;// 1000millisecons = 1sec
    public static string classIndicator="";//this is used to indicate each chunk's classification for the presence of vocalization
    //private final boolean bReportAnalysis=true;

    private const bool bRecordChildOnly=true;//based on the value of peak3Ration over three buffers
    private const int THRESHOLD_PEAK3_RATIO_DB=50;//for high frequency children vocalization detection

    //create a buffer for data for the last 1.5 sec
    private const int DATACHUNKS_TOSAVE_AFTERDETECTION  = ((WORDDURATION_AFTERDETECTION *nRecordingHZ) / bufferSize)/1000;
    private const int DATACHUNKS_TOSAVE_BEFOREDETECTION = ((WORDDURATION_BEFOREDETECTION*nRecordingHZ) / bufferSize)/1000;
    private const int DATACHUNKS_TOSAVE_TOTAL=DATACHUNKS_TOSAVE_BEFOREDETECTION + DATACHUNKS_TOSAVE_AFTERDETECTION;//shall be 23 at the sampling frequency @16000 samples per second that we use
    //Log.i(TAG, "DATACHUNKS_TOSAVE_TOTAL=" + DATACHUNKS_TOSAVE_TOTAL);
    short[,]  tempBuffers = new short[DATACHUNKS_TOSAVE_TOTAL,bufferSize];//last 15 buffers (1.5sec) will be recorded here. When there was a vocalization, I will dump this buffers into a file
    int tempBuffersIndex = 0;
    int nDataChunksToWait = 0;

    //create a buffer for threshold for the last 1 sec
    private const int NUMBUFFHIST=((THRESHOLDBUFFERDURATION*nRecordingHZ) / bufferSize)/1000;//every buffer is approximately 100ms
    private double[] rmsHistory =  new double[NUMBUFFHIST]; //remember means of 9 previous buffers. This is my amplitude threshold    
    private float[]peak3RatioHistory = new float[NUMBUFFHIST];//remember ratios of 9 previous buffers. This is my amplitude threshold
    /**************AV Definitions End*******************************************************/
    /*****************************************************************************************/
    private void Awake() 
    {
        //wait for end of video to enable voice detection
        blockCorrelation = true;
        //initialize buffer data
        buffer = new float[bufferSize];
        //initialize output
        micEnvelope = new Envelope();
        micSpectrogram = new Spectrogram();
        chunkSize = Mathf.RoundToInt(nRecordingHZ / chunkFrequency);
        micEnvelope.update((int)(nRecordingHZ * envelopeTime), chunkSize);
        micSpectrogram.update((int)(nRecordingHZ * envelopeTime), bufferSize);
        micSpectrum = new Complex[bufferSize];
        micMagnitude= new float[magnLen];
        micMagnitude_DB= new float[magnLen];
        /*****************************************************************************************/
        /**************AV Definitions Start*******************************************************/
        for(int i=0; i<NUMBUFFHIST; i++) rmsHistory[i]=0;
        Debug.Log("NUMBUFFHIST=" + NUMBUFFHIST); if(NUMBUFFHIST<3) Debug.Log("ERROR!!!  NUMBUFFHIST<3");
        for(int i=0; i<NUMBUFFHIST; i++) peak3RatioHistory[i]=0;//remember ratios of 9 previous buffers. This is my amplitude threshold
        /**************AV Definitions End*******************************************************/
        /*****************************************************************************************/
    }
    //start recording audio buffers 
    public void StartRecording()
    {
        if (!nRecordingRoutine)
        {
            nRecordingRoutine = true;
            StartCoroutine(RecordingHandler());
        }
    }
    //process buffers in coroutine
    private IEnumerator RecordingHandler()
    {
        //record nRecordingHz samples to a circular buffer every second
        acRecording = Microphone.Start(sMicrophoneID, true, 1, nRecordingHZ);
        //wait for mic to start
        while (!(Microphone.GetPosition(null) > 0))
        {
            yield return null;
        }
        //error check
        if (acRecording == null)
        { //null pointer protection
            Debug.LogError("acRecording null pointer error.");
            StopRecording();
            yield break;
        }
        //retrieve buffer
        while (true)
        {
            //error check
            int pos = Microphone.GetPosition(sMicrophoneID);
            if (pos > acRecording.samples || !Microphone.IsRecording(sMicrophoneID))
            {
                Debug.Log("MicrophoneWidget Microphone disconnected.");
                StopRecording();
                yield break;
            }
            //get buffer if full
            int diff = (nRecordingHZ + pos - lastSample) % nRecordingHZ; //get sample diff in circular buffer
            if (diff > bufferSize) //only do if buffer is filled
            {
                //get buffer data
                acRecording.GetData(buffer, lastSample);
                //=========================================envelope====================================
                //load buffer RMS values into micEnvelope and store peak value in "max"
                micEnvelope.process(buffer);
                //set micLevel
                micLevel = Mathf.Max(buffer);
                //=========================================spectrum====================================
                //apply hann window
                Speech.applyHannWindow(ref buffer);
                //get buffer micSpectrum data
                micSpectrum = Speech.FFT(Speech.getComplexBuffer(buffer));
                micMagnitude = Speech.getFFTmag(micSpectrum);
                for (int i = 0; i < magnLen; i++) { micMagnitude_DB[i] = Speech.gainTodB(micMagnitude[i]); }
                /****************************************************************************************/
                /**************AV Playground Start*******************************************************/
                nBuffers++; if(nBuffers==int.MinValue) nBuffers=NUMBUFFHIST;

                //1. remember the RMS of last 8 buffers in order to generate a threshold. The current buffer is rmsHistory[0]; the oldest buffer is rmsHistory[NUMBUFFHIST-1]
                string s="";
                for (int i=(NUMBUFFHIST-1); i>0; i--){ rmsHistory[i]=rmsHistory[i-1]; s+=rmsHistory[i].ToString("F2")+",";}//remember the RMS of last 8 buffers
                rmsHistory[0] = Speech.getRMSFromFT(300, nRecordingHZ, bufferSize, micMagnitude);//the current buffer Math.round(mean);
                s=rmsHistory[0].ToString("F2")+","+s;
                
                //2. determine the threshold for this buffer
                double ampThreshold=0;
                for(int i=0; i<NUMBUFFHIST-1; i++) ampThreshold+=rmsHistory[i];//do not include the current buffer
                ampThreshold/=(NUMBUFFHIST-1);//classIndicator = "ampThreshold=" + String.valueOf(ampThreshold) + "; activity.dtRMSFromFT=" + String.valueOf(activity.dtRMSFromFT);
                //Debug.Log("ampThreshold="+ampThreshold + "; "+s);

                //3. Identify and describe harmonics in freq domain
                //ToDo: consider more harmonics and dynamically identified regions: so the 1st peak is identified then the 2nd peak to the right, then the peak to the left. That will entail identifying local minima.
                int freqOfPeak1=Speech.getIndexOfPeakFreq(300, 800, nRecordingHZ, bufferSize, micMagnitude);
                int freqOfPeak2=Speech.getIndexOfPeakFreq(800, 1800, nRecordingHZ, bufferSize, micMagnitude);
                int freqOfPeak3=Speech.getIndexOfPeakFreq(1800,3500, nRecordingHZ, bufferSize, micMagnitude);
                float mean = Speech.getMeanPSD_dB(300,3500, nRecordingHZ, bufferSize, micMagnitude_DB);
                float max1 = Speech.getPeakAmpl_dB(freqOfPeak1, nRecordingHZ, bufferSize, micMagnitude_DB);//calculated by averaging three points: the peak, one point to the right and one to the left.
                float max2 = Speech.getPeakAmpl_dB(freqOfPeak2, nRecordingHZ, bufferSize, micMagnitude_DB);//calculated by averaging three points: the peak, one point to the right and one to the left.
                float max3 = Speech.getPeakAmpl_dB(freqOfPeak3, nRecordingHZ, bufferSize, micMagnitude_DB);//calculated by averaging three points: the peak, one point to the right and one to the left.
                float ratio1 = Speech.getPeakToMeanRatio(freqOfPeak1, 120, nRecordingHZ, bufferSize, micMagnitude);//1. The ratio tells us how high the harmonic stands above background LOCALLY.
                float ratio2 = Speech.getPeakToMeanRatio(freqOfPeak2, 500, nRecordingHZ, bufferSize, micMagnitude);
                float ratio3 = Speech.getPeakToMeanRatio(freqOfPeak3, 500, nRecordingHZ, bufferSize, micMagnitude);
                
                //4. i'd like to make sure that each harmonic is not some kind of not-audible digital artifact. For that I am using absolute values in dB.
                bool bP1, bP2, bP3; bP1=bP2=bP3=false;//markers for the three peaks standing above the background;
                double minPeakPower=mean+THRESHOLD_DB;//min peak power in dB
                if(max1>minPeakPower) bP1=true;
                if(max2>minPeakPower) bP2=true;
                if(max3>minPeakPower) bP3=true;
                //Debug.Log("minPeakPower="+minPeakPower.ToString("F0")+"; max1="+max1.ToString("F0")+"; max2="+max2.ToString("F0")+"; max3="+max3.ToString("F0")+"; ampThreshold="+ampThreshold.ToString("F1") + "; s="+s);
                
                //5. Weighted ratio: use only harmonics with minimum power. We really need to remove meaningless ratios that do not represent any sound, these are basically just artifacts
                float weightedRatio=-1;
                if		(bP1 && bP2 && bP3) weightedRatio=(ratio1+ratio2+ratio3)/3;
                else if (bP2 && bP3) weightedRatio=(ratio2+ratio3)/2;
                else if (bP1 && bP3) weightedRatio=(ratio1+ratio3)/2;
                else if (bP1 && bP2) weightedRatio=(ratio1+ratio2)/2;
                //else if (bP3) weightedRatio=ratio3;//I do not want a lonely P3 reak. I have never seen voice in P3 without harmonics in P1 and P2
                else if (bP2) weightedRatio=ratio2;
                else if (bP1) weightedRatio=ratio1;
                else weightedRatio=-1;//- none of the peaks stand above background.
                
                //5B. Store the history of the 3d peak ratio
                for(int i=(NUMBUFFHIST-1); i>0; i--){ peak3RatioHistory[i]=peak3RatioHistory[i-1];}//remember the ratio3 of last 8 buffers
                peak3RatioHistory[0] = ratio3;//
                float peak3Ratio3Chunks=(peak3RatioHistory[0]+peak3RatioHistory[1]+peak3RatioHistory[2])/3;
                
                //6. Assign the classifier for this buffer based on weightedRatio
                string bC="";//string to hold the classifier
                if(weightedRatio==-1) bC="_";//'_' - none of the peaks stand above background.
                //else if(!bP2 && !bP3 && freqOfPeak1<295) bC="-";//frequency of the only peak is too low for human voice
                else if (weightedRatio>(100/SENSITIVITY2PEAKS)) bC="v";//vocalization was detected
                else if (weightedRatio>(90/SENSITIVITY2PEAKS) ) bC="c";//'c' for candidate
                else if (weightedRatio>(60/SENSITIVITY2PEAKS) ) bC="a";//'e' for candidate who almost did it., i.e. 70% of min_ratio.
                else    bC="~"; //~ there were peaks detected, but the ratio was too small to become a candidate

                //7. if the buffer contains loud noise that crosses threshold AND has some reasonable harmonics=>mark as 't'=buffer that crossed threshold.
                if( nBuffers>=NUMBUFFHIST )
                {
                    if(      bC=="v" && rmsHistory[0]>(2*ampThreshold) ) bC="t";//Threshold crossing //classIndicator="Crossed Threshold"; else classIndicator="Not crossed threshold";
                    else if( bC=="c" && rmsHistory[0]>(6*ampThreshold) ) bC="t";//Threshold crossing //classIndicator="Crossed Threshold"; else classIndicator="Not crossed threshold";
                    else if( bC=="a" && rmsHistory[0]>(8*ampThreshold) ) bC="t";//Threshold crossing //classIndicator="Crossed Threshold"; else classIndicator="Not crossed threshold";
                }
                
                //8. Find the beginning of the vocalization as the fist buffer that crossed the threshold - this approach is NOT USED. We simply always grab 600ms before the data chunk with vocalization
                bool bReportAnalysis=false;
                if( bC=="t")
                {
                    if (!blockCorrelation) //cross correlate if video is done playing
                    {
                        blockCorrelation = true; //block until next video
                        modelSpectrogram = modifiedVideoPlayer.modelSpectrogram.expected;
                        Invoke("evaluateVocalization", envelopeTimeAfterDetection); //trigger envelope processing after about 1 sec
                    }
                    bReportAnalysis =true;//report all info only for buffers with vocalization
                    /*  if( !bRecordChildOnly || peak3Ratio3Chunks>THRESHOLD_PEAK3_RATIO_DB )
                      {//this buffer is definitely part of a vocalization => wait for DATACHUNKS_TOSAVE_AFTERDETECTION buffers (1sec), then dump the tempAudioSample into a file
                          if(nDataChunksToWait==0){ nDataChunksToWait = DATACHUNKS_TOSAVE_AFTERDETECTION; Debug.Log("I have detected a word. I will wait for 1sec (DATACHUNKS_TOSAVE_AFTERDETECTION) and save a wav file");}//this is the marker for vocalization. It is also a counter of buffers to wait
                          else{ Debug.Log("I have detected a vocalization in this data chunk, but I am saving this data chunk as part of a previous word and therefore I will NOT restart buffering.");}
                      }*/

                }

                //9. Report the classification of the current buffer //Consider reporting peak thickness
                if(classIndicator.Length>200) classIndicator = classIndicator.Substring(0, classIndicator.Length - 1);
                classIndicator = bC + classIndicator;//inserted in front of the string
                if(bReportAnalysis) {Debug.Log(classIndicator);}
                if(bReportAnalysis) {//report all info only for buffers with vocalization
                    Debug.Log(bC + "; weightedRatio=" + Mathf.RoundToInt(weightedRatio) +
                            " |  f1=" + freqOfPeak1 + ", m1=" + Mathf.RoundToInt(max1) + "dB, r1=" + Mathf.RoundToInt(ratio1) +
                            " |  f2=" + freqOfPeak2 + ", m2=" + Mathf.RoundToInt(max2) + "dB, r2=" + Mathf.RoundToInt(ratio2) +
                            " |  f3=" + freqOfPeak3 + ", m3=" + Mathf.RoundToInt(max3) + "dB, r3=" + Mathf.RoundToInt(ratio3) +
                            " |  r3-1b=" + Mathf.RoundToInt(peak3RatioHistory[1]) + ", r3-2b=" + Mathf.RoundToInt(peak3RatioHistory[2]) + ", r3-3b=" + Mathf.RoundToInt(peak3RatioHistory[3])+
                            " |  peak3Ratio3Chunks=" + Mathf.RoundToInt(peak3Ratio3Chunks) + "  |  mean=" + Mathf.RoundToInt(mean) + "dB"
                    );
                }
                //EXPECTED RESULTS: /measured
                //ratios ~ 30 / 3
                //mean=-100dB / -40dB = difference=60dB
                //peak=-30dB / -10dB  = difference=30dB

                //10. This is vocalization => Save the last 1.5 sec into a file
                //See above: if(nDataChunksToWait>0)

                /**************AV Playground Ends Here*******************************************************/
                /********************************************************************************************/
                //update next buffer
                if ((lastSample += bufferSize) >= nRecordingHZ)
                {
                    lastSample %=nRecordingHZ;
                }
            }
            yield return null;
        }  
    }
    //only stop buffer capture on errors
    private void StopRecording()
    {
        if (nRecordingRoutine)
        {
            Microphone.End(sMicrophoneID);
            lastSample = 0;
            nRecordingRoutine = false;
        }
    }
    //Call when vocalization detected. Calculates score and save the last 'envelopeTime' seconds to a wav.
    private void evaluateVocalization()
    {
        //micEnvelope
        micEnvelope.get();
        //TempMicData_NoTrim
        Speech.saveArrayToWav(micEnvelope.samples, "TempMicData_NoTrim", nRecordingHZ);//save to wav, this is used for Envelope Matching
        Debug.Log(" Mic: Length of TempMicData_NoTrim: " + (float)micEnvelope.samples.Length / nRecordingHZ + " seconds");
        //Envelope score
        envelopeError = Speech.errorNoRamp(modelEnvelope, micEnvelope.envelope, pValue, overshootPenalty);
        //Factor in ModelWord_Trim_Envelope time (normalize)
        float score = Speech.getScore(envelopeError, 1.0f);
        if (modelWordTrimEnvelopeTime > 1.5f)
            score *= (modelWordTrimEnvelopeTime-1.5f) * lengthNorm + 1.0f;
        //TempMicData_Trim_Other
        Speech.trim(ref micEnvelope, chunkSize, nRecordingHZ, modifiedVideoPlayer.otherTrimThreshold, modifiedVideoPlayer.otherTrimExtra, true);
        Speech.trim(ref micEnvelope, chunkSize, nRecordingHZ, modifiedVideoPlayer.otherTrimThreshold, modifiedVideoPlayer.otherTrimExtra,  false);
        Speech.saveArrayToWav(micEnvelope.samples, "TempMicData_Trim_Other", nRecordingHZ);
        //Factor in input time
        float timeDiff = (float)micEnvelope.samples.Length / nRecordingHZ /  modelWordTrimOtherTime;
        Debug.Log("Mic: time diff " + timeDiff);
        if (timeDiff < 1.0f)
            score *= timeDiff;
        Debug.Log("Mic: Envelope accuracy score using p value of " + pValue + ": " + score + "%");
        //micSpectrogram
        float[] micBuffer = new float[bufferSize];
        int bufferAlignedSize = micEnvelope.samples.Length - (micEnvelope.samples.Length % micBuffer.Length);
        micSpectrogram.update(bufferAlignedSize, micBuffer.Length);
        for (int i = 0; i < bufferAlignedSize; i += micBuffer.Length)
        {
            System.Array.Copy(micEnvelope.samples, i, micBuffer, 0, micBuffer.Length);
            micSpectrogram.process(Speech.getFFTmag(Speech.FFT(Speech.getComplexBuffer(micBuffer))));
        }
        micSpectrogram.get();
        //How much change in high and low halves of PSD for the first and second half of vocalization
        float[] diffLowHigh = Speech.getDiffLowHigh(micSpectrogram);
        Debug.Log("Mic: Low energy diff: " + diffLowHigh[0] + " High energy diff: " + diffLowHigh[1]);
        //range [0,1], higher the number, the later in the word the peak high frequency energy is
        float frequencyScore = (float)Speech.getArgMax(micSpectrogram.expected) / micSpectrogram.expected.Length;
        Debug.Log("Mic: Frequency Score: " + frequencyScore);
        //Factor in Frequency Score
        float freqDiff = Mathf.Abs(frequencyScore - modelFrequencyScore) - 0.2f;
        if (freqDiff > 0)
            score /= (freqDiff*freqDiffPenalty) + 1;
        Debug.Log("Final score with frequency matching: " + score + "%");
        #region TODO
        //get DET
        micDET = Speech.DET(micEnvelope.envelope, fastDET, slowDET);
        Speech.normalizeMax(ref micDET);
        #endregion
        Debug.Log(">>>>>Vocalization Captured<<<<<");
    }
    //update timing of temp mic recordings, use before the start of each video
    public void updateRecordingTime(float timeBeforeDetection, float timeAfterDetection)
    {
        envelopeTimeAfterDetection = timeAfterDetection;
        envelopeTime = timeBeforeDetection + timeAfterDetection;
        micEnvelope.update((int)(nRecordingHZ * envelopeTime), chunkSize);
    }
}