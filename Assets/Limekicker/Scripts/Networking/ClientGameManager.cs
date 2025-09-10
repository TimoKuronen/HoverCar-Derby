using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Core;
using UnityEngine;

public class ClientGameManager 
{
    public async Task InitAsync()
    {
        await UnityServices.InitializeAsync();

        AuthenticatorState authenticatorState = await AuthenticatorHandler.DoAuthentication();

        if(authenticatorState != AuthenticatorState.Authenticated)
        {
            Debug.LogError("Authentication failed. Cannot proceed with ClientGameManager initialization.");
            return;
        }
    }
}
