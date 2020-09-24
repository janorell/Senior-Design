using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;


public class Parent_Settings : MonoBehaviour
{
    //public buttonclicked bc;
    private TMP_Text CurrentLevel;
    public Text textbox;
    public int clevel;

    // Start is called before the first frame update
    void Start()
    {
        clevel = PlayerPrefs.GetInt("CurrentVideo");
        Debug.Log("Checking Parent Scene: " + (PlayerPrefs.GetInt("CurrentVideo")+1));
    }

    // Update is called once per frame
    void Update()
    {

        CurrentLevel.text = string.Format("Current Level: {0}", clevel + 1);
    }

private void Awake()
    { 
        CurrentLevel = GetComponent<TMP_Text>();
    }
}

//unity ui panel animation tutorial