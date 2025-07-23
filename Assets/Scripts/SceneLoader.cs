using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    public void SwitchToLoginPage()
    {
        Debug.Log("Switching to Login scene");
        SceneManager.LoadScene("Login");
    }

    public void SwitchToRegisterPage()
    {
        Debug.Log("Switching to Register scene");
        SceneManager.LoadScene("SignUp");
    }
}
