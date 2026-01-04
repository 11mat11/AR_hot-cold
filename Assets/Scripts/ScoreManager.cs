using TMPro;
using UnityEngine;
using GooglePlayGames;
using GooglePlayGames.BasicApi;
using UnityEngine.SocialPlatforms;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance;
    public TMP_Text scoreText;

    // --- LOGIKA GRY ---
    private int score = 0;
    private int objectsFoundInCurrentLevel = 0;
    private int objectsToFindInLevel = 1;
    public int ObjectsRequiredPerLevel => objectsToFindInLevel;

    private int[] milestones = { 25, 50, 100, 250 };

    // --- DANE ---
    private long totalLifetimeOrbs = 0;
    private const string SAVE_KEY = "GPGS_LifetimeObjects";

    void Awake()
    {
        Instance = this;

        // KROK 1: ZAWSZE wczytujemy najpierw dane lokalne.
        // Dzi�ki temu gra dzia�a natychmiast, nawet bez internetu.
        totalLifetimeOrbs = PlayerPrefs.GetInt(SAVE_KEY, 0);
        Debug.Log("Tryb Offline/Start: Wczytano lokalnie: " + totalLifetimeOrbs);

        UpdateUI();
    }

    // Podmie� t� funkcj� w ScoreManager.cs
    public void TrySyncScoreFromCloud()
    {
        if (!Social.localUser.authenticated) return;

        // W nowej wersji wtyczki (v2) u�ywamy kr�tszej wersji tej funkcji
        PlayGamesPlatform.Instance.LoadScores(
             GPGSIds.leaderboard_collected_elemental_orbs,
             LeaderboardStart.PlayerCentered,
             1,
             LeaderboardCollection.Public, // Zmieniono z TeamScope na LeaderboardCollection
             LeaderboardTimeSpan.AllTime, // Dodano TimeSpan (zawsze bierzemy wynik z ca�ego okresu)
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
                         PostScoreToGoogle();
                         Debug.Log("Wys�ano zaleg�y wynik lokalny do chmury.");
                     }
                 }
             });
    }

    public void RegisterObjectFound()
    {
        // 1. Logika Gry (Zawsze dzia�a)
        objectsFoundInCurrentLevel++;

        // 2. Zwi�kszamy licznik (Zawsze dzia�a)
        totalLifetimeOrbs++;

        // 3. Zapisujemy lokalnie (Zawsze dzia�a)
        PlayerPrefs.SetInt(SAVE_KEY, (int)totalLifetimeOrbs);
        PlayerPrefs.Save();

        // 4. Pr�bujemy wys�a� do Google (Dzia�a tylko jak zalogowany, jak nie - ignoruje b��d)
        PostScoreToGoogle();

        // Reszta logiki poziom�w...
        if (objectsFoundInCurrentLevel >= ObjectsRequiredPerLevel)
        {
            score++;
            objectsFoundInCurrentLevel = 0;
            if (score % GetScoreNeededForLevel(objectsToFindInLevel) == 0) objectsToFindInLevel++;
            FindFirstObjectByType<CreateObjectInRandomPlace>()?.SpawnLevelObjects();
        }
        UpdateUI();
    }

    private void PostScoreToGoogle()
    {
        // Ta linijka sprawia, �e gra nie wyrzuca b��d�w offline.
        // Je�li nie zalogowany -> po prostu wychodzimy z funkcji.
        if (!Social.localUser.authenticated) return;

        Social.ReportScore(totalLifetimeOrbs, GPGSIds.leaderboard_collected_elemental_orbs, (bool success) => { });
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
        // Nie resetujemy totalLifetimeOrbs!
    }

    private void UpdateUI()
    {
        if (scoreText != null) scoreText.text = score.ToString();
    }
}