using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private float gridSize = 1f;
    [SerializeField] private float moveCooldown = 0.2f;
    [SerializeField] private LayerMask wallLayer;

    // Timer / scene settings
    [SerializeField] private float timerDuration = 10f;
    [SerializeField] private string sceneToLoad = "GameOver";

    public TextMeshProUGUI timeText;

    private Vector2 moveInput;
    private bool isMoving = false;

    private float timerRemaining = 0f;
    private bool timerRunning = false;

    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    private void Update()
    {
        if (!isMoving && moveInput != Vector2.zero)
        {
            Vector2 direction = GetDirection();

            if (direction != Vector2.zero)
            {
                StartCoroutine(MoveStep(direction));
            }
        }

        if (timerRunning)
        {
            timerRemaining -= Time.deltaTime;
            if (timerRemaining <= 0f)
            {
                timerRemaining = 0f;
                timerRunning = false;
                UpdateTimeUI();
                SceneManager.LoadScene(sceneToLoad);
            }
            else
            {
                UpdateTimeUI();
            }
        }
    }

    private Vector2 GetDirection()
    {
        if (Mathf.Abs(moveInput.x) > Mathf.Abs(moveInput.y))
        {
            return new Vector2(moveInput.x > 0 ? gridSize : -gridSize, 0f);
        }
        else if (Mathf.Abs(moveInput.y) > Mathf.Abs(moveInput.x))
        {
            return new Vector2(0f, moveInput.y > 0 ? gridSize : -gridSize);
        }
        return Vector2.zero;
    }

    private IEnumerator MoveStep(Vector2 direction)
    {
        isMoving = true;
        Vector3 targetPosition = transform.position + (Vector3)direction;
        if (!Physics2D.OverlapCircle(targetPosition, 0.2f, wallLayer))
        {
            transform.position = targetPosition;
        }
        yield return new WaitForSeconds(moveCooldown);

        isMoving = false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Collectible"))
        {
            // Start (or restart) the countdown timer when collecting
            timerRemaining = timerDuration;
            timerRunning = true;

            Destroy(other.gameObject);
            UpdateTimeUI();
        }
    }

    private void UpdateTimeUI()
    {
        if (timeText != null)
        {
            if (timerRunning)
            {
                // Show remaining seconds rounded up for player-friendly display
                timeText.text = "Time: " + Mathf.Ceil(timerRemaining).ToString();
            }
            else if (timerRemaining > 0f)
            {
                timeText.text = "Time: " + Mathf.Ceil(timerRemaining).ToString();
            }
            else
            {
                timeText.text = string.Empty;
            }
        }
    }
}