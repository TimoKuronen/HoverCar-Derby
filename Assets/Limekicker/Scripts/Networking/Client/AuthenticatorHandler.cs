using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;

/// <summary>
/// Unity Services anonymous sign-in with retry and per-virtual-player profile isolation.
/// </summary>
public static class AuthenticatorHandler
{
    public static AuthenticatorState AuthenticatorState { get; private set; } = AuthenticatorState.NotAuthenticated;

    /// <summary>Performs anonymous authentication with Unity Services. Retries up to maxTries times.</summary>
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

        ApplyVirtualPlayerProfileIfNeeded();

        await SignInAnonymouslyAsync(maxTries);

        return AuthenticatorState;
    }

    /// <summary>
    /// Multiplayer Play Mode virtual players share the main editor's Company/Product identity and with it
    /// Unity Authentication's cached anonymous session; a per-player profile gives each its own identity.
    /// </summary>
    private static void ApplyVirtualPlayerProfileIfNeeded()
    {
        if (!DevPlayerIdentity.IsVirtualPlayer)
        {
            return;
        }

        try
        {
            AuthenticationService.Instance.SwitchProfile(DevPlayerIdentity.GetAuthProfileName());
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[AuthenticatorHandler] Failed to switch virtual player auth profile: {e.Message}");
        }
    }

    /// <summary>Waits for ongoing authentication to complete.</summary>
    private static async Task<AuthenticatorState> Authenticating()
    {
        while (AuthenticatorState == AuthenticatorState.Authenticating || AuthenticatorState == AuthenticatorState.NotAuthenticated)
        {
            await Task.Delay(200);
        }

        return AuthenticatorState;
    }

    /// <summary>Attempts anonymous sign-in with retry logic.</summary>
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

/// <summary>
/// Unity Authentication sign-in progress and outcome.
/// </summary>
public enum AuthenticatorState
{
    NotAuthenticated,
    Authenticating,
    Authenticated,
    Error,
    Timeout
}