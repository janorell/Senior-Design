using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ManageScenes : MonoBehaviour
{
    private string levelToLock = "Video_Level";
    [SerializeField]
    private static float timeToLock = 60; //seconds to lock Video_Level scene after too many failed unlocks
    static float timeLockedChildVideo = -61;
    private void Awake()
    {
        Application.backgroundLoadingPriority = ThreadPriority.High; //load scenes fast
    }
    public void LockChildVideo(float timeStart)//sets Video_Level lock start time
    {
        timeLockedChildVideo = timeStart;
    }
    public void ChangeScene(string sceneToLoad)//Public function to be triggered via UI and game events 
    {
        Debug.Log("Request to change scene " + (Time.time - timeLockedChildVideo).ToString() + " seconds after last lock");
        if (SceneManager.GetActiveScene().name != levelToLock 
            || Time.time - timeLockedChildVideo > timeToLock
            ||sceneToLoad.Equals("Almost There")
            || sceneToLoad.Equals("Great Job")
            || sceneToLoad.Equals("Perfect Win")) //switch scenes only if not in Child_Video locked state
        {
            StartCoroutine(LoadAsyncScene(sceneToLoad));
        }
    }
    IEnumerator LoadAsyncScene(string sceneToLoad)//Asynchronous Coroutine helper function
    {
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneToLoad);
        // Wait until the asynchronous scene fully loads
        while (!asyncLoad.isDone)
        {
            yield return null;
        }
    }
}
