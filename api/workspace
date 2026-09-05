<?xml version="1.0" encoding="utf-8"?>
<ResourceCollection PublishedDate="2026-08-01T12:00:00Z" SchemaVersion="2.1" xmlns="http://schemas.microsoft.com/ts/2007/05/tswf">
  <Publisher Name="RAWEB-DEMO" ID="demo.raweb.local" LastUpdated="2026-08-01T12:00:00Z" SupportsReconnect="true" Description="RAWeb Demo Workspace">
    <TerminalServers>
      <TerminalServer ID="ts01.demo.raweb.local" />
      <TerminalServer ID="ts02.demo.raweb.local" />
      <TerminalServer ID="ts07.demo.raweb.local:13193" />
      <TerminalServer ID="JACKDESKTOP2024.local" />
    </TerminalServers>
    <Resources>
      <Resource ID="notepad" Alias="managed-resources/notepad" Title="Notepad" Type="RemoteApp" LastUpdated="2026-08-01T12:00:00Z">
        <HostingTerminalServer>
          <TerminalServerRef Ref="ts01.demo.raweb.local" />
          <ResourceFile URL="/api/resources/managed-resources/notepad?from=mr&amp;features=supportsWake" FileExtension=".rdp" />
        </HostingTerminalServer>
        <FileExtension Name=".txt" />
        <FileExtension Name=".log" />
        <Folders>
          <Folder Name="/" />
          <Folder Name="/Productivity" />
        </Folders>
        <Icons>
          <Icon FileType="Png" FileURL="/api/resources/image/managed-resources/notepad?format=png" Dimensions="64x64" />
        </Icons>
      </Resource>
      <Resource ID="excel" Alias="managed-resources/excel" Title="Excel" Type="RemoteApp" LastUpdated="2026-08-01T12:00:00Z">
        <HostingTerminalServer>
          <TerminalServerRef Ref="ts01.demo.raweb.local" />
          <ResourceFile URL="/api/resources/managed-resources/excel?from=mr" FileExtension=".rdp" />
        </HostingTerminalServer>
        <FileExtension Name=".xlsx" />
        <FileExtension Name=".csv" />
        <Folders>
          <Folder Name="/" />
          <Folder Name="/Productivity" />
        </Folders>
        <Icons>
          <Icon FileType="Png" FileURL="/api/resources/image/managed-resources/excel?format=png" Dimensions="64x64" />
        </Icons>
      </Resource>
      <Resource ID="accounting-desktop" Alias="managed-resources/accounting-desktop" Title="Accounting Desktop" Type="Desktop" LastUpdated="2026-08-01T12:00:00Z">
        <HostingTerminalServer>
          <TerminalServerRef Ref="ts02.demo.raweb.local" />
          <ResourceFile URL="/api/resources/managed-resources/accounting-desktop?from=mr&amp;features=supportsWake" FileExtension=".rdp" />
        </HostingTerminalServer>
        <Folders>
          <Folder Name="/Desktops" />
        </Folders>
        <Icons>
          <Icon FileType="Png" FileURL="/api/resources/image/managed-resources/accounting-desktop?format=png&amp;frame=pc" Dimensions="512x320" />
        </Icons>
      </Resource>
      <Resource ID="gis-server" Alias="managed-resources/GIS Server" Title="GIS Server" Type="Desktop" LastUpdated="2026-08-01T12:00:00Z">
        <HostingTerminalServer>
          <TerminalServerRef Ref="ts07.demo.raweb.local:13193" />
          <ResourceFile URL="/api/resources/managed-resources/GIS%20Server?from=mr&amp;features=supportsWake" FileExtension=".rdp" />
        </HostingTerminalServer>
        <Folders>
          <Folder Name="/Desktops" />
        </Folders>
        <Icons>
          <Icon FileType="Png" FileURL="/api/resources/image/managed-resources/GIS%20Server?format=png&amp;frame=pc" Dimensions="512x320" />
        </Icons>
      </Resource>
      <Resource ID="davinci-resolve" Alias="managed-resources/DaVinci Resolve" Title="DaVinci Resolve" Type="RemoteApp" LastUpdated="2026-08-01T12:00:00Z">
        <HostingTerminalServer>
          <TerminalServerRef Ref="JACKDESKTOP2024.local" />
          <ResourceFile URL="/api/resources/managed-resources/DaVinci%20Resolve?from=mr" FileExtension=".rdp" />
        </HostingTerminalServer>
        <Folders>
          <Folder Name="/" />
        </Folders>
        <Icons>
          <Icon FileType="Png" FileURL="/api/resources/image/managed-resources/DaVinci%20Resolve?format=png" Dimensions="64x64" />
        </Icons>
      </Resource>
    </Resources>
  </Publisher>
</ResourceCollection>
