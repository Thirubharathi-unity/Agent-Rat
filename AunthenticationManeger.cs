using GooglePlayGames;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;
using GooglePlayGames.BasicApi;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.SceneManagement;
using System;

public class AuthenticationManager : MonoBehaviour
{
    public GameObject[] Buttons;
    public GameObject Slider;
    public Text LoadingText;
    public Slider LoadingBar;
    public Text DebugText;
    [Space]
    public Button playGamesButton;
    public Button guestButton;
    public string Token;
    public string Error;

    private Coroutine autoLogin;

    async void Awake()
    {
        
        try
        {
            await UnityServices.InitializeAsync();
            PlayGamesPlatform.Activate();
            SetupEvents();
        }
        catch (Exception ex)
        {
            DebugText.text = "Login Failed";
            Debug.LogException(ex);
            ProceedToMainMenu();
        }

    }

    private void Start()
    {
        int temp = PlayerPrefs.GetInt("Login", 0);
        if (temp == 0)
        {
            playGamesButton.onClick.AddListener(PlaygamesListener);
            guestButton.onClick.AddListener(GuestSignInListener);
            autoLogin = StartCoroutine(AutoSignIn());
        }
        else if (temp == 1)
        {
            GuestSignInListener();
        }
        else if (temp == 2)
        {
            PlaygamesListener();
        }
    }

    private IEnumerator AutoSignIn()
    {
        yield return new WaitForSeconds(3f);
        PlaygamesListener();
    }

    public async void PlaygamesListener()
    {
        if (autoLogin != null)
        {
            StopCoroutine(autoLogin);
        }
        ReadyForLoading();
        if (PlayGamesPlatform.Instance == null)
        {
            DebugText.text = "Google Play Games is not available.";
            await SignInAnonymouslyFallback();
            return;
        }

        if (PlayGamesPlatform.Instance.IsAuthenticated())
        {
            DebugText.text = "Already had a account.";
            try
            {
                PlayGamesPlatform.Instance.RequestServerSideAccess(true, async code =>
                {
                    DebugText.text = "Requesting Server-side Access";
                    Token = code;
                    await SignInWithGooglePlayGamesAsync(Token);
                    PlayerPrefs.SetInt("Login", 2);
                    StartCoroutine(MainMenuLoader(2));
                });

            }
            catch (Exception e)
            {
                print(e);
                DebugText.text = "Server-side Access Failed";
                StartCoroutine(MainMenuLoader(2));
            }
            return;
        }

        DebugText.text = " PlayGames Authenticating...";
        bool success = await LoginGooglePlayGames();
        if (success)
        {
            DebugText.text = " PlayGames Authenticating from Server-side...";
            await SignInWithGooglePlayGamesAsync(Token);
            PlayerPrefs.SetInt("Login", 2);
            StartCoroutine(MainMenuLoader(2));
        }
        else
        {
            DebugText.text = "Failed to sign in with Google Play Games.";
            await SignInAnonymouslyFallback();
        }
    }

    private async Task<bool> LoginGooglePlayGames()
    {
        var tcs = new TaskCompletionSource<bool>();
        PlayGamesPlatform.Instance.Authenticate((success) =>
        {
            if (success == SignInStatus.Success)
            {
                PlayGamesPlatform.Instance.RequestServerSideAccess(true, code =>
                {
                    Token = code;
                    tcs.SetResult(true);
                });
            }
            else
            {
                Error = "Failed to retrieve Google play games authorization code";
                Debug.LogError("Login Unsuccessful");
                tcs.SetResult(false);
            }
        });
        return await tcs.Task;
    }



    private async Task SignInWithGooglePlayGamesAsync(string authCode)
    {
        try
        {
            await AuthenticationService.Instance.SignInWithGooglePlayGamesAsync(authCode);
            Debug.Log($"PlayerID: {AuthenticationService.Instance.PlayerId}");
        }
        catch (AuthenticationException ex)
        {
            Debug.LogException(ex);
        }
        catch (RequestFailedException ex)
        {
            Debug.LogException(ex);
        }
    }

    public async void GuestSignInListener()
    {
        if (autoLogin != null)
        {
            StopCoroutine(autoLogin);
        }
        ReadyForLoading();
        await SignInAnonymouslyFallback();
    }

    private async Task SignInAnonymouslyFallback()
    {
        DebugText.text = "Anonumously Authenticating...";
        bool success = await SignInAnonymously();
        if (success)
        {
            PlayerPrefs.SetInt("Login", 1);
        }
        StartCoroutine(MainMenuLoader(2));
    }

    private async Task<bool> SignInAnonymously()
    {
        try
        {
            DebugText.text = "trying Anonymously Authenticating...";
            var signInTask = AuthenticationService.Instance.SignInAnonymouslyAsync();

            // Timeout for the task (e.g., 5 seconds)
            if (await Task.WhenAny(signInTask, Task.Delay(5000)) == signInTask)
            {
                await signInTask; // Task completed successfully
                Debug.Log($"PlayerID: {AuthenticationService.Instance.PlayerId}");
                return true;
            }
            else
            {
                DebugText.text = "Authentication timed out. Proceeding to Main Menu...";
                Debug.LogWarning("Authentication timed out");
                ProceedToMainMenu(); // Call the fallback method
                return false;
            }
        }
        catch (Exception ex)
        {
            DebugText.text = "Unexpected error during authentication. Proceeding to Main Menu...";
            Debug.LogException(ex);
            ProceedToMainMenu(); // Call the fallback method
            return false;
        }

    }

    private IEnumerator MainMenuLoader(int sceneIndex)
    {
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneIndex);
        while (!operation.isDone)
        {
            float progress = Mathf.Clamp01(operation.progress / 0.9f);
            LoadingBar.value = progress;
            LoadingText.text = "Loading..." + (progress * 100f).ToString("F0") + "%";
            yield return null;
        }
    }

    private void ReadyForLoading()
    {
        foreach (var obj in Buttons)
        {
            obj.SetActive(false);
        }
        Slider.SetActive(true);
    }

    private void ProceedToMainMenu()
    {
        Debug.Log("Proceeding to main menu.");
        ReadyForLoading();
        StartCoroutine(MainMenuLoader(2));
    }


    void SetupEvents()
    {
        AuthenticationService.Instance.SignedIn += () =>
        {
            Debug.Log($"PlayerID: {AuthenticationService.Instance.PlayerId}");
            Debug.Log($"Access Token: {AuthenticationService.Instance.AccessToken}");
        };

        AuthenticationService.Instance.SignInFailed += (err) =>
        {
            Debug.LogError(err);
        };

        AuthenticationService.Instance.SignedOut += () =>
        {
            Debug.Log("Player signed out.");
        };

        AuthenticationService.Instance.Expired += () =>
        {
            Debug.Log("Player session could not be refreshed and expired.");
        };
    }

}


