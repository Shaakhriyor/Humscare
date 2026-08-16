using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    [Header("Timer Settings")]
    public float timeRemaining = 60f; // Change this in the Inspector
    private bool isGameActive = true;

    [Header("UI References")]
    public TextMeshProUGUI timerText;

    void Update()
    {
        if (isGameActive)
        {
            if (timeRemaining > 0)
            {
                timeRemaining -= Time.deltaTime;
                UpdateUI();
            }
            else
            {
                timeRemaining = 0;
                isGameActive = false;
                UpdateUI(); // Final update to show 0
                GameOver();
            }
        }
    }

    void UpdateUI()
    {
        if (timerText != null)
        {
            // Displays time rounded up to the nearest whole second
            timerText.text = "Time: " + Mathf.CeilToInt(timeRemaining).ToString();
        }
    }

    void GameOver()
    {
        Debug.Log("Time's up! Restarting level...");
        // Restarts the current level
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}