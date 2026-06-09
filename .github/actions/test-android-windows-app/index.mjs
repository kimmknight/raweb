// @ts-check
import { execSync, spawn, spawnSync } from "child_process";
import fs from "fs";
import net from "net";
import path from "path";

if (process.platform !== "win32") {
  throw new Error("This action is only supported on Windows runners.");
}

if (!process.env.GITHUB_WORKSPACE) {
  throw new Error("GITHUB_WORKSPACE environment variable is not set.");
}
const GITHUB_WORKSPACE = process.env.GITHUB_WORKSPACE;

if (!process.env.GITHUB_ACTION_PATH) {
  throw new Error("GITHUB_ACTION_PATH environment variable is not set.");
}
const GITHUB_ACTION_PATH = process.env.GITHUB_ACTION_PATH;

if (!process.env.ANDROID_HOME) {
  throw new Error("ANDROID_HOME environment variable is not set.");
}
const ANDROID_HOME = process.env.ANDROID_HOME;

class TestWindowsAppAction {
  frontendDir = path.join(GITHUB_WORKSPACE, "frontend");
  distPath = path.join(GITHUB_WORKSPACE, "dotnet", "RAWeb.Server", "dist");
  serverExe = path.join(this.distPath, "raweb.exe");
  certsDir = path.join(this.frontendDir, "certs", "localhost");
  appiumDir = path.join(GITHUB_ACTION_PATH, "appium");

  async start() {
    /** @type {number | undefined} */
    let serverPid = undefined;
    /** @type {number | undefined} */
    let androidPid = undefined;
    /** @type {number | undefined} */
    let appiumPid = undefined;

    try {
      await this.buildFrontend();
      await this.buildServer();
      await this.installAndroidEmulator();
      await this.installAppium();
      serverPid = await this.startServer();
      androidPid = await this.startAndroidEmulator();
      appiumPid = await this.startAppium();
      await this.testWindowsApp();
    } finally {
      await this.cleanup(
        [serverPid, androidPid, appiumPid].filter((x) => typeof x === "number"),
      );
    }
  }

  async buildFrontend() {
    console.log("\n::group::Install frontend dependencies");
    run("pnpm install --frozen-lockfile", { cwd: this.frontendDir });
    console.log("::endgroup::");

    console.log("\n::group::Build frontend");
    run("pnpm run build", {
      cwd: this.frontendDir,
      env: { ...process.env, RAWEB_SERVER_TYPE: "aspnetcore" },
    });
    console.log("::endgroup::");

    console.log("\n::group::Generate self-signed certificate");
    // Use spawnSync with an explicit args array to avoid shell quoting issues with the eval string.
    runSync(
      process.execPath,
      [
        "--input-type=module",
        "--eval",
        "import { generateCertificate } from './vite.config.ts'; await generateCertificate();",
        "--experimental-strip-types",
      ],
      { cwd: this.frontendDir },
    );
    console.log("::endgroup::");
  }

  async buildServer() {
    console.log("\n::group::Restore .NET solution");
    run("dotnet restore RAWeb.Server.slnf --locked-mode", {
      cwd: GITHUB_WORKSPACE,
    });
    console.log("::endgroup::");

    console.log("\n::group::Publish RAWeb.Server");
    run(
      "dotnet publish dotnet/RAWeb.Server/RAWeb.Server.csproj --no-restore --configuration Release",
      { cwd: GITHUB_WORKSPACE },
    );
    console.log("::endgroup::");
  }

  async startServer() {
    console.log("\n::group::Start RAWeb.Server in background");

    console.log("\nCopying App_Data to server output directory...");
    copyDir(
      path.join(GITHUB_ACTION_PATH, "App_Data"),
      path.join(this.distPath, "App_Data"),
    );

    const serverLogOut = path.join(GITHUB_WORKSPACE, "server-stdout.log");
    const serverLogErr = path.join(GITHUB_WORKSPACE, "server-stderr.log");
    const serverPid = spawnDetached(this.serverExe, [], {
      cwd: this.distPath,
      env: {
        ...process.env,
        ASPNETCORE_Kestrel__Certificates__Default__Path: path.join(
          this.certsDir,
          "cert.pem",
        ),
        ASPNETCORE_Kestrel__Certificates__Default__KeyPath: path.join(
          this.certsDir,
          "private-key.pem",
        ),
        ASPNETCORE_URLS: "https://+:5004",
      },
      logStdout: serverLogOut,
      logStderr: serverLogErr,
    });

    console.log("\nWaiting for RAWeb.Server on port 5004...");
    try {
      await waitForPort(5004);
      console.log("Server is ready.");
    } catch {
      console.error("--- server-stdout.log ---");
      try {
        console.error(fs.readFileSync(serverLogOut, "utf8"));
      } catch {}
      console.error("--- server-stderr.log ---");
      try {
        console.error(fs.readFileSync(serverLogErr, "utf8"));
      } catch {}
      throw new Error("Server did not start within 60 seconds.");
    }

    console.log("::endgroup::");

    return serverPid;
  }

  async installAndroidEmulator() {
    console.log("\n::group::Install Android emulator components");
    run(
      'sdkmanager "platform-tools" "platforms;android-36" "emulator" "system-images;android-36;google_apis;x86_64"',
      { cwd: GITHUB_WORKSPACE },
    );
    console.log("::endgroup::");
  }

  async startAndroidEmulator() {
    console.log("\n::group::Starting Android emulator in background");

    // kill any stale emulator holding the AVD lock from a previous run.
    try {
      execSync("taskkill /F /IM emulator.exe", {
        stdio: "ignore",
        shell: "cmd.exe",
      });
    } catch {}

    console.log("\nCreating Android virtual device...");
    run(
      'echo no | avdmanager create avd --force --name test_avd --package "system-images;android-36;google_apis;x86_64"',
      { cwd: GITHUB_WORKSPACE },
    );

    const emulatorExe = path.join(ANDROID_HOME, "emulator", "emulator.exe");
    const emulatorPid = spawnDetached(
      emulatorExe,
      [
        "-avd",
        "test_avd",
        "-no-window",
        "-accel",
        "on",
        "-no-audio",
        "-no-boot-anim",
        "-no-snapshot-save",
        "-no-metrics",
        "-writable-system",
        "-memory",
        "6144",
      ],
      {
        cwd: GITHUB_WORKSPACE,
        env: {
          ...process.env,
          ANDROID_SDK_ROOT: ANDROID_HOME,
          // The runner has no GPU drivers, so vulkan-1.dll is not installed.
          // Point the Vulkan loader at the SwiftShader ICD bundled with the emulator
          // so gfxstream can initialize without a system Vulkan driver.
          VK_ICD_FILENAMES: path.join(
            ANDROID_HOME,
            "emulator",
            "lib64",
            "vulkan",
            "vk_swiftshader_icd.json",
          ),
          VK_DRIVER_FILES: path.join(
            ANDROID_HOME,
            "emulator",
            "lib64",
            "vulkan",
            "vk_swiftshader_icd.json",
          ),
        },
        logStdout: path.join(GITHUB_WORKSPACE, "emulator-stdout.log"),
        logStderr: path.join(GITHUB_WORKSPACE, "emulator-stderr.log"),
        detached: true,
      },
    );

    // Waits for the emulator to start, remounts system partions RW (so we can manipulate rhe hosts file),
    // copies over the local CA certificate, and installs the APK on the emulator.
    runSync(
      "pwsh",
      ["-File", path.join(GITHUB_ACTION_PATH, "prepare-emulator.ps1")],
      {
        cwd: GITHUB_ACTION_PATH,
      },
    );

    console.log("::endgroup::");

    return emulatorPid;
  }

  async installAppium() {
    console.log("\n::group::Install Appium dependencies");
    run("npm install", { cwd: this.appiumDir });
    console.log("::endgroup::");
  }

  async startAppium() {
    console.log("\n::group::Start Appium in background");

    const appiumLogOut = path.join(GITHUB_WORKSPACE, "appium-stdout.log");
    const appiumLogErr = path.join(GITHUB_WORKSPACE, "appium-stderr.log");
    // Use cmd.exe /c so npm.cmd is resolved correctly, with output going
    // directly to file descriptors — no pipe buffering.
    const appiumPid = spawnDetached("cmd.exe", ["/c", "npm run appium"], {
      cwd: this.appiumDir,
      logStdout: appiumLogOut,
      logStderr: appiumLogErr,
    });

    console.log("\nWaiting for Appium on port 4723...");
    try {
      await waitForPort(4723);
      console.log("Appium is ready.");
    } catch {
      console.error("--- appium-stdout.log ---");
      try {
        console.error(fs.readFileSync(appiumLogOut, "utf8"));
      } catch {}
      console.error("--- appium-stderr.log ---");
      try {
        console.error(fs.readFileSync(appiumLogErr, "utf8"));
      } catch {}
      throw new Error("Appium did not start within 60 seconds.");
    }

    console.log("::endgroup::");

    return appiumPid;
  }

  async testWindowsApp() {
    console.log("\n::group::Run tests");
    run("npm run test", {
      cwd: this.appiumDir,
      env: { ...process.env, WORKSPACE_PORT: "5004" },
    });
    console.log("::endgroup::");
  }

  /**
   * @param {number[]} pidsToKill
   */
  async cleanup(pidsToKill) {
    console.log("\nKilling background processes...");
    for (const pid of pidsToKill) {
      killTree(pid);
    }

    console.log("\nDeleting AVD...");
    run("avdmanager delete avd --name test_avd", { cwd: GITHUB_WORKSPACE });
  }
}

/**
 * @param {string} cmd
 * @param {Partial<import("child_process").ExecSyncOptions>} opts
 */
function run(cmd, opts = {}) {
  console.log(`\n$ ${cmd}`);
  execSync(cmd, { stdio: "inherit", shell: "cmd.exe", ...opts });
}

/**
 *
 * @param {string} exe
 * @param {string[]} args
 * @param {Partial<import("child_process").SpawnSyncOptionsWithStringEncoding>} opts
 * @returns
 */
function runSync(exe, args, opts = {}) {
  console.log(`\n$ ${exe} ${args.join(" ")}`);
  const result = spawnSync(exe, args, { stdio: "inherit", ...opts });
  if ((result.status ?? 1) !== 0) {
    throw new Error(
      `Command failed with exit code ${result.status}: ${exe} ${args.join(" ")}`,
    );
  }
  return result;
}

/**
 * Spawn a background process whose stdout/stderr go directly to log files via
 * inherited file descriptors. No pipe is involved, so there is no buffer that
 * can fill up and block the child.
 *
 * @param {string} exe
 * @param {string[]} args
 * @param {{ cwd: string, env?: NodeJS.ProcessEnv, logStdout: string, logStderr: string, detached?: boolean }} opts
 * @returns {number | undefined} The PID of the spawned process.
 */
function spawnDetached(
  exe,
  args,
  { cwd, env, logStdout, logStderr, detached = false },
) {
  console.log(`\nStarting background: ${exe} ${args.join(" ")}`);
  const outFd = fs.openSync(logStdout, "w");
  const errFd = fs.openSync(logStderr, "w");
  const child = spawn(exe, args, {
    cwd,
    env: env ?? process.env,
    detached,
    stdio: ["ignore", outFd, errFd],
    windowsHide: true,
  });
  child.unref();
  fs.closeSync(outFd);
  fs.closeSync(errFd);
  return child.pid;
}

/**
 * Kills a process and all of its child processes on Windows using taskkill.
 * @param {number} pid
 */
function killTree(pid) {
  try {
    spawnSync("taskkill", ["/F", "/T", "/PID", String(pid)], {
      stdio: "inherit",
    });
  } catch {}
}

/**
 * Waits for a TCP port to be open on localhost, retrying until the timeout is reached.
 *
 * @param {number} port
 * @param {number} timeoutMs
 * @returns {Promise<void>}
 */
function waitForPort(port, timeoutMs = 60_000) {
  return new Promise((resolve, reject) => {
    const deadline = Date.now() + timeoutMs;
    function attempt() {
      const sock = net.createConnection(port, "localhost");
      sock.on("connect", () => {
        sock.destroy();
        resolve();
      });
      sock.on("error", () => {
        sock.destroy();
        if (Date.now() < deadline) {
          setTimeout(attempt, 1000);
        } else {
          reject(new Error(`Port ${port} did not open within ${timeoutMs}ms`));
        }
      });
    }
    attempt();
  });
}

/**
 * Copies a directory recursively, creating the destination directory if it doesn't exist.
 * @param {string} sourcePath
 * @param {string} destinationPath
 */
function copyDir(sourcePath, destinationPath) {
  fs.mkdirSync(destinationPath, { recursive: true });
  for (const entry of fs.readdirSync(sourcePath, { withFileTypes: true })) {
    const s = path.join(sourcePath, entry.name);
    const d = path.join(destinationPath, entry.name);
    if (entry.isDirectory()) copyDir(s, d);
    else fs.copyFileSync(s, d);
  }
}

new TestWindowsAppAction().start();
