using UnityEngine;
using UnityEngine.UI;
using Firebase;
using Firebase.Auth;
using Firebase.Database;
using System.Threading.Tasks;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using System.Threading;


public class AuthManager : MonoBehaviour
{
    public static AuthManager Instance;
    public static string currentUserId;

    [Header("Input Fields")]
    public TMP_InputField usernameInput;
    public TMP_InputField emailInput;
    public TMP_InputField passwordInput;

    [Header("UI Elements")]
    public TMP_Text messageText;
    public GameObject loginCanvas;

    private FirebaseAuth auth;
    private DatabaseReference dbRef;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else Destroy(gameObject);
    }

    void Start()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task =>
        {
            if (task.Result == DependencyStatus.Available)
            {

                auth = FirebaseAuth.DefaultInstance;
                dbRef = FirebaseDatabase.DefaultInstance.RootReference;

                Debug.Log("✅ Firebase initialized.");
            }
            else
            {
                UnityMainThreadDispatcher.Instance().Enqueue(() =>
                    ShowMessage("Firebase not available."));
                Debug.LogError("❌ Firebase dependency error: " + task.Result.ToString());
            }
        });
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.M)) ShowMessage("✅ Test message visible");
    }

    private void ShowMessage(string message)
    {
        Debug.Log("Message: " + message);
        messageText.text = message;
        StopAllCoroutines();
        StartCoroutine(ClearMessageAfterDelay(3f));
    }

    private IEnumerator ClearMessageAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        messageText.text = "";
    }

    // ──────────────────────────────────────────────────────
    // ✅ SIGN UP
    // ──────────────────────────────────────────────────────
    public void SignUp()
    {
        string email = emailInput.text.Trim();
        string password = passwordInput.text;
        string username = usernameInput.text.Trim();

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(username))
        {
            ShowMessage("Please fill in all fields.");
            return;
        }

        if (password.Length < 6)
        {
            ShowMessage("Password must be at least 6 characters long.");
            return;
        }

        auth.CreateUserWithEmailAndPasswordAsync(email, password).ContinueWith(task =>
        {
            if (task.IsCanceled || task.IsFaulted)
            {
                string errorMsg = ParseFirebaseError(task.Exception);
                UnityMainThreadDispatcher.Instance().Enqueue(() => ShowMessage(errorMsg));
                return;
            }

            FirebaseUser newUser = task.Result.User;
            currentUserId = newUser.UserId;

            Debug.Log($"📤 Saving user {currentUserId} with username: {username}, email: {email}");
            SaveUserToDatabase(currentUserId, username, email);

        });
    }

    private void SaveUserToDatabase(string userId, string username, string email)
    {
        var userData = new Dictionary<string, object>
        {
            { "username", username },
            { "email", email }
        };

        dbRef.Child("users").Child(userId).SetValueAsync(userData).ContinueWith(task =>
        {
            UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                if (task.IsFaulted || task.IsCanceled)
                {
                    ShowMessage("Failed to save user data.");
                    Debug.LogError("❌ SaveUserToDatabase failed: " + task.Exception);
                }
                else
                {
                    Debug.Log("✅ SaveUserToDatabase succeeded.");
                    ShowMessage("✅ Registration successful!");
                }
            });
        });
    }

    // ──────────────────────────────────────────────────────
    // ✅ LOGIN
    // ──────────────────────────────────────────────────────
    public void Login()
    {
        string email = emailInput.text.Trim();
        string password = passwordInput.text;

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            ShowMessage("Please enter email and password.");
            return;
        }

        auth.SignInWithEmailAndPasswordAsync(email, password).ContinueWith(task =>
        {
            UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                if (task.IsCanceled || task.IsFaulted)
                {
                    string errorMsg = ParseFirebaseError(task.Exception);
                    ShowMessage(errorMsg);
                    return;
                }

                FirebaseUser user = task.Result.User;
                currentUserId = user.UserId;

                LoadUsername(currentUserId);
            });
        });
    }

    private void LoadUsername(string userId)
    {
        dbRef.Child("users").Child(userId).Child("username").GetValueAsync().ContinueWith(task =>
        {
            UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                if (task.IsCompleted && task.Result.Exists)
                {
                    Debug.Log("✅ Username: " + task.Result.Value.ToString());
                    SwitchToHomePage();
                }
                else
                {
                    ShowMessage("Login succeeded, but no username found.");
                }
            });
        });
    }

    // ──────────────────────────────────────────────────────
    // 🔁 Reset Password
    // ──────────────────────────────────────────────────────
    public void OnForgotPasswordButtonPressed() => ForgotPassword(emailInput.text);

    public void ForgotPassword(string email)
    {
        if (string.IsNullOrEmpty(email))
        {
            ShowMessage("Please enter your email.");
            return;
        }

        auth.SendPasswordResetEmailAsync(email).ContinueWith(task =>
        {
            UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                if (task.IsCanceled || task.IsFaulted)
                {
                    ShowMessage("Reset failed: " + task.Exception.Flatten().InnerExceptions[0].Message);
                }
                else
                {
                    ShowMessage("✅ Reset email sent.");
                }
            });
        });
    }

    private string ParseFirebaseError(System.AggregateException exception)
    {
        var firebaseEx = exception.Flatten().InnerExceptions[0] as FirebaseException;
        if (firebaseEx != null)
        {
            var errorCode = (AuthError)firebaseEx.ErrorCode;
            switch (errorCode)
            {
                case AuthError.EmailAlreadyInUse: return "Email already registered.";
                case AuthError.InvalidEmail: return "Invalid email format.";
                case AuthError.WeakPassword: return "Weak password.";
                case AuthError.UserNotFound: return "User not found.";
                case AuthError.WrongPassword: return "Wrong password.";
                default: return firebaseEx.Message;
            }
        }
        return exception.Flatten().InnerExceptions[0].Message;
    }

    /*
    // ──────────────────────────────────────────────────────
    // 🏆 Save High Score — TEMPORARILY DISABLED
    // ──────────────────────────────────────────────────────
    public void SaveHighScore(int score)
    {
        if (string.IsNullOrEmpty(currentUserId)) return;

        var scoreRef = dbRef.Child("users").Child(currentUserId).Child("highscore");

        scoreRef.GetValueAsync().ContinueWith(task =>
        {
            int currentHigh = 0;
            if (task.Result.Exists && int.TryParse(task.Result.Value.ToString(), out currentHigh))
            {
                if (score > currentHigh) scoreRef.SetValueAsync(score);
            }
            else
            {
                scoreRef.SetValueAsync(score);
            }
        });
    }
    */

    // ──────────────────────────────────────────────────────
    // 🚪 Sign Out
    // ──────────────────────────────────────────────────────
    public void SignOut()
    {
        auth.SignOut();
        currentUserId = null;
        SceneManager.LoadScene("LoginSignUpPage");
        ShowMessage("Signed out successfully.");
    }

    private void SwitchToHomePage()
    {
        SceneManager.LoadScene("HomePage");
    }
}

