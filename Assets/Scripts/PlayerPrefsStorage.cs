using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Assertions;
using WalletConnectSharp.Storage;

namespace WalletConnectSharpUnity
{
    public class PlayerPrefsStorage : InMemoryStorage
    {
        private const string Key = "WalletConnectSharp";
        
        private readonly TaskScheduler _unityScheduler;
        private readonly int _mainThreadId = Environment.CurrentManagedThreadId;
        
        public PlayerPrefsStorage()
        {
            // This assumes that the constructor is called from the main thread
            _unityScheduler = TaskScheduler.FromCurrentSynchronizationContext();
        }
        
        public override async Task Init()
        {
            await LoadAsync();
            await base.Init();
        }
        
        public override async Task SetItem<T>(string key, T value)
        {
            await base.SetItem(key, value);
            await SaveAsync();
        }
        
        public override async Task RemoveItem(string key)
        {
            await base.RemoveItem(key);
            await SaveAsync();
        }
        
        public override async Task Clear()
        {
            await base.Clear();
            await SaveAsync();
        }

        private async ValueTask SaveAsync()
        {
            if (Environment.CurrentManagedThreadId == _mainThreadId)
            {
                Save();
            }
            else
            {
                await Task.Factory.StartNew(Save, CancellationToken.None, TaskCreationOptions.None, _unityScheduler);
            }
        }
        
        private async ValueTask LoadAsync()
        {
            if (Environment.CurrentManagedThreadId == _mainThreadId)
            {
                Load();
            }
            else
            {
                await Task.Factory.StartNew(Load, CancellationToken.None, TaskCreationOptions.None, _unityScheduler);
            }
        }
        
        private void Save()
        {
            Assert.AreEqual(Environment.CurrentManagedThreadId, _mainThreadId, "Save must be called from the main thread");

            PlayerPrefs.SetString(Key, JsonConvert.SerializeObject(Entries, new JsonSerializerSettings()
            {
                TypeNameHandling = TypeNameHandling.All
            }));
            PlayerPrefs.Save();
        }

        private void Load()
        {
            Assert.AreEqual(Environment.CurrentManagedThreadId, _mainThreadId, "Load must be called from the main thread");
            
            if (!PlayerPrefs.HasKey(Key)) return;
            
            var json = PlayerPrefs.GetString(Key);
            try
            {
                Entries = JsonConvert.DeserializeObject<Dictionary<string, object>>(json,
                    new JsonSerializerSettings() {TypeNameHandling = TypeNameHandling.All});
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
    }
}