---
title: Configure the user cache
nav_title: User cache
redirects:
  - policies/UserCache.Enabled
  - policies/UserCache.StaleWhileRevalidate
---

## Enable the user cache

The user cache stores details about a user every time they sign in, and RAWeb will fall back to the details in the user cache if the domain controller cannot be reached. If RAWeb is unable to load group memberships from the domain, the group membership cached in the user cache is used instead.

For domain-joined Windows machines, when the user cache is enabled and the domain controller cannot be accessed, the authentication mechanism will populate RAWeb's user cache with the [cached domain logon information](https://learn.microsoft.com/en-us/troubleshoot/windows-server/user-profiles-and-logon/cached-domain-logon-information) stored by Windows, if available. By default, Windows caches the credentials of the last 10 users who have logged on to the machine.

RAWeb's mechanism for verifying user and group information may be unable to access user information if the machine with RAWeb is in a domain environment with one-way trust relationships. In such environments, if the domain controller for the user's domain cannot be reached, RAWeb will only be able to populate the user cache upon initial logon.

When the `UserCache.Enabled` appSetting (policy) is enabled, a SQLite database is created in the App_Data folder that contains username, full name, domain, user sid, and group names and sids.

<PolicyDetails translationKeyPrefix="policies.UserCache.Enabled" />

## Leverge the user cache for faster load times

The user cache also improves the time it takes for RAWeb to load user details. When the user cache is enabled, RAWeb will use the cached user details while it revalidates the details in the background. This can significantly improve performance in environments with a large number of groups or slow domain controllers.

By default, RAWeb will use the cached user details for up to 1 minute before requiring revalidation. This duration can be adjusted using the `UserCache.StaleWhileRevalidate` policy. \
If RAWeb is unable to revalidate the user details (for example, if the domain controller is unreachable), it will continue to use the cached details until revalidation is successful.

<PolicyDetails translationKeyPrefix="policies.UserCache.StaleWhileRevalidate" />
