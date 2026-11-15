---
title: $t{{ policies.App.StatusBarVisibility.title }}
nav_title: Status bar
redirects:
  - policies/App.StatusBarVisibility
---

The status bar appears at the bottom of pages that list resources. It shows the number of items and has buttons for switching between views. By default, it only appears in the details view. Each user can change this in their settings. This policy sets it for all users.

The policy accepts these values:

| Value          | Behavior                                         |
| -------------- | ------------------------------------------------ |
| `never`        | The status bar never appears.                    |
| `always`       | The status bar appears in every view.            |
| `details-only` | The status bar only appears in the details view. |

<PolicyDetails translationKeyPrefix="policies.App.StatusBarVisibility" open />
