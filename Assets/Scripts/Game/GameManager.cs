using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI timerText;
    public GameObject gameOverPanel;

    private int score = 0;
    private float timeLeft = 10f;
    private bool isGameOver = false;
    public bool IsGameOver => isGameOver;


    void Awake()
    {
        Instance = this;
    }

    void Update()
    {
        if (isGameOver) return;

        timeLeft -= Time.deltaTime;
        timerText.text = "Time: " + Mathf.CeilToInt(timeLeft);

        if (timeLeft <= 0f)
        {
            GameOver();
        }
    }

    public void AddScore(int amount)
    {
        if (isGameOver) return;
        score += amount;
        scoreText.text = "Score: " + score;
    }

    public void GameOver()
    {
        isGameOver = true;
        gameOverPanel.SetActive(true);
    }

    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
