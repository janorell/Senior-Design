using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

[RequireComponent(typeof(VideoPlayer))]
public class VideoScript : MonoBehaviour
{
    public VideoPlayer videoPlayer;
    public int clevel;

    // Start is called before the first frame update
    public void Start()
    {
        Application.runInBackground = true;

        //clevel = PlayerPrefs.GetInt("CurrentVideo");
        //Debug.Log("Video Number:" + clevel);

        // If CurrentDiff = Easy, but also need If CurrentDiff = Medium and If CurrentDiff = Hard
        string videoName = "Easy/E";
        // sting videoName = "Medium/M";
        // string videoName = "Hard/H";


        // Calls videoPlayer
        StartCoroutine(PlayVideoInternal(videoName));
    }

    //int count;

    public string PlayVideoInternal(string videoName)
    {
        // Assigns VideoPlayer object
        videoPlayer = gameObject.GetComponent<VideoPlayer>();

        // Chooses video to play
        clevel = PlayerPrefs.GetInt("CurrentVideo")+1;   // Talk to Jenni about Current Level actual value; adding 1 to compensate
        Debug.Log("Video Number:" + clevel);

        videoName += clevel;

        // Loads video
        VideoClip clip = Resources.Load<VideoClip>(videoName) as VideoClip;

        videoPlayer.clip = clip;

        //Disable Play on Awake for both Video and Audio
        //videoPlayer.playOnAwake = false;

        // Prepares video
        //videoPlayer.Prepare();

        //Wait until video is prepared
        //while (!videoPlayer.isPrepared)
        //{
        //   yield return null;
        //}

        //Debug.Log("Done Preparing Video");

        //Play Video
        videoPlayer.Play();

        //Debug.Log("Playing Video");
        //while (videoPlayer.isPlaying)
        //{
        //Debug.LogWarning("Video Time: " + Mathf.FloorToInt((float)videoPlayer.time));
        //yield return null;
        //}

        //Debug.Log("Done Playing Video");

        //count += 1;

        //if (count < 2)
        //{
        //yield return new WaitForSeconds(5);
        //StartCoroutine(PlayVideoInternal(clevel));
        //}
        return null;
    }
}
