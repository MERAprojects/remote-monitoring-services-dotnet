// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.Azure.IoTSolutions.StorageAdapter.Services.Runtime
{
    public interface IServicesConfig
    {
        string StorageType { get; set; }
        string StorageCollection { get; set; }
        string StorageConnectionString { get; set; }
    }

    public class ServicesConfig : IServicesConfig
    {
        public string StorageType { get; set; }
        public string StorageCollection { get; set; }
        public string StorageConnectionString { get; set; }
    }
}
