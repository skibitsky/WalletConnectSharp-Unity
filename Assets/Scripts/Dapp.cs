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
        [SerializeField] private ErrorScreen _errorScreen;
        
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
                _errorScreen.Show("Initialization timed out.");
        }
        
        private async void OnAuthRequired(string uri)
        {
            try
            {
                _authScreen.Show(uri);
                await Wallet.Authenticate();
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
            _errorScreen.Show(error);
        }
    }
}