using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;

public class VideoSequence : MonoBehaviour
{
    //public VideoClip[] videoClips;
    //private int videoClipIndex;

    public VideoPlayer videoPlayer;
    public Sequence sequence;
    public Camera effectCamera;


    void Awake()
    {
        //videoPlayer = GetComponent<VideoPlayer>();
    }

    // Use this for initialization
    void Start()
    {
        videoPlayer.targetTexture.Release();
        videoPlayer.clip = sequence.GetCurrent();
    }

    // Update is called once per frame
    void Update()
    {
        if (videoPlayer.isPlaying)
        {
            SetCurrentTimeUI();
        }
    }

    public void SetNextClip()
    {
        if (effectCamera != null)
            effectCamera.gameObject.SetActive(false);
        sequence.SetNextClip();
        videoPlayer.clip = sequence.GetCurrent();
        SetTotalTimeUI();
        videoPlayer.Play();
    }

    public void Replay()
    {
        if (effectCamera != null)
            effectCamera.gameObject.SetActive(false);

        videoPlayer.clip = sequence.GetCurrent();
        SetTotalTimeUI();
        videoPlayer.Play();
    }

    public void PlayPause()
    {
        if (videoPlayer.isPlaying)
        {
            videoPlayer.Pause();
            //playButtonRenderer.material = playButtonMaterial;
        }
        else
        {
            videoPlayer.Play();
            SetTotalTimeUI();
            //playButtonRenderer.material = pauseButtonMaterial;
        }
    }

    void SetCurrentTimeUI()
    {
        string minutes = Mathf.Floor((int)videoPlayer.time / 60).ToString("00");
        string seconds = ((int)videoPlayer.time % 60).ToString("00");

        //currentMinutes.text = minutes;
        //currentSeconds.text = seconds;
    }

    void SetTotalTimeUI()
    {
        string minutes = Mathf.Floor((int)videoPlayer.clip.length / 60).ToString("00");
        string seconds = ((int)videoPlayer.clip.length % 60).ToString("00");

        //totalMinutes.text = minutes;
        //totalSeconds.text = seconds;
    }

    double CalculatePlayedFraction()
    {
        double fraction = (double)videoPlayer.frame / (double)videoPlayer.clip.frameCount;
        return fraction;
    }
}