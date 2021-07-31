﻿using Azure.Messaging.ServiceBus;
using DDAC_Assignment_Mining_Commerce.Services;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DDAC_Assignment_Mining_Commerce.Worker
{
    public class AnalyticLoggingWorker : BackgroundService
    {
        private readonly IServiceProvider _services;

        public AnalyticLoggingWorker(IServiceProvider _services) {
            this._services = _services;
        }

        public Task ErrorHandler(ProcessErrorEventArgs args)
        {
            using (var scope = this._services.CreateScope())
            {
                AnalyticService _analytic = scope.ServiceProvider.GetRequiredService<AnalyticService>();
                Task.Run(() => _analytic.enqueueErrorMsg(args.Exception.Message.ToString()));
            }
            return Task.CompletedTask;
        }

        public async Task MessageHandler(ProcessMessageEventArgs arg)
        {
            var scope = this._services.CreateScope();
            Console.WriteLine("Message Handler working");
            try
            {
                var msg = arg.Message;
                //Console.WriteLine("Message Received");
                TableEntity analyticObj = JsonConvert.DeserializeObject<TableEntity>(msg.Body.ToString());
                //Console.WriteLine(msg.Body.ToString());
                //Console.WriteLine("Table: ");
                //Console.WriteLine(msg.ApplicationProperties["table"]);
                AnalyticService _analytic = scope.ServiceProvider.GetRequiredService<AnalyticService>();
                string tablename = msg.ApplicationProperties["table"].ToString();
                if (tablename == null) {
                    throw new Exception(message: "Table name not found");
                }
                var table = _analytic.getTable(tablename);
                TableOperation logOperation = TableOperation.InsertOrReplace(analyticObj);
                await table.ExecuteAsync(logOperation);
                await arg.CompleteMessageAsync(arg.Message);
                //Console.WriteLine("New Analytic data logged");
            }
            catch (Exception ex)
            {
                AnalyticService _analytic = scope.ServiceProvider.GetRequiredService<AnalyticService>();
                //Console.WriteLine("=====Errror When Handling Message===");
                //Console.WriteLine(ex.ToString());
                await Task.Run(() => _analytic.enqueueErrorMsg(ex.ToString()));
            }
        }


        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
           while (!stoppingToken.IsCancellationRequested) {
                using (var scope = this._services.CreateScope()) {
                    AnalyticService _analytic = scope.ServiceProvider.GetRequiredService<AnalyticService>();
                    try {
                        var client = _analytic.SBclient;
                        var processor = client.CreateProcessor("analytic-log", new ServiceBusProcessorOptions());
                        try
                        {
                            // add handler to process messages
                            processor.ProcessMessageAsync += MessageHandler;

                            // add handler to process any errors
                            processor.ProcessErrorAsync += ErrorHandler;

                            // start processing 
                            await processor.StartProcessingAsync();
                            Console.WriteLine("Analytic Log Processor started...");
                        }
                        catch (Exception ex) {
                            throw ex;
                        }
                    } catch (Exception ex){
                        await Task.Run(()=>_analytic.enqueueErrorMsg(ex.ToString()));
                    }
                }
                break;
            }
        }
    }  
}