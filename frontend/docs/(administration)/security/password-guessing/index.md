---
title: Protect the sign-in page from password guessing
nav_title: Password guessing protection
---

RAWeb does not limit the number of sign-in attempts. If RAWeb can be reached from the internet, anyone can use its sign-in page to guess passwords for your users' accounts. We recommend placing a reverse proxy, a web application firewall, or a service such as Cloudflare in front of RAWeb and using it to limit sign-in attempts.

<InfoBar severity="caution" title="Workspace clients require NTLM">

RemoteApp and Desktop Connections and Windows App use NTLM (Windows authentication) to sign in to RAWeb. Many reverse proxies and similar services do not support NTLM, and some break it in ways that are dangerous. Before you put a tool in front of RAWeb, make sure that it supports NTLM. See [Reverse proxy via Nginx](/docs/reverse-proxy/nginx/) for an example of how a misconfigured proxy can apply one user's authentication to other users.

</InfoBar>

Jump to a section:

- [Protection that Windows provides](#windows)
- [Limit sign-in attempts with another tool](#rate-limit)
- [Make sure the tool supports NTLM](#ntlm)
- [Other options](#other-options)

## Protection that Windows provides {#windows}

RAWeb checks every username and password with Windows, so account lockout policies configured in Windows or Active Directory apply to sign-in attempts in RAWeb. An account lockout policy locks an account after a number of incorrect passwords.

Account lockout policies do not fully protect against password guessing:

- Someone can try one common password for many different accounts without locking any of them.
- Someone who knows a username can lock that user out on purpose.

Limiting the number of sign-in attempts from each network address helps with both problems.

## Limit sign-in attempts with another tool {#rate-limit}

If RAWeb is installed in a folder of the web site (for example, `/RAWeb`), add that folder to the start of each path in this section.

### Web app sign-in endpoints

Configure the tool to limit the number of **failed** requests that each network address can send to the following endpoints. When RAWeb rejects a username or password on these endpoints, it responds with status code `400 Bad Request`. Successful requests respond with `200 OK`.

| Endpoint                           | Used for                                                        |
| ---------------------------------- | --------------------------------------------------------------- |
| `POST /api/auth/authenticate`      | Signing in to the web app                                       |
| `POST /api/auth/authenticate/sudo` | Confirming a password before a privileged administrative action |
| `POST /api/auth/change-password`   | Changing a password                                             |

Counting only failed requests is important when many users share one public network address, such as an office network. If the tool counts every request, users who type their password correctly can be blocked when many of them sign in at the same time.

The `400 Bad Request` status code is also used for a few failures where the password was correct. For example, a new password that does not meet the password policy, or a non-administrator who tries to confirm a password for an administrative action. Leave enough room in the limit for users who make these mistakes.

If the tool cannot filter by response status code, limit all requests to these endpoints instead, and choose a limit that is high enough for the number of users who share each network address.

### Workspace sign-in endpoints

Workspace clients sign in with NTLM on the following endpoints. A tool cannot tell failed NTLM sign-ins apart from successful ones by status code because every NTLM sign-in includes `401 Unauthorized` responses while the client and server exchange messages. Limit all requests to these endpoints instead.

| Endpoint                               | Used for                                                           |
| -------------------------------------- | ------------------------------------------------------------------ |
| `GET /api/auth/authenticate-workspace` | Signing in from a workspace client                                 |
| `GET /auth/loginfeed.aspx`             | Signing in from a workspace client (RD Web Access-compatible path) |

Each sign-in from a workspace client involves sending several requests. Workspace clients also sign in again whenever they refresh their list of RemoteApps and desktops, and many clients that share one network address may refresh at the same time (for example, when users sign in to their computers in the morning). Choose a limit that is high enough for these refreshes. If the tool blocks one of the requests in the middle of a sign-in, the workspace client fails to sign in and tries again during its next refresh.

Windows account lockout policies are the main protection for these endpoints. See [Protection that Windows provides](#windows).

<InfoBar title="What users see">

When the tool blocks a sign-in request, RAWeb's sign-in page cannot read the tool's response, so it shows its usual message for an incorrect username or password. Let your users know that they may see this message after several failed attempts, even when they type their password correctly.

</InfoBar>

## Make sure the tool supports NTLM {#ntlm}

NTLM authenticates a connection instead of each individual request. After a workspace client signs in, it keeps using the same connection. For NTLM to work through a reverse proxy, the proxy must keep each client's connection paired with its own connection to RAWeb.

Some tools handle NTLM incorrectly:

- Tools that do not support NTLM cause workspace clients to fail to sign in, even with the correct password.
- Tools that share connections to RAWeb between different clients can let one user's authentication apply to other users. This is a serious security problem. See [Reverse proxy via Nginx](/docs/reverse-proxy/nginx/).

Check your tool's documentation for NTLM or Windows authentication support. After you put the tool in front of RAWeb, test it:

1. On a Windows device, add RAWeb as a workspace in RemoteApp and Desktop Connections. See [Access RAWeb resources as a workspace](/docs/workspaces/).
2. Confirm that the workspace signs in and lists your RemoteApps and desktops.
3. On a second device, sign in as a different user. Confirm that the second user sees only their own RemoteApps and desktops.

## Other options {#other-options}

If you do not use workspace clients, enable the [Block workspace client authentication](/docs/policies/block-workspace-auth/) policy. With the policy enabled, workspace clients cannot sign in, so a tool that does not support NTLM no longer breaks a feature that your users need.

If you only need RAWeb on your internal network, do not expose it to the internet. Users outside the network can connect through a VPN instead.

[Multi-factor authentication](/docs/security/mfa/) does not limit password guessing, but it prevents a guessed password from being enough to sign in to the web app.
