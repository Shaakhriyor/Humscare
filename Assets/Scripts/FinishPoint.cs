using UnityEngine;
using UnityEngine.SceneManagement;

public class FinishPoint : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Type the exact name of the scene you want to load next")]
    public string nextSceneName;

    private void OnTriggerEnter2D(Collider2D other)
    {
        
        if (other.CompareTag("Player"))
        {
            CompleteLevel();
        }
    }

    void CompleteLevel()
    {
        Debug.Log("Level Complete! Loading: " + nextSceneName);

        if (!string.IsNullOrEmpty(nextSceneName))
        {
            SceneManager.LoadScene(nextSceneName);
        }
        else
        {
            Debug.LogWarning("You forgot to type a scene name in the Inspector!");
        }
    }
}