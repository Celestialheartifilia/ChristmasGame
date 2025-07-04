using Firebase;
using Firebase.Database;
using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class LeaderboardManager : MonoBehaviour
{
    public GameObject entryPrefab;
    public Transform contentParent;

    void OnEnable()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task =>
        {
            if (task.Result == DependencyStatus.Available)
            {
                // ✅ Set the database URL before anything else
                if (FirebaseApp.DefaultInstance.Options.DatabaseUrl == null)
                {
                    FirebaseApp.DefaultInstance.Options.DatabaseUrl =
                        new System.Uri("https://atchthepresentgame-default-rtdb.asia-southeast1.firebasedatabase.app/");
                }

                LoadLeaderboard(); // Now it's safe to call
            }
            else
            {
                Debug.LogError("❌ Firebase not ready: " + task.Result);
            }
        });
    }

    public void LoadLeaderboard()
    {
        FirebaseDatabase.DefaultInstance
            .GetReference("users")
            .OrderByChild("highscore")
            .GetValueAsync().ContinueWith(task =>
            {
                if (task.IsFaulted)
                {
                    Debug.LogError("❌ Failed to load leaderboard: " + task.Exception);
                    return;
                }

                if (task.IsCompleted)
                {
                    DataSnapshot snapshot = task.Result;
                    List<(string, int)> entries = new List<(string, int)>();

                    foreach (DataSnapshot userSnap in snapshot.Children)
                    {
                        string name = userSnap.Child("username").Value?.ToString() ?? "Unknown";
                        int score = int.TryParse(userSnap.Child("highscore").Value?.ToString(), out int s) ? s : 0;
                        entries.Add((name, score));
                    }

                    entries.Sort((a, b) => b.Item2.CompareTo(a.Item2)); // Descending

                    foreach (Transform child in contentParent)
                        Destroy(child.gameObject);

                    foreach (var (name, score) in entries)
                    {
                        GameObject entry = Instantiate(entryPrefab, contentParent);
                        entry.transform.Find("UsernameText").GetComponent<TMP_Text>().text = name;
                        entry.transform.Find("ScoreText").GetComponent<TMP_Text>().text = score.ToString();
                    }

                    Debug.Log("✅ Leaderboard loaded.");
                }
            });
    }
}
