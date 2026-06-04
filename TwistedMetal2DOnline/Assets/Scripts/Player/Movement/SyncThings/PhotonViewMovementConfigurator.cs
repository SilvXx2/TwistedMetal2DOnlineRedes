using Photon.Pun;
using UnityEngine;

internal static class PhotonViewMovementConfigurator
{
    public static void Configure(PhotonView photonView, IPunObservable observable)
    {
        if (photonView == null || observable == null)
        {
            return;
        }

        photonView.observableSearch = PhotonView.ObservableSearch.AutoFindAll;
        photonView.FindObservables(true);

        Component observableComponent = observable as Component;
        if (observableComponent != null && !photonView.ObservedComponents.Contains(observableComponent))
        {
            photonView.ObservedComponents.Add(observableComponent);
        }

        if (photonView.Synchronization == ViewSynchronization.Off)
        {
            photonView.Synchronization = ViewSynchronization.UnreliableOnChange;
        }
    }
}
