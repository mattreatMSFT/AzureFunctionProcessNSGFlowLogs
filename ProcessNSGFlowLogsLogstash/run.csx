﻿#load "../shared/chunk.csx"
#load "../shared/sendDownstream.csx"
#load "../shared/getEnvironmentVariable.csx"

#r "Microsoft.WindowsAzure.Storage"

using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host.Bindings.Runtime;
using Microsoft.WindowsAzure.Storage.Blob;

public static async Task Run(Chunk inputChunk, Binder binder, TraceWriter log)
{
    log.Info($"C# Queue trigger function processed: {inputChunk}");

    string nsgSourceDataAccount = getEnvironmentVariable("nsgSourceDataAccount");
    if (nsgSourceDataAccount.Length == 0)
    {
        log.Error("Value for nsgSourceDataAccount is required.");
        return;
    }

    var attributes = new Attribute[]
    {    
        new BlobAttribute(inputChunk.BlobName),
        new StorageAccountAttribute(nsgSourceDataAccount)
    };

    string nsgMessagesString;
    try
    {
        byte[] nsgMessages = new byte[inputChunk.Length];
        CloudBlockBlob blob = await binder.BindAsync<CloudBlockBlob>(attributes);
        await blob.DownloadRangeToByteArrayAsync(nsgMessages, 0, inputChunk.Start, inputChunk.Length);
        nsgMessagesString = System.Text.Encoding.UTF8.GetString(nsgMessages);
    }
    catch (Exception ex)
    {
        log.Error(string.Format("Error binding blob input: {0}", ex.Message));
        return;
    }

    await SendMessagesDownstream(nsgMessagesString, log);
}
