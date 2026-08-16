using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private float gridSize = 1f;
    [SerializeField] private float moveCooldown = 0.2f;
    [SerializeField] private LayerMask wallLayer;
    public int score = 0;
    public TextMeshProUGUI scoreText;

    private Vector2 moveInput;
    private bool isMoving = false;

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

            score += 10;
            Debug.Log("Pisteet: " + score);
            Destroy(other.gameObject);
            UpdateScoreUI();
        }
    }
    void UpdateScoreUI()
    {
        if (scoreText != null)
        {
            scoreText.text = "Score: " + score;
        }
    }
}