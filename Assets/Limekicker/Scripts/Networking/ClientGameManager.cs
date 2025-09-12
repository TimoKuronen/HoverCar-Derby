using System;
using System.Threading.Tasks;
using Unity.Services.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ClientGameManager 
{
    private const string MenuSceneName = "MainMenu";

    public async Task<bool> InitAsync()
    {
        await UnityServices.InitializeAsync();

        AuthenticatorState authenticatorState = await AuthenticatorHandler.DoAuthentication();

        if(authenticatorState != AuthenticatorState.Authenticated)
        {
            Debug.LogError("Authentication failed. Cannot proceed with ClientGameManager initialization.");
            return false;
        }

        return true;
    }

    public void GoToMenu()
    {
        SceneManager.LoadScene(MenuSceneName);
    }

    internal async Task StartHostAsync()
    {
        throw new NotImplementedException();
    }
}
