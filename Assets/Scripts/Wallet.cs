using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using WalletConnectSharp.Common.Utils;
using WalletConnectSharp.Core.Models.Pairing;
using WalletConnectSharp.Network.Models;
using WalletConnectSharp.Sign;
using WalletConnectSharp.Sign.Models;
using WalletConnectSharp.Sign.Models.Engine;

namespace WalletConnectSharpUnity
{
    public class Wallet
    {
        private readonly SignClientOptions _clientOptions;
        private readonly ConnectOptions _connectOptions;

        private ConnectedData _connectedData;
        private SessionStruct _session;

        public Wallet()
        {
            _clientOptions = new SignClientOptions
            {
                ProjectId = "c2ffbc3af6a72a131de05addc14b201c",
                Metadata = new Metadata
                {
                    Description = "An example dapp to showcase WalletConnectSharpv2",
                    Icons = new[] { "https://gitlab.com/uploads/-/system/project/avatar/13434887/tu3gt6ysfxq71.png" },
                    Name = "Unity Dapp",
                    Url = "https://walletconnect.com"
                },
                Storage = new PlayerPrefsStorage()
            };

            _connectOptions = new ConnectOptions
            {
                RequiredNamespaces = new RequiredNamespaces
                {
                    {
                        "eip155", new ProposedNamespace
                        {
                            Methods = new[]
                            {
                                "eth_sendTransaction",
                                "eth_sign",
                                "personal_sign",
                                "eth_signTypedData"
                                // These don't work in some wallets
                                // "eth_signTransaction",
                                // "eth_getBalance",
                            },
                            Chains = new[]
                            {
                                "eip155:1"
                            },
                            Events = new[]
                            {
                                "chainChanged",
                                "accountsChanged"
                            }
                        }
                    }
                }
            };
        }

        public static Wallet Instance { get; } = new();

        public WalletConnectSignClient Client { get; private set; }

        public SessionStruct Session
        {
            get => _session;
            private set
            {
                _session = value;
                SessionChanged?.Invoke(_session);
            }
        }

        public event Action<string> AuthRequired;
        public event Action<SessionStruct> SessionChanged;

        public async Task Init()
        {
            Client = await WalletConnectSignClient.Init(_clientOptions);
            _connectedData = await Client.Connect(_connectOptions);
            var sessions = Client.Find(_connectOptions.RequiredNamespaces);

            if (sessions.Length > 0)
                Session = sessions.First(x => x.Acknowledged ?? false);
            else
                AuthRequired?.Invoke(_connectedData.Uri);
        }

        public async Task Authenticate()
        {
            Session = await _connectedData.Approval;
        }

        public async Task<string> SendTransaction(Transaction transaction)
        {
            var request = new EthSendTransaction(transaction);

            var result = await Client.Request<EthSendTransaction, string>(Session.Topic, request);

            return result;
        }

        public class Transaction
        {
            [JsonProperty("from")] public string From { get; set; }

            [JsonProperty("to")] public string To { get; set; }

            [JsonProperty("gas", NullValueHandling = NullValueHandling.Ignore)]
            public string Gas { get; set; }

            [JsonProperty("gasPrice", NullValueHandling = NullValueHandling.Ignore)]
            public string GasPrice { get; set; }

            [JsonProperty("value")] public string Value { get; set; }

            [JsonProperty("data", NullValueHandling = NullValueHandling.Ignore)]
            public string Data { get; set; } = "0x";
        }

        [RpcMethod("eth_sendTransaction")]
        [RpcRequestOptions(Clock.ONE_MINUTE, 99997)]
        public class EthSendTransaction : List<Transaction>
        {
            public EthSendTransaction()
            {
            }

            public EthSendTransaction(params Transaction[] transactions) : base(transactions)
            {
            }
        }
    }
}