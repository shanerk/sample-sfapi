using System;

namespace library.models {
  class SFTask {
    public const string SObjectTypeName = "Task";
    public bool IsMarkedForDeletion { get; set; }
    public string Id { get; set; }
    public string AccountId { get; set; }
    public DateTime? ActivityDate { get; set; }
    public string Description { get; set; }
    public string Type { get; set; }
    public string TaskSubtype { get; set; }
    public string Prosperworks_ID__c { get; set; }

    public SFTask() {
      IsMarkedForDeletion = false;
    }

    public override string ToString() {
      var result = "";
      var date = ActivityDate != null ? ActivityDate.Value.ToString("MM/dd/yyyy HH:mm:ss") : "n/a";
      result = $"[{IsMarkedForDeletion}] {Id}, {Prosperworks_ID__c}, {date}, {Type}, {TaskSubtype}";
      return result;
    }
  }
}