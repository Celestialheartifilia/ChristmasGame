using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;


public class HomePageUI : MonoBehaviour
{
    public TMP_Text welcomeText;

    void Start()
    {
        welcomeText.text = "Welcome, " + AuthManager.currentUsername + "!";
    }
}
