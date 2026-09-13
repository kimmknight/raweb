using System;
using System.Linq;

namespace RAWeb.Server.Management;

public static class RdpSignableProperties {
  /// <summary>
  /// The fixed, ordered set of RDP properties that can be included in a signature and the
  /// display name used for each in the "signscope:s:" value.
  /// <br /><br />
  /// Properties must be present in this order in the "signscope:s:" value.
  /// </summary>
  public static readonly (string Prefix, string DisplayName)[] All = [
    ("full address:s:", "Full Address"),
    ("alternate full address:s:", "Alternate Full Address"),
    ("pcb:s:", "PCB"),
    ("use redirection server name:i:", "Use Redirection Server Name"),
    ("server port:i:", "Server Port"),
    ("negotiate security layer:i:", "Negotiate Security Layer"),
    ("enablecredsspsupport:i:", "EnableCredSspSupport"),
    ("disableconnectionsharing:i:", "DisableConnectionSharing"),
    ("autoreconnection enabled:i:", "AutoReconnection Enabled"),
    ("gatewayhostname:s:", "GatewayHostname"),
    ("gatewayusagemethod:i:", "GatewayUsageMethod"),
    ("gatewayprofileusagemethod:i:", "GatewayProfileUsageMethod"),
    ("gatewaycredentialssource:i:", "GatewayCredentialsSource"),
    ("support url:s:", "Support URL"),
    ("promptcredentialonce:i:", "PromptCredentialOnce"),
    ("require pre-authentication:i:", "Require pre-authentication"),
    ("pre-authentication server address:s:", "Pre-authentication server address"),
    ("alternate shell:s:", "Alternate Shell"),
    ("shell working directory:s:", "Shell Working Directory"),
    ("remoteapplicationprogram:s:", "RemoteApplicationProgram"),
    ("remoteapplicationexpandworkingdir:s:", "RemoteApplicationExpandWorkingdir"),
    ("remoteapplicationmode:i:", "RemoteApplicationMode"),
    ("remoteapplicationguid:s:", "RemoteApplicationGuid"),
    ("remoteapplicationname:s:", "RemoteApplicationName"),
    ("remoteapplicationicon:s:", "RemoteApplicationIcon"),
    ("remoteapplicationfile:s:", "RemoteApplicationFile"),
    ("remoteapplicationfileextensions:s:", "RemoteApplicationFileExtensions"),
    ("remoteapplicationcmdline:s:", "RemoteApplicationCmdLine"),
    ("remoteapplicationexpandcmdline:s:", "RemoteApplicationExpandCmdLine"),
    ("prompt for credentials:i:", "Prompt For Credentials"),
    ("authentication level:i:", "Authentication Level"),
    ("audiomode:i:", "AudioMode"),
    ("redirectdrives:i:", "RedirectDrives"),
    ("redirectprinters:i:", "RedirectPrinters"),
    ("redirectcomports:i:", "RedirectCOMPorts"),
    ("redirectsmartcards:i:", "RedirectSmartCards"),
    ("redirectposdevices:i:", "RedirectPOSDevices"),
    ("redirectclipboard:i:", "RedirectClipboard"),
    ("devicestoredirect:s:", "DevicesToRedirect"),
    ("drivestoredirect:s:", "DrivesToRedirect"),
    ("loadbalanceinfo:s:", "LoadBalanceInfo"),
    ("redirectdirectx:i:", "RedirectDirectX"),
    ("rdgiskdcproxy:i:", "RDGIsKDCProxy"),
    ("kdcproxyname:s:", "KDCProxyName"),
    ("eventloguploadaddress:s:", "EventLogUploadAddress"),
    ("enablerdsaadauth:i:", "EnableRdsAadAuth"),
    ("redirectwebauthn:i:", "RedirectWebAuthn"),
  ];

  private static readonly string[] s_lineSeparators = ["\r\n", "\n"];

  /// <summary>
  /// True if the RDP file contents contain a "signature:s:" line.
  /// </summary>
  public static bool ContainsSignature(string content) {
    return content
      .Split(s_lineSeparators, StringSplitOptions.None)
      .Any(line => line.StartsWith("signature:s:", StringComparison.OrdinalIgnoreCase));
  }

  /// <summary>
  /// True if the given property line (e.g. "full address:s:myserver") starts with one of the
  /// properties covered by RDP file signing.
  /// </summary>
  public static bool IsSignableLine(string propertyLine) {
    return All.Any(p => propertyLine.StartsWith(p.Prefix, StringComparison.OrdinalIgnoreCase));
  }
}
