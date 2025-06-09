using AuthUtilities;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Web.Script.Serialization;
using System.Web.Script.Services;
using System.Web.Services;
using System.Web.Services.Protocols;

[WebService(Namespace = "https://raweb.app/PoliciesService")]
[WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
[ScriptService]
public class PoliciesService : WebService
{
    [WebMethod]
    [ScriptMethod(UseHttpGet = true)]
    public string GetAppSettings()
    {
        RequireLocalAdministrator();

        NameValueCollection appSettings = System.Configuration.ConfigurationManager.AppSettings;
        var settingsDict = new Dictionary<string, string>();
        foreach (string key in appSettings.AllKeys)
        {
            settingsDict.Add(key, appSettings[key]);
        }

        JavaScriptSerializer serializer = new JavaScriptSerializer();
        return serializer.Serialize(settingsDict);
    }

    [WebMethod]
    [ScriptMethod(UseHttpGet = true)]
    public string GetAppSetting(string key)
    {
        RequireLocalAdministrator();
        string value = System.Configuration.ConfigurationManager.AppSettings[key];
        return value;
    }

    [WebMethod]
    [ScriptMethod]
    public void SetAppSetting(string key, string value)
    {
        RequireLocalAdministrator();

        bool shouldRemove = string.IsNullOrEmpty(value) || value == "nil";

        System.Configuration.Configuration config = System.Web.Configuration.WebConfigurationManager.OpenWebConfiguration("~");

        if (shouldRemove)
        {
            if (config.AppSettings.Settings[key] != null)
            {
                config.AppSettings.Settings.Remove(key);
            }
        }
        else
        {
            if (config.AppSettings.Settings[key] == null)
            {
                config.AppSettings.Settings.Add(key, value);
            }
            else
            {
                config.AppSettings.Settings[key].Value = value;
            }
        }

        config.Save(System.Configuration.ConfigurationSaveMode.Modified);
    }

    private AuthCookieHandler authCookieHandler = new AuthCookieHandler();

    private void RequireLocalAdministrator()
    {
        UserInformation userInfo = authCookieHandler.GetUserInformationSafe(Context.Request);

        if (userInfo == null)
        {
            Context.Response.StatusCode = 401;
            Context.Response.End();
        }

        if (!userInfo.IsLocalAdministrator)
        {
            Context.Response.StatusCode = 403;
            Context.Response.End();
        }
    }
}