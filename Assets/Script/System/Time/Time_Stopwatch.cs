using UnityEngine;
using TMPro;
public class Time_Stopwatch : MonoBehaviour
{
    public static Time_Stopwatch Instance;
    public TextMeshProUGUI timerText;
    public bool autoStart;

    private float elapsedTime;
    private bool isRunning;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        timerText.text = "00:00:0";
        if (autoStart)
        {
            StartStopwatch();
        }
    }

    private void Update()
    {
        if (!isRunning)
        {
            return;
        }

        elapsedTime += Time.deltaTime;

        int minutes = Mathf.FloorToInt(elapsedTime / 60f);
        int seconds = Mathf.FloorToInt(elapsedTime) % 60;
        int milliseconds = Mathf.FloorToInt((elapsedTime - Mathf.Floor(elapsedTime)) * 10f);
        timerText.text = string.Format("{0:00}:{1:00}:{2:0}", minutes, seconds, milliseconds);
    }

    // To be called by a manager or UnityEvent
    public void StartStopwatch()
    {
        Time.timeScale = 1;
        isRunning = true;
    }

    public void StopStopwatch()
    {
        isRunning = false;
        timerText.gameObject.SetActive(false);
    }
}