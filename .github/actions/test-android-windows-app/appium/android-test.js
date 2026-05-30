import { execSync } from "child_process";
import { remote } from "webdriverio";

/** @type {string} */
// @ts-ignore
const localUserHomePath = process.env.USERPROFILE;
process.env.ANDROID_HOME = localUserHomePath + "\\.android\\sdk";

const WORKSPACE_DOMAIN = "794693d8-4d0e-4a0b-b0d7-5a5f0a957091-rawebdev.local";
const WORKSPACE_URL = `https://${WORKSPACE_DOMAIN}:5174/api/workspace`;

async function installCertificate() {
  const driver = await remote({
    protocol: "http",
    hostname: "localhost",
    port: 4723,
    path: "/",
    capabilities: {
      platformName: "Android",
      "appium:deviceName": "emulator-5554",
      "appium:appPackage": "com.android.settings",
      "appium:appActivity":
        "com.google.android.settings.security.SecurityAdvancedSettingsActivity",
      "appium:automationName": "UiAutomator2",
      "appium:noReset": true,
      "appium:forceAppLaunch": true,
    },
  });

  try {
    console.log("App launched. Waiting for UI...");

    // wait for the "More security settings" screen to be displayed
    const androidSafeBrowsingOption = await driver.$(
      'android=new UiSelector().className("android.widget.LinearLayout").childSelector(new UiSelector().text("Android Safe Browsing"))',
    );
    await androidSafeBrowsingOption.waitForDisplayed({ timeout: 30000 });

    // scroll down
    await driver.$(
      "android=new UiScrollable(new UiSelector().scrollable(true)).flingForward()",
    );

    // click the "Encryption & credentials" option in the settings list
    const encryptionOption = await driver.$(
      'android=new UiSelector().className("android.widget.LinearLayout").childSelector(new UiSelector().text("Encryption & credentials"))',
    );
    await encryptionOption.waitForDisplayed({ timeout: 10000 });
    await encryptionOption.click();

    // click the "Install a certificate" option in the Encryption & credentials list
    const installCertOption = await driver.$(
      'android=new UiSelector().className("android.widget.LinearLayout").childSelector(new UiSelector().text("Install a certificate"))',
    );
    await installCertOption.waitForDisplayed({ timeout: 10000 });
    await installCertOption.click();

    // click the "CA certificate" option in the Install a certificate list
    const caCertOption = await driver.$(
      'android=new UiSelector().className("android.widget.LinearLayout").childSelector(new UiSelector().text("CA certificate"))',
    );
    await caCertOption.waitForDisplayed({ timeout: 10000 });
    await caCertOption.click();

    // click "INSTALL ANYWAY" in the warning screen to open the file picker
    const installAnywayButton = await driver.$(
      'android=new UiSelector().className("android.widget.Button").text("INSTALL ANYWAY")',
    );
    await installAnywayButton.waitForDisplayed({ timeout: 10000 });
    await installAnywayButton.click();
    await driver.pause(3000);

    // swipe from left edge to right edge to open the navigation drawer
    const { width, height } = await driver.getWindowSize();
    const startX = 0;
    const endX = width;
    const y = height / 2;
    await driver
      .action("pointer", { parameters: { pointerType: "touch" } })
      .move({ x: startX, y })
      .down()
      .pause(5)
      .move({ x: endX, y, duration: 25 })
      .up()
      .perform();

    // click the Downloads option in the navigation drawer
    const downloadsOption = await driver.$(
      'android=new UiSelector().className("android.widget.LinearLayout").childSelector(new UiSelector().text("Downloads"))',
    );
    await downloadsOption.waitForDisplayed({ timeout: 10000 });
    await downloadsOption.click();

    // click the ca-cert.crt file in the Downloads list to select it for installation
    const caCertFile = await driver.$(
      'android=new UiSelector().className("android.widget.LinearLayout").childSelector(new UiSelector().text("ca-cert.crt"))',
    );
    await caCertFile.waitForDisplayed({ timeout: 10000 });
    await caCertFile.click();
  } catch (error) {
    if (error instanceof Error) {
      console.error("✗ Test failed:", error.message);
    }
    try {
      await driver.saveScreenshot("./cert-failure.png");
    } catch {
      console.error("Failed to save screenshot");
    }
    throw error;
  } finally {
    console.log("Cleaning up session for installCertificate...");
    await driver.deleteSession();
  }
}

async function runTest() {
  // Set up port forwarding and /etc/hosts before starting the Appium session.
  // `adb root` restarts adbd, which would sever an active UiAutomator2
  // instrumentation connection if run after the session is created.
  const adbPath = `${process.env.ANDROID_HOME}\\platform-tools\\adb.exe`;
  console.log(
    `Binding ports 5174 (vite dev server), 5135 (raweb.exe dev server), and 5000 (raweb.exe server) on host to emulated Android device using adb at ${adbPath}`,
  );
  execSync(`"${adbPath}" reverse tcp:5174 tcp:5174`);
  execSync(`"${adbPath}" reverse tcp:5135 tcp:5135`);
  execSync(`"${adbPath}" reverse tcp:5000 tcp:5000`);
  console.log("Enable adb root mode");
  execSync(`"${adbPath}" root`);
  console.log(
    `Adding ${WORKSPACE_DOMAIN} to /etc/hosts on emulated Android device using adb at ${adbPath}`,
  );
  execSync(
    `"${adbPath}" shell "grep -qF '${WORKSPACE_DOMAIN}' /etc/hosts || echo '10.0.2.2 ${WORKSPACE_DOMAIN}' >> /etc/hosts"`,
  );
  console.log("Disable adb root mode");
  execSync(`"${adbPath}" unroot`);

  const driver = await remote({
    protocol: "http",
    hostname: "localhost",
    port: 4723,
    path: "/",
    capabilities: {
      platformName: "Android",
      "appium:deviceName": "emulator-5554",
      "appium:appPackage": "com.microsoft.rdc.androidx",
      "appium:appActivity": "com.microsoft.windowsapp.ui.HomeActivity",
      "appium:automationName": "UiAutomator2",
      "appium:noReset": false, // discard previous app state to ensure a clean test environment
      "appium:forceAppLaunch": true,
    },
  });

  try {
    console.log("App launched. Waiting for UI...");
    await driver.pause(3000);

    // If we are on the welcome screen that requires accepting the
    // Microsoft privacy statement, click the "Accept" button to proceed.
    const isPrivacyPolicyScreen = await driver
      .$(
        'android=new UiSelector().textContains("Welcome to Windows App. Please read the Microsoft Privacy Statement")',
      )
      .isExisting();
    console.log("isPrivacyPolicyScreen:", isPrivacyPolicyScreen);
    if (isPrivacyPolicyScreen) {
      // scroll down
      await driver.$(
        "android=new UiScrollable(new UiSelector().scrollable(true)).flingForward()",
      );

      // click the "Accept" button to proceed
      const acceptButton = await driver.$(
        'android=new UiSelector().className("android.widget.EditText").text("Accept")',
      );
      await acceptButton.waitForDisplayed({ timeout: 10000 });
      await acceptButton.click();
    }

    const isNewAppNameScreen = await driver
      .$(
        'android=new UiSelector().textContains("Remote Desktop is now Windows App")',
      )
      .isExisting();
    if (isNewAppNameScreen) {
      // click the skip button to proceed
      const skipButton = await driver.$(
        'android=new UiSelector().textContains("Skip")',
      );
      await skipButton.waitForDisplayed({ timeout: 10000 });
      await skipButton.click();
    }

    // click the add button in the app bar to open the add sheet
    const addButton = await driver.$(
      'android=new UiSelector().className("android.widget.ImageView").description("Add")',
    );
    await addButton.waitForDisplayed({ timeout: 10000 });
    await addButton.click();

    // click the "Workspace" option in the add sheet
    const workspaceOption = await driver.$(
      'android=new UiSelector().className("android.view.View").childSelector(new UiSelector().text("Workspace"))',
    );
    await workspaceOption.waitForDisplayed({ timeout: 10000 });
    await workspaceOption.click();

    // click the "OK" button in the add sheet
    const okButton = await driver.$(
      'android=new UiSelector().className("android.view.View").childSelector(new UiSelector().text("OK"))',
    );
    await okButton.waitForDisplayed({ timeout: 10000 });
    await okButton.click();

    // type the workspace URL into the text field
    const workspaceUrlField = await driver.$(
      'android=new UiSelector().resourceId("com.microsoft.rdc.androidx:id/auto_complete_text_view")',
    );
    await workspaceUrlField.waitForDisplayed({ timeout: 10000 });
    await workspaceUrlField.clearValue();

    // The Windows App sends a special user agent to the server, and the
    // server responds with NTLM or Negotiate. This is how the Windows App
    // knows that it is a valid endpoint for a workspace.
    // See UseWorkspaceDiscovery.cs for server-side implementation.
    await workspaceUrlField.setValue(WORKSPACE_URL);

    // wait until searching for workspaces message goes away
    while (
      await driver
        .$(
          'android=new UiSelector().textContains("Searching for workspaces...")',
        )
        .isExisting()
    ) {
      console.log("Waiting for workspace discovery to complete...");
      await driver.pause(1000);
    }

    // if the discovery failed, throw an error to fail the test
    const isDiscoveryFailed = await driver
      .$(
        'android=new UiSelector().textContains("No workspace is associated with this URL.")',
      )
      .isExisting();
    if (isDiscoveryFailed) {
      throw new Error("Workspace discovery failed");
    }

    // click the "Next" button in the app bar to proceed to the next screen
    const nextButton = await driver.$(
      'android=new UiSelector().className("android.widget.Button").text("NEXT")',
    );
    await nextButton.waitForDisplayed({ timeout: 10000 });
    await nextButton.click();

    // wait for "Connecting to workspace..." message to go away
    while (
      await driver
        .$(
          'android=new UiSelector().textContains("Connecting to workspace...")',
        )
        .isExisting()
    ) {
      console.log("Waiting for workspace connection to complete...");
      await driver.pause(1000);
    }

    // click the "CONNECT" button to ignore the certificate error
    const connectButton = await driver.$(
      'android=new UiSelector().className("android.widget.Button").text("CONNECT")',
    );
    await connectButton.waitForDisplayed({ timeout: 10000 });
    await connectButton.click();

    // wait for "Connecting to workspace..." message to go away
    while (
      await driver
        .$(
          'android=new UiSelector().textContains("Connecting to workspace...")',
        )
        .isExisting()
    ) {
      console.log("Waiting for workspace connection to complete...");
      await driver.pause(1000);
    }

    // fill in credentials and continue
    await driver
      .$(
        'android=new UiSelector().resourceId("com.microsoft.rdc.androidx:id/username")',
      )
      .setValue("testuser");
    await driver
      .$(
        'android=new UiSelector().resourceId("com.microsoft.rdc.androidx:id/password")',
      )
      .setValue("Password24");
    const continueButton = await driver.$(
      'android=new UiSelector().className("android.widget.Button").text("CONTINUE")',
    );
    await continueButton.waitForDisplayed({ timeout: 10000 });
    await continueButton.click();

    // wait for "Setting up workspace..." message to go away
    while (
      await driver
        .$('android=new UiSelector().textContains("Setting up workspace...")')
        .isExisting()
    ) {
      console.log("Waiting for workspace setup to complete...");
      await driver.pause(1000);
    }

    // confirm that the GIS Server device on JACKDESKTOP2024 is displayed in the list of available devices
    const gisServerDevice = await driver.$(
      'android=new UiSelector().description("GIS Server, JACKDESKTOP2024, button, double tap to activate")',
    );
    await gisServerDevice.waitForDisplayed({ timeout: 10000 });
    console.log(
      "✓ GIS Server device is displayed in the list of available devices",
    );

    // switch to the Apps tab
    const appsTabButton = await driver.$(
      'android=new UiSelector().className("android.view.View").description("Apps")',
    );
    await appsTabButton.waitForDisplayed({ timeout: 10000 });
    await appsTabButton.click();

    // confirm that both ArcGIS Pro RemoteApps are listed. These would normally be merged together in RAWeb's web app
    // becuase they are identical except for the terminal server, but RAWeb does not merge them for other workspace clients.
    const arcgisProTextViews = await driver.$$(
      'android=new UiSelector().className("android.widget.TextView").text("ArcGIS Pro")',
    );
    await arcgisProTextViews[0]?.waitForDisplayed({ timeout: 10000 });
    await arcgisProTextViews[1]?.waitForDisplayed({ timeout: 10000 });
    if (arcgisProTextViews.length !== 2) {
      throw new Error(
        `Expected 2 ArcGIS Pro TextView elements, but found ${arcgisProTextViews.length}`,
      );
    }
  } catch (error) {
    if (error instanceof Error) {
      console.error("✗ Test failed:", error.message);
    }
    try {
      await driver.saveScreenshot("./failure.png");
    } catch {
      console.error("Failed to save screenshot");
    }
    throw error;
  } finally {
    console.log("Cleaning up session for runTest...");
    await driver.deleteSession();
  }
}

installCertificate()
  .then(() => runTest())
  .then(() => {
    console.log("Tests completed successfully");
    process.exit(0);
  })
  .catch((error) => {
    console.error("Tests failed:", error);
    process.exit(1);
  });
