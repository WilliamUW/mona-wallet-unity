using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Monaverse.Core;
using Monaverse.Redcode.Awaiting;
using Monaverse.Wallets.Common;
using UnityEngine;
using WalletConnectSharp.Common.Model.Errors;
using WalletConnectSharp.Sign.Models;
using WalletConnectUnity.Core;
using WalletConnectUnity.Core.Evm;

namespace Monaverse.Wallets
{
    public sealed class MonaWalletConnect : IMonaWallet
    {
        private readonly string _walletConnectProjectId;
        private KeyValuePair<string, Namespace> _namespace;

        public MonaWalletConnect(string walletConnectProjectId)
        {
            _walletConnectProjectId = walletConnectProjectId;
        }

        public async Task<string> Connect(MonaWalletConnection monaWalletConnection)
        {
            try
            {
                if (MonaWalletConnectUI.Instance == null)
                {
                    GameObject.Instantiate(MonaverseManager.Instance.WalletConnectPrefab);
                    await new WaitForSeconds(0.5f);
                }

                await MonaWalletConnectUI.Instance.Connect(monaWalletConnection.ChainId);
                _namespace = WalletConnect.Instance.ActiveSession.Namespaces.First();

                return await GetAddress();
            }
            catch (WalletConnectException walletConnectException)
            {
                MonaDebug.LogException(walletConnectException);
                //Forced disconnect to avoid lingering sessions
                await Disconnect();
                throw;
            }
            catch (Exception exception)
            {
                MonaDebug.LogException(exception);
                throw;
            }
        }

        public async Task<bool> Disconnect()
        {
            try
            {
                await WalletConnect.Instance.DisconnectAsync();
                return true;
            }
            catch (Exception e)
            {
                MonaDebug.LogWarning($"Error disconnecting WalletConnect: {e.Message}");
                return false;
            }
        }

        public Task<string> GetAddress()
        {
            var ethAccs = new[] { WalletConnect.Instance.ActiveSession.CurrentAddress(_namespace.Key).Address };
            var addy = ethAccs[0];
            if (addy != null)
                addy = addy.ToChecksumAddress();
            return Task.FromResult(addy);
        }

        public async Task<string> SignMessage(string message)
        {
            try
            {
                var address = await GetAddress();
                var data = new PersonalSign(message, address);
                var signature = await WalletConnect.Instance.RequestAsync<PersonalSign, string>(data);
                return signature;
            }
            catch (WalletConnectException walletConnectException)
            {
                MonaDebug.LogError("WalletConnect error signing message: " + walletConnectException.Message);
                //Forced disconnect to avoid lingering sessions
                await Disconnect();
                throw;
            }
            catch (Exception exception)
            {
                MonaDebug.LogError("Error signing message: " + exception.Message);
                throw;
            }
        }

        public Task<bool> IsConnected()
        {
            return Task.FromResult(WalletConnect.Instance.IsConnected);
        }

        public MonaWalletProvider GetProvider() => MonaWalletProvider.WalletConnect;
    }
}