using System;
using System.Collections.Generic;
using System.Linq;

namespace Jobbr.Storage.MsSql.Tests
{
    public class SqlHelper
    {
        public static IEnumerable<string> SplitSqlStatements(string sqlScript)
        {
            var statements = sqlScript.Split(new[] { "GO" }, StringSplitOptions.RemoveEmptyEntries);

            return statements
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Select(s => s.Trim(' ', '\r', '\n'));
        }
    }
}
