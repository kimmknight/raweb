<%@ Control Language="C#" AutoEventWireup="true" %>

<script runat="server">
    public string title { get; set; }
    public string additional { get; set; }
</script>

<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title><%= title ?? "RemoteApps" %></title>
    <link rel="stylesheet" href='<%= ResolveUrl("~/lib/winui.css") %>'>
    <meta theme-color="#000000">
    <link rel="icon" href="<%= ResolveUrl("~/lib/assets/icon.svg") %>" type="image/svg+xml">
    <link rel="manifest" href="<%= ResolveUrl("~/manifest.aspx") %>">
    <%= additional %>
</head>