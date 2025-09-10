using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using UnityEngine;

public static class AuthenticatorHandler
{
    public static AuthenticatorState AuthenticatorState { get; private set; } = AuthenticatorState.NotAuthenticated;

    public static async Task<AuthenticatorState> DoAuthentication(int maxTries = 5)
    {
        if (AuthenticatorState == AuthenticatorState.Authenticated)
            return AuthenticatorState;

        AuthenticatorState = AuthenticatorState.Authenticating;

        int tries = 0;
        while (AuthenticatorState == AuthenticatorState.Authenticating && tries < maxTries)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();

            if(AuthenticationService.Instance.IsSignedIn && AuthenticationService.Instance.IsAuthorized)
            {
                AuthenticatorState = AuthenticatorState.Authenticated;
                break;
            }

            tries++;
            await Task.Delay(1000);
        }

        return AuthenticatorState;
    }
}

public enum AuthenticatorState
{
    NotAuthenticated,
    Authenticating,
    Authenticated,
    Error,
    Timeout
}