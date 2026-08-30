using System.IO;
using NJsonSchema;
using NJsonSchema.NewtonsoftJson.Generation;

namespace RAWeb.Server.Installer.Setup;

/// <summary>
/// Generates setup.schema.json from <see cref="SetupManifest"/>. It is invoked via the
/// "--generate-schema &lt;path&gt;" command-line switch by RAWeb.Server.Installer.csproj's
/// post-build target.
/// </summary>
public static class SetupSchemaGenerator {
  public const string Switch = "--generate-schema";

  public static void GenerateAndWrite(string outputPath) {
    var settings = new NewtonsoftJsonSchemaGeneratorSettings { SchemaType = SchemaType.JsonSchema };
    var schema = NewtonsoftJsonSchemaGenerator.FromType<SetupManifest>(settings);

    schema.Title = "RAWeb setup manifest";
    schema.Description =
      "Describes how a RAWeb release archive should be installed. Shipped as setup.json in the root " +
      "of each release archive. The presence of this file tells RAWeb.Server.Installer that it can " +
      "perform the installation natively instead of handing off to setup.ps1. Generated from " +
      "SetupManifest.cs (do not hand-edit).";

    // A handful of constraints don't have a clean attribute-based expression on the POCO, so they're
    // patched onto the generated schema directly.
    var optionSchema = schema.Definitions["SetupOption"];
    optionSchema.Properties["type"].Pattern = null;
    optionSchema.Properties["type"].Enumeration.Clear();
    foreach (var value in new[] { "bool", "enum", "string" }) {
      optionSchema.Properties["type"].Enumeration.Add(value);
    }

    var defaultProperty = optionSchema.Properties["default"];
    defaultProperty.OneOf.Clear();
    defaultProperty.Type = JsonObjectType.String | JsonObjectType.Boolean;

    // Files that reference this schema do so via a "$schema" property at the document root, which
    // isn't part of the SetupManifest wire format itself, so additionalProperties:false would
    // otherwise reject it.
    schema.Properties["$schema"] = new JsonSchemaProperty { Type = JsonObjectType.String };

    var fullPath = Path.GetFullPath(outputPath);
    Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
    File.WriteAllText(fullPath, schema.ToJson());
  }
}
