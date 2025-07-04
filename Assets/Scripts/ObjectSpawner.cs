using UnityEngine;
using UnityEngine.UI;

public class ObjectSpawner : MonoBehaviour
{
    public GameObject presentPrefab;

    public RectTransform canvasRect;
    public float spawnInterval = 1.5f;
    private float timer;

    void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver)
            return;  // Stop spawning when game over

        timer += Time.deltaTime;
        if (timer >= spawnInterval)
        {
            timer = 0f;
            SpawnPresent();
        }
    }

    void SpawnPresent()
    {
        if (!presentPrefab)
        {
            Debug.LogError("Present prefab not assigned!");
            return;
        }

        var obj = Instantiate(presentPrefab, canvasRect);
        var rt = obj.GetComponent<RectTransform>();

        float w = canvasRect.rect.width;
        float hw = rt.rect.width / 2f;
        float x = Random.Range(-w / 2 + hw, w / 2 - hw);

        // Position inside the top edge of the canvas (fully visible)
        float y = canvasRect.rect.height / 2 - rt.rect.height / 2;
        rt.anchoredPosition = new Vector2(x, y);

        // Add collider if none exists
        if (obj.GetComponent<Collider2D>() == null)
        {
            var collider = obj.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(rt.rect.width, rt.rect.height);
            collider.isTrigger = true;
        }
    
    }


}
