using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using WalletConnectSharp.Common.Logging;
using WalletConnectSharp.Common.Utils;
using WalletConnectSharp.Core;
using WalletConnectSharp.Core.Models.Pairing.Methods;
using WalletConnectSharp.Network.Models;
using WalletConnectSharp.Sign;
using WalletConnectSharp.Sign.Models;
using WalletConnectSharp.Sign.Models.Engine;
using WalletConnectSharp.Storage;
using WalletConnectSharp.Storage.Interfaces;

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
                Storage = BuildStorage()
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
            WCLogger.Logger = new WCUnityLogger();
            Client = await WalletConnectSignClient.Init(_clientOptions);
            var sessions = Client.Find(_connectOptions.RequiredNamespaces);

            if (sessions.Length > 0)
            {
                Session = sessions.First(x => x.Acknowledged ?? false);
            }
            else
            {
                _connectedData = await Client.Connect(_connectOptions);
                // TODO: refactor flow
                AuthRequired?.Invoke(_connectedData.Uri);
            }
        }

        private static IKeyValueStorage BuildStorage()
        {
            var path = Application.persistentDataPath + "/walletconnect.json";
            Debug.Log("Using storage location: " + path);
            return new FileSystemStorage(Application.persistentDataPath + "/walletconnect.json");
        }

        public async Task AuthenticateAsync()
        {
            Session = await _connectedData.Approval;
        }

        public async Task DisconnectAsync()
        {
            await Client.Disconnect(Session.Topic, new PairingDelete());
            _connectedData = await Client.Connect(_connectOptions);
            AuthRequired?.Invoke(_connectedData.Uri);
        }

        public async Task<string> SendTransactionAsync(Transaction transaction)
        {
            Debug.Log("SendTransactionAsync");

            var request = new EthSendTransaction(transaction);

            var result = await Client.Request<EthSendTransaction, string>(Session.Topic, request);

            return result;
        }

        public async Task<string> PersonalSignAsync(string message)
        {
            Debug.Log("PersonalSignAsync");

            var account = GetCurrentAddress();

            var hexUtf8 = "0x" + Encoding.UTF8.GetBytes(message).ToHex();
            var request = new PersonalSign(hexUtf8, account.Address);

            var result = await Client.Request<PersonalSign, string>(Session.Topic, request, account.ChainId);

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

        [RpcMethod("personal_sign")]
        [RpcRequestOptions(Clock.ONE_MINUTE, 99998)]
        public class PersonalSign : List<string>
        {
            public PersonalSign(string hexUtf8, string account) : base(new[] { hexUtf8, account })
            {
            }

            public PersonalSign()
            {
            }
        }


        public class Caip25Address
        {
            public string Address;
            public string ChainId;
        }

        public Caip25Address GetCurrentAddress(string chain)
        {
            if (string.IsNullOrWhiteSpace(chain))
                return null;

            var defaultNamespace = Session.Namespaces[chain];

            if (defaultNamespace.Accounts.Length == 0)
                return null;

            var fullAddress = defaultNamespace.Accounts[0];
            var addressParts = fullAddress.Split(":");

            var address = addressParts[2];
            var chainId = string.Join(':', addressParts.Take(2));

            return new Caip25Address()
            {
                Address = address,
                ChainId = chainId
            };
        }

        public Caip25Address GetCurrentAddress()
        {
            var currentSession = Client.Session.Get(Client.Session.Keys[0]);

            var defaultChain = currentSession.Namespaces.Keys.FirstOrDefault();

            if (string.IsNullOrWhiteSpace(defaultChain))
                return null;

            return GetCurrentAddress(defaultChain);
        }
    }
}