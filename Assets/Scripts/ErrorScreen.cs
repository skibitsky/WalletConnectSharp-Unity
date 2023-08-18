using TMPro;
using UnityEngine;

namespace WalletConnectSharpUnity
{
    public class ErrorScreen : MonoBehaviour
    {
        [SerializeField] private Canvas _canvas;
        
        [SerializeField] private TMP_Text _errorText;
        
        public void Show(string error)
        {
            _errorText.text = error;
            
            _canvas.enabled = true;
        }
        
        public void Hide()
        {
            _canvas.enabled = false;
        }
        
        public void OnBtnClose() => Hide();
    }
}