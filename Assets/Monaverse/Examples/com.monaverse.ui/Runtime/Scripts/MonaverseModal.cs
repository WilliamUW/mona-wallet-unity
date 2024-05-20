using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;
using Monaverse.Api;
using Monaverse.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using WalletConnectSharp.Sign.Models;
using WalletConnectSharp.Sign.Models.Engine;
using WalletConnectUnity.Core;
using ZXing;
using ZXing.QrCode;

namespace Monaverse.UI
{
    public class MonaverseModal : MonoBehaviour
    {
        [SerializeField] private Canvas _providerSelectionCanvas;
        [SerializeField] private GameObject _walletQRView;
        [SerializeField] private GameObject _walletDisconnectView;
        [SerializeField] private GameObject _walletConnectModalPrefab;
        [SerializeField] private GameObject _monaManagerPrefab;

        private bool _walletConnected;
        private bool _walletAuthorizing;

        public static MonaverseModal Instance { get; private set; }

        private string[] SupportedMethods = new[] { "eth_sendTransaction", "personal_sign", "eth_signTypedData" };

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
            DontDestroyOnLoad(this.gameObject);

            if (FindObjectOfType<EventSystem>() == null)
            {
                GameObject eventSystemObject = new GameObject("EventSystem");
                eventSystemObject.AddComponent<EventSystem>();
                eventSystemObject.AddComponent<StandaloneInputModule>();
            }

            StartCoroutine(InitMonaverseManager(MonaverseManagerInitialized));
        }

        public static bool OpenModal()
        {
            if(Instance == null)
            {
                Debug.LogError("Mona Wallet Modal is missing from the scene.");
                return false;
            }

            if(Instance._providerSelectionCanvas == null)
            {
                Debug.LogError("Wallet provider selection canvas is null.");
                return false;
            }

            Instance._providerSelectionCanvas.enabled = true;

            return true;
        }

        public static bool CloseModal()
        {
            if (Instance == null)
            {
                Debug.LogError("Mona Wallet Modal is missing from the scene.");
                return false;
            }

            if (Instance._providerSelectionCanvas == null)
            {
                Debug.LogError("Wallet provider selection canvas is null.");
                return false;
            }

            Instance._providerSelectionCanvas.enabled = false;

            return true;
        }

        private IEnumerator InitMonaverseManager(Action callback)
        {
            if (MonaverseManager.Instance == null)
            {
                Instantiate(_monaManagerPrefab);
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

            if (MonaverseManager.Instance.SDK.IsWalletAuthorized())
            {
                var message = "Wallet Already authorized.";
                Debug.Log(message);
                StatusWindow.Instance.Show(message);
            }
        }

        public void OnWalletConnectButton() => OpenWalletConnect();

        public async void OnWalletDisonnectButton()
        {
            await MonaverseManager.Instance.SDK.Disconnect();
            MonaverseManager.Instance.SDK.ApiClient.ClearSession();
            _walletConnected = false;

            if (_walletDisconnectView != null)
                _walletDisconnectView.SetActive(false);

            StatusWindow.Instance.Show("Wallet Disconnected");
        }

        public void OnWalletQRCancelButton()
        {
            if (_walletQRView != null)
                _walletQRView.SetActive(false);
        }

        private async void OpenWalletConnect()
        {
            UnityEngine.Vector2Int qrSize = new(800,800);

            if (_walletQRView != null)
                _walletQRView.SetActive(true);

            var monaConnectionOpts = new MonaWalletConnection()
            {
                ChainId = 1,
                MonaWalletProvider = MonaWalletProvider.WalletConnect
            };

            var walletAddress = MonaverseManager.Instance.SDK.ConnectWallet(monaConnectionOpts);

            var connectOpts = BuildConnectOptions(monaConnectionOpts.ChainId);

            // REQUIRED TO GET QR URI

            if (WalletConnect.Instance.SignClient == null)
                await WalletConnect.Instance.InitializeAsync();

            var connectedData = await WalletConnect.Instance.ConnectAsync(connectOpts);

            // REQUIRED TO GET QR URI

            var walletConnect = new GameObject("WalletConnect");
            var canvas = walletConnect.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var QRCode = new GameObject("QRCode");
            var QRRawImage = QRCode.AddComponent<RawImage>();

            QRCode.transform.SetParent(walletConnect.transform);
            var color32 = EncodeToQR(connectedData.Uri, qrSize.x, qrSize.y);
            var texture2D = new Texture2D(qrSize.x, qrSize.y);
            texture2D.SetPixels32(color32);
            texture2D.Apply();
            QRRawImage.texture = texture2D;
            QRRawImage.texture.filterMode = FilterMode.Point;

            var wcRectTransform = walletConnect.GetComponent<RectTransform>();
            wcRectTransform.anchoredPosition = UnityEngine.Vector2.zero;

            var qrRectTransform = QRCode.GetComponent<RectTransform>();
            qrRectTransform.anchoredPosition = UnityEngine.Vector2.zero;
            qrRectTransform.anchoredPosition = new UnityEngine.Vector2(0.0f, 120.0f);
            qrRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, qrSize.x);
            qrRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, qrSize.y);

            await connectedData.Approval;

            UnityEngine.Object.Destroy(walletConnect);

            if (_walletQRView != null)
                _walletQRView.SetActive(false);

            if (_walletDisconnectView != null)
                _walletDisconnectView.SetActive(true);

            await AuthorizeWallet();

            RequestCollectibles();
        }

        private ConnectOptions BuildConnectOptions(BigInteger chainId)
        {
            var requiredNamespaces = new RequiredNamespaces
            {
                {
                    "eip155", new ProposedNamespace
                    {
                        Methods = SupportedMethods,
                        Chains = new[]
                        {
                            $"eip155:{chainId}"
                        },
                        Events = new[]
                        {
                            "chainChanged",
                            "accountsChanged"
                        }
                    }
                }
            };

            return new ConnectOptions
            {
                RequiredNamespaces = requiredNamespaces,
            };
        }

        public virtual Color32[] EncodeToQR(string textForEncoding, int width, int height)
        {
            var writer = new BarcodeWriter
            {
                Format = BarcodeFormat.QR_CODE,
                Options = new QrCodeEncodingOptions { Height = height, Width = width }
            };
            return writer.Write(textForEncoding);
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

        public async Task<bool> AuthorizeWallet()
        {
            if (MonaverseManager.Instance.SDK.IsWalletAuthorized())
            {
                StatusWindow.Instance.Show("Authorized!");
                return await Task.FromResult(true);
            }

            if (_walletAuthorizing)
                return await Task.FromResult(true);

            _walletAuthorizing = true;

            try
            {
                if (!await MonaverseManager.Instance.SDK.IsWalletConnected())
                {
                    await MonaverseManager.Instance.SDK.ConnectWallet();
                    _walletConnected = await MonaverseManager.Instance.SDK.IsWalletConnected();
                }

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
                    return await Task.FromResult(false);
                }

                if (!validateWalletAddressResponse.Data.IsValid)
                {
                    if (validateWalletAddressResponse.Data.ErrorMessage == null)
                        UnauthorizedPopup.Instance.Show();
                    else
                        NotificationManager.Instance.ShowNotification("ERROR",
                            validateWalletAddressResponse.Data.ErrorMessage,
                            NotificationManager.Severity.Error);
                    _walletAuthorizing = false;
                    return await Task.FromResult(false);
                }

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
                    return await Task.FromResult(false);
                }

                StatusWindow.Instance.Show("Authorization Successful!");

                return await Task.FromResult(true);

            } catch(Exception ex)
            {
                Debug.LogError(ex);
                _walletAuthorizing = false;

                return await Task.FromResult(true);
            }
        }

        public async void RequestCollectibles()
        {
            if (!MonaverseManager.Instance.SDK.IsWalletAuthorized())
            {
                StatusWindow.Instance.Show("Authorize before requsting collectibles");
                return;
            }

            try
            {
                var getCollectiblesResult = await MonaApi.ApiClient.Collectibles.GetWalletCollectibles();
                Debug.Log("Collectibles: " + getCollectiblesResult);
                if (!getCollectiblesResult.IsSuccess)
                {
                    NotificationManager.Instance.ShowNotification("ERROR",
                        "GetCollectibles Failed: " + getCollectiblesResult.Message,
                        NotificationManager.Severity.Error);
                    return;
                }

                StatusWindow.Instance.Show("Pulled Collectibles. Total Count: " + getCollectiblesResult.Data.TotalCount);

            }
            catch (Exception ex)
            {
                Debug.LogError(ex);
            }
        }
    }
}