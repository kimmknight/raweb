---
title: $t{{ policies.LogFiles.DiscardAgeDays.title }}
nav_title: Log file retention
redirects:
  - policies/LogFiles.DiscardAgeDays
---

RAWeb's server will generate log files for select components to help with troubleshooting issues. They are generated and stored in the `App_Data\logs` folder. (For a standard installation, the full path is `C:\inetpub\RAWeb\App_Data\logs`.)

The number of log files can grow over time, so it is recommended to configure a retention policy to automatically delete old log files. If you do not configure a retention policy, RAWeb will discard log files older than 3 days.

<PolicyDetails translationKeyPrefix="policies.LogFiles.DiscardAgeDays" />

To completely disable log file generation, set this policy to **Disabled**.

To retain log files for a specific number of days, set this policy to **Enabled** and specify the desired number of days. For example, setting it to 7 days will retain log files for one week before they are automatically deleted. There is no limit on the number of days you may retain log files, but keep in mind that retaining log files for longer periods will consume more disk space.
