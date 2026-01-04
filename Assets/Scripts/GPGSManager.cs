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
            DontDestroyOnLoad(gameObject); // Wa¿ne: ¿eby manager nie znika³ przy zmianie scen
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
        // Próba cichego logowania na starcie
        SignIn();
    }

    public void SignIn()
    {
        Social.localUser.Authenticate((bool success) =>
        {
            if (success)
            {
                Debug.Log("Zalogowano do Google Play Games: " + Social.localUser.userName);

                // SUKCES: Mówimy ScoreManagerowi "Hej, mam internet, sprawdŸ czy w chmurze nie ma lepszego wyniku"
                if (ScoreManager.Instance != null)
                {
                    ScoreManager.Instance.TrySyncScoreFromCloud();
                }
            }
            else
            {
                // PORA¯KA: Trudno, gra dzia³a dalej na lokalnych danych bez ¿adnych b³êdów
                Debug.LogWarning("Nie uda³o siê zalogowaæ (brak internetu lub anulowano). Gramy offline.");
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
            // Opcjonalnie: Tutaj mo¿esz wywo³aæ SignIn(), jeœli gracz klikn¹³ guzik a nie jest zalogowany
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
}