using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//a class that automatically returns to Video_Level after preset time
//used for rewards scene
public class RewardsTimer : MonoBehaviour
{
    public float rewardTime = 10.0f;
    public ManageScenes manageScenes;
    void Awake()
    {
        Invoke("newVideo", rewardTime);
    }
    private void newVideo()
    {
        manageScenes.ChangeScene("Video_Level");
    }
}
