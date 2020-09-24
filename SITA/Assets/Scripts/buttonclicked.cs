using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;

public class buttonclicked : MonoBehaviour
{
    public ManageScenes manageScenes;
    public Button[] ButtonList;
    //public Button[] DifficultyButton;
    //add button listener
    void Start()
    {
        for (int i = 0; i < ButtonList.Length; i++)
        {
            int temp = i;
            ButtonList[i].onClick.AddListener(() => CheckVideo(temp));
        }
    }

    private void CheckVideo(int id)
    {
        Debug.Log("Previous video was: " + (PlayerPrefs.GetInt("CurrentVideo")+1));
        PlayerPrefs.SetInt("CurrentVideo", id);
        Debug.Log("Changing to current video: " + (PlayerPrefs.GetInt("CurrentVideo")+1));
        manageScenes.ChangeScene("Video_Level");
    }
}
