﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Atlassian.Jira
{
    /// <summary>
    /// Represents the operations on the issue link of jira.
    /// </summary>
    public interface IIssueRemoteLinkService
    {
        /// <summary>
        /// Creates an remote issue link for an issue.
        /// </summary>
        /// <param name="issueKey">Key of the issue.</param>
        /// <param name="remoteUrl">Remote url to link to.</param>
        /// <param name="title">Title of the remote link.</param>
        /// <param name="summary">Summary of the remote link.</param>
        /// <param name="token">Cancellation token for this operation.</param>
        Task CreateRemoteLinkAsync(string issueKey, string remoteUrl, string title, string summary, CancellationToken token = default(CancellationToken));

        /// <summary>
        /// Returns all remote links associated with a given issue.
        /// </summary>
        /// <param name="issue">The issue to retrieve remote links for.</param>
        /// <param name="token">Cancellation token for this operation.</param>
        Task<IEnumerable<IssueRemoteLink>> GetRemoteLinksForIssueAsync(string issueKey, CancellationToken token = default(CancellationToken));
    }
}
