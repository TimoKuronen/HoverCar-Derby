using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainMenu : MonoBehaviour
{
    public async void StartHost()
    {
        await HostSingleton.Instance.GameManager.StartHostAsync();
    }
}
