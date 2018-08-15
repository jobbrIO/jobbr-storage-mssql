using System;

namespace Jobbr.Storage.MsSql
{
    /// <summary>
    /// Flat Trigger-DTO
    /// </summary>
    public class Trigger
    {
        public long Id { get; set; }

        public long JobId { get; set; }

        public string UniqueName { get; set; }

        public string TriggerType { get; set; }

        public string Defintion { get; set; }
        
        public DateTime StartDateTimeUtc { get; set; }

        public DateTime? EndDateTimeUtc { get; set; }

        public int DelayedMinutes { get; set; }

        public bool IsActive { get; set; }

        public string UserId { get; set; }

        public string UserDisplayName { get; set; }

        public string Parameters { get; set; }

        public string Comment { get; set; }

        public DateTime CreatedDateTimeUtc { get; set; }

        public bool NoParallelExecution { get; set; }
    }
}