using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Settings : MonoBehaviour
{
    private TMP_Text CurrentDiff;
    public Text textbox;
    public int difficulty;
    public string VideoDiff;

    // Start is called before the first frame update
    void Start()
    {
        difficulty = PlayerPrefs.GetInt("DiffValue");
        Debug.Log("Checking Current Difficulty: " + (PlayerPrefs.GetInt("DiffValue") + 1));
    }
    // Update is called once per frame
    void Update()
    {
        switch (difficulty)
        {
            case 0:
                CurrentDiff.text = string.Format("Easy");
                VideoDiff = "Easy";
                PlayerPrefs.SetString("VideoDiff", VideoDiff);
                break;
            case 1:
                CurrentDiff.text = string.Format("Medium");
                VideoDiff = "Medium";
                PlayerPrefs.SetString("VideoDiff", VideoDiff);
                break;
            case 2:
                CurrentDiff.text = string.Format("Hard");
                VideoDiff = "Hard";
                PlayerPrefs.SetString("VideoDiff", VideoDiff);
                break;
        }
    }

    private void Awake()
    {
        CurrentDiff = GetComponent<TMP_Text>();
    }

}