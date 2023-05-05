// Copyright (c) Microsoft Corporation. All Rights Reserved.
// Licensed under the MIT License.

using Microsoft.Datasync.Client;
using Microsoft.Datasync.Client.SQLiteStore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using TodoApp.Data.Models;

namespace TodoApp.Data.Services
{
    /// <summary>
    /// An implementation of the <see cref="ITodoService"/> interface that uses
    /// a remote table on a Datasync Service.
    /// </summary>
    public class RemoteTodoService : ITodoService
    {
        /// <summary>
        /// Reference to the client used for datasync operations.
        /// </summary>
        private DatasyncClient _client = null;

        /// <summary>
        /// Reference to the table used for datasync operations.
        /// </summary>
        //private IRemoteTable<TodoItem> _table = null;
        private IOfflineTable<TodoItem> _table = null;

        /// <summary>
        /// When set to true, the client and table and both initialized.
        /// </summary>
        private bool _initialized = false;

        /// <summary>
        /// Used for locking the initialization block to ensure only one initialization happens.
        /// </summary>
        private readonly SemaphoreSlim _asyncLock = new(1, 1);

        /// <summary>
        /// An event handler that is triggered when the list of items changes.
        /// </summary>
        public event EventHandler<TodoServiceEventArgs> TodoItemsUpdated;

        /// <summary>
        /// When using authentication, the token requestor to use.
        /// </summary>
        public Func<Task<AuthenticationToken>> TokenRequestor;

        /// <summary>
        /// The path to the offline database
        /// </summary>
        public string OfflineDb { get; set; }

        /// <summary>
        /// Creates a new <see cref="RemoteTodoService"/> with no authentication.
        /// </summary>
        public RemoteTodoService()
        {
            TokenRequestor = null; // no authentication
        }

        /// <summary>
        /// Creates a new <see cref="RemoteTodoService"/> with authentication.
        /// </summary>
        public RemoteTodoService(Func<Task<AuthenticationToken>> tokenRequestor)
        {
            TokenRequestor = tokenRequestor;
        }

        /// <summary>
        /// Initialize the connection to the remote table.
        /// </summary>
        /// <returns></returns>
        private async Task InitializeAsync()
        {
            // Short circuit, in case we are already initialized.
            if (_initialized)
            {
                return;
            }

            try
            {
                // Wait to get the async initialization lock
                await _asyncLock.WaitAsync();
                if (_initialized)
                {
                    // This will also execute the async lock.
                    return;
                }

                //this.SetupRemoteTable();
                await this.SetupOfflineTableAsync();

                // Set _initialied to true to prevent duplication of locking.
                _initialized = true;
            }
            catch (Exception)
            {
                // Re-throw the exception.
                throw;
            }
            finally
            {
                _asyncLock.Release();
            }
        }

        private async Task SetupOfflineTableAsync()
        {
            var connectionString = new UriBuilder { Scheme = "file", Path = OfflineDb, Query = "?mode=rwc" }.Uri.ToString();
            var store = new OfflineSQLiteStore(connectionString);
            store.DefineTable<TodoItem>();
            var options = new DatasyncClientOptions
            {
                OfflineStore = store,
                HttpPipeline = new HttpMessageHandler[] { new LoggingHandler() }
            };

            // Create the datasync client.
            _client = TokenRequestor == null
                ? new DatasyncClient(Constants.ServiceUri, options)
                : new DatasyncClient(Constants.ServiceUri, new GenericAuthenticationProvider(TokenRequestor), options);

            // Initialize the database
            await _client.InitializeOfflineStoreAsync();

            // Get a reference to the offline table.
            _table = _client.GetOfflineTable<TodoItem>();
        }

        //private void SetupRemoteTable()
        //{
        //    var options = new DatasyncClientOptions
        //    {
        //        HttpPipeline = new HttpMessageHandler[] { new LoggingHandler() }
        //    };

        //    // Initialize the client.
        //    _client = TokenRequestor == null
        //        ? new DatasyncClient(Constants.ServiceUri, options)
        //        : new DatasyncClient(Constants.ServiceUri, new GenericAuthenticationProvider(TokenRequestor), options);
        //    _table = _client.GetRemoteTable<TodoItem>();
        //}

        /// <summary>
        /// Get all the items in the list.
        /// </summary>
        /// <returns>The list of items (asynchronously)</returns>
        public async Task<IEnumerable<TodoItem>> GetItemsAsync()
        {
            await InitializeAsync();
            return await _table.GetAsyncItems().ToListAsync();
        }

        /// <summary>
        /// Refreshes the TodoItems list manually.
        /// </summary>
        /// <returns>A task that completes when the refresh is done.</returns>
        public async Task RefreshItemsAsync()
        {
            await InitializeAsync();

            // First, push all the items in the table.
            await _table.PushItemsAsync();

            // Then, pull all the items in the table.
            await _table.PullItemsAsync();

            return;
        }

        /// <summary>
        /// Removes an item in the list, if it exists.
        /// </summary>
        /// <param name="item">The item to be removed.</param>
        /// <returns>A task that completes when the item is removed.</returns>
        public async Task RemoveItemAsync(TodoItem item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            if (item.Id == null)
            {
                // Short circuit for when the item has not been saved yet.
                return;
            }
            await InitializeAsync();
            await _table.DeleteItemAsync(item);
            TodoItemsUpdated?.Invoke(this, new TodoServiceEventArgs(TodoServiceEventArgs.ListAction.Delete, item));
        }

        /// <summary>
        /// Saves an item to the list.  If the item does not have an Id, then the item
        /// is considered new and will be added to the end of the list.  Otherwise, the
        /// item is considered existing and is replaced.
        /// </summary>
        /// <param name="item">The new item</param>
        /// <returns>A task that completes when the item is saved.</returns>
        public async Task SaveItemAsync(TodoItem item)
        {
            if (item == null)
            {
                throw new ArgumentException(nameof(item));
            }

            await InitializeAsync();

            TodoServiceEventArgs.ListAction action = (item.Id == null) ? TodoServiceEventArgs.ListAction.Add : TodoServiceEventArgs.ListAction.Update;
            if (item.Id == null)
            {
                await _table.InsertItemAsync(item);
            }
            else
            {
                await _table.ReplaceItemAsync(item);
            }
            TodoItemsUpdated?.Invoke(this, new TodoServiceEventArgs(action, item));
        }
    }
}
