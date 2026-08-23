using System.Runtime.InteropServices;
using Microsoft.Web.Administration;

namespace RAWeb.Server.Installer.Setup;

public sealed record IisApplicationInfo(string SiteName, string Path, string PhysicalPath, string ApplicationPool);

public sealed record IisBindingInfo(string Protocol, int Port, string? CertificateHash);

/// <summary>
/// Thin wrapper over <see cref="ServerManager"/> covering everything setup.ps1 did through the
/// WebAdministration module. Each public method opens its own ServerManager so a failure never
/// leaves a half-committed configuration handle open.
/// </summary>
public sealed class IisManager {
  private const string AnonymousAuthenticationSection = "system.webServer/security/authentication/anonymousAuthentication";
  private const string WindowsAuthenticationSection = "system.webServer/security/authentication/windowsAuthentication";

  /// <summary>
  /// Gets the names of all IIS web sites on the machine.
  /// </summary>
  public IReadOnlyList<string> GetSiteNames() {
    using var manager = new ServerManager();
    return [.. manager.Sites.Select(site => site.Name)];
  }

  /// <summary>
  /// Gets the full physical path of the root application of the named IIS web site.
  /// <br/><br/>
  /// If the site does not exist, it returns null.
  /// </summary>
  public string? GetSitePhysicalPath(string siteName) {
    using var manager = new ServerManager();
    var root = manager.Sites[siteName]?.Applications["/"]?.VirtualDirectories["/"]?.PhysicalPath;
    return root is null ? null : Environment.ExpandEnvironmentVariables(root);
  }

  /// <summary>
  /// Gets the list of all applications across all sites, including their physical paths and application pools.
  /// </summary>
  /// <returns></returns>
  public IReadOnlyList<IisApplicationInfo> GetApplications() {
    using var manager = new ServerManager();

    var applications = new List<IisApplicationInfo>();
    foreach (var site in manager.Sites) {
      foreach (var app in site.Applications) {
        var physicalPath = Environment.ExpandEnvironmentVariables(app.VirtualDirectories["/"]?.PhysicalPath ?? "");
        applications.Add(new IisApplicationInfo(site.Name, app.Path, physicalPath, app.ApplicationPoolName));
      }
    }

    return applications;
  }

  /// <summary>
  /// Gets the application info for the given site and virtual path.
  /// </summary>
  public IisApplicationInfo? GetApplication(string siteName, string virtualPath) {
    using var manager = new ServerManager();
    var site = manager.Sites[siteName];
    var application = site?.Applications[ToApplicationPath(virtualPath)];
    if (application is null) {
      return null;
    }

    return new IisApplicationInfo(
      siteName,
      application.Path,
      Environment.ExpandEnvironmentVariables(application.VirtualDirectories["/"]?.PhysicalPath ?? ""),
      application.ApplicationPoolName
    );
  }

  /// <summary>
  /// A convenience method to get the physical path of an application.
  /// </summary>
  public string? GetApplicationPhysicalPath(string siteName, string virtualPath) =>
    GetApplication(siteName, virtualPath)?.PhysicalPath;

  /// <summary>
  /// Gets the list of bindings for a named IIS web site.
  /// <br/><br/>
  /// In IIS parlance, a "binding" is a combination of protocol, port, and optional
  /// certificate that tells IIS how to listen for incoming requests.
  /// </summary>
  public IReadOnlyList<IisBindingInfo> GetBindings(string siteName) {
    using var manager = new ServerManager();
    var site = manager.Sites[siteName];
    if (site is null) {
      return [];
    }

    var bindings = site.Bindings
      .Select(binding => new IisBindingInfo(
        binding.Protocol,
        binding.EndPoint?.Port ?? 0,
        binding.CertificateHash is { Length: > 0 } hash ? BitConverter.ToString(hash).Replace("-", "") : null)
      );

    return [.. bindings];
  }

  /// <summary>
  /// Returns true when the specified application pool name exists.
  /// </summary>
  /// <param name="name"></param>
  /// <returns></returns>
  public bool ApplicationPoolExists(string name) {
    using var manager = new ServerManager();
    return manager.ApplicationPools[name] is not null;
  }

  /// <summary>
  /// Creates the pool when missing and applies the identity settings RAWeb requires.
  /// </summary>
  /// <returns>True when the pool was created by this call.</returns>
  public bool EnsureApplicationPool(string name) {
    using var manager = new ServerManager();
    var pool = manager.ApplicationPools[name];
    var created = false;

    if (pool is null) {
      pool = manager.ApplicationPools.Add(name);
      created = true;
    }

    pool.ProcessModel.IdentityType = ProcessModelIdentityType.ApplicationPoolIdentity;
    pool.ProcessModel.LoadUserProfile = true; // required for use of WSL2
    manager.CommitChanges();

    return created;
  }

  /// <summary>
  /// Removes the named application pool if it exists.
  /// </summary>
  /// <param name="name"></param>
  public void RemoveApplicationPool(string name) {
    using var manager = new ServerManager();
    var pool = manager.ApplicationPools[name];
    if (pool is null) {
      return;
    }

    manager.ApplicationPools.Remove(pool);
    manager.CommitChanges();
  }

  /// <summary>
  /// Starts the named application pool if it exists and is not already started.
  /// </summary>
  /// <param name="name"></param>
  public void StartApplicationPool(string name) => SetApplicationPoolState(name, start: true);

  /// <summary>
  /// Stops the named application pool if it exists and is not already stopped.
  /// </summary>
  /// <param name="name"></param>
  public void StopApplicationPool(string name) => SetApplicationPoolState(name, start: false);

  /// <summary>
  /// Recycles the pool so it releases file locks on the previous version's directory.
  /// </summary>
  public void RecycleApplicationPool(string name) {
    using var manager = new ServerManager();
    var pool = manager.ApplicationPools[name];
    if (pool is null) {
      return;
    }

    try {
      pool.Recycle();
      Thread.Sleep(2000);
      return;
    }
    catch (Exception exception) when (exception is COMException or ServerManagerException) { }

    StopApplicationPool(name);
    Thread.Sleep(3000);
    StartApplicationPool(name);
  }

  /// <summary>
  /// Creates an application under the given site with the specified virtual path, physical path, and application pool.
  /// </summary>
  /// <exception cref="InstallFailedException">When the web site does not exist</exception>
  public void CreateApplication(string siteName, string virtualPath, string physicalPath, string applicationPoolName) {
    using var manager = new ServerManager();
    var site = manager.Sites[siteName] ?? throw new InstallFailedException($"IIS web site '{siteName}' does not exist.");

    var application = site.Applications.Add(ToApplicationPath(virtualPath), physicalPath);
    application.ApplicationPoolName = applicationPoolName;
    manager.CommitChanges();
  }

  /// <summary>
  /// Updates the physical path and/or application pool of an existing application. If either
  /// parameter is null, that property is left unchanged.
  /// <br/><br/>
  /// This method DOES NOT allow you to change the site name or virtual path since those are
  /// fundamental identifiers of the application in IIS.
  /// </summary>
  public void UpdateApplication(string siteName, string virtualPath, string? physicalPath, string? applicationPoolName) {
    using var manager = new ServerManager();
    var application = manager.Sites[siteName]?.Applications[ToApplicationPath(virtualPath)];
    if (application is null) {
      return;
    }

    if (physicalPath is not null) {
      application.VirtualDirectories["/"].PhysicalPath = physicalPath;
    }

    if (applicationPoolName is { Length: > 0 }) {
      application.ApplicationPoolName = applicationPoolName;
    }

    manager.CommitChanges();
  }

  /// <summary>
  /// Removes the application from the given site.
  /// <br/><br/>
  /// This does not delete the physical files.
  /// </summary>
  public void RemoveApplication(string siteName, string virtualPath) {
    using var manager = new ServerManager();
    var site = manager.Sites[siteName];
    var application = site?.Applications[ToApplicationPath(virtualPath)];
    if (site is null || application is null) {
      return;
    }

    site.Applications.Remove(application);
    manager.CommitChanges();
  }

  /// <summary>
  /// Enables WebSockets on the given site.
  /// <br/><br/>
  /// This is required for the websocket functionality
  /// used by guacd (the web connection client).
  /// </summary>
  /// <param name="siteName"></param>
  public void EnableWebSockets(string siteName) {
    using var manager = new ServerManager();
    var configuration = manager.GetWebConfiguration(siteName);
    var section = configuration.GetSection("system.webServer/webSocket");
    section["enabled"] = true;
    manager.CommitChanges();
  }

  /// <summary>
  /// Adds an HTTPS binding to the given site on the specified port.
  /// To link a certificate to the binding, call <see cref="BindCertificate"/> afterwards.
  /// </summary>
  /// <param name="siteName"></param>
  /// <param name="port"></param>
  /// <exception cref="InstallFailedException"></exception>
  public void AddHttpsBinding(string siteName, int port) {
    using var manager = new ServerManager();
    var site = manager.Sites[siteName]
      ?? throw new InstallFailedException($"IIS web site '{siteName}' does not exist.");

    site.Bindings.Add($"*:{port}:", "https");
    manager.CommitChanges();
  }

  /// <summary>
  /// Binds a certificate to an existing HTTPS binding on the given site and port.
  /// To create the HTTPS binding, use <see cref="AddHttpsBinding"/>.
  /// </summary>
  public void BindCertificate(string siteName, int port, byte[] certificateHash, string certificateStore) {
    using var manager = new ServerManager();
    var site = manager.Sites[siteName]
      ?? throw new InstallFailedException($"IIS web site '{siteName}' does not exist.");

    var binding = site.Bindings.FirstOrDefault(candidate =>
      candidate.Protocol == "https" && candidate.EndPoint?.Port == port);

    if (binding is null) {
      throw new InstallFailedException($"No HTTPS binding on port {port} to attach the certificate to.");
    }

    binding.CertificateStoreName = certificateStore;
    binding.CertificateHash = certificateHash;
    manager.CommitChanges();
  }

  /// <summary>
  /// Configures anonymous and Windows authentication on the application
  /// </summary>
  public void ConfigureAuthentication(string siteName, string virtualPath) {
    var location = ToLocation(siteName, virtualPath);

    using var manager = new ServerManager();
    var configuration = manager.GetApplicationHostConfiguration();

    // we include /auth for legacy reasons
    string[] paths = [location, location + "/auth"];

    foreach (var path in paths) {
      var anonymous = configuration.GetSection(AnonymousAuthenticationSection, path);
      anonymous["enabled"] = true;
      anonymous["userName"] = "";

      var windows = configuration.GetSection(WindowsAuthenticationSection, path);
      windows["enabled"] = true;
    }

    // we include /api/auth/authenticate-workspace for legacy reasons
    var workspace = configuration.GetSection(WindowsAuthenticationSection, location + "/api/auth/authenticate-workspace");
    workspace["enabled"] = true;

    manager.CommitChanges();
  }

  /// <summary>
  /// Removes the named Windows authentication providers so IIS stops adding WWW-Authenticate headers
  /// to its own 401 responses.
  /// </summary>
  /// <returns>The providers that were actually removed, so they can be restored on rollback.</returns>
  public IReadOnlyList<string> RemoveWindowsAuthenticationProviders(string siteName, string virtualPath, IReadOnlyList<string> providers) {
    var location = ToLocation(siteName, virtualPath);
    var removed = new List<string>();

    using var manager = new ServerManager();
    var collection = GetWindowsAuthenticationProviderCollection(manager, location);

    foreach (var provider in providers) {
      var element = collection.FirstOrDefault(item =>
        string.Equals((string?)item["value"], provider, StringComparison.OrdinalIgnoreCase)
      );

      if (element is not null) {
        collection.Remove(element);
        removed.Add(provider);
      }
    }

    if (removed.Count > 0) {
      manager.CommitChanges();
    }

    return removed;
  }

  /// <summary>
  /// Adds a list of Windows authentication providers to the given application. If any are already present, they are skipped.
  /// <br/><br/>
  /// Returns true when at least one provider was actually added (some may already be present).
  /// </summary>
  public bool AddAuthenticationProviders(string siteName, string virtualPathOrSubPath, IReadOnlyList<string> providers) {
    var location = ToLocation(siteName, virtualPathOrSubPath);

    using var manager = new ServerManager();
    var collection = GetWindowsAuthenticationProviderCollection(manager, location);
    var added = false;

    foreach (var provider in providers) {
      var exists = collection.Any(item =>
        string.Equals((string?)item["value"], provider, StringComparison.OrdinalIgnoreCase)
      );

      if (exists) {
        continue;
      }

      var element = collection.CreateElement("add");
      element["value"] = provider;
      collection.Add(element);
      added = true;
    }

    if (added) {
      manager.CommitChanges();
    }

    return added;
  }

  private static ConfigurationElementCollection GetWindowsAuthenticationProviderCollection(ServerManager manager, string location) {
    var section = manager.GetApplicationHostConfiguration().GetSection(WindowsAuthenticationSection, location);
    return section.GetCollection("providers");
  }

  /// <summary>
  /// Starts or stops the named application pool if it exists and is not already in the desired state.
  /// </summary>
  private void SetApplicationPoolState(string name, bool start) {
    using var manager = new ServerManager();
    var pool = manager.ApplicationPools[name];
    if (pool is null) {
      return;
    }

    try {
      if (start) {
        if (pool.State != ObjectState.Started) {
          pool.Start();
        }
      }
      else if (pool.State != ObjectState.Stopped) {
        pool.Stop();
      }
    }
    catch (Exception exception) when (exception is COMException or ServerManagerException) {
      // A pool that is mid-transition throws; the caller of this method polls or retries when it matters.
    }
  }

  /// <summary>
  /// Normalizes an application path so that is always starts with a
  /// single forward slash and has no trailing slash.
  /// This is the form the IIS API expects.
  /// </summary>
  public static string ToApplicationPath(string virtualPath) {
    var trimmed = virtualPath.Trim('/', '\\').Replace('\\', '/');
    return trimmed.Length == 0 ? "/" : "/" + trimmed;
  }

  /// <summary>
  /// Builds the applicationHost.config location path, such as "Default Web Site/RAWeb".
  /// </summary>
  public static string ToLocation(string siteName, string virtualPath) {
    var trimmed = virtualPath.Trim('/', '\\').Replace('\\', '/');
    return trimmed.Length == 0 ? siteName : siteName + "/" + trimmed;
  }
}
