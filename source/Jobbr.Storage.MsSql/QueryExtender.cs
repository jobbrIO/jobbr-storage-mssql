using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Jobbr.ComponentModel.JobStorage.Model;

namespace Jobbr.Storage.MsSql
{
    public static class QueryExtender
    {
        private static readonly List<string> AllowedProperties = new List<string> {"", ""};

        private static readonly Dictionary<string, Func<JobRun, object>> Mapping = new Dictionary<string, Func<JobRun, object>>
        {
            {"id", run => run.Id },
            {"instanceparameters", run => run.InstanceParameters },
            {"jobparameters", run => run.JobParameters },
            {"pid", run => run.Pid },
            {"plannedstartdatetimeutc", run => run.PlannedStartDateTimeUtc },
            {"progress", run => run.Progress },
            {"actualenddatetimeutc", run => run.ActualEndDateTimeUtc },
            {"estimatedenddatetimeutc", run => run.EstimatedEndDateTimeUtc },
            {"state", run => run.State },

        };

        public static IEnumerable<JobRun> JobRunQuery(IEnumerable<JobRun> jobRunQuery, string query)
        {
            // Propertynames can only consist of alphanumeric values
            var matches = Regex.Matches(query, "^[a-zA-Z][a-zA-Z0-9]*$").Cast<Match>().Select(m => m.Value).ToList();

            // Our properties should be the union of all allowed properties and those which were in the searchquersy
            var propertiesInQuery = AllowedProperties.Union(matches);

            return jobRunQuery;
        }

        public static IEnumerable<JobRun> SortFilteredJobRuns(int page, int pageSize, string jobTypeFilter, string jobUniqueNameFilter,
            string query, string[] sort, IEnumerable<JobRun> jobRuns)
        {
            //// TODO: Doesnt belong here. Has to be its own class
            if (jobTypeFilter != null)
            {
                jobRuns = jobRuns.Where(w => w.Job.Type == jobTypeFilter);
            }

            if (jobUniqueNameFilter != null)
            {
                jobRuns = jobRuns.Where(w => w.Job.UniqueName == jobUniqueNameFilter);
            }

            if (query != null)
            {
                ////var jobRunQuery = JobRunQuery(jobRuns, query);
            }

            jobRuns = SortJobRuns(jobRuns, sort);
            jobRuns = jobRuns.Take(pageSize).Skip(pageSize * (page - 1));
            return jobRuns;
        }

        public static IEnumerable<JobRun> SortJobRuns(IEnumerable<JobRun> jobRuns, string[] sort)
        {
            foreach (var criterion in sort.Where(s => !string.IsNullOrWhiteSpace(s)))
            {
                jobRuns = criterion[0] != '-'
                    ? jobRuns.OrderBy(Mapping[GetPropertyName(criterion)])
                    : jobRuns.OrderByDescending(Mapping[GetPropertyName(criterion)]);
            }

            return jobRuns;

            string GetPropertyName(string sortString)
            {
                var hasSign = sortString[0] == '+' || sortString[0] == '-';
                return hasSign ? sortString.Substring(1, sortString.Length).ToLower() : sortString.ToLower();
            }
        }
    }
}