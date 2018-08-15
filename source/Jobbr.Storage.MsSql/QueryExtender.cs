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

        private static readonly Dictionary<string, Func<JobRun, object>> JobRunPropertyMapping = new Dictionary<string, Func<JobRun, object>>
        {
            {"id", run => run.Id },
            {"instanceparameters", run => run.InstanceParameters },
            {"jobparameters", run => run.JobParameters },
            {"pid", run => run.Pid },
            {"plannedstartdatetimeutc", run => run.PlannedStartDateTimeUtc },
            {"progress", run => run.Progress },
            {"actualenddatetimeutc", run => run.ActualEndDateTimeUtc },
            {"estimatedenddatetimeutc", run => run.EstimatedEndDateTimeUtc },
            {"state", run => run.State }
        };

        private static readonly Dictionary<string, Func<Job, object>> JobPropertyMapping = new Dictionary<string, Func<Job, object>>
        {
            {"id", run => run.Id },
            {"createddatetimeutc", run => run.CreatedDateTimeUtc },
            {"parameters", run => run.Parameters },
            {"title", run => run.Title },
            {"type", run => run.Type },
            {"uniquename", run => run.UniqueName },
            {"updatedDatetimeutc", run => run.UpdatedDateTimeUtc },
        };

        public static IEnumerable<JobRun> JobRunQuery(IEnumerable<JobRun> jobRunQuery, string query)
        {
            // Propertynames can only consist of alphanumeric values
            var matches = Regex.Matches(query, "^[a-zA-Z][a-zA-Z0-9]*$").Cast<Match>().Select(m => m.Value).ToList();

            // Our properties should be the union of all allowed properties and those which were in the searchquersy
            var propertiesInQuery = AllowedProperties.Union(matches);

            return jobRunQuery;
        }

        public static IEnumerable<JobRun> SortFilteredJobRuns(IEnumerable<JobRun> jobRuns, int page, int pageSize,
            string jobTypeFilter, string jobUniqueNameFilter,
            string query, string[] sort)
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

        public static IEnumerable<Job> SortFilteredJobs(IEnumerable<Job> jobs, int page, int pageSize,
            string jobTypeFilter, string jobUniqueNameFilter,
            string query, string[] sort)
        {
            if (jobTypeFilter != null)
            {
                jobs = jobs.Where(w => w.Type == jobTypeFilter);
            }

            if (jobUniqueNameFilter != null)
            {
                jobs = jobs.Where(w => w.UniqueName == jobUniqueNameFilter);
            }

            if (query != null)
            {
                ////var jobRunQuery = JobRunQuery(jobRuns, query);
            }

            jobs = SortJobs(jobs, sort);
            jobs = jobs.Take(pageSize).Skip(pageSize * (page - 1));
            return jobs;
        }

        public static IEnumerable<JobRun> SortJobRuns(IEnumerable<JobRun> jobRuns, string[] sort)
        {
            foreach (var criterion in sort.Where(s => !string.IsNullOrWhiteSpace(s)))
            {
                jobRuns = criterion[0] != '-'
                    ? jobRuns.OrderBy(JobRunPropertyMapping[GetPropertyName(criterion)])
                    : jobRuns.OrderByDescending(JobRunPropertyMapping[GetPropertyName(criterion)]);
            }

            return jobRuns;
        }

        public static IEnumerable<Job> SortJobs(IEnumerable<Job> jobs, string[] sort)
        {
            foreach (var criterion in sort.Where(s => !string.IsNullOrWhiteSpace(s)))
            {
                jobs = criterion[0] != '-'
                    ? jobs.OrderBy(JobPropertyMapping[GetPropertyName(criterion)])
                    : jobs.OrderByDescending(JobPropertyMapping[GetPropertyName(criterion)]);
            }

            return jobs;

        }

        private static string GetPropertyName(string sortString)
        {
            var hasSign = sortString[0] == '+' || sortString[0] == '-';
            return hasSign ? sortString.Substring(1, sortString.Length).ToLower() : sortString.ToLower();
        }
    }
}