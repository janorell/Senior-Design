using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;

public class difficultybutton : MonoBehaviour
{
    public ManageScenes manageScenes;
    public Button[] DifficultyButton;

    void Start()
    {
        for (int i = 0; i < DifficultyButton.Length; i++)
        {
            int temp = i;
            DifficultyButton[i].onClick.AddListener(() => CheckDiff(temp));
        }

    }

    private void CheckDiff(int id)
    {
        Debug.Log("Previous level was: " + (PlayerPrefs.GetInt("DiffValue") + 1));
        PlayerPrefs.SetInt("DiffValue", id);
        Debug.Log("Changing to level: " + (PlayerPrefs.GetInt("DiffValue") + 1));
        manageScenes.ChangeScene("Settings");
    }
}
