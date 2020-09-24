using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

//Video player and driver code for model word matching
public class ModifiedVideoPlayer : MonoBehaviour
{
    //=================public/serializable=================
    //DEPENDENCIES (REQUIRED)
    public VideoClip currentVideo;
    public AudioClip currentAudio;
    public string videoName = "";
    public string audioName = "";
    [SerializeField]
    private Mic micro;
    [SerializeField]
    private RawImage image;
    [SerializeField]
    private GameObject playIcon;
    //parameters
    public float envelopeTrimThreshold = 0.2f, envelopeTrimExtraStart = 0.1f, envelopeTrimExtraEnd = 0.7f; //ModelWord_Trim_Envelope
    public float otherTrimThreshold = 0.25f, otherTrimExtra = 0.2f; //ModelWord_Trim_Other
    public float pauseTime = 20; //secs to wait for a response
    //output data
    public Envelope modelEnvelope;
    public Spectrogram modelSpectrogram;
    public float[] modelDET = new float[1];
    public float modelExpectedFrequency; 
    public float audioClipLength= 20;
    //=================private=================
    //video player
    VideoPlayer videoPlayer;
    AudioSource audioSource;
    //resource requests
    ResourceRequest cVidRR, cAudRR;
    //processing context
    int samplingRate, modelChunkSize;
    float[] modelBuffer = new float[1];
    //initialize local data
    private void Awake()
    {
        //allocate blank envelope and spectogram
        modelEnvelope = new Envelope();
        modelSpectrogram = new Spectrogram();
        //start with icon
        playIcon.SetActive(true);
    }
    //use Start to initialize after dependencies are set up
    void Start()
    {
        //Add VideoPlayer
        videoPlayer = gameObject.AddComponent<VideoPlayer>();
        videoPlayer.isLooping = false;
        videoPlayer.loopPointReached += endHandler;
        //Add AudioSource
        audioSource = gameObject.GetComponent<AudioSource>();
        //initialize video output
        videoPlayer.source = VideoSource.VideoClip;
        //initialize audio output
        videoPlayer.audioOutputMode = VideoAudioOutputMode.None;
        //videoPlayer.EnableAudioTrack(0, true);
        //videoPlayer.SetTargetAudioSource(0, audioSource);
        //Disable Play on Awake for both Video and Audio
        videoPlayer.playOnAwake = false;
        videoPlayer.Pause();
        audioSource.playOnAwake = false;
        audioSource.Pause();
        //start mic buffer system
        micro.StartRecording();
    } 
    //record microphone response and call newVideo after pauseTime seconds
    void endHandler(VideoPlayer vp)
    {
        //restart video
        audioSource.Stop();
        Invoke("newVideo", pauseTime);
    }
    //allow mic matching after video ends, called via invoke
    private void unblockMic()
    {
        Debug.Log(">>>>>Capturing Vocalization<<<<<");
        micro.blockCorrelation = false;
    }
    //start the next video, called via invoke
    private void newVideo()
    {
        StartCoroutine(startVideo());
    }
    //get modelEnvelope and start video
    IEnumerator startVideo()
    {
        //block microphone
        micro.blockCorrelation = true;
        //load target video/audio
        cVidRR = Resources.LoadAsync<VideoClip>("MOV/" + videoName);
        cAudRR = Resources.LoadAsync<AudioClip>("WAV/" + audioName);
        while (!cVidRR.isDone || !cAudRR.isDone) { yield return null; }
        currentVideo = (VideoClip)cVidRR.asset;
        currentAudio = (AudioClip)cAudRR.asset;
        audioClipLength = (float)currentVideo.length;
        //error check
        if (currentAudio.length < 1.0f)
            Debug.LogError("Model Word is less than 1 second!");
        //processing context
        samplingRate = currentAudio.frequency;
        modelChunkSize = Mathf.FloorToInt((float)samplingRate / Mic.nRecordingHZ * micro.chunkSize);
        modelBuffer = new float[Mathf.FloorToInt((float)samplingRate / Mic.nRecordingHZ * Mic.bufferSize)];
        //audioClipData
        float[] audioClipData = new float[currentAudio.samples];
        currentAudio.GetData(audioClipData, 0);
        //modelEnvelope
        modelEnvelope.update(audioClipData.Length, modelChunkSize);
        modelEnvelope.process(audioClipData);
        modelEnvelope.get();
        //ModelWord_NoTrim
        Speech.saveArrayToWav(modelEnvelope.samples, "ModelWord_NoTrim", samplingRate);
        Debug.Log("Model Word: Length of ModelWord_NoTrim: " + (float)modelEnvelope.samples.Length / samplingRate + " seconds");
        //ModelWord_Trim_Envelope
        Speech.trim(ref modelEnvelope, modelChunkSize, samplingRate, envelopeTrimThreshold, envelopeTrimExtraEnd, true);
        Speech.trim(ref modelEnvelope, modelChunkSize, samplingRate, envelopeTrimThreshold, envelopeTrimExtraStart, false);
        Speech.saveArrayToWav(modelEnvelope.samples, "ModelWord_Trim_Envelope", samplingRate);
        Debug.Log("Model Word: Length of ModelWord_Trim_Envelope: " + (float)modelEnvelope.samples.Length / samplingRate + " seconds");
        //play ModelWord_Trim_Envelope
        audioSource.clip = AudioClip.Create("ModelWord_Trim_Envelope", modelEnvelope.samples.Length, 1, samplingRate, false);
        audioSource.clip.SetData(modelEnvelope.samples, 0);
        //save ModelWord_Trim_Envelope in micro
        micro.modelWordTrimEnvelopeTime = (float)modelEnvelope.samples.Length / samplingRate;
        micro.modelEnvelope = new float[modelEnvelope.envelope.Length];
        System.Array.Copy(modelEnvelope.envelope, micro.modelEnvelope, modelEnvelope.envelope.Length);
        //ModelWord_Trim_Other
        Speech.trim(ref modelEnvelope, modelChunkSize, samplingRate, otherTrimThreshold, otherTrimExtra, true);
        Speech.trim(ref modelEnvelope, modelChunkSize, samplingRate, otherTrimThreshold, otherTrimExtra, false);
        Speech.saveArrayToWav(modelEnvelope.samples, "ModelWord_Trim_Other", samplingRate);
        Debug.Log("Model Word: Length of ModelWord_Trim_Other: " + (float)modelEnvelope.samples.Length / samplingRate + " seconds");
        //save 
        micro.modelWordTrimOtherTime = (float)modelEnvelope.samples.Length / samplingRate;
        #region TODO
        //perform syllable detection
        modelDET = Speech.DET(modelEnvelope.envelope, micro.fastDET, micro.slowDET);
        Speech.normalizeMax(ref modelDET);
        #endregion 
        //modelSpectogram
        int bufferAlignedSize = modelEnvelope.samples.Length - (modelEnvelope.samples.Length % modelBuffer.Length);
        modelSpectrogram.update(bufferAlignedSize, modelBuffer.Length);
        for (int i = 0; i < bufferAlignedSize; i += modelBuffer.Length)
        {
            System.Array.Copy(modelEnvelope.samples, i, modelBuffer, 0, modelBuffer.Length);
            modelSpectrogram.process(Speech.getFFTmag(Speech.FFT(Speech.getComplexBuffer(modelBuffer))));
        }
        modelSpectrogram.get();
        //High-Low diff of model word
        float[] diffLowHigh = Speech.getDiffLowHigh(modelSpectrogram);
        Debug.Log("Model Word: Low energy change = " + diffLowHigh[0] + " High energy change = " + diffLowHigh[1]);
        //modelFrequencyScore
        micro.modelFrequencyScore = (float)Speech.getArgMax(modelSpectrogram.expected) / modelSpectrogram.expected.Length; ;
        Debug.Log("Model Word: Frequency Score: " + micro.modelFrequencyScore);
        //updateRecordingTime
        micro.updateRecordingTime(micro.envelopeTimeBefore, micro.modelWordTrimEnvelopeTime + micro.envelopeTimeAfter);
        //unblockMic
        if (micro.startCapture <= -micro.modelWordTrimEnvelopeTime)
        {
            Debug.Log("Immediate mic capture");
            unblockMic();
        }
        else
        {
            //unblock mic as soon as rest of model word envelope is less than 'envelopeTrimThreshold' 
            Invoke("unblockMic", micro.modelWordTrimEnvelopeTime + micro.startCapture);
        }
        //hide icon
        playIcon.SetActive(false);
        //Prepare Video
        videoPlayer.clip = currentVideo;
        videoPlayer.Prepare();
        while (!videoPlayer.isPrepared)
        {
            yield return null;
        }
        image.texture = videoPlayer.texture;
        //Play Video
        videoPlayer.Play();
        //Play Sound
        audioSource.Play();
    }
    //start video via button
    public void Play()
    {
        StartCoroutine(startVideo());
    }
}