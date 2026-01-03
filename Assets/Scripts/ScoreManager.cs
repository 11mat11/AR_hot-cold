using TMPro;
using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance;
    public TMP_Text scoreText;
    private int score = 0;
    private int objectsFoundInCurrentLevel = 0;
    private int objectsToFindInLevel = 1;

    public int ObjectsRequiredPerLevel => objectsToFindInLevel;

    private float[] multipliers = { 10f, 25f, 50f };

    void Awake()
    {
        Instance = this;
        UpdateUI();
    }

    public void RegisterObjectFound()
    {
        objectsFoundInCurrentLevel++;

        if (objectsFoundInCurrentLevel >= ObjectsRequiredPerLevel)
        {
            score++;
            objectsFoundInCurrentLevel = 0;

            if (score % GetScoreNeededForLevel(objectsToFindInLevel) == 0)
            {
                objectsToFindInLevel++;
            }

            FindFirstObjectByType<CreateObjectInRandomPlace>()?.SpawnLevelObjects();
        }

        UpdateUI();
    }

    public int GetScoreNeededForLevel(int level)
    {
        int cycle = (level - 1) / 3;

        int index = (level - 1) % 3;

        return (int)(multipliers[index] * Mathf.Pow(10, cycle));
    }

    public void Reset()
    {
        score = 0;
        objectsFoundInCurrentLevel = 0;
        objectsToFindInLevel = 1;
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (scoreText != null)
            scoreText.text = score.ToString();
    }
}
