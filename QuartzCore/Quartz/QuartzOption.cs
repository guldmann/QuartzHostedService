using System;
using System.Collections.Specialized;
using System.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Quartz.Impl.AdoJobStore;

namespace Quartz.HostedService
{
    /// <summary>
    /// More details:
    /// https://github.com/quartznet/quartznet/blob/master/src/Quartz/Impl/StdSchedulerFactory.cs
    /// </summary>
    public class QuartzOption
    {
        private readonly ILogger<QuartzOption> _logger;

        public QuartzOption(IConfiguration config, ILogger<QuartzOption> logger = null)
        {
            if (logger == null)
            {
                _logger = new NullLogger<QuartzOption>();
            }
            else
            {
                _logger = logger;
            }
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            var section = config.GetSection("quartz");
            section.Bind(this);

            SetupProvider();
        }

        private void SetupProvider()
        {
            if (JobStore?.Provider == "SqlServer")
            {
                _logger.LogDebug($"Provider set to {JobStore.Provider}");
                if (!DbExist())
                {
                    _logger.LogDebug($"Database: {JobStore.Databasename} not found...");
                    CreateDatabse();
                }
            }
        }

        private void CreateDatabse()
        {
            try
            {
                var connectionString = GetConnectionString();
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    var command = connection.CreateCommand();
                    command.CommandText = $"CREATE DATABASE {JobStore.Databasename}";
                    command.ExecuteNonQuery();
                }

                using (var connection = new SqlConnection(JobStore.ConnectionString))
                {
                    //REad in sql from QuartzSqlServer.sql here
                    connection.Open();
                    var command = connection.CreateCommand();
                    command.CommandText = $"CREATE DATABASE {JobStore.Databasename}";
                    command.ExecuteNonQuery();
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to create database.");
            }
        }

        private string GetConnectionString()
        {
            if (!JobStore.ConnectionString.Contains("Database=")) return JobStore.ConnectionString;

            string connectionString = "";
            var parts = JobStore.ConnectionString.Split(';');

            foreach (var part in parts)
            {
                if (!part.Contains("Database="))
                {
                    connectionString += part + ";";
                }
            }

            return connectionString.TrimEnd(';');
        }

        private bool DbExist()
        {
            //string sqlCreateDBQuery;
            try
            {
                var connection = new SqlConnection(GetConnectionString());

                string sqlCreateDBQuery = string.Format("SELECT database_id FROM sys.databases WHERE Name = '{0}'", JobStore.Databasename);

                using (connection)
                {
                    using (SqlCommand sqlCmd = new SqlCommand(sqlCreateDBQuery, connection))
                    {
                        connection.Open();

                        object resultObj = sqlCmd.ExecuteScalar();

                        int databaseID = 0;

                        if (resultObj != null)
                        {
                            int.TryParse(resultObj.ToString(), out databaseID);
                        }

                        connection.Close();

                        return (databaseID > 0);
                    }
                }
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public Scheduler Scheduler { get; set; }

        public ThreadPool ThreadPool { get; set; }

        public Plugin Plugin { get; set; }

        public JobStore JobStore { get; set; }

        public Serializer Serializer { get; set; }

        public NameValueCollection ToProperties()
        {
            var properties = new NameValueCollection
            {
                ["quartz.scheduler.instanceName"] = Scheduler?.InstanceName,
                ["quartz.threadPool.type"] = ThreadPool?.Type,
                ["quartz.threadPool.threadPriority"] = ThreadPool?.ThreadPriority,
                ["quartz.threadPool.threadCount"] = ThreadPool?.ThreadCount.ToString(),
                ["quartz.plugin.jobInitializer.type"] = Plugin?.JobInitializer?.Type,
                ["quartz.plugin.jobInitializer.fileNames"] = Plugin?.JobInitializer?.FileNames,
                ["quartz.jobStore.misfireThreshold"] = JobStore?.MisfireThreshold,
                ["quartz.jobStore.type"] = JobStore?.Type,
                ["quartz.jobStore.useProperties"] = JobStore?.UseProperties,
                ["quartz.jobStore.dataSource"] = JobStore?.DataSource,
                ["quartz.jobStore.tablePrefix"] = JobStore?.TablePrefix,
                ["quartz.jobStore.lockHandler.type"] = JobStore?.LockHandler,
                ["quartz.dataSource.default.connectionString"] = JobStore?.ConnectionString,
                ["quartz.dataSource.default.provider"] = JobStore?.Provider,
                ["quartz.serializer.type"] = Serializer.Type
            };

            /*
                        "Server=localhost;Database=Quartz;User Id=sa;Password=Secret123!%;MultipleActiveResultSets=true"
             */

            return properties;
        }
    }

    public class JobStore
    {
        public string MisfireThreshold { get; set; } = "600000";
        public string Type { get; set; }
        public string UseProperties { get; set; } = "True";
        public string DataSource { get; } = "default";
        public string TablePrefix { get; set; } = "QRTZ_";
        public string LockHandler { get; set; }
        public string ConnectionString { get; set; }
        public string Provider { get; set; }
        public string Databasename { get; } = "Quartz";
    }

    public class Serializer
    {
        public string Type { get; set; } = "json";
    }

    public class Scheduler
    {
        public string InstanceName { get; set; }
    }

    public class ThreadPool
    {
        public string Type { get; set; }

        public string ThreadPriority { get; set; }

        public int ThreadCount { get; set; }
    }

    public class Plugin
    {
        public JobInitializer JobInitializer { get; set; }
    }

    public class JobInitializer
    {
        public string Type { get; set; }
        public string FileNames { get; set; }
    }
}