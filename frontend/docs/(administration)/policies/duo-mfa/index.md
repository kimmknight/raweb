---
title: $t{{ policies.App.Auth.MFA.Duo.title }}
nav_title: Duo Universal Prompt
---

Enable this policy to require users to provide a second factor of authentication via Duo Security's Universal Prompt when signing in to RAWeb.

For alternative providers and MFA caveats, see [Enable multi-factor authentication (MFA) for the web app](/docs/security/mfa).

<PolicyDetails translationKeyPrefix="policies.App.Auth.MFA.Duo" />

## Authentication flow

When a user signs in to RAWeb with the Duo MFA policy enabled, the following flow occurs:

1. The user enters their username and password in RAWeb's sign-in form.
2. RAWeb verifies that the username and password are correct.
3. RAWeb updates its cache of the user's details (if the user cache is enabled).
4. RAWeb instructs the web client to load Duo's Universal Prompt.
5. The user selects their preferred second factor method in the Duo Universal Prompt and completes the authentication.
6. Duo redirects to RAWeb.
7. If RAWeb receives a successful authentication response from Duo, the user is signed in to RAWeb. If the response indicates a failure or is missing, the sign-in attempt is rejected.

<img width="500" src="./duo-auth-flow.webp">

## About Duo Security

[Duo](https://duo.com/) provides [multi-factor authentication services](https://duo.com/product/multi-factor-authentication-mfa) for a variety of applications and services. RAWeb integrates with Duo via the Duo Universal Prompt, which provides an interface for users to select their preferred second factor method during authentication.

Duo provides a free tier for up to 10 users. Larger teams can choose from several paid plans based on their needs. See [Duo's pricing page](https://duo.com/pricing) for more information. RAWeb's integration only requires the MFA feature, which is included in all plans.

## Create RAWeb application in Duo

1. Sign in to [admin.duosecurity.com](https://admin.duosecurity.com/) with your Duo account's admin credentials.
    - If you do not have an account, you can start a free trial at [signup.duo.com](https://signup.duo.com/). The trial will automatically switch to the free tier after 30 days.
2. From the home page, click **Add new..** and choose **Application**.\
   <img width="200" src="./add-new-application.webp">
3. Find the **Partner WebSDK** application in the catalog and click **Add**.\
   <img width="440" src="./catalog.webp">
4. In the **Basic Configuration** section, change the **Application name** to RAWeb (or another name of your choice).
5. In the **Basic Configuration** section, set **User access** to **Enable for all users** (or another option of your choice).
6. IN the **Universal Prompt** section, set **Activate Universal Prompt** to **Show new Universal Prompt**. RAWeb has not been tested with the classic prompt.
6. In the **Settings** section, set **Username normalization** to **Simple**. RAWeb sends the username in the format DOMAIN\username.
7. In the **Settings** section, set **Voice greeting** to *Sign in to RAWeb* (or another greeting of your choice). This greeting will be played when users choose to authenticate via phone call.
8. Click **Save** to create the application.

## Configure RAWeb to use Duo

1. From the application's page in Duo's Admin panel, locate the **Client ID**, **Client secret**, and **API hostname** values in the **Details** section. You will need these values to configure RAWeb.
2. In RAWeb's web interface, navigate to the **Policies** page.
3. Open the **Configure Duo Universal Prompt multi-factor authentication (MFA)** policy dialog.
4. Enable the policy and enter the **Client ID**, **Client secret**, and **API hostname** values obtained from Duo.
5. Click OK to save the policy.
6. Sign out of RAWeb and sign back in to test the configuration. After entering your credentials, you should be prompted to complete the second factor authentication via Duo's Universal Prompt.
