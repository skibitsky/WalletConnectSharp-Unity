using System.Collections;
using System.Net;
using UnityEngine;
using UnityEngine.UI;
using ZXing;
using ZXing.QrCode;

namespace WalletConnectSharpUnity
{
    public class AuthScreen : MonoBehaviour
    {
        private const int Width = 1024;
        private const int Height = 1024;
        [SerializeField] private Canvas _canvas;
        [SerializeField] private RawImage _image;
        [SerializeField] private Button _btnLedgerLive;

        public void Show(string uri)
        {
            StartCoroutine(ShowRoutine(uri));
        }

        private IEnumerator ShowRoutine(string uri)
        {
            _btnLedgerLive.onClick.AddListener(() =>
            {
                // NB! This had been tested on macOS only
                var encodedUri = WebUtility.UrlEncode(uri);
                var ledgerUrl = $"ledgerlive://wc?uri={encodedUri}";
                Application.OpenURL(ledgerUrl);
                Debug.Log(ledgerUrl);
            });

            var pixels = Encode(uri, Width, Height);

            var texture = new Texture2D(Width, Height);
            texture.SetPixels32(pixels);
            texture.Apply();

            _image.texture = texture;

            // There is a delay between the moment when the texture is applied and the moment when it is actually displayed.
            // This is a simple workaround.
            yield return new WaitForSeconds(1);

            _canvas.enabled = true;
        }

        public void Hide()
        {
            _btnLedgerLive.onClick.RemoveAllListeners();
            _canvas.enabled = false;
        }

        private static Color32[] Encode(string textForEncoding, int width, int height)
        {
            var qrCodeEncodingOptions = new QrCodeEncodingOptions
            {
                Height = height,
                Width = width
            };

            var writer = new BarcodeWriter
            {
                Format = BarcodeFormat.QR_CODE,
                Options = qrCodeEncodingOptions
            };

            return writer.Write(textForEncoding);
        }
    }
}