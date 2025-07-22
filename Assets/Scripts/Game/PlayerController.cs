using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float speed = 200f;
    private RectTransform rect;
    private float canvasWidth;

    void Start()
    {
        rect = GetComponent<RectTransform>();
        canvasWidth = rect.parent.GetComponent<RectTransform>().rect.width;
    }

    void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver)

            return;

        // Movement logic
        float x = Input.GetAxis("Horizontal") * speed * Time.deltaTime;
        rect.anchoredPosition += new Vector2(x, 0);

        float halfW = rect.rect.width / 2f;
        float limit = canvasWidth / 2f - halfW;
        rect.anchoredPosition = new Vector2(
            Mathf.Clamp(rect.anchoredPosition.x, -limit, limit),
            rect.anchoredPosition.y
        );
    }


    void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log("🎁 Caught a present!");
        if (other.CompareTag("Present"))
        {
            Destroy(other.gameObject);
            GameManager.Instance.AddScore(1);
            Debug.Log("🎁 Caught a present!");
            // TODO: Increase score here
        }
    }
}
