using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using PriFormat;

namespace RAWeb.Server.Management;

/// <summary>
/// Provides functionality for reading string resources from a PRI file.
/// <br /><br />
/// This depends on the PriFormat library to parse and read resources from PRI files.
/// Its source code can be found at: <see href="https://github.com/chausner/PriTools"/>.
/// <br />
/// To build the PriFormat libary, clone the repository (and the submodule), modify the
/// .csproj file to target net462, open a terminal in the repository root, and run
/// <c>dotnet build -c Release</c>.
/// </summary>
internal class PriReader : IDisposable {
  private readonly FileStream _priStream;
  private readonly PriFile _priFile;
  private readonly ResourceMapSection _resourceMapSection;

  /// <summary>
  /// Initializes a new instance of the <see cref="PriReader"/> class using the specified PRI file path.
  /// </summary>
  /// <param name="priFilePath">The full path to the PRI file.</param>
  /// <exception cref="ArgumentNullException">Thrown if <paramref name="priFilePath"/> is null or empty.</exception>
  /// <exception cref="FileNotFoundException">Thrown if the specified PRI file cannot be found.</exception>
  /// <exception cref="InvalidDataException">Thrown if the PRI file does not contain a resource map section.</exception>
  public PriReader(string priFilePath) {
    if (string.IsNullOrEmpty(priFilePath)) {
      throw new ArgumentNullException(nameof(priFilePath));
    }

    if (!File.Exists(priFilePath)) {
      throw new FileNotFoundException("PRI file not found", priFilePath);
    }

    _priStream = File.OpenRead(priFilePath);
    _priFile = PriFile.Parse(_priStream);

    // get the section of the PRI file that contains the resource map
    var resourceMapSectionRef = _priFile.PriDescriptorSection.PrimaryResourceMapSection;
    if (resourceMapSectionRef is null) {
      throw new InvalidDataException("PRI file does not contain a resource map section");
    }
    _resourceMapSection = _priFile.GetSectionByRef(resourceMapSectionRef.Value);
  }

  /// <summary>
  /// Reads the string resource value associated with a given full resource name.
  /// </summary>
  /// <param name="fullName">The full name of the resource to read.</param>
  /// <returns>
  /// The string value of the resource if found; otherwise, <c>null</c>.
  /// </returns>
  public string? ReadResource(string fullName) {
    if (fullName.StartsWith("ms-resource:")) {
      fullName = "\\" + fullName.Substring("ms-resource:".Length);
    }

    if (!fullName.StartsWith("\\")) {
      fullName = "\\" + fullName;
    }

    fullName = fullName.Replace('/', '\\');

    // find a candidate set in the resource map with the specified name
    var candidateSet = _resourceMapSection.CandidateSets.Values.FirstOrDefault(c => {
      var item = _priFile.GetResourceMapItemByRef(c.ResourceMapItem);
      return string.Equals(item.FullName, fullName, StringComparison.OrdinalIgnoreCase);
    });
    if (candidateSet is null) {
      // if there was no match, and the full name does no start with the implicit folder Resources, try again with that prefix
      if (!fullName.StartsWith(@"\Resources\", StringComparison.OrdinalIgnoreCase)) {
        return ReadResource(@"\Resources" + fullName);
      }

      return null;
    }

    // look through the candidates for a data item
    foreach (var candidate in candidateSet.Candidates) {

      // check if the candidate is for the current system language
      var systemLanguage = CultureInfo.CurrentUICulture.Name;
      var candidateIsForSystemLanguage = true; // default to true for language-neutral candidates
      var qualifierSet = _priFile.GetSectionByRef(_resourceMapSection.DecisionInfoSection).QualifierSets[candidate.QualifierSet];
      foreach (var qualifier in qualifierSet.Qualifiers) {
        if (qualifier.Type == QualifierType.Language) {
          candidateIsForSystemLanguage = qualifier.Value.Equals(systemLanguage, StringComparison.OrdinalIgnoreCase);
          break; // once we have found the language qualifier, we do not need to look at other qualifiers
        }
      }

      // skip candidates that are not for the system language (or language-neutral)
      if (!candidateIsForSystemLanguage) {
        continue;
      }

      // keep skipping candidates until we find one with data
      ByteSpan span;
      if (candidate.DataItem != null)
        span = _priFile.GetDataItemByRef(candidate.DataItem.Value);
      else if (candidate.Data != null)
        span = candidate.Data.Value;
      else
        continue;

      var result = ReadStringFromByteSpan(span, candidate.Type);
      if (fullName.Contains("ScreenClippingAppName")) {
        Console.WriteLine($"Candidate Type: {candidate.Type}, Result: '{result}'");
      }
      if (!string.IsNullOrEmpty(result)) {
        return result;
      }
    }

    return null;
  }

  /// <summary>
  /// Reads and decodes a string from a specific byte span in the PRI file stream.
  /// </summary>
  /// <param name="span">The byte span representing the location and length of the data to read.</param>
  /// <param name="type">The encoding type used for the resource value.</param>
  /// <returns>
  /// The decoded string value, or <c>null</c> if the data could not be read.
  /// </returns>
  private string? ReadStringFromByteSpan(ByteSpan span, ResourceValueType type) {
    _priStream.Seek(span.Offset, SeekOrigin.Begin);
    var buffer = new byte[span.Length];
    var bytesRead = _priStream.Read(buffer, 0, buffer.Length);

    Encoding enc = type switch {
      ResourceValueType.AsciiString or ResourceValueType.AsciiPath => Encoding.ASCII,
      ResourceValueType.Utf8String or ResourceValueType.Utf8Path => Encoding.UTF8,
      _ => Encoding.Unicode
    };

    return enc.GetString(buffer, 0, bytesRead).TrimEnd('\0');
  }

  /// <summary>
  /// Releases the underlying file stream associated with the PRI file.
  /// </summary>
  public void Dispose() {
    _priStream.Dispose();
  }
}
