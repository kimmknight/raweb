---
title: $t{{ policies.App.ForcedLanguage.title }}
nav_title: Force UI language
redirects:
  - policies/App.ForcedLanguage
---

By default, the RAWeb web client uses each user's browser language preference to determine which language to display the interface in. This policy overrides that preference and forces all users to see the interface in a specific language.

## Language codes

The language must be specified as a [BCP 47](https://en.wikipedia.org/wiki/IETF_language_tag) language tag. Some examples:

| Language              | Code    |
| --------------------- | ------- |
| English (US)          | `en-US` |
| English (UK)          | `en-GB` |
| English (Australia)   | `en-AU` |
| English (Canada)      | `en-CA` |
| English (New Zealand) | `en-NZ` |
| Chinese (Simplified)  | `zh-CN` |

RAWeb ships with translation files for the languages listed above. If a forced language is specified that does not have a translation file, the interface will fall back to en-US English.

To add additional langauges, please review [Translating RAWeb](https://github.com/kimmknight/raweb/blob/master/TRANSLATING.md).

<PolicyDetails translationKeyPrefix="policies.App.ForcedLanguage" open />
