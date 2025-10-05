---
title: File type associations for RAWeb webfeed clients
nav_title: File type associations
---

RAWeb can associate file types on clients to RemoteApps. For example, you can configure RAWeb so that when a client opens a DOC file, it opens with a Microsoft Word RemoteApp.

**Requirements**

- Client must be connected to RAWeb's webfeed.
- The client webfeed URL must be configured via Group Policy (or Local Policy).
- RDP files need to have file types listed within.
- RemoteApps on host must be configured to allow command line parameters.

File type associations **will not work** if you configure the webfeed URL via the RemoteApp and Desktop Connections control panel! You **must** configure it via policy. If you cannot configure the policy, you may be able to use [Kelbin Tegelaar's workaround](https://www.cyberdrain.com/adding-remote-app-file-associations-via-powershell/).

## Add file types to RDP files

With a text editor, open the RDP file for the application that you would like to add file type associations for.

Add a new line:

     remoteapplicationfileextensions:s:[file extensions separated by commas]

![][1]

[1]: ./add-file-types-to-rdp-files.png

## Configure the RemoteApp to allow command-line parameters

Using RemoteApp Tool, edit your RemoteApp and

set **Command line option** to **Optional**.

## Configure webfeed URL via local group policy on the client

Navigate to **User Configuration > Administrative Templates > Windows Components > Remote Desktop Services > RemoteApp and Desktop Connections**.

Edit the policy setting **Specify default connection URL**. Click **Enabled** and enter the URL to the webfeed below. Click OK.

![][2]

[2]: ./configure-webfeed-url-via-local-group-policy-on-the-client.png

## Add document icons

If you want a custom icon displayed for a particular file type, put an **ICO** file into the icon folder with the same name as the rdp file followed by a dot (.) and then the file extension.

Example: To set an icon for all XLS files which will open in Excel 2010 (Excel2010.rdp) your would need an icon file named: Excel2010.xls.ico
