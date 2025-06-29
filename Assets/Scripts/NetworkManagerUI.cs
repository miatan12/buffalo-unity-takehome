using UnityEngine;
using Unity.Netcode;
using System.Collections;

/// <summary>
/// Simple network mode controller via UI. Supports host/client/server restarts.
/// </summary>
public class NetworkManagerUI : MonoBehaviour
{
    private const float RestartDelay = 0.1f;

    public void RestartHost()
    {
        if (IsNetworkActive())
            Restart(NetworkMode.Host);
        else
            StartHost();
    }

    public void RestartClient()
    {
        if (IsNetworkActive())
            Restart(NetworkMode.Client);
        else
            StartClient();
    }

    public void RestartServer()
    {
        if (IsNetworkActive())
            Restart(NetworkMode.Server);
        else
            StartServer();
    }

    private bool IsNetworkActive() =>
        NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsServer;

    private void Restart(NetworkMode mode)
    {
        NetworkManager.Singleton.Shutdown();
        StartCoroutine(RestartAfterDelay(mode));
    }

    private IEnumerator RestartAfterDelay(NetworkMode mode)
    {
        yield return new WaitForSeconds(RestartDelay);

        switch (mode)
        {
            case NetworkMode.Host: StartHost(); break;
            case NetworkMode.Client: StartClient(); break;
            case NetworkMode.Server: StartServer(); break;
        }
    }

    private void StartHost()
    {
        NetworkManager.Singleton.StartHost();
        Debug.Log("[NetworkManagerUI] Host started.");
    }

    private void StartClient()
    {
        NetworkManager.Singleton.StartClient();
        Debug.Log("[NetworkManagerUI] Client started.");
    }

    private void StartServer()
    {
        NetworkManager.Singleton.StartServer();
        Debug.Log("[NetworkManagerUI] Server started.");
    }

    private enum NetworkMode { Host, Client, Server }
}
