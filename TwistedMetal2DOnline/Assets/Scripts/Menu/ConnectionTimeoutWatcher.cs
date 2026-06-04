using Photon.Pun;


internal sealed class ConnectionTimeoutWatcher
{
    private readonly float timeoutSeconds;

    private float startTime;
    private bool isWatching;
    private bool hasTimedOut;

    public ConnectionTimeoutWatcher(float timeoutSeconds)
    {
        this.timeoutSeconds = timeoutSeconds;
    }


    public void Start(float currentTime)
    {
        startTime = currentTime;
        isWatching = true;
        hasTimedOut = false;
    }


    public void Stop()
    {
        isWatching = false;
    }

    public bool CheckTimeout(float currentTime)
    {
        if (!isWatching || hasTimedOut)
        {
            return false;
        }


        if (PhotonNetwork.IsConnectedAndReady)
        {
            Stop();
            return false;
        }

        if (currentTime - startTime < timeoutSeconds)
        {
            return false;
        }

        hasTimedOut = true;
        isWatching = false;
        return true;
    }

    public bool IsWatching => isWatching;
}
