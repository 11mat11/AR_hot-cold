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

    private int[] milestones = { 25, 50, 100, 250 };

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
        return milestones[level - 1];
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
