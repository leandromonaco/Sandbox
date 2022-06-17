﻿using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.KeyManagementService;
using Amazon.Runtime;
using AWS.Logger;
using AWS.Logger.SeriLog;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using ServiceName.Core.Common.Interfaces;
using ServiceName.Core.Model;
using ServiceName.Infrastructure.Repositories;

namespace ServiceName.Infrastructure
{
    public static class ConfigureServices
    {
        static ConfigurationManager _configurationManager;
        
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, ConfigurationManager configurationManager)
        {
            _configurationManager = configurationManager;
            
            services.AddSingleton<IConfiguration>(GetConfiguration(configurationManager));
            services.AddScoped<IRepositoryService<Settings>, SettingsRepositoryService>();

            var loggingSink = _configurationManager["ModuleConfiguration:Logging:Sink"];

            switch (loggingSink)
            {
                case "Seq":
                    services.AddSingleton<ILogger>(GetCloudSeqLogger());
                    break;
                default:
                    services.AddSingleton<ILogger>(GetCloudWatchLogger());
                    break;
            }

            services.AddSingleton<IDistributedCache>(GetRedisCache());
            services.AddSingleton<IDynamoDBContext>(GetDynamoDBContext());
            services.AddSingleton<IAmazonKeyManagementService>(GetAmazonKms());

            return services;
        }

        private static IAmazonKeyManagementService GetAmazonKms()
        {
            var accessKey = _configurationManager["ModuleConfiguration:AwsServices:Kms:AccessKey"];
            var secretKey = _configurationManager["ModuleConfiguration:AwsServices:Kms:SecretKey"];
            var regionEndpoint = RegionEndpoint.GetBySystemName(_configurationManager["ModuleConfiguration:AwsServices:Kms:RegionEndpoint"]);
            var localTestEndpoint = _configurationManager["ModuleConfiguration:AwsServices:Kms:LocalTestEndpoint"];
            
            AmazonKeyManagementServiceConfig amazonKeyManagementServiceConfig = new()
            {
                RegionEndpoint = regionEndpoint,
            };

            if (!string.IsNullOrEmpty(localTestEndpoint))
            {
                amazonKeyManagementServiceConfig.UseHttp = true;
                amazonKeyManagementServiceConfig.ServiceURL = localTestEndpoint;
            }

            if (!string.IsNullOrEmpty(accessKey) && !string.IsNullOrEmpty(secretKey))
            {
                return new AmazonKeyManagementServiceClient(accessKey, secretKey, amazonKeyManagementServiceConfig);
            }

            return new AmazonKeyManagementServiceClient(amazonKeyManagementServiceConfig);
        }

        private static IDistributedCache GetRedisCache()
        {
            var cache = new RedisCache(new RedisCacheOptions
            {
                Configuration = _configurationManager["ModuleConfiguration:ConnectionStrings:Redis"]
            });

            return cache;
        }

        private static ILogger GetCloudSeqLogger()
        {
            var serverUrl = _configurationManager["ModuleConfiguration:Logging:Seq:ServerUrl"];
            var apiKey = _configurationManager["ModuleConfiguration:Logging:Seq:ApiKey"];

            Log.Logger = new LoggerConfiguration()
                               .WriteTo.Console()
                               .WriteTo.Seq(serverUrl: serverUrl,
                                            apiKey: apiKey)
                               .CreateLogger();

            return Log.Logger;
        }

        private static ILogger GetCloudWatchLogger()
        {
            var accessKey = _configurationManager["ModuleConfiguration:Logging:AwsCloudWatch:AccessKey"];
            var secretKey = _configurationManager["ModuleConfiguration:Logging:AwsCloudWatch:SecretKey"];
            var regionEndpoint = _configurationManager["ModuleConfiguration:Logging:AwsCloudWatch:RegionEndpoint"];
            var localTestEndpoint = _configurationManager["ModuleConfiguration:Logging:AwsCloudWatch:LocalTestEndpoint"];
            var logGroupName = _configurationManager["ModuleConfiguration:Logging:AwsCloudWatch:LogGroupName"];

            AWSLoggerConfig configuration;

            //If logGroupName is empty it uses the default AWS Lambad Log Group
            if (!string.IsNullOrEmpty(logGroupName))
            {
                configuration = new(logGroupName)
                {
                    Region = regionEndpoint,
                    Credentials = new BasicAWSCredentials(accessKey, secretKey)
                };

                //used for local testing only
                if (!string.IsNullOrEmpty(localTestEndpoint))
                {
                    configuration.ServiceUrl = localTestEndpoint;
                }

                return new LoggerConfiguration().WriteTo.AWSSeriLog(configuration)
                                                  .WriteTo.Console()
                                                  .CreateLogger();
            }

            return new LoggerConfiguration().WriteTo.AWSSeriLog()
                                                  .WriteTo.Console()
                                                  .CreateLogger();            
        }

        private static IConfiguration GetConfiguration(ConfigurationManager configurationManager)
        {
            configurationManager
                         .SetBasePath(Environment.CurrentDirectory)
                         .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                         .AddJsonFile($"appsettings.Development.json", optional: true, reloadOnChange: true)
                         .AddEnvironmentVariables()
                         .Build();
    
            return configurationManager;
        }

        private static DynamoDBContext GetDynamoDBContext()
        {
            var accessKey = _configurationManager["ModuleConfiguration:ConnectionStrings:DynamoDb:AccessKey"];
            var secretKey = _configurationManager["ModuleConfiguration:ConnectionStrings:DynamoDb:SecretKey"];
            var regionEndpoint = RegionEndpoint.GetBySystemName(_configurationManager["ModuleConfiguration:ConnectionStrings:DynamoDb:RegionEndpoint"]);
            var localTestEndpoint = _configurationManager["ModuleConfiguration:ConnectionStrings:DynamoDb:LocalTestEndpoint"];

            var dynamoDBContextConfig = new DynamoDBContextConfig() { ConsistentRead = true };
            
            AmazonDynamoDBConfig amazonDynamoDBConfig = new()
            {
                RegionEndpoint = regionEndpoint
            };

            if (!string.IsNullOrEmpty(localTestEndpoint))
            {
                amazonDynamoDBConfig.UseHttp = true;
                amazonDynamoDBConfig.ServiceURL = localTestEndpoint;
            }

            if (!string.IsNullOrEmpty(accessKey) && !string.IsNullOrEmpty(secretKey))
            {
                var amazonDynamoDBClientWithCredentials = new AmazonDynamoDBClient(accessKey, secretKey, amazonDynamoDBConfig);
                return new DynamoDBContext(amazonDynamoDBClientWithCredentials, dynamoDBContextConfig);
            }

            var amazonDynamoDBClientWithoutCredentials = new AmazonDynamoDBClient(amazonDynamoDBConfig);
            return new DynamoDBContext(amazonDynamoDBClientWithoutCredentials, dynamoDBContextConfig);
        }
    }
}
