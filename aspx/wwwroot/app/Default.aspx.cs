using AliasUtilities;
using System;
using System.Web;
using System.Web.UI;
using System.Web.Security;
using System.Xml;

public partial class AppHome : Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        // Get the authenticated user from the cookie
        // and check if the user is authenticated
        string authUser = getAuthenticatedUser();
        if (string.IsNullOrEmpty(authUser))
        {
            // Redirect to login page if not authenticated
            Response.Redirect("~/app/login.aspx" + "?ReturnUrl=" + Uri.EscapeUriString(HttpContext.Current.Request.Url.AbsolutePath));
        }

        if (!IsPostBack)
        {
            // Code to execute on initial page load
        }

    }

    // make the alias resolver available to the page
    public AliasUtilities.AliasResolver resolver = new AliasUtilities.AliasResolver();

    // Get the first element from an XML node list
    private XmlNode getFirstElement(XmlNodeList nodeList)
    {
        if (nodeList == null || nodeList.Count == 0) return null;
        return nodeList[0];
    }

    // Function to write the XML document to the response stream, splitting it at <slot />
    // writing the first part, and then writing the second part after the aspx page has been loaded
    private void writeXmlToResponse(XmlDocument doc)
    {
        // Find the <slot /> element and split the document
        var slotNode = getFirstElement(doc.GetElementsByTagName("slot"));
        if (slotNode != null)
        {
            // Save the first part of the document before the <slot />
            string firstPart = doc.OuterXml.Substring(0, doc.OuterXml.IndexOf("<slot>"));
            Response.Write(firstPart);

            // Write the second part after the aspx page content has been inserted
            string secondPart = doc.OuterXml.Substring(doc.OuterXml.IndexOf("</slot>") + "</slot>".Length);
            Response.Write("<script>document.addEventListener('DOMContentLoaded', function() { " +
                          "document.documentElement.innerHTML += `" + secondPart + "`; });</script>");
        }
    }

    // Define the getAuthenticatedUser method inside the class
    public string getAuthenticatedUser()
    {
        HttpCookie authCookie = HttpContext.Current.Request.Cookies[".ASPXAUTH"];
        if (authCookie == null || authCookie.Value == "") return "";
        try
        {
            // Decrypt may throw an exception if authCookie.Value is total garbage
            FormsAuthenticationTicket authTicket = FormsAuthentication.Decrypt(authCookie.Value);
            if (authTicket == null)
            {
                return "";
            }
            return authTicket.Name;
        }
        catch
        {
            return "";
        }
    }

}
