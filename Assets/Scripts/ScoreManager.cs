using TMPro;
using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance;
    public TMP_Text scoreText;
    private int score = 0;

    void Awake()
    {
        Instance = this;
        UpdateUI();
    }

    public void AddPoint()
    {
        score++;
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (scoreText != null)
            scoreText.text = score.ToString();
    }
}
