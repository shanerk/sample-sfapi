using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using Salesforce.Common;
using Salesforce.Force;
using System.Threading.Tasks;
using System.Dynamic;
using System.Net;
using log4net;
using library.models;
using System.IO;

namespace library
{
    public class SalesforceController
    {
        private static ILog log;
        private static readonly string SecurityToken = ConfigurationManager.AppSettings["SecurityToken"];
        private static readonly string ConsumerKey = ConfigurationManager.AppSettings["ConsumerKey"];
        private static readonly string ConsumerSecret = ConfigurationManager.AppSettings["ConsumerSecret"];
        private static readonly string Username = ConfigurationManager.AppSettings["Username"];
        private static readonly string Password = ConfigurationManager.AppSettings["Password"] + SecurityToken;
        private static readonly string IsSandboxUser = ConfigurationManager.AppSettings["IsSandboxUser"];

        public SalesforceController() {
            System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            log = LogManager.GetLogger(typeof(SalesforceController));
        }
        public async Task RunSample()
        {
            string qry;

            ///// SALESFORCE Authentication
            var auth = new Salesforce.Common.AuthenticationClient();
            log.Info("Authenticating with Salesforce");
            var url = IsSandboxUser.Equals("true", StringComparison.CurrentCultureIgnoreCase)
                ? "https://test.salesforce.com/services/oauth2/token"
                : "https://login.salesforce.com/services/oauth2/token";

            await auth.UsernamePasswordAsync(ConsumerKey, ConsumerSecret, Username, Password, url);
            log.Info("Connected to Salesforce");

            ///// Create SALESFORCE Client
            var client = new Salesforce.Force.ForceClient(auth.InstanceUrl, auth.AccessToken, auth.ApiVersion);

            // retrieve all accounts
            log.Info("Get Tasks");

            // This SOQL gets the list of IDs with dupes
            qry = "SELECT Prosperworks_ID__c " +
                "FROM Task GROUP BY Prosperworks_ID__c " +
                "HAVING Prosperworks_ID__c != '' AND Count(ID) > 1";

            var filter = await client.QueryAsync<SFTask>(qry);
            var ids = string.Join(",", filter.Records.Select(f => f.Prosperworks_ID__c).ToList().ToArray());
            ids = ids.Substring(0,ids.Length);

            ids = string.Join(",",ids.Split(',').Where(id => id != "").Select(id => $"'{id}'"));

            if (ids.Length == 0) {
                log.Warn("No duplicates found, exiting!");
                return;
            }

            qry = "SELECT ID, Prosperworks_ID__c, ActivityDate, Description, Type, TaskSubtype " +
                "FROM Task " +
                "WHERE ProsperWorks_ID__c IN (" + ids + ")";

            log.Info(qry);

            var tasks = new List<SFTask>();
            var results = await client.QueryAsync<SFTask>(qry);
            var totalSize = results.TotalSize;
            var totalDeleted = 0;
            var totalFlagged = 0;

            log.Info("Retrieved " + totalSize + " tasks.");

            // if (totalSize > 258) {
            //     log.Error("Too many records, exiting!");
            //     return;
            // }

            tasks.AddRange(results.Records);

            foreach (var task in tasks) {
                tasks.Where(t => t.Prosperworks_ID__c == task.Prosperworks_ID__c
                    &! String.Equals(t.Id,task.Id)
                    &! task.IsMarkedForDeletion).ToList()
                    .ForEach(item => item.IsMarkedForDeletion = true);
            }

            var isDeleteEnabled = false;

            int i = 0;
            foreach (var task in tasks) {
                i++;
                if (task.IsMarkedForDeletion && task.Prosperworks_ID__c != "") {
                    totalFlagged++;
                    log.Debug($"[{i}] Task flagged {task.Id} {task.Prosperworks_ID__c}");
                    var isDeleted = false;
                    if (isDeleteEnabled) {
                        //isDeleted = await client.DeleteAsync(SFTask.SObjectTypeName, task.Id);
                    }
                    if (!isDeleted) {
                        if (isDeleteEnabled) {
                            log.Error($"[{i}] Task delete FAILED {task.Id} {task.Prosperworks_ID__c}!");
                        }
                    } else {
                        log.Warn($"[{i}] Task deleted {task.Id} {task.Prosperworks_ID__c}.");
                        totalDeleted++;
                    }
                } else {
                    log.Debug($"[{i}] Task retained {task.Id} {task.Prosperworks_ID__c}");
                }
            }

            log.Debug($"Retrieved tasks = {tasks.Count()}, flagged = {totalFlagged}, deleted = {totalDeleted}");
        }
    }
}
