using UnityEngine;
#if UNITY_ANDROID
using GooglePlayGames;
using GooglePlayGames.BasicApi;
#endif
using UnityEngine.SocialPlatforms;

public class GPGSManager : MonoBehaviour
{
    public static GPGSManager Instance;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

#if UNITY_ANDROID
        PlayGamesPlatform.Activate();
#endif
    }

    void Start()
    {
        SignIn();
    }

    public void SignIn()
    {
#if UNITY_ANDROID
        Social.localUser.Authenticate((bool success) =>
        {
            if (success)
            {
                Debug.Log("Zalogowano do Google Play Games: " + Social.localUser.userName);
                if (ScoreManager.Instance != null)
                {
                    ScoreManager.Instance.TrySyncScoreFromCloud();
                }
            }
            else
            {
                Debug.LogWarning("Nie udało się zalogować (brak internetu lub anulowano). Gramy offline.");
            }
        });
#endif
    }

    public void ShowAchievementsUI()
    {
#if UNITY_ANDROID
        if (Social.localUser.authenticated)
        {
            Social.ShowAchievementsUI();
        }
        else
        {
            SignIn();
        }
#endif
    }

    public void ShowLeaderboardsUI()
    {
#if UNITY_ANDROID
        if (Social.localUser.authenticated)
        {
            Social.ShowLeaderboardUI();
        }
#endif
    }

    public void UnlockAchievement(string achievementId)
    {
#if UNITY_ANDROID
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
#endif
    }

    public void IncrementAchievement(string achievementId, int steps)
    {
#if UNITY_ANDROID
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
#endif
    }
}