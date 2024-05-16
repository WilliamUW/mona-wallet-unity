using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using WalletConnectSharp.Sign.Models;
using WalletConnectSharp.Sign.Models.Engine;
using WalletConnectUnity.Core;
using WalletConnectUnity.Modal;
using Monaverse.Api;
using Monaverse.Core;
using WalletConnectUnity.Core.Evm;
using UnityEngine.EventSystems;

namespace Monaverse.UI
{
    public class MonaWalletConnectUI : MonoBehaviour
    {
        [SerializeField] private Button _walletConnectButton;
        [Space] [SerializeField] private GameObject _walletConnectModalPrefab;
        [Space] [SerializeField] private GameObject _monaManagerPrefab;

        private bool _walletConnected;
        private bool _walletAuthorized;
        private bool _walletAuthorizing;
        private bool _modalInitialized;

        public static MonaWalletConnectUI Instance { get; private set; }

        private float _initWCTimeout = 30.0f;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }

            MonaApi.Init("fakeAppId");
        }

        private void Start()
        {
            if (FindObjectOfType<EventSystem>() == null)
            {
                GameObject eventSystemObject = new GameObject("EventSystem");
                eventSystemObject.AddComponent<EventSystem>();
                eventSystemObject.AddComponent<StandaloneInputModule>();
            }

            StartCoroutine(InitMonaverseManager(MonaverseManagerInitialized));
        }

        private IEnumerator InitMonaverseManager(Action callback)
        {
            if (MonaverseManager.Instance == null)
            {
                Instantiate(_monaManagerPrefab, Vector3.zero, Quaternion.identity);
                while (MonaverseManager.Instance == null)
                    yield return new WaitForSeconds(0.5f);
            }

            callback?.Invoke();
        }

        private async void MonaverseManagerInitialized()
        {
            _walletConnected = await MonaverseManager.Instance.SDK.IsWalletConnected();

            if (_walletConnected)
            {
                var message = "Wallet Already connected.";
                Debug.Log(message);
                StatusWindow.Instance.Show(message);
            }

            _walletAuthorized = MonaverseManager.Instance.SDK.IsWalletAuthorized();

            if (_walletAuthorized)
            {
                var message = "Wallet Already authorized.";
                Debug.Log(message);
                StatusWindow.Instance.Show(message);
            }

            WalletConnectModal.Ready += (_, args) => { Init(args.SessionResumed); };
        }

        private void Init(bool connected)
        {
            if (_modalInitialized)
                return;

            _modalInitialized = true;

            WalletConnect.Instance.ActiveSessionChanged += (_, @struct) =>
            {
                if (string.IsNullOrEmpty(@struct.Topic))
                    return;
                
                _walletConnectButton.interactable = false;

                Debug.Log($"Session connected. Topic: {@struct.Topic}");
                StatusWindow.Instance.Show("Wallet connected.");

                AuthorizeWallet();
            };

            WalletConnect.Instance.SessionConnected += (_, @struct) =>
            {
                if (string.IsNullOrEmpty(@struct.Topic))
                    return;

                _walletConnectButton.interactable = false;

                Debug.Log($"Session connected. Topic: {@struct.Topic}");
                StatusWindow.Instance.Show("Wallet connected.");

                AuthorizeWallet();
            };

            WalletConnect.Instance.SessionDisconnected += (_, _) =>
            {
                Debug.Log("Wallet disconnected.");
                StatusWindow.Instance.Show("Wallet disconnected.");

                _walletConnectButton.interactable = true;
            };
        }

        public void OnWalletConnectButton() => StartCoroutine(OpenWalletConnect());

        private IEnumerator OpenWalletConnect()
        {
            if (!WalletConnectModal.IsReady)
            {
                Instantiate(_walletConnectModalPrefab, Vector3.zero, Quaternion.identity);

                var startTime = Time.time;

                do
                {
                    yield return new WaitForSeconds(0.5f);

                    if(Time.time - startTime > _initWCTimeout)
                    {
                        Debug.LogError("WalletConnect failed to instantiate.");
                        NotificationManager.Instance.ShowNotification("ERROR", "WalletConnect failed to instantiate.", NotificationManager.Severity.Error);
                        yield break;
                    }

                } while ((!WalletConnectModal.IsReady));
            }

            var options = new WalletConnectModalOptions
            {
                ConnectOptions = BuildConnectOptions()
            };

            WalletConnectModal.Open(options);
        }

        private ConnectOptions BuildConnectOptions()
        {
            // Using optional namespaces. Wallet will approve only chains it supports.
            var optionalNamespaces = new Dictionary<string, ProposedNamespace>();
            var methods = new[]
            {
                "wallet_switchEthereumChain",
                "wallet_addEthereumChain",
                "eth_sendTransaction",
                "personal_sign"
            };

            var events = new[]
            {
                "chainChanged", "accountsChanged"
            };

            var chainIds = new[] { ChainConstants.Chains.Ethereum.ChainId };

            optionalNamespaces.Add(ChainConstants.Namespaces.Evm, new ProposedNamespace
            {
                Chains = chainIds,
                Events = events,
                Methods = methods
            });

            if (optionalNamespaces.Count == 0)
                throw new InvalidOperationException("No chains selected");

            return new ConnectOptions
            {
                OptionalNamespaces = optionalNamespaces
            };
        }

        public async void AuthorizeWallet()
        {
            if (_walletAuthorized || _walletAuthorizing)
                return;

            _walletAuthorizing = true;

            //var session = WalletConnect.Instance.ActiveSession;
            //var address = WalletConnect.Instance.ActiveSession.CurrentAddress(session.Namespaces.Keys.FirstOrDefault())
            //    .Address;

            var address = await MonaverseManager.Instance.SDK.ActiveWallet.GetAddress();

            var validateWalletAddressResponse = await MonaApi.ApiClient.Auth.ValidateWallet(address);
            Debug.Log("ValidateWallet Done!\nResponse: " + validateWalletAddressResponse);
            StatusWindow.Instance.Show("ValidateWallet Done!");

            if (!validateWalletAddressResponse.IsSuccess)
            {
                NotificationManager.Instance.ShowNotification("ERROR",
                    validateWalletAddressResponse.Message,
                    NotificationManager.Severity.Error);
                _walletAuthorizing = false;
                return;
            }

            if (!validateWalletAddressResponse.Data.IsValid)
            {
                if(validateWalletAddressResponse.Data.ErrorMessage == null)
                    UnauthorizedPopup.Instance.Show();
                else
                    NotificationManager.Instance.ShowNotification("ERROR",
                        validateWalletAddressResponse.Data.ErrorMessage,
                        NotificationManager.Severity.Error);
                _walletAuthorizing = false;
                return;
            }

            //var data = new PersonalSign(validateWalletAddressResponse.Data.SiweMessage, address);
            //var signature = await WalletConnect.Instance.RequestAsync<PersonalSign, string>(data);
            var signature = await MonaverseManager.Instance.SDK.ActiveWallet.SignMessage(validateWalletAddressResponse.Data.SiweMessage);

            Debug.Log("Wallet Connect Signature: " + signature);
            StatusWindow.Instance.Show("Authorizing with Mona...");

            var authorizeResponse = await MonaApi.ApiClient.Auth.Authorize(signature, validateWalletAddressResponse.Data.SiweMessage);
            Debug.Log("Authorize Done!\nResponse: " + authorizeResponse);
            StatusWindow.Instance.Show("Authorize Done!");

            if (!authorizeResponse.IsSuccess)
            {
                NotificationManager.Instance.ShowNotification("ERROR",
                    "Authorization Failed",
                    NotificationManager.Severity.Error);
                _walletAuthorizing = false;
                return;
            }

            StatusWindow.Instance.Show("Authorization Successful!");

            var getCollectiblesResult = await MonaApi.ApiClient.Collectibles.GetWalletCollectibles();
            Debug.Log("Collectibles: " + getCollectiblesResult);
            if (!getCollectiblesResult.IsSuccess)
            {
                NotificationManager.Instance.ShowNotification("ERROR",
                    "GetCollectibles Failed: " + getCollectiblesResult.Message,
                    NotificationManager.Severity.Error);
                _walletAuthorizing = false;
                return;
            }

            StatusWindow.Instance.Show("Pulled Collectibles. Total Count: " + getCollectiblesResult.Data.TotalCount);
            _walletAuthorizing = false;
        }
    }
}