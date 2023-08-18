using System;
using Newtonsoft.Json;
using TMPro;
using UnityEngine;
using WalletConnectSharp.Sign.Models;

namespace WalletConnectSharpUnity
{
    public class AccountScreen : MonoBehaviour
    {
        [SerializeField] private Canvas _canvas;
        
        [SerializeField] private TMP_Text _expiryText;
        [SerializeField] private TMP_Text _sessionJsonText;
        
        [SerializeField] private ErrorScreen _errorScreen;
        
        public void Show(SessionStruct session)
        {
            _expiryText.text = UnixTimeStampToDateTime(session.Expiry ?? 0).ToString("dd.MM.yyyy HH:mm:ss");

            var sessionJson = JsonConvert.SerializeObject(session, Formatting.Indented);
            _sessionJsonText.text = sessionJson;
            
            _canvas.enabled = true;
        }

        public void Hide()
        {
            _expiryText.text = string.Empty;
            _sessionJsonText.text = string.Empty;
            _canvas.enabled = false;
        }
        
        public async void OnBtnSendTransaction()
        {
            try
            {
                var tx = new Wallet.Transaction
                {
                    From = "0x0000000000000000000000000000000000000000",
                    To = "0x0000000000000000000000000000000000000000",
                    Value = "0"
                };
                
                await Wallet.Instance.SendTransaction(tx);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                _errorScreen.Show(e.Message);
            }
        }
        
        private static DateTime UnixTimeStampToDateTime( double unixTimeStamp )
        {
            var dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dateTime = dateTime.AddSeconds( unixTimeStamp ).ToLocalTime();
            return dateTime;
        }
    }
}