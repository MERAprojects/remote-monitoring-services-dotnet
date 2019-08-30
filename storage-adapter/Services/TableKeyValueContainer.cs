using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Azure.IoTSolutions.StorageAdapter.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.StorageAdapter.Services.Exceptions;
using Microsoft.Azure.IoTSolutions.StorageAdapter.Services.Models;
using Microsoft.Azure.IoTSolutions.StorageAdapter.Services.Runtime;
using Microsoft.Azure.IoTSolutions.StorageAdapter.Services.Wrappers;

namespace Microsoft.Azure.IoTSolutions.StorageAdapter.Services
{
    public sealed class TableKeyValueContainer : IKeyValueContainer
    {
        private readonly CloudStorageAccount storageAccount;
        private readonly CloudTableClient tableClient;
        private readonly string tableName;
         
         // initialized in SetupStorageAsync; make sure to call the method before using it
        private CloudTable table;
        private readonly IExceptionChecker exceptionChecker;
        private readonly ILogger log;

        public TableKeyValueContainer(IServicesConfig config, IExceptionChecker exceptionChecker, ILogger logger)
        {
            this.storageAccount = CreateStorageAccount(config.StorageConnectionString);
            this.tableClient = storageAccount.CreateCloudTableClient(new TableClientConfiguration());
            // Azure Storage Table cannot have names with non-alphanumeric chars
            this.tableName = Regex.Replace(config.StorageCollection, @"[^A-Za-z0-9]+", "");

            this.exceptionChecker = exceptionChecker;
            this.log = logger;
            this.log.Info("Started", () => { });
        }
        
        public async Task<ValueServiceModel> CreateAsync(string collectionId, string key, ValueServiceModel input)
        {
            await this.SetupStorageAsync();

            try
            {
                var entity = new KeyValueEntity(collectionId, key, input.Data);
                TableOperation insert = TableOperation.Insert(entity);
                var result = await table.ExecuteAsync(insert);
                return new ValueServiceModel(result.Result as KeyValueEntity);
            }
            catch (Exception ex)
            {
                if (!this.exceptionChecker.IsConflictException(ex)) throw;

                const string message = "There is already a value with the key specified.";
                this.log.Info(message, () => new { collectionId, key });
                throw new ConflictingResourceException(message);
            }
        }

        public async Task DeleteAsync(string collectionId, string key)
        {
            await this.SetupStorageAsync();

            var entity = new KeyValueEntity(collectionId, key, null);
            var delete = TableOperation.Delete(entity);
            await table.ExecuteAsync(delete);
        }

        public async Task<IEnumerable<ValueServiceModel>> GetAllAsync(string collectionId)
        {
            await this.SetupStorageAsync();

            var condition = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, collectionId);
            var query = new TableQuery<KeyValueEntity>().Where(condition);
            var entities = new List<KeyValueEntity>();
            TableContinuationToken token = null;
            do
            {
                var queryResult = await table.ExecuteQuerySegmentedAsync(query, token);
                entities.AddRange(queryResult.Results);
                token = queryResult.ContinuationToken;
            } while (token != null);
            
            return await Task.FromResult(entities.Select(entity => new ValueServiceModel(entity)));
        }

        public async Task<ValueServiceModel> GetAsync(string collectionId, string key)
        {
            await this.SetupStorageAsync();

            TableOperation retrieve = TableOperation.Retrieve<KeyValueEntity>(collectionId, key);
            TableResult result = await table.ExecuteAsync(retrieve);
            KeyValueEntity entity = result.Result as KeyValueEntity;
            if (entity != null)
            {
                return new ValueServiceModel(entity);
            }
            else
            {
                const string message = "Requested resource doesn't exist";
                this.log.Info(message, () => new
                {
                    collectionId,
                    key
                });

                throw new ResourceNotFoundException(message);
            }
        }

        public async Task<ValueServiceModel> UpsertAsync(string collectionId, string key, ValueServiceModel input)
        {
            await this.SetupStorageAsync();

            try
            {
                var entity = new KeyValueEntity(collectionId, key, input.Data);
                var upsert = TableOperation.InsertOrReplace(entity);
                var response = await table.ExecuteAsync(upsert);
                return new ValueServiceModel(response.Result as KeyValueEntity);
            }
            catch (Exception ex)
            {
                if (!this.exceptionChecker.IsPreconditionFailedException(ex))
                {
                    const string message = "ETag mismatch: the resource has been updated by another client.";
                    this.log.Info(message, () => new { collectionId, key, input.ETag });
                    throw new ConflictingResourceException(message);
                }
                throw;
            }
        }

        public async Task<StatusResultServiceModel> PingAsync()
        {
            var result = new StatusResultServiceModel(false, "Storage check failed");
            try
            {
                var props = await tableClient.GetServicePropertiesAsync();
                result.IsHealthy = true;
                result.Message = "Alive and well!";
            }
            catch (Exception e)
            {
                this.log.Info(result.Message, () => new { e });
            }

            return result;
        }

        private CloudStorageAccount CreateStorageAccount(string storageConnectionString)
        {
            CloudStorageAccount account;
            try
            {
                account = CloudStorageAccount.Parse(storageConnectionString);
            }
            catch (Exception ex) when (ex is FormatException || ex is ArgumentException)
            {
                log.Error("Invalid storage account information provided", () => { });
                throw;
            }

           return account;
        }

        private async Task SetupStorageAsync()
        {
            if (this.table == null)
            {
                CloudTable table = tableClient.GetTableReference(tableName);
                await table.CreateIfNotExistsAsync();
                this.table = table;
            }
        }
    }
}