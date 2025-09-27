using System.IO;

namespace RAWebServer.Utilities {
  public class ResourceUtilities {
    /// <summary>
    /// Reads an RDP file and extracts the value of a specified property. If the property is not found,
    /// returns the provided fallback value (or an empty string if no fallback is provided).
    /// </summary>
    /// <param name="path"></param>
    /// <param name="propertyName"></param>
    /// <param name="fallbackValue"></param>
    /// <returns></returns>
    /// <exception cref="FileNotFoundException">When the path does not resolve to a file.</exception>
    /// <exception cref="InvalidDataException">When the property name is missing the type (e.g., :s:, :i:, etc.)</exception>
    public static string GetRdpFileProperty(string path, string propertyName, string fallbackValue = "") {
      // check if the path exists
      if (!File.Exists(path)) {
        throw new FileNotFoundException("The specified RDP file does not exist.", path);
      }

      // ensure the property name includes the type (:s:, :i:, etc.)
      if (!propertyName.Contains(":")) {
        throw new InvalidDataException("The property name must include the type (e.g., :s:, :i:, etc.).");
      }
      if (!propertyName.EndsWith(":")) {
        propertyName += ":";
      }

      var fileReader = new StreamReader(path);
      string line;
      var value = fallbackValue;
      while ((line = fileReader.ReadLine()) != null) {
        if (line.StartsWith(propertyName)) {
          // split into 3 parts only (property name, type, value)]
          var parts = line.Split(new char[] { ':' }, 3);
          if (parts.Length == 3) {
            // set the found value to the third part
            value = parts[2];
            // we do not break because we want the last instance of the property
          }
        }
      }
      fileReader.Close();

      value = value.Trim();

      if (string.IsNullOrEmpty(value) && !string.IsNullOrEmpty(fallbackValue)) {
        return fallbackValue;
      }
      return value;
    }
  }
}
