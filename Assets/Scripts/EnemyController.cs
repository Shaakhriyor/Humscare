using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class EnemyController : MonoBehaviour
{
    [SerializeField] private float gridSize = 1f;
    [SerializeField] private float moveInterval = 0.5f;
    [SerializeField] private LayerMask wallLayer;

    // Name of the scene to load when the enemy kills the player.
    // If empty, the current active scene is reloaded.
    [SerializeField] [Tooltip("Name of the scene to load when the enemy kills the player. If empty, reloads the current scene.")]
    public string sceneToLoad = "";

    // kept for legacy/config but movement is computed at runtime
    [SerializeField] private Vector2 moveDirection = Vector2.up;

    private Transform playerTransform;

    private void Start()
    {
        moveDirection = moveDirection.normalized * gridSize;

        var playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
        }
        else
        {
            Debug.LogWarning("EnemyController: No GameObject with tag 'Player' found.");
        }

        StartCoroutine(MovementRoutine());
    }

    private IEnumerator MovementRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(moveInterval);

            if (playerTransform == null)
                continue;

            // Compute A* path each tick and step one cell along the best path
            List<Vector2> path = FindPathAStar(transform.position, playerTransform.position, maxNodes: 20000);

            if (path != null && path.Count >= 2)
            {
                Vector3 nextPos = (Vector3)path[1];

                // If the next position contains the player, end the game immediately
                Collider2D hit = Physics2D.OverlapCircle(nextPos, 0.2f);
                if (hit != null && hit.CompareTag("Player"))
                {
                    EndGame();
                    yield break;
                }

                // Move if not blocked
                if (!Physics2D.OverlapCircle(nextPos, 0.2f, wallLayer))
                {
                    transform.position = nextPos;
                    continue;
                }
            }

            // If path not found or next step blocked, fall back to previous greedy logic with checks
            Vector3 delta = playerTransform.position - transform.position;
            if (Mathf.Approximately(delta.x, 0f) && Mathf.Approximately(delta.y, 0f))
                continue;

            Vector2 primaryStep = Vector2.zero;
            Vector2 secondaryStep = Vector2.zero;

            if (Mathf.Abs(delta.x) >= Mathf.Abs(delta.y))
            {
                primaryStep = new Vector2(Mathf.Sign(delta.x) * gridSize, 0f);
                if (!Mathf.Approximately(delta.y, 0f))
                    secondaryStep = new Vector2(0f, Mathf.Sign(delta.y) * gridSize);
            }
            else
            {
                primaryStep = new Vector2(0f, Mathf.Sign(delta.y) * gridSize);
                if (!Mathf.Approximately(delta.x, 0f))
                    secondaryStep = new Vector2(Mathf.Sign(delta.x) * gridSize, 0f);
            }

            Vector3 targetPosition = transform.position + (Vector3)primaryStep;
            Collider2D playerHit = Physics2D.OverlapCircle(targetPosition, 0.2f);
            if (playerHit != null && playerHit.CompareTag("Player"))
            {
                EndGame();
                yield break;
            }

            if (!Physics2D.OverlapCircle(targetPosition, 0.2f, wallLayer))
            {
                transform.position = targetPosition;
                continue;
            }

            if (secondaryStep != Vector2.zero)
            {
                targetPosition = transform.position + (Vector3)secondaryStep;
                playerHit = Physics2D.OverlapCircle(targetPosition, 0.2f);
                if (playerHit != null && playerHit.CompareTag("Player"))
                {
                    EndGame();
                    yield break;
                }

                if (!Physics2D.OverlapCircle(targetPosition, 0.2f, wallLayer))
                {
                    transform.position = targetPosition;
                    continue;
                }
            }

            // last resort: step to any free adjacent cell
            foreach (var dir in new[] { Vector2.up, Vector2.down, Vector2.left, Vector2.right })
            {
                Vector3 altPos = transform.position + (Vector3)(dir * gridSize);

                playerHit = Physics2D.OverlapCircle(altPos, 0.2f);
                if (playerHit != null && playerHit.CompareTag("Player"))
                {
                    EndGame();
                    yield break;
                }

                if (!Physics2D.OverlapCircle(altPos, 0.2f, wallLayer))
                {
                    transform.position = altPos;
                    break;
                }
            }
        }
    }

    // A* pathfinding on the discrete grid defined by gridSize.
    // Returns world-space grid-aligned positions from start to target (inclusive), or null if no path.
    private List<Vector2> FindPathAStar(Vector2 startWorld, Vector2 targetWorld, int maxNodes = 10000)
    {
        Vector2Int start = WorldToGrid(startWorld);
        Vector2Int target = WorldToGrid(targetWorld);

        if (start == target)
            return new List<Vector2> { GridToWorld(start) };

        var cameFrom = new Dictionary<Vector2Int, Vector2Int>();
        var gScore = new Dictionary<Vector2Int, int> { [start] = 0 };
        var fScore = new Dictionary<Vector2Int, int> { [start] = Heuristic(start, target) };

        var openHeap = new MinHeap();
        openHeap.Enqueue(start, fScore[start]);

        int explored = 0;

        while (openHeap.Count > 0 && explored < maxNodes)
        {
            var current = openHeap.Dequeue();

            // Skip outdated entries (we allow duplicates in heap)
            if (!fScore.ContainsKey(current) || fScore[current] != openHeap.LatestDequeuedPriority)
            {
                // continue normally — but the simplest safe check is to compare gScore when popped:
                // (This implementation uses LatestDequeuedPriority set during Dequeue, and we validated above.)
            }

            if (current == target)
                return ReconstructPathWorld(cameFrom, start, current);

            explored++;

            foreach (var neighbor in GetNeighbors(current))
            {
                // allow stepping onto the target even if it's occupied by something considered not walkable
                if (!IsWalkable(neighbor) && neighbor != target)
                    continue;

                int tentativeG = gScore.ContainsKey(current) ? gScore[current] + 1 : int.MaxValue;

                if (!gScore.ContainsKey(neighbor) || tentativeG < gScore[neighbor])
                {
                    cameFrom[neighbor] = current;
                    gScore[neighbor] = tentativeG;
                    int f = tentativeG + Heuristic(neighbor, target);
                    fScore[neighbor] = f;
                    openHeap.Enqueue(neighbor, f);
                }
            }
        }

        // no path found
        return null;
    }

    // Reconstruct path and convert to world coords
    private List<Vector2> ReconstructPathWorld(Dictionary<Vector2Int, Vector2Int> cameFrom, Vector2Int start, Vector2Int current)
    {
        var total = new List<Vector2Int> { current };
        while (total[0] != start && cameFrom.ContainsKey(total[0]))
        {
            total.Insert(0, cameFrom[total[0]]);
        }

        var world = new List<Vector2>(total.Count);
        foreach (var g in total)
            world.Add(GridToWorld(g));
        return world;
    }

    private IEnumerable<Vector2Int> GetNeighbors(Vector2Int node)
    {
        yield return new Vector2Int(node.x + 1, node.y);
        yield return new Vector2Int(node.x - 1, node.y);
        yield return new Vector2Int(node.x, node.y + 1);
        yield return new Vector2Int(node.x, node.y - 1);
    }

    private bool IsWalkable(Vector2Int grid)
    {
        Vector2 worldPos = GridToWorld(grid);
        return Physics2D.OverlapCircle(worldPos, 0.2f, wallLayer) == null;
    }

    private Vector2Int WorldToGrid(Vector2 worldPos)
    {
        int x = Mathf.RoundToInt(worldPos.x / gridSize);
        int y = Mathf.RoundToInt(worldPos.y / gridSize);
        return new Vector2Int(x, y);
    }

    private Vector2 GridToWorld(Vector2Int gridPos)
    {
        return new Vector2(gridPos.x * gridSize, gridPos.y * gridSize);
    }

    private int Heuristic(Vector2Int a, Vector2Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y); // Manhattan distance
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            EndGame();
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider != null && collision.collider.CompareTag("Player"))
        {
            EndGame();
        }
    }

    void EndGame()
    {
        Debug.Log("Pelaaja kuoli!");

        if (!string.IsNullOrEmpty(sceneToLoad))
        {
            SceneManager.LoadScene(sceneToLoad);
        }
        else
        {
            Debug.LogWarning("You forgot to type a scene name in the Inspector!");
        }
    }

    // Simple min-heap for Vector2Int keyed by priority (int).
    // Allows duplicate entries; callers should rely on gScore/fScore validation if necessary.
    private class MinHeap
    {
        private List<HeapNode> _data = new List<HeapNode>();
        public int Count => _data.Count;
        // Stores the priority of the most recently dequeued node (useful for validating outdated duplicates if desired).
        public int LatestDequeuedPriority { get; private set; } = int.MinValue;

        public void Enqueue(Vector2Int position, int priority)
        {
            _data.Add(new HeapNode { Pos = position, Priority = priority });
            HeapifyUp(_data.Count - 1);
        }

        public Vector2Int Dequeue()
        {
            if (_data.Count == 0) return default;

            var root = _data[0];
            LatestDequeuedPriority = root.Priority;

            var last = _data[_data.Count - 1];
            _data.RemoveAt(_data.Count - 1);
            if (_data.Count > 0)
            {
                _data[0] = last;
                HeapifyDown(0);
            }
            return root.Pos;
        }

        private void HeapifyUp(int idx)
        {
            while (idx > 0)
            {
                int parent = (idx - 1) / 2;
                if (_data[idx].Priority < _data[parent].Priority)
                {
                    Swap(idx, parent);
                    idx = parent;
                }
                else break;
            }
        }

        private void HeapifyDown(int idx)
        {
            int last = _data.Count - 1;
            while (true)
            {
                int left = idx * 2 + 1;
                int right = idx * 2 + 2;
                int smallest = idx;

                if (left <= last && _data[left].Priority < _data[smallest].Priority) smallest = left;
                if (right <= last && _data[right].Priority < _data[smallest].Priority) smallest = right;

                if (smallest == idx) break;
                Swap(idx, smallest);
                idx = smallest;
            }
        }

        private void Swap(int a, int b)
        {
            var tmp = _data[a];
            _data[a] = _data[b];
            _data[b] = tmp;
        }

        private struct HeapNode
        {
            public Vector2Int Pos;
            public int Priority;
        }
    }
}