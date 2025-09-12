using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;

public static class AuthenticatorHandler
{
    public static AuthenticatorState AuthenticatorState { get; private set; } = AuthenticatorState.NotAuthenticated;

    public static async Task<AuthenticatorState> DoAuthentication(int maxTries = 5)
    {
        if (AuthenticatorState == AuthenticatorState.Authenticated)
        {
            return AuthenticatorState;
        }

        if (AuthenticatorState == AuthenticatorState.Authenticating)
        {
            Debug.Log("Already authenticating, waiting for result...");
            await Authenticating();
            return AuthenticatorState;
        }

        AuthenticatorState = AuthenticatorState.Authenticating;

        await SignInAnonymouslyAsync(maxTries);

        return AuthenticatorState;
    }

    private static async Task<AuthenticatorState> Authenticating()
    {
        while (AuthenticatorState == AuthenticatorState.Authenticating || AuthenticatorState == AuthenticatorState.NotAuthenticated)
        {
            await Task.Delay(200);
        }

        return AuthenticatorState;
    }

    private static async Task SignInAnonymouslyAsync(int maxTries)
    {
        int tries = 0;
        while (AuthenticatorState == AuthenticatorState.Authenticating && tries < maxTries)
        {
            try
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();

                if (AuthenticationService.Instance.IsSignedIn && AuthenticationService.Instance.IsAuthorized)
                {
                    AuthenticatorState = AuthenticatorState.Authenticated;
                    break;
                }
            }
            catch (AuthenticationException authException)
            {
                Debug.LogError($"Authentication failed: {authException.Message}");
                AuthenticatorState = AuthenticatorState.Error;
                break;
            }
            catch (RequestFailedException requestException)
            {
                Debug.LogError($"Request failed: {requestException.Message}");
                AuthenticatorState = AuthenticatorState.Error;
            }

            tries++;
            await Task.Delay(1000);
        }

        if (AuthenticatorState != AuthenticatorState.Authenticated)
        {
            AuthenticatorState = AuthenticatorState.Timeout;
            Debug.LogError("Authentication timed out after maximum retries.");
        }
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