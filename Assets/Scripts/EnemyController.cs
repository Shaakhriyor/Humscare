using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class EnemyController : MonoBehaviour
{
    [SerializeField] private float gridSize = 1f;
    [SerializeField] private float moveInterval = 0.5f;
    [SerializeField] private LayerMask wallLayer;

    [SerializeField] private Vector2 moveDirection = Vector2.up;

    private bool isMoving = false;

    private void Start()
    {
        moveDirection = moveDirection.normalized * gridSize;

        StartCoroutine(MovementRoutine());
    }

    private IEnumerator MovementRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(moveInterval);

            Vector3 targetPosition = transform.position + (Vector3)moveDirection;

            if (!Physics2D.OverlapCircle(targetPosition, 0.2f, wallLayer))
            {
                transform.position = targetPosition;
            }
            else
            {
                moveDirection = -moveDirection;

                targetPosition = transform.position + (Vector3)moveDirection;
                if (!Physics2D.OverlapCircle(targetPosition, 0.2f, wallLayer))
                {
                    transform.position = targetPosition;
                }
            }
        }
    }
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("Pelaaja kuoli!");
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }
}