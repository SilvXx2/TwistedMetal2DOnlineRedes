internal sealed class PhotonRoomState
{
    public bool IsLeavingRoom { get; private set; }

    public void MarkLeavingRoom()
    {
        IsLeavingRoom = true;
    }

    public void ResetRoomFlags()
    {
        IsLeavingRoom = false;
    }
}
