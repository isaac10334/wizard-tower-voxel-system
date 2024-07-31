using System.Collections;
using System.Collections.Generic;
using FishNet;
using FishNet.Managing;
using UnityEngine;
using UnityEngine.UI;

namespace Mirror.Examples.Common
{
    public class CanvasNetworkManagerHUD : MonoBehaviour
    {
        [SerializeField] private GameObject startButtonsGroup;
        [SerializeField] private GameObject statusLabelsGroup;

        [SerializeField] private Button startHostButton;
        [SerializeField] private Button startServerOnlyButton;
        [SerializeField] private Button startClientButton;

        [SerializeField] private Button mainStopButton;
        [SerializeField] private Text mainStopButtonText;
        [SerializeField] private Button secondaryStopButton;
        [SerializeField] private Text statusText;

        [SerializeField] private InputField inputNetworkAddress;

        private void Start()
        {
            // Init the input field with Network Manager's network address.
            inputNetworkAddress.text = InstanceFinder.NetworkManager.TransportManager.Transport.GetClientAddress();

            RegisterListeners();

            //RegisterClientEvents();

            CheckWebGLPlayer();
        }

        private void RegisterListeners()
        {
            // Add button listeners. These buttons are already added in the inspector.
            startHostButton.onClick.AddListener(OnClickStartHostButton);
            startServerOnlyButton.onClick.AddListener(OnClickStartServerButton);
            startClientButton.onClick.AddListener(OnClickStartClientButton);
            mainStopButton.onClick.AddListener(OnClickMainStopButton);
            secondaryStopButton.onClick.AddListener(OnClickSecondaryStopButton);

            // Add input field listener to update NetworkManager's Network Address
            // when changed.
            inputNetworkAddress.onValueChanged.AddListener(delegate { OnNetworkAddressChange(); });
        }

        // Not working at the moment. Can't register events.
        /*private void RegisterClientEvents()
        {
            NetworkClient.OnConnectedEvent += OnClientConnect;
            NetworkClient.OnDisconnectedEvent += OnClientDisconnect;
        }*/

        private void CheckWebGLPlayer()
        {
            // WebGL can't be host or server.
            if (Application.platform == RuntimePlatform.WebGLPlayer)
            {
                startHostButton.interactable = false;
                startServerOnlyButton.interactable = false;
            }
        }

        private void RefreshHUD()
        {
            if (!InstanceFinder.IsClientOnlyStarted && !InstanceFinder.IsServerOnlyStarted &&
                !InstanceFinder.IsHostStarted)
            {
                StartButtons();
            }
            else
            {
                StatusLabelsAndStopButtons();
            }
        }

        private void StartButtons()
        {
            if (!InstanceFinder.IsClientStarted)
            {
                statusLabelsGroup.SetActive(false);
                startButtonsGroup.SetActive(true);
            }
            else
            {
                ShowConnectingStatus();
            }
        }

        private void StatusLabelsAndStopButtons()
        {
            startButtonsGroup.SetActive(false);
            statusLabelsGroup.SetActive(true);

            var transport = InstanceFinder.TransportManager.Transport;

            // Host
            if (InstanceFinder.IsHostStarted)
            {
                statusText.text = $"<b>Host</b>: running via {transport}";

                mainStopButtonText.text = "Stop Client";
            }
            // Server only
            else if (InstanceFinder.IsServerOnlyStarted)
            {
                statusText.text = $"<b>Server</b>: running via {transport}";

                mainStopButtonText.text = "Stop Server";
            }
            // Client only
            else if (InstanceFinder.IsClientOnlyStarted)
            {
                statusText.text =
                    $"<b>Client</b>: connected to {InstanceFinder.ClientManager.Connection.GetAddress()} via {transport}";

                mainStopButtonText.text = "Stop Client";
            }

            // Note secondary button is only used to Stop Host, and is only needed in host mode.
            secondaryStopButton.gameObject.SetActive(InstanceFinder.IsHostStarted);
        }

        private void ShowConnectingStatus()
        {
            startButtonsGroup.SetActive(false);
            statusLabelsGroup.SetActive(true);

            secondaryStopButton.gameObject.SetActive(false);

            // InstanceFinder.ClientManager.NetworkManager.

            statusText.text = "Connecting...";
            mainStopButtonText.text = "Cancel Connection Attempt";
        }

        private void OnClickStartHostButton()
        {
            InstanceFinder.ServerManager.StartConnection();
            InstanceFinder.ClientManager.StartConnection(inputNetworkAddress.text);
        }

        private void OnClickStartServerButton()
        {
            InstanceFinder.ServerManager.StartConnection();
        }

        private void OnClickStartClientButton()
        {
            InstanceFinder.ClientManager.StartConnection(inputNetworkAddress.text);
            // NetworkManager.singleton.StartClient();
            //ShowConnectingStatus();
        }

        private void OnClickMainStopButton()
        {
            if (InstanceFinder.IsClientStarted)
            {
                InstanceFinder.ClientManager.StopConnection();
            }

            if (InstanceFinder.IsServerStarted)
            {
                InstanceFinder.ServerManager.StopConnection(true);
            }
        }

        private void OnClickSecondaryStopButton()
        {
            OnClickMainStopButton();
            // NetworkManager.singleton.StopHost();
        }

        private void OnNetworkAddressChange()
        {
            // InstanceFinder.ClientManager.
            // NetworkManager.singleton.networkAddress = inputNetworkAddress.text;
        }

        private void Update()
        {
            RefreshHUD();
        }

        /* This does not work because we can't register the handler.
        void OnClientConnect() {}

        private void OnClientDisconnect()
        {
            RefreshHUD();
        }
        */

        // Do a check for the presence of a Network Manager component when
        // you first add this script to a gameobject.
        private void Reset()
        {
#if UNITY_2021_3_OR_NEWER
            if (!FindAnyObjectByType<NetworkManager>())
                Debug.LogError(
                    "This component requires a NetworkManager component to be present in the scene. Please add!");
#else
        // Deprecated in Unity 2023.1
        if (!FindObjectOfType<NetworkManager>())
            Debug.LogError("This component requires a NetworkManager component to be present in the scene. Please add!");
#endif
        }
    }
}