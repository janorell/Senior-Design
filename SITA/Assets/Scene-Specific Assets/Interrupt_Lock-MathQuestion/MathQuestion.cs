using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

//class to be attached to math question text of lock screen
public class MathQuestion : MonoBehaviour
{
    //get other instances of other classes
    [SerializeField]
    private ManageScenes manageScenes;
    [SerializeField]
    private TMP_InputField answer;
    [SerializeField]
    private float timeToAnswer = 10;
    [SerializeField]
    private Slider timerBar;
    [SerializeField]
    private Button submitButton;
    [SerializeField]
    Color submitButtonInactive;
    [SerializeField]
    Color submitButtonActive;
    //private data
    static private int maxTries = int.MaxValue; //unlimted only at first appearance of scene
    private float lastTest;
    private enum Operation { add, sub, mult, div };
    private int r1, r2;
    private int tries;
    private TMP_Text question;
    private string correctAnswer;
    static T GetRandomEnum<T>()//helper function
    {   
        //https://forum.unity.com/threads/random-range-from-enum.121933/
        System.Array A = System.Enum.GetValues(typeof(T));
        T V = (T)A.GetValue(UnityEngine.Random.Range(0, A.Length));
        return V;
    }
    void newQuestion() //generate a new question/correctAnswer, clear input field, and prevent rapid button presses
    {
        //prevent rapid presses
        submitButton.GetComponent<Image>().color = new Color(submitButtonInactive.r, submitButtonInactive.g, submitButtonInactive.b, submitButtonInactive.a);
        lastTest = Time.time;
        //clear input
        answer.text = "";
        //new question
        Operation operation = GetRandomEnum<Operation>();
        switch (operation)
        {
            case Operation.add:
                r1 = Random.Range(1, 10);
                r2 = Random.Range(1, 10);
                correctAnswer = (r1 + r2).ToString();
                question.text = string.Format("What is {0} + {1}?", r1, r2);
                break;
            case Operation.sub:
                r1 = Random.Range(1, 10);
                r2 = Random.Range(1, 10);
                if (r1 < r2) //swap to ensure positive answer
                {
                    int temp = r1;
                    r1 = r2;
                    r2 = temp;
                }
                correctAnswer = (r1 - r2).ToString();
                question.text = string.Format("What is {0} - {1}?", r1, r2); ;
                break;
            case Operation.mult:
                r1 = Random.Range(1, 10);
                r2 = Random.Range(1, 10);
                correctAnswer = (r1 * r2).ToString();
                question.text = string.Format("What is {0} x {1}?", r1, r2); ;
                break;
            default: //Operation.div
                r1 = Random.Range(1, 5);
                r2 = Random.Range(1, 5);
                correctAnswer = r1.ToString();
                question.text = string.Format("What is {0} ÷ {1}?", r1 * r2, r2); ;
                break;
        }
        //ready to call TestAnswer()
        StartCoroutine(QuestionTimer(Time.time));
    }
    IEnumerator QuestionTimer(float startTimer)
    {
        timerBar.value = 0;
        float dT = Time.time - startTimer;
        while (dT < timeToAnswer)
        {
            dT = Time.time - startTimer;
            timerBar.value = (dT);
            if (dT > 2) { submitButton.GetComponent<Image>().color = new Color(submitButtonActive.r, submitButtonActive.g, submitButtonActive.b, submitButtonActive.a); }
            yield return null;
        }
        TestAnswer();
    }
    public void TestAnswer() //check answer on click or on timer expire
    {
        if (Time.time - lastTest < 2) { return; } //do not test answer in rapid succession (ie rapid button presses)
        if (answer.text.Trim().Equals(correctAnswer)) //test vs correctAnswer and ignore white space
        {
            maxTries = 2; //do not allow unlimited tries after parent launches video exercises
            manageScenes.ChangeScene("LevelMenu");
        }
        else
        {
            if (++tries > maxTries) //unsuccessful unlock, prevent lock screen access for x minutes
            {
                manageScenes.LockChildVideo(Time.time);
                manageScenes.ChangeScene("Video_Level");
            }
            StopAllCoroutines();
            newQuestion();
        }

    }
    private void Awake() //define question and correctAnswer
    {
        timerBar.maxValue = timeToAnswer;
        question = GetComponent<TMP_Text>();
        newQuestion();
    }
}
