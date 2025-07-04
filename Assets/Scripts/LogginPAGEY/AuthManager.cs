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

    void Start()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task =>
        {
            if (task.Result == DependencyStatus.Available)
            {
                // ✅ Set the database URL BEFORE using FirebaseDatabase
                FirebaseApp.DefaultInstance.Options.DatabaseUrl =
                    new System.Uri("https://atchthepresentgame-default-rtdb.asia-southeast1.firebasedatabase.app/");

                auth = FirebaseAuth.DefaultInstance;
                dbRef = FirebaseDatabase.DefaultInstance.RootReference;

                Debug.Log("✅ Firebase is ready.");

            }
            else
            {
                UnityMainThreadDispatcher.Instance().Enqueue(() =>
                {
                    ShowMessage("Firebase not available.");
                });
                Debug.LogError("❌ Firebase dependency error: " + task.Result.ToString());
            }
        });
    }



    private void ShowMessage(string message)
    {
        Debug.Log("ShowMessage called with: " + message);
        messageText.text = message;
        StopAllCoroutines();  // stop previous clear message coroutines
        StartCoroutine(ClearMessageAfterDelay(3f));
    }
    private IEnumerator ClearMessageAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        messageText.text = "";  // clears the message text
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.M))
        {
            ShowMessage("✅ Test message visible");
        }
    }

    //register error handling for Register

    public void SignUp()
    {
        Debug.Log("📩 SignUp called.");

        string email = emailInput.text;
        string password = passwordInput.text;
        string username = usernameInput.text;

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
                string errorMsg = "Signup failed.";

                if (task.Exception != null)
                {
                    var firebaseEx = task.Exception.Flatten().InnerExceptions[0] as Firebase.FirebaseException;
                    if (firebaseEx != null)
                    {
                        var errorCode = (Firebase.Auth.AuthError)firebaseEx.ErrorCode;
                        switch (errorCode)
                        {
                            case Firebase.Auth.AuthError.EmailAlreadyInUse:
                                errorMsg = "This email is already registered.";
                                break;
                            case Firebase.Auth.AuthError.InvalidEmail:
                                errorMsg = "Invalid email format.";
                                break;
                            case Firebase.Auth.AuthError.WeakPassword:
                                errorMsg = "Password is too weak.";
                                break;
                            default:
                                errorMsg = firebaseEx.Message;
                                break;
                        }
                    }
                }

                // Back to main thread
                Task.Factory.StartNew(() => ShowMessage(errorMsg),
                    System.Threading.CancellationToken.None,
                    TaskCreationOptions.None,
                    TaskScheduler.FromCurrentSynchronizationContext());

                return;
            }

            FirebaseUser newUser = task.Result.User;
            currentUserId = newUser.UserId;

            Debug.Log("✅ User created. Saving to database...");
            SaveUsernameToDatabase(newUser.UserId, username);
        });
    }


    private void SaveUsernameToDatabase(string userId, string username)
    {
        var userData = new Dictionary<string, object>
    {
        { "username", username },
        { "email", emailInput.text }
    };

        dbRef.Child("users").Child(userId).SetValueAsync(userData).ContinueWith(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
                Debug.LogError("Failed to save user data: " + task.Exception);
            else
                Debug.Log("User data saved successfully.");
        });
    }



    //Login error handler

    public void Login()
    {
        string email = emailInput.text;
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
                    string errorMsg = "Login failed.";
                    if (task.Exception != null)
                    {
                        var firebaseEx = task.Exception.Flatten().InnerExceptions[0] as FirebaseException;
                        if (firebaseEx != null)
                        {
                            var authError = (AuthError)firebaseEx.ErrorCode;
                            switch (authError)
                            {
                                case AuthError.WrongPassword:
                                    errorMsg = "Incorrect password.";
                                    break;
                                case AuthError.UserNotFound:
                                    errorMsg = "User not found.";
                                    break;
                                case AuthError.InvalidEmail:
                                    errorMsg = "Invalid email address.";
                                    break;
                                default:
                                    errorMsg = firebaseEx.Message;
                                    break;
                            }
                        }
                        else
                        {
                            errorMsg = task.Exception.Flatten().InnerExceptions[0].Message;
                        }
                    }

                    ShowMessage(errorMsg);
                    Debug.LogError(task.Exception);
                    return;
                }

                FirebaseUser user = task.Result.User;
                currentUserId = user.UserId;
                LoadUsername(user.UserId);
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
                    string username = task.Result.Value.ToString();
                    SwitchToHomePage();
                }
                else
                {
                    ShowMessage("Login succeeded, but username not found.");
                }
            });
        });
    }

    public void OnForgotPasswordButtonPressed()
    {
        ForgotPassword(emailInput.text);
    }

    public void ForgotPassword(string email)
    {
        if (string.IsNullOrEmpty(email))
        {
            ShowMessage("Please enter your email to reset password.");
            return;
        }

        auth.SendPasswordResetEmailAsync(email).ContinueWith(task =>
        {
            if (task.IsCanceled || task.IsFaulted)
            {
                var errorMsg = task.Exception.Flatten().InnerExceptions[0].Message;
                Debug.LogError("Reset Error: " + errorMsg);
                UnityMainThreadDispatcher.Instance().Enqueue(() =>
                {
                    ShowMessage("Reset failed: " + errorMsg);
                });
                return;
            }

            UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                ShowMessage("✅ Reset email sent! Please check your inbox.");
                Debug.Log("Password reset email sent to: " + email);
            });
        });
    }


    private void SwitchToHomePage()
    {
        SceneManager.LoadScene("HomePage");
    }

    public void SaveHighScore(int score)
    {
        if (string.IsNullOrEmpty(currentUserId)) return;

        var userHighscoreRef = FirebaseDatabase.DefaultInstance
            .RootReference
            .Child("users")
            .Child(currentUserId)
            .Child("highscore");

        userHighscoreRef.GetValueAsync().ContinueWith(task =>
        {
            if (task.IsCompleted)
            {
                int currentHigh = 0;
                if (task.Result.Exists && int.TryParse(task.Result.Value.ToString(), out currentHigh))
                {
                    if (score > currentHigh)
                    {
                        userHighscoreRef.SetValueAsync(score);
                        Debug.Log("✅ New high score saved: " + score);
                    }
                    else
                    {
                        Debug.Log("ℹ️ Score not higher than high score. Not updated.");
                    }
                }
                else
                {
                    // No high score yet, save it
                    userHighscoreRef.SetValueAsync(score);
                    Debug.Log("✅ First high score saved: " + score);
                }
            }
        });
    }


    public void SignOut()
    {
        auth.SignOut();
        currentUserId = null;

        SceneManager.LoadScene("LoginSignUpPage");

        ShowMessage("Signed out successfully!");
        Debug.Log("✅ User signed out.");
    }


    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // optional if you want to keep it between scenes
        }
        else
        {
            Destroy(gameObject);
        }
    }

}


