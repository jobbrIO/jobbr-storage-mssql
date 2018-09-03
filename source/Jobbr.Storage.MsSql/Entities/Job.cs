using System;
using ServiceStack.DataAnnotations;

namespace Jobbr.Storage.MsSql.Entities
{
    [Alias("Jobs")]
    public class Job
    {
        [AutoIncrement]
        public long Id { get; set; }

        [Unique]
        [StringLength(100)]
        public string UniqueName { get; set; }

        [StringLength(100)]
        public string Title { get; set; }

        [StringLength(int.MaxValue)]
        public string Parameters { get; set; }

        [StringLength(100)]
        public string Type { get; set; }

        public DateTime? UpdatedDateTimeUtc { get; set; }
        public DateTime? CreatedDateTimeUtc { get; set; }
    }
}
