using UnityEngine;
using GooglePlayGames;
using GooglePlayGames.BasicApi;
using UnityEngine.SocialPlatforms;

public class GPGSManager : MonoBehaviour
{
    public static GPGSManager Instance;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Wa�ne: �eby manager nie znika� przy zmianie scen
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Aktywacja wtyczki
        PlayGamesPlatform.Activate();
    }

    void Start()
    {
        // Pr�ba cichego logowania na starcie
        SignIn();
    }

    public void SignIn()
    {
        Social.localUser.Authenticate((bool success) =>
        {
            if (success)
            {
                Debug.Log("Zalogowano do Google Play Games: " + Social.localUser.userName);

                // SUKCES: M�wimy ScoreManagerowi "Hej, mam internet, sprawd� czy w chmurze nie ma lepszego wyniku"
                if (ScoreManager.Instance != null)
                {
                    ScoreManager.Instance.TrySyncScoreFromCloud();
                }
            }
            else
            {
                // PORA�KA: Trudno, gra dzia�a dalej na lokalnych danych bez �adnych b��d�w
                Debug.LogWarning("Nie uda�o si� zalogowa� (brak internetu lub anulowano). Gramy offline.");
            }
        });
    }

    public void ShowAchievementsUI()
    {
        if (Social.localUser.authenticated)
        {
            Social.ShowAchievementsUI();
        }
        else
        {
            // Opcjonalnie: Tutaj mo�esz wywo�a� SignIn(), je�li gracz klikn�� guzik a nie jest zalogowany
            SignIn();
        }
    }

    public void ShowLeaderboardsUI()
    {
        if (Social.localUser.authenticated)
        {
            Social.ShowLeaderboardUI();
        }
    }

    public void UnlockAchievement(string achievementId)
    {
        if (Social.localUser.authenticated)
        {
            Social.ReportProgress(achievementId, 100.0f, (bool success) =>
            {
                if (success)
                {
                    Debug.Log("Achievement unlocked: " + achievementId);
                }
            });
        }
    }

    public void IncrementAchievement(string achievementId, int steps)
    {
        if (Social.localUser.authenticated)
        {
            PlayGamesPlatform.Instance.IncrementAchievement(achievementId, steps, (bool success) =>
            {
                if (success)
                {
                     Debug.Log("Dodano postęp do osiągnięcia: " + achievementId);
                }
            });
        }
    }
}