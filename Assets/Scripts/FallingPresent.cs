using UnityEngine;

public class FallingPresent : MonoBehaviour
{
    public float fallSpeed = 150f;

    RectTransform rectTransform;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    void Update()
    {
        // Stop falling if game is over
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver)
            return;

        // Move present downwards
        rectTransform.anchoredPosition += Vector2.down * fallSpeed * Time.deltaTime;

        // Destroy if it goes below bottom of canvas
        if (rectTransform.anchoredPosition.y < -rectTransform.parent.GetComponent<RectTransform>().rect.height / 2 - rectTransform.rect.height)
        {
            Destroy(gameObject);
        }
    }
}
