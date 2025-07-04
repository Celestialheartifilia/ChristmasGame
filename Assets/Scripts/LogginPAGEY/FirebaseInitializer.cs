using UnityEngine;
using Firebase;

public class FirebaseInitializer : MonoBehaviour
{
    void Awake()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task =>
        {
            if (task.Result == DependencyStatus.Available)
            {
                var app = FirebaseApp.DefaultInstance;

                // ✅ Set database URL only once
                if (app.Options.DatabaseUrl == null || app.Options.DatabaseUrl.ToString() == "")
                {
                    app.Options.DatabaseUrl = new System.Uri("https://atchthepresentgame-default-rtdb.asia-southeast1.firebasedatabase.app/");
                    Debug.Log("✅ Firebase Database URL set.");
                }
            }
            else
            {
                Debug.LogError("❌ Firebase dependency error: " + task.Result);
            }
        });
    }
}
