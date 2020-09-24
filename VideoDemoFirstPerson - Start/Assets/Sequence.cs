using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

public class Sequence : MonoBehaviour
{
    public VideoClip[] videoClips;
    private int videoClipIndex;

    public VideoClip GetCurrent()
    {
        return videoClips[videoClipIndex];
    }

    public void SetNextClip()
    {
        videoClipIndex++;

        if (videoClipIndex >= videoClips.Length)
        {
            videoClipIndex = videoClipIndex % videoClips.Length;
        }
    }


}
