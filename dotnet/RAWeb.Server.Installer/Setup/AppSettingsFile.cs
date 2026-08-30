using System.IO;
using System.Xml;

namespace RAWeb.Server.Installer.Setup;

/// <summary>
/// Reads and writes App_Data\appSettings.config, the file that carries option values into the app.
/// </summary>
public sealed class AppSettingsFile {
  private readonly XmlDocument _document;
  private readonly XmlElement _root;

  private AppSettingsFile(XmlDocument document, XmlElement root) {
    _document = document;
    _root = root;
  }

  public static AppSettingsFile Open(string path) {
    var document = new XmlDocument();

    if (File.Exists(path)) {
      document.Load(path);
    }
    else {
      document.LoadXml("<?xml version=\"1.0\"?><appSettings></appSettings>");
    }

    if (document.SelectSingleNode("//appSettings") is not XmlElement root) {
      root = document.CreateElement("appSettings");
      document.AppendChild(root);
    }

    return new AppSettingsFile(document, root);
  }

  /// <summary>
  /// Reads a key without throwing, used to carry an existing installation's choices into an upgrade.
  /// </summary>
  public static string? TryRead(string path, string key) {
    if (!File.Exists(path)) {
      return null;
    }

    try {
      return Open(path).Get(key);
    }
    catch (XmlException) {
      return null;
    }
  }

  public string? Get(string key) => (_root.SelectSingleNode($"add[@key='{key}']") as XmlElement)?.GetAttribute("value");

  public void Set(string key, string value) {
    if (_root.SelectSingleNode($"add[@key='{key}']") is XmlElement existing) {
      existing.SetAttribute("value", value);
      return;
    }

    var element = _document.CreateElement("add");
    element.SetAttribute("key", key);
    element.SetAttribute("value", value);
    _root.AppendChild(element);
  }

  public void Save(string path) {
    Directory.CreateDirectory(Path.GetDirectoryName(path)!);
    _document.Save(path);
  }
}
