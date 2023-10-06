using System;
using System.Threading.Tasks;
using UnityEngine;
using WalletConnectSharp.Sign.Models;

namespace WalletConnectSharpUnity
{
    public class Dapp : MonoBehaviour
    {
        [SerializeField] private AuthScreen _authScreen;
        [SerializeField] private AccountScreen _accountScreen;
        [SerializeField] private NotificationScreen _notificationScreen;

        private static Wallet Wallet => Wallet.Instance;

        private async void Start()
        {
            Wallet.AuthRequired += OnAuthRequired;
            Wallet.SessionChanged += OnSessionChanged;

            var timeout = TimeSpan.FromSeconds(5);
            var initTask = Wallet.Init();

            if (await Task.WhenAny(initTask, Task.Delay(timeout)) == initTask)
                await initTask;
            else
                _notificationScreen.Show("Initialization timed out.");
        }

        public async void ClearStorage()
        {
            await Wallet.Instance.Client.Options.Storage.Clear();
            Debug.Log("Storage cleared", this);
        }

        private async void OnAuthRequired(string uri)
        {
            try
            {
                _accountScreen.Hide();
                _authScreen.Show(uri);
                await Wallet.AuthenticateAsync();
            }
            catch (Exception e)
            {
                ShowError(e.Message);
            }
            finally
            {
                _authScreen.Hide();
            }
        }

        private void OnSessionChanged(SessionStruct session)
        {
            _authScreen.Hide();
            _accountScreen.Show(session);
        }

        private void ShowError(string error)
        {
            Debug.LogError(error, this);
            _notificationScreen.Show(error);
        }
    }
}