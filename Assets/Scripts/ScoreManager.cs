using TMPro;
using UnityEngine;
using GooglePlayGames;
using GooglePlayGames.BasicApi;
using UnityEngine.SocialPlatforms;
using System.Collections.Generic;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance;
    public TMP_Text scoreText;
    private long bestScore = 0;
    private const string SAVE_KEY_BEST_SCORE = "GPGS_BestScore";
    private int score = 0;
    private int objectsFoundInCurrentLevel = 0;
    private int objectsToFindInLevel = 1;
    public int ObjectsRequiredPerLevel => objectsToFindInLevel;

    private int[] milestones = { 25, 50, 100, 250 };

    // For achievements and leaderboards
    private long totalLifetimeOrbs = 0;
    private const string SAVE_KEY = "GPGS_LifetimeObjects";

    private float lastCatchTime;
    private int quickCatchCounter = 0;
    private float silenceTimer = 0f;

    private HashSet<int> listenedObjects = new HashSet<int>();
    private float totalListeningTime = 0f;
    private const float LISTENING_DISTANCE = 3.0f;
    private const int MIN_DIFFERENT_OBJECTS = 3;
    private const float REQUIRED_LISTENING_TIME = 180f;

    private float timeSearchingForOrb = 0f;
    private bool hasActiveOrbs = false;


    void Awake()
    {
        Instance = this;

        totalLifetimeOrbs = PlayerPrefs.GetInt(SAVE_KEY, 0);
        bestScore = PlayerPrefs.GetInt(SAVE_KEY_BEST_SCORE, 0);
        Debug.Log($"Start Offline: Lifetime={totalLifetimeOrbs}, BestScore={bestScore}");

        UpdateUI();
    }

    void Update()
    {
        CheckSilenceAchievements();
        CheckSoundListenerAchievement();
        CheckBetterLateThanNeverAchievement();
    }

    public void TrySyncScoreFromCloud()
    {
        if (!Social.localUser.authenticated) return;

        PlayGamesPlatform.Instance.LoadScores(
             GPGSIds.leaderboard_collected_elemental_orbs,
             LeaderboardStart.PlayerCentered,
             1,
             LeaderboardCollection.Public,
             LeaderboardTimeSpan.AllTime,
             (LeaderboardScoreData data) =>
             {
                 if (data.Valid && data.PlayerScore != null)
                 {
                     long cloudScore = data.PlayerScore.value;
                     Debug.Log($"Synchronizacja: Chmura={cloudScore}, Lokalne={totalLifetimeOrbs}");

                     if (cloudScore > totalLifetimeOrbs)
                     {
                         totalLifetimeOrbs = cloudScore;
                         PlayerPrefs.SetInt(SAVE_KEY, (int)totalLifetimeOrbs);
                         PlayerPrefs.Save();
                         Debug.Log("Zaktualizowano wynik lokalny na podstawie chmury.");
                     }
                     else if (totalLifetimeOrbs > cloudScore)
                     {
                         Social.ReportScore(totalLifetimeOrbs, GPGSIds.leaderboard_collected_elemental_orbs, (bool s) => { });
                         Debug.Log("Wys�ano zaleg�y wynik lokalny do chmury.");
                     }
                 }
             });
        PlayGamesPlatform.Instance.LoadScores(
             GPGSIds.leaderboard_best_score,
             LeaderboardStart.PlayerCentered,
             1,
             LeaderboardCollection.Public,
             LeaderboardTimeSpan.AllTime,
             (LeaderboardScoreData data) =>
             {
                 if (data.Valid && data.PlayerScore != null)
                 {
                     long cloudBest = data.PlayerScore.value;

                     if (cloudBest > bestScore)
                     {
                         bestScore = cloudBest;
                         PlayerPrefs.SetInt(SAVE_KEY_BEST_SCORE, (int)bestScore);
                         PlayerPrefs.Save();
                         Debug.Log("Zaktualizowano Best Score z chmury: " + bestScore);
                     }
                 }
             });
    }

    public void RegisterObjectFound()
    {
        QuickCatchCheck();
        MidnightExplorerCheck();

        objectsFoundInCurrentLevel++;

        totalLifetimeOrbs++;

        PlayerPrefs.SetInt(SAVE_KEY, (int)totalLifetimeOrbs);
        PlayerPrefs.Save();

        PostScoreToGoogle(totalLifetimeOrbs, GPGSIds.leaderboard_collected_elemental_orbs);

        timeSearchingForOrb = 0f;

        CheckMilestoneAchievements();

        CheckMasterOf5ElementsAchievement();

        if (objectsFoundInCurrentLevel >= ObjectsRequiredPerLevel)
        {
            score++;
            objectsFoundInCurrentLevel = 0;
            if (score > bestScore)
            {
                bestScore = score;

                PlayerPrefs.SetInt(SAVE_KEY_BEST_SCORE, (int)bestScore);
                PlayerPrefs.Save();

                Debug.Log($"Nowy rekord! Wysyłam {bestScore} do tabeli Best Score.");
                PostScoreToGoogle(bestScore, GPGSIds.leaderboard_best_score);
            }
            if (score % GetScoreNeededForLevel(objectsToFindInLevel) == 0) objectsToFindInLevel++;
            FindFirstObjectByType<CreateObjectInRandomPlace>()?.SpawnLevelObjects();

            CheckMarathonerAchievement();
           
        }
        UpdateUI();
    }

    private void QuickCatchCheck()
    {
        if (Time.time - lastCatchTime < 5f)
            quickCatchCounter++;
        else
            quickCatchCounter = 1;

        lastCatchTime = Time.time;

        if (quickCatchCounter >= 5)
            GPGSManager.Instance.UnlockAchievement(GPGSIds.achievement_quick_catch);
    }

    private void MidnightExplorerCheck()
    {
        int hour = System.DateTime.Now.Hour;
        if (hour >= 0 && hour < 1)
            GPGSManager.Instance.UnlockAchievement(GPGSIds.achievement_midnight_explorer);
    }

    private void CheckSilenceAchievements()
    {
        if (GPGSManager.Instance == null) return;

        GameObject[] orbs = GameObject.FindGameObjectsWithTag("ElementalOrb");
        if (orbs.Length == 0) return;

        bool canHearAnything = false;
        foreach (var orb in orbs)
        {
            SpatialSound sound = orb.GetComponent<SpatialSound>();
            if (sound != null)
            {
                float dist = Vector3.Distance(Camera.main.transform.position, orb.transform.position);
                if (dist < sound.noVolumeDistance)
                {
                    canHearAnything = true;
                    break;
                }
            }
        }

        if (!canHearAnything)
            GPGSManager.Instance.UnlockAchievement(GPGSIds.achievement_silence_seeker);

        if (!canHearAnything && orbs.Length >= 5)
        {
            silenceTimer += Time.deltaTime;
            if (silenceTimer >= 10f)
                GPGSManager.Instance.UnlockAchievement(GPGSIds.achievement_absolute_silence);
        }
        else
        {
            silenceTimer = 0f;
        }
    }

    private void CheckSoundListenerAchievement()
    {
        if (GPGSManager.Instance == null || Camera.main == null) return;

        GameObject[] orbs = GameObject.FindGameObjectsWithTag("ElementalOrb");
        if (orbs.Length == 0) return;

        bool isListening = false;
        foreach (var orb in orbs)
        {
            float dist = Vector3.Distance(Camera.main.transform.position, orb.transform.position);
            if (dist < LISTENING_DISTANCE)
            {
                isListening = true;
                int objectId = orb.GetInstanceID();
                listenedObjects.Add(objectId);
                break;
            }
        }

        if (isListening)
        {
            totalListeningTime += Time.deltaTime;

            if (listenedObjects.Count >= MIN_DIFFERENT_OBJECTS && totalListeningTime >= REQUIRED_LISTENING_TIME)
            {
                GPGSManager.Instance.UnlockAchievement(GPGSIds.achievement_sound_listener);
            }
        }
    }

    private void CheckBetterLateThanNeverAchievement()
    {
        if (GPGSManager.Instance == null) return;

        GameObject[] orbs = GameObject.FindGameObjectsWithTag("ElementalOrb");
        hasActiveOrbs = orbs.Length > 0;

        if (hasActiveOrbs)
        {
            timeSearchingForOrb += Time.deltaTime;

            if (timeSearchingForOrb >= 300f)
            {
                GPGSManager.Instance.UnlockAchievement(GPGSIds.achievement_better_late_than_never);
            }
        }
    }

    private void PostScoreToGoogle(long scoreVal, string leaderboardId)
    {
        if (!Social.localUser.authenticated) return;
        Social.ReportScore(scoreVal, leaderboardId, (bool success) => { });
    }

    public int GetScoreNeededForLevel(int level)
    {
        if (level - 1 < milestones.Length)
        {
            return milestones[level - 1];
        }
        return milestones[milestones.Length - 1];
    }

    public void Reset()
    {
        score = 0;
        objectsFoundInCurrentLevel = 0;
        objectsToFindInLevel = 1;
        timeSearchingForOrb = 0f;
        UpdateUI();
    }

    private void CheckMilestoneAchievements()
    {
        if (GPGSManager.Instance == null) return;

        // Beginner (wymaga 10)
        GPGSManager.Instance.IncrementAchievement(GPGSIds.achievement_beginner, 1);

        // Veteran (wymaga 100)
        GPGSManager.Instance.IncrementAchievement(GPGSIds.achievement_veteran, 1);

        // Master (wymaga 1000)
        GPGSManager.Instance.IncrementAchievement(GPGSIds.achievement_master, 1);
    }

    private void CheckMarathonerAchievement()
    {
        if (score >= 100)
        {
            GPGSManager.Instance?.UnlockAchievement(GPGSIds.achievement_marathoner);
        }
    }

    private void CheckMasterOf5ElementsAchievement()
    {
        if (GPGSManager.Instance == null) return;

        if (objectsToFindInLevel == 5)
        {
            GPGSManager.Instance.UnlockAchievement(GPGSIds.achievement_master_of_five_elements);
        }
    }

    private void UpdateUI()
    {
        if (scoreText != null) scoreText.text = score.ToString();
    }
}