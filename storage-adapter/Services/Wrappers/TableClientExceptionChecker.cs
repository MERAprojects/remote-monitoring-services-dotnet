// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Net;
using Microsoft.Azure.Cosmos.Table;

namespace Microsoft.Azure.IoTSolutions.StorageAdapter.Services.Wrappers
{
    public class TableClientExceptionChecker : IExceptionChecker
    {
        public bool IsConflictException(Exception exception)
        {
            var ex = exception as StorageException;
            return ex != null && ex.RequestInformation.HttpStatusCode == (int)HttpStatusCode.Conflict;
        }

        public bool IsPreconditionFailedException(Exception exception)
        {
            var ex = exception as StorageException;
            return ex != null && ex.RequestInformation.HttpStatusCode == (int)HttpStatusCode.PreconditionFailed;
        }

        public bool IsNotFoundException(Exception exception)
        {
            var ex = exception as StorageException;
            return ex != null && ex.RequestInformation.HttpStatusCode == (int)HttpStatusCode.NotFound;
        }
    }
}
