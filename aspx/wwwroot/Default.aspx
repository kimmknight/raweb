<%@ Page language="C#" explicit="true"  %>
<script runat="server">
    public string GetRDPvalue(string eachfile, string valuename)
    {
        string GetRDPvalue = "";
        string fileline = "";

        string theName = "";
        int valuenamelen = 0;
        System.IO.StreamReader contentfile = new System.IO.StreamReader(eachfile);
        valuenamelen = valuename.Length;
        while ((fileline = contentfile.ReadLine()) != null)
        {
            if (fileline.Length >= valuenamelen)
            {
                string filelineleft = fileline.Substring(0, valuenamelen);
                if ((filelineleft.ToLower() == valuename))
                {
                    theName = fileline.Substring(valuenamelen, fileline.Length - valuenamelen);
                }
            }
        }
        GetRDPvalue = theName.Replace("|", "");
        return GetRDPvalue;
    }
</script>

<html>
<head>
<title>
RAWeb - Remote Applications
</title>
<style type="text/css">
a:link {color:#444444;text-decoration:none;}
a:visited {color:#444444;text-decoration:none;}
a:hover{color:#000000;text-decoration:underline;}
a:active {color:#000000;text-decoration:underline;}
   #apptile
{
width:150px;
height:135px;
text-align:center;
vertical-align:bottom;
border-style:solid;
border-width:0px;
float:left;
font-size:14px;
font-weight:bold;
}
h1 {
    font-family:Arial, Helvetica, sans-serif;
    font-size: 30px;
    font-style: italic;
    color:rgb(0,0,0)
}
body
{
font-family:Arial,sans-serif;
}
</style>
<link rel="shortcut icon" href="favicon.ico">
</head>
<body>
<div style="text-align:left;"><h1>Remote<font style="color:rgb(100,100,100)">Apps</font></h1></div><br>
<% 
   string appname = "";
   string basefilename = "";
   string pngname = "";
   string pngpath = "";

   string Whichfolder = HttpContext.Current.Server.MapPath("rdp\\") + "/";
   string[] allfiles = System.IO.Directory.GetFiles(Whichfolder);
   foreach(string eachfile in allfiles)
   {
      string extfile = eachfile.Substring(eachfile.Length - 4, 4);       
      if (extfile.ToLower() == ".rdp")
      {
         if (!(GetRDPvalue(eachfile,"full address:s:") == ""))
         {
            appname = GetRDPvalue(eachfile, "remoteapplicationname:s:");
            basefilename = eachfile.Substring(Whichfolder.Length, eachfile.Length - Whichfolder.Length - 4); 
            if (appname == "")
            {
               appname = basefilename;
            }
            pngname = basefilename + ".png";
            if (System.IO.File.Exists(HttpContext.Current.Server.MapPath("png\\" + pngname)))
            {
               pngpath = "png/" + pngname;
            }
            else
            {
               pngpath = "rdpicon.png";
            }
            HttpContext.Current.Response.Write("<div id=apptile>");
            HttpContext.Current.Response.Write("<a href=\"" + "rdp/" + eachfile.Substring(Whichfolder.Length, eachfile.Length - Whichfolder.Length) + "\"><img border=0 height=64 width=64 src=\"" + pngpath + "\"><br>" + appname + "</a>");
            HttpContext.Current.Response.Write("</div>");
         }
      }
   }
%>
</body>
</html>
