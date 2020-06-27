<%
Set objWSHNetwork = Server.CreateObject("WScript.Network")
ServerName = objWSHNetwork.ComputerName

datetime = Year(now()) & "-" & Right(Cstr(Month(now()) + 100),2) & "-" & Right(Cstr(Day(now()) + 100),2) & "T" & Right(Cstr(Hour(now()) + 100),2) & ":" & Right(Cstr(Minute(now()) + 100),2) & ":" & Right(Cstr(Second(now()) + 100),2) & ".0Z"

Response.write "<ResourceCollection PubDate=""" & datetime & """ SchemaVersion=""1.1"" xmlns=""http://schemas.microsoft.com/ts/2007/05/tswf"">" & vbCRLF
Response.write "<Publisher LastUpdated=""" & datetime & """ Name=""" & ServerName & """ ID=""" & ServerName & """ Description="""">" & vbCRLF
Response.write "<Resources>" & vbCRLF

Whichfolder=server.mappath("rdp\") & "/"
Dim fs, f, f1, fc
set fs = CreateObject("Scripting.FileSystemObject")
set f = fs.GetFolder(Whichfolder)
set fc = f.files

For Each f1 in fc
	if (LCase(Right(f1.name,4)) = ".rdp") then
		if not GetRDPvalue(f1,"full address:s:") = "" then
			basefilename = Left(f1.name,Len(f1.name) - 4)
			appalias = GetRDPvalue(f1,"remoteapplicationprogram:s:")
			apptitle = GetRDPvalue(f1,"remoteapplicationname:s:")
			appicon = basefilename & ".ico"
			appicon32 = basefilename & ".png"
			apprdpfile = f1.name
			appresourceid = appalias
			appftastring = GetRDPvalue(f1,"remoteapplicationfileextensions:s:")
			appfulladdress = GetRDPvalue(f1,"full address:s:")
			rdptype = "RemoteApp"

			if appalias = "" then
				rdptype = "Desktop"
				appalias = basefilename
				apptitle = basefilename
				appresourceid = basefilename
			else
				rdptype = "RemoteApp"
			end if

			filedatetimeraw = f1.DateLastModified
			filedatetime = Year(now()) & "-" & Right(Cstr(Month(filedatetimeraw) + 100),2) & "-" & Right(Cstr(Day(filedatetimeraw) + 100),2) & "T" & Right(Cstr(Hour(filedatetimeraw) + 100),2) & ":" & Right(Cstr(Minute(filedatetimeraw) + 100),2) & ":" & Right(Cstr(Second(filedatetimeraw) + 100),2) & ".0Z"

			Response.write "<Resource ID=""" & appresourceid & """ Alias=""" & appalias & """ Title=""" & apptitle & """ LastUpdated=""" & filedatetime & """ Type=""" & rdptype & """>" & vbCRLF
			Response.write "<Icons>" & vbCRLF
			Response.write "<IconRaw FileType=""Ico"" FileURL=""" & Root & "icon/" & appicon & """ />" & vbCRLF
			if fs.FileExists(server.mappath("icon32/" & appicon32)) then Response.write "<Icon32 Dimensions=""32x32"" FileType=""Png"" FileURL=""" & Root & "icon32/" & appicon32 & """ />" & vbCRLF
			Response.write "</Icons>" & vbCRLF
			if appftastring <> "" then
				Response.write "<FileExtensions>" & vbCRLF
				appftaarray = Split(appftastring,",")
				for each filetype in appftaarray
					docicon = basefilename & "." & filetype & ".ico"
					Response.write "<FileExtension Name=""" & filetype & """ PrimaryHandler=""True"">" & vbCRLF
					Response.write "<FileAssociationIcons>" & vbCRLF
					Response.write "<IconRaw FileType=""Ico"" FileURL=""" & Root & "icon/" & docicon & """ />" & vbCRLF
					Response.write "</FileAssociationIcons>" & vbCRLF
					Response.write "</FileExtension>"  & vbCRLF
				next
				Response.write "</FileExtensions>" & vbCRLF
			Else
				Response.write "<FileExtensions />" & vbCRLF
			end if
			Response.write "<HostingTerminalServers>" & vbCRLF
			Response.write "<HostingTerminalServer>" & vbCRLF
			Response.write "<ResourceFile FileExtension="".rdp"" URL=""" & Root & "rdp/" & apprdpfile & """ />" & vbCRLF
			Response.write "<TerminalServerRef Ref=""" & ServerName & """ />" & vbCRLF
			Response.write "</HostingTerminalServer>" & vbCRLF
			Response.write "</HostingTerminalServers>" & vbCRLF
			Response.write "</Resource>" & vbCRLF
		end if
	end if
Next

Response.write "</Resources>" & vbCRLF
Response.write "<TerminalServers>" & vbCRLF
Response.write "<TerminalServer ID=""" & ServerName & """ Name=""" & ServerName & """ LastUpdated=""" & datetime & """ />" & vbCRLF
Response.write "</TerminalServers>" & vbCRLF
Response.write "</Publisher>" & vbCRLF
Response.write "</ResourceCollection>" & vbCRLF

function GetRDPvalue(f1,valuename)
	Err.Clear
	on error resume next
	Dim ts
	valuenamelen = Len(valuename)
	set ts = f1.OpenAsTextStream(1,-2)
	if Err.Number = 0 then
		Do While Not ts.AtEndOfStream
			Dim Line
			Line = ts.readline
			if (Lcase(Left(Line,valuenamelen)) = valuename) then
				theName = Right(Line,Len(Line)-valuenamelen)
			end if
		Loop
		theName = Replace(theName,"|","")
		GetRDPvalue = theName
	Else
		GetRDPvalue = ""
	end if
	on error goto 0
end function

Function Root()
    DocPath = Request.ServerVariables("PATH_INFO")
	aPath = Split("/" & DocPath, "/")
	Root = Left(DocPath,Len(DocPath)-Len(aPath(UBound(aPath))))
End Function

%>
