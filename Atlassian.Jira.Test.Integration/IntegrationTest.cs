﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;
using System.IO;

namespace Atlassian.Jira.Test.Integration
{
    public class IntegrationTest
    {
        private readonly Jira _jira;
        private readonly Random _random;

        public IntegrationTest()
        {
            _jira = new Jira("http://localhost:2990/jira", "admin", "admin");
            _jira.Debug = true;
            _random = new Random();
        }

        [Fact]
        public void QueryWithZeroResults()
        {
            var issues = from i in _jira.Issues
                         where i.Created == new DateTime(2010,1,1)
                         select i;

            Assert.Equal(0, issues.Count());
        }

        [Fact]
        public void CreateAndQueryForIssueWithMinimumFieldsSet()
        {
            var summaryValue = "Test Summary " + _random.Next(int.MaxValue);

            var issue = new Issue(_jira, "TST")
            {
                Type = "1",
                Summary = summaryValue
            };

            issue = issue.SaveChanges();

            var issues = (from i in _jira.Issues
                                where i.Key == issue.Key
                                select i).ToArray();

            Assert.Equal(1, issues.Count());

            Assert.Equal(summaryValue, issues[0].Summary);
            Assert.Equal("TST", issues[0].Project);
            Assert.Equal("1", issues[0].Type);
        }


        [Fact]
        public void CreateAndQueryIssueWithAllFieldsSet()
        {
            var summaryValue = "Test Summary " + _random.Next(int.MaxValue);

            var issue = new Issue(_jira, "TST")
            {
                Assignee = "admin",
                Description = "Test Description",
                DueDate = new DateTime(2011, 12, 12),
                Environment = "Test Environment",
                Reporter = "admin",
                Type = "1",
                Summary = summaryValue
            };

            issue = issue.SaveChanges();


            var queriedIssues = (from i in _jira.Issues
                          where i.Key == issue.Key
                          select i).ToArray();

            Assert.Equal(summaryValue, queriedIssues[0].Summary);
        }

        [Fact]
        public void UpdateWithAllFieldsSet()
        {
            // arrange, create an issue to test.
            var summaryValue = "Test Summary " + _random.Next(int.MaxValue);
            var issue = new Issue(_jira, "TST")
            {
                Assignee = "admin",
                Description = "Test Description",
                DueDate = new DateTime(2011, 12, 12),
                Environment = "Test Environment",
                Reporter = "admin",
                Type = "1",
                Summary = summaryValue
            };
            issue = issue.SaveChanges();


            // act, get an issue and update it
            var serverIssue = (from i in _jira.Issues
                                 where i.Key == issue.Key
                                 select i).ToArray().First();

            serverIssue.Description = "Updated Description";
            serverIssue.DueDate = new DateTime(2011, 10, 10);
            serverIssue.Environment = "Updated Environment";
            serverIssue.Summary = "Updated " + summaryValue;
            serverIssue.SaveChanges();

            // assert, get the issue again and verify
            var newServerIssue = (from i in _jira.Issues
                               where i.Key == issue.Key
                               select i).ToArray().First();

            Assert.Equal("Updated " + summaryValue, newServerIssue.Summary);
            Assert.Equal("Updated Description", newServerIssue.Description);
            Assert.Equal("Updated Environment", newServerIssue.Environment);

            // Note: Dates returned from JIRA are UTC
            Assert.Equal(new DateTime(2011, 10, 10).ToUniversalTime(), newServerIssue.DueDate);
        }

        [Fact]
        public void UploadAndDownloadOfAttachments()
        {
            var summaryValue = "Test Summary with attachment " + _random.Next(int.MaxValue);
            var issue = new Issue(_jira, "TST")
            {
                Type = "1",
                Summary = summaryValue
            };

            // create an issue, verify no attachments
            issue = issue.SaveChanges();
            Assert.Equal(0, issue.GetAttachments().Count);

            // upload an attachment
            File.WriteAllText("testfile.txt", "Test File Content");
            issue.AddAttachment("testfile.txt");

            var attachments = issue.GetAttachments();
            Assert.Equal(1, attachments.Count);
            Assert.Equal("testfile.txt", attachments[0].FileName);

            // download an attachment
            var tempFile = Path.GetTempFileName();
            attachments[0].Download(tempFile);
            Assert.Equal("Test File Content", File.ReadAllText(tempFile));
        }

        [Fact]
        public void AddingAndRetrievingComments()
        {
            var summaryValue = "Test Summary " + _random.Next(int.MaxValue);
            var issue = new Issue(_jira, "TST")
            {
                Type = "1",
                Summary = summaryValue
            };

            // create an issue, verify no comments
            issue = issue.SaveChanges();
            Assert.Equal(0, issue.GetComments().Count);

            // Add a comment
            issue.AddComment("new comment");

            var comments = issue.GetComments();
            Assert.Equal(1, comments.Count);
            Assert.Equal("new comment", comments[0].Body);

        }

        [Fact]
        public void MaximumNumberOfIssuesPerRequest()
        {
            // create 2 issues with same summary
            var randomNumber = _random.Next(int.MaxValue);
            (new Issue(_jira, "TST") { Type = "1", Summary = "Test Summary " + randomNumber }).SaveChanges();
            (new Issue(_jira, "TST") { Type = "1", Summary = "Test Summary " + randomNumber }).SaveChanges(); 

            //set maximum issues and query
            _jira.MaxIssuesPerRequest = 1;
            var issues = from i in _jira.Issues
                         where i.Summary == randomNumber.ToString()
                         select i;

            Assert.Equal(1, issues.Count());

        }

        [Fact]
        public void QueryIssuesWithTakeExpression()
        {
            // create 2 issues with same summary
            var randomNumber = _random.Next(int.MaxValue);
            (new Issue(_jira, "TST") { Type = "1", Summary = "Test Summary " + randomNumber }).SaveChanges();
            (new Issue(_jira, "TST") { Type = "1", Summary = "Test Summary " + randomNumber }).SaveChanges();

            // query with take method to only return 1
            var issues = (from i in _jira.Issues
                         where i.Summary == randomNumber.ToString()
                         select i).Take(1);

            Assert.Equal(1, issues.Count());
        }

        [Fact]
        public void RetrieveIssueTypesForProject()
        {
            var issueTypes = _jira.GetIssueTypes("TST");

            Assert.Equal(4, issueTypes.Count());
            Assert.True(issueTypes.Any(i => i.Name == "Bug"));
        }

        [Fact]
        public void RetrievesIssuePriorities()
        {
            var priorities = _jira.GetIssuePriorities();

            Assert.True(priorities.Any(i => i.Name == "Blocker"));
        }

        [Fact]
        public void RetrievesIssueResolutions()
        {
            var resolutions = _jira.GetIssueResolutions();

            Assert.True(resolutions.Any(i => i.Name == "Fixed"));
        }

        [Fact]
        public void RetrievesIssueStatuses()
        {
            var statuses = _jira.GetIssueStatuses();

            Assert.True(statuses.Any(i => i.Name == "Open"));
        }

        /// <summary>
        /// https://bitbucket.org/farmas/atlassian.net-sdk/issue/3/serialization-error-when-querying-some
        /// </summary>
        [Fact]
        public void HandleRetrievalOfMessagesWithLargeContentStrings()
        {
            var issue = new Issue(_jira, "TST")
            {
                Type = "1",
                Summary = "Serialization nastiness"
            };

            issue.Description = File.ReadAllText("LongIssueDescription.txt"); 
            var serverIssue = issue.SaveChanges();

            Assert.Contains("Second stack trace:", serverIssue.Description);
        }

        [Fact]
        public void GetCustomFields()
        {
            var fields = _jira.GetCustomFields();
            Assert.Equal(1, fields.Count());
        }

        [Fact]
        public void GetProjectVersions()
        {
            var versions = _jira.GetProjectVersions("TST");
            Assert.Equal(3, versions.Count());
        }

        [Fact]
        public void GetProjectComponents()
        {
            var components = _jira.GetProjectComponents("TST");
            Assert.Equal(2, components.Count());
        }

        [Fact]
        public void UpdateVersionsOfIssue()
        {
            var summaryValue = "Test issue with versions (Updated)" + _random.Next(int.MaxValue);

            var issue = new Issue(_jira, "TST")
            {
                Type = "1",
                Summary = summaryValue
            };

            issue = issue.SaveChanges();

            issue.AffectsVersions.Add("1.0");
            issue.AffectsVersions.Add("2.0");

            issue.FixVersions.Add("3.0");
            issue.FixVersions.Add("2.0");

            var newIssue = issue.SaveChanges();

            Assert.Equal(2, newIssue.AffectsVersions.Count);
            Assert.True(newIssue.AffectsVersions.Any(v => v.Name == "1.0"));
            Assert.True(newIssue.AffectsVersions.Any(v => v.Name == "2.0"));

            Assert.Equal(2, newIssue.FixVersions.Count);
            Assert.True(newIssue.FixVersions.Any(v => v.Name == "2.0"));
            Assert.True(newIssue.FixVersions.Any(v => v.Name == "3.0"));

        }

        [Fact]
        public void CreateAndQueryIssueWithVersions()
        {
            var summaryValue = "Test issue with versions (Created)" + _random.Next(int.MaxValue);

            var issue = new Issue(_jira, "TST")
            {
                Type = "1",
                Summary = summaryValue
            };

            issue.AffectsVersions.Add("1.0");
            issue.AffectsVersions.Add("2.0");

            issue.FixVersions.Add("3.0");
            issue.FixVersions.Add("2.0");

            issue.SaveChanges();

            var newIssue = (from i in _jira.Issues
                            where i.AffectsVersions == "1.0" && i.AffectsVersions == "2.0" 
                                    && i.FixVersions == "2.0" && i.FixVersions == "3.0"
                            select i).First();

            Assert.Equal(2, newIssue.AffectsVersions.Count);
            Assert.True(newIssue.AffectsVersions.Any(v => v.Name == "1.0"));
            Assert.True(newIssue.AffectsVersions.Any(v => v.Name == "2.0"));

            Assert.Equal(2, newIssue.FixVersions.Count);
            Assert.True(newIssue.FixVersions.Any(v => v.Name == "2.0"));
            Assert.True(newIssue.FixVersions.Any(v => v.Name == "3.0"));
        }

        [Fact]
        public void CreateAndQueryIssueWithComponents()
        {
            var summaryValue = "Test issue with components (Created)" + _random.Next(int.MaxValue);

            var issue = new Issue(_jira, "TST")
            {
                Type = "1",
                Summary = summaryValue
            };

            issue.Components.Add("Server");
            issue.Components.Add("Client");

            issue = issue.SaveChanges();

            var newIssue = (from i in _jira.Issues
                            where i.Summary == summaryValue && i.Components == "Server" && i.Components == "Client"
                            select i).First();

            Assert.Equal(2, newIssue.Components.Count);
            Assert.True(newIssue.Components.Any(c => c.Name == "Server"));
            Assert.True(newIssue.Components.Any(c => c.Name == "Client"));
        }

        [Fact]
        public void UpdateComponentsOfIssue()
        {
            var summaryValue = "Test issue with components (Updated)" + _random.Next(int.MaxValue);

            var issue = new Issue(_jira, "TST")
            {
                Type = "1",
                Summary = summaryValue
            };

            issue = issue.SaveChanges();

            issue.Components.Add("Server");
            issue.Components.Add("Client");

            var newIssue = issue.SaveChanges();

            Assert.Equal(2, newIssue.Components.Count);
            Assert.True(newIssue.Components.Any(c => c.Name == "Server"));
            Assert.True(newIssue.Components.Any(c => c.Name == "Client"));
        }

        [Fact]
        public void AddLabelsToIssue()
        {
            var summaryValue = "Test issue with labels (Updated)" + _random.Next(int.MaxValue);

            var issue = new Issue(_jira, "TST")
            {
                Type = "1",
                Summary = summaryValue
            };

            issue = issue.SaveChanges();

            issue.AddLabels("label1", "label2");
        }

        [Fact]
        public void CreateAndQueryIssueWithCustomField()
        {
            var summaryValue = "Test issue with custom field (Created)" + _random.Next(int.MaxValue);

            var issue = new Issue(_jira, "TST")
            {
                Type = "1",
                Summary = summaryValue
            };
            issue["Custom Text Field"] = "My new value";

            issue.SaveChanges();

            var newIssue = (from i in _jira.Issues
                            where i.Summary == summaryValue && i["Custom Text Field"] == "My new value"
                            select i).First();

            Assert.Equal("My new value", newIssue["Custom Text Field"]);
        }

        [Fact]
        public void UpdateIssueWithCustomField()
        {
            var summaryValue = "Test issue with custom field (Updated)" + _random.Next(int.MaxValue);

            var issue = new Issue(_jira, "TST")
            {
                Type = "1",
                Summary = summaryValue
            };
            issue["Custom Text Field"] = "My new value";

            issue = issue.SaveChanges();

            issue["Custom Text Field"] = "My updated value";
            issue = issue.SaveChanges();

            Assert.Equal("My updated value", issue["Custom Text Field"]);
        }
    }
}
