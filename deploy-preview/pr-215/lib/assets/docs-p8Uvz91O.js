const __vite__mapDeps=(i,m=__vite__mapDeps,d=(m.f||(m.f=["lib/assets/shared-DEFpb1Q4.js","lib/assets/shared-iqqtHGk3.css"])))=>i.map(i=>d[i]);
import{A as p,l as c,E as a,n as t,m as o,G as n,x as s,p as g,I as L,a9 as A,aa as Be,ab as pe,ac as xe,ad as me,ae as Ye,af as fe,ag as Ve,ah as Ae,ai as je,aj as ge,ak as _e,al as Ie,d as Te,r as F,o as Z,e as qe,i as be,a as ye,c as Je,f as K,D as ze,g as Re,am as se,an as Ke,ao as Xe,ap as $e,w as Qe,aq as te,T as Ze,q as _,s as q,a7 as et,a6 as ee,F as X,y as Ee,ar as tt,J as we,H as k,P as Se,as as nt,at as ot,au as it,z as j,K as rt,av as st,aw as at,ax as lt,ay as dt,az as ct,aA as ae,aB as ht,aC as ut,aD as pt,aE as mt,h as Oe,j as Ne,L as ve,aF as ft,aG as At,a1 as gt,aH as It,a2 as Tt,Y as le,aI as bt,O as yt,M as Rt,aJ as Et,$ as wt,u as de,a0 as St,aK as Ot}from"./shared-DEFpb1Q4.js";const Nt="/deploy-preview/pr-215/lib/assets/configure-webfeed-url-via-local-group-policy-on-the-client-BzmZh0rB.png",vt={class:"markdown-body"},Lt="Deploy RAWeb workspaces",Ct={__name:"index",setup(l,{expose:r}){return r({frontmatter:{title:"Deploy RAWeb workspaces"}}),(d,e)=>{const i=p("RouterLink");return a(),c("div",vt,[t("p",null,[e[1]||(e[1]=n("On Windows, you can deploy RAWeb workspaces to clients via Group Policy. This will automatically configure the workspace URL on the client without user interaction. See ")),o(i,{to:"/docs/workspaces/"},{default:s(()=>e[0]||(e[0]=[n("Access RAWeb resources as a workspace")])),_:1}),e[2]||(e[2]=n(" for more information."))]),o(g(L),{severity:"caution",title:"Caution"},{default:s(()=>e[3]||(e[3]=[n(" The user account on the client must have a valid account on the RAWeb server in order to access the workspace. ")])),_:1}),e[15]||(e[15]=t("p",null,"Use the following steps to deploy RAWeb workspaces via Group Policy:",-1)),t("ol",null,[e[11]||(e[11]=t("li",null,[n("Open the Group Policy Management Console ("),t("code",null,"gpmc.msc"),n("). Alternativly, you can use the Local Group Policy Editor ("),t("code",null,"gpedit.msc"),n(") on the client machine.")],-1)),e[12]||(e[12]=t("li",null,[n("Navigate to "),t("strong",null,"User Configuration » Administrative Templates » Windows Components » Remote Desktop Services » RemoteApp and Desktop Connections"),n("."),t("br"),t("img",{alt:"",src:Nt,xmlns:"http://www.w3.org/1999/xhtml"})],-1)),e[13]||(e[13]=t("li",null,[n("Double click the policy setting "),t("strong",null,"Specify default connection URL"),n(".")],-1)),t("li",null,[e[5]||(e[5]=n("Click ")),e[6]||(e[6]=t("strong",null,"Enabled",-1)),e[7]||(e[7]=n(" and enter the ")),o(i,{to:"/docs/workspaces/#workspace-url"},{default:s(()=>e[4]||(e[4]=[n("RAWeb workspace URL")])),_:1}),e[8]||(e[8]=n(" in the ")),e[9]||(e[9]=t("strong",null,"Default connection URL",-1)),e[10]||(e[10]=n(" text box."))]),e[14]||(e[14]=t("li",null,[n("Click "),t("strong",null,"OK"),n(".")],-1))])])}}},Dt=Object.freeze(Object.defineProperty({__proto__:null,default:Ct,title:Lt},Symbol.toStringTag,{value:"Module"})),Pt={class:"markdown-body"},Ft="Install RAWeb",Wt={__name:"index",setup(l,{expose:r}){return r({frontmatter:{title:"Install RAWeb"}}),(d,e)=>{const i=p("RouterLink"),h=p("CodeBlock");return a(),c("div",Pt,[e[31]||(e[31]=t("h2",null,"Understanding RAWeb’s installation requirements",-1)),e[32]||(e[32]=t("h3",null,"Server",-1)),e[33]||(e[33]=t("p",null,"RAWeb is built using a combination of ASP.NET and Vue.js, and it runs on Internet Information Services (IIS). Therefore, to install and run RAWeb, your system must be a Windows machine capable of running IIS and ASP.NET web applications.",-1)),t("p",null,[e[1]||(e[1]=n("For more information about supported installation environments, including specific Windows versions, refer to our ")),o(i,{to:"/docs/supported-environments"},{default:s(()=>e[0]||(e[0]=[n("supported environments documentation")])),_:1}),e[2]||(e[2]=n("."))]),e[34]||(e[34]=t("h3",null,"Clients",-1)),e[35]||(e[35]=t("p",null,"Any client device can connect to RAWeb using a modern web browser, such as Microsoft Edge, Google Chrome, Mozilla Firefox, or Safari. Older versions of these browsers may not be fully supported.",-1)),t("p",null,[e[4]||(e[4]=n("Additionally, RAWeb exposes RemoteApps and desktops using the Terminal Server Workspace Provisioning specification, so any client that supports MS-TWSP workspaces can load RAWeb’s resources. You can review the steps for using workspaces in our ")),o(i,{to:"/docs/workspaces"},{default:s(()=>e[3]||(e[3]=[n("Access RAWeb resources as a workspace documentation")])),_:1}),e[5]||(e[5]=n(". Microsoft provides clients for Windows, macOS, iOS/iPadOS, and Android."))]),e[36]||(e[36]=A('<h2 id="installation">Installation</h2><p>RAWeb provides a few different installation methods. The easiest way to get started is to use our installation script, which automatically installs RAWeb and any required components.</p><p>Jump to an section:</p><ul><li><a href="docs/installation/#interactive-installation-script">Interactive installation script (recommended)</a></li><li><a href="docs/installation/#non-interactive-installation">Non-interactive installation</a></li><li><a href="docs/installation/#install-unreleased-features">Install unreleased features</a></li><li><a href="docs/installation/#manual-installation-in-iis">Manual installation in IIS</a></li><li><a href="docs/installation/#install-development-branches">Install development branches</a></li></ul><h3 id="interactive-installation-script">Interactive installation script (recommended)</h3>',5)),t("ol",null,[e[12]||(e[12]=t("li",null,[t("p",null,[t("strong",null,"Open PowerShell as an administrator."),n(),t("br"),n(" Press the Windows key + X, then select PowerShell (Administrator) or Terminal (Administrator).")])],-1)),t("li",null,[e[6]||(e[6]=t("p",null,[t("strong",null,[n("Copy and paste the code below"),t("sup",{class:"footnote-ref"},[t("a",{href:"docs/installation/#fn1",id:"fnref1"},"[1]")]),n(", and then press enter.")])],-1)),o(h,{code:`irm https://github.com/kimmknight/raweb/releases/latest/download/install.ps1 | iex
`})]),e[13]||(e[13]=t("li",null,[t("p",null,"Follow the prompts.")],-1)),t("li",null,[t("p",null,[e[8]||(e[8]=t("strong",null,"Install web client prerequisites.",-1)),e[9]||(e[9]=t("br",null,null,-1)),e[10]||(e[10]=n(" If you plan to use the web client connection method, follow the instructions in our ")),o(i,{to:"/docs/web-client/prerequisites"},{default:s(()=>e[7]||(e[7]=[n("web client prerequisites documentation")])),_:1}),e[11]||(e[11]=n(" to install and configure the required software."))])])]),o(g(L),{severity:"attention",title:"Important"},{default:s(()=>e[14]||(e[14]=[t("p",null,[n("The installer will retrieve the pre-built version of RAWeb from the latest release and install it to "),t("code",null,"C:\\inetpub\\RAWeb"),n(".")],-1),t("p",null,[n("Refer to "),t("a",{href:"https://github.com/kimmknight/raweb/releases/latest",target:"_blank",rel:"noopener noreferrer"},"the release page"),n(" for more details.")],-1)])),_:1}),o(g(L),{title:"Note"},{default:s(()=>e[15]||(e[15]=[n(" Internet Information Services (IIS) or other required components are not already installed, the RAWeb installer will retreive and install them. ")])),_:1}),e[37]||(e[37]=t("p",null,[n("To install other versions, visit the "),t("a",{href:"https://github.com/kimmknight/raweb/releases",target:"_blank",rel:"noopener noreferrer"},"the releases page"),n(" on GitHub.")],-1)),e[38]||(e[38]=t("h3",{id:"non-interactive-installation"},"Non-interactive installation",-1)),e[39]||(e[39]=t("p",null,"To install the latest version without prompts, use the following command instead:",-1)),t("ol",null,[e[22]||(e[22]=t("li",null,[t("p",null,[t("strong",null,"Open PowerShell as an administrator."),n(),t("br"),n(" Press the Windows key + X, then select PowerShell (Administrator) or Terminal (Administrator).")])],-1)),t("li",null,[e[16]||(e[16]=t("p",null,[t("strong",null,[n("Copy and paste the code below"),t("sup",{class:"footnote-ref"},[t("a",{href:"docs/installation/#fn1",id:"fnref1:1"},"[1:1]")]),n(", then press enter.")])],-1)),o(h,{code:`& ([scriptblock]::Create((irm https://github.com/kimmknight/raweb/releases/latest/download/install.ps1)) -AcceptAll
`})]),t("li",null,[t("p",null,[e[18]||(e[18]=t("strong",null,"Install web client prerequisites.",-1)),e[19]||(e[19]=t("br",null,null,-1)),e[20]||(e[20]=n(" If you plan to use the web client connection method, follow the instructions in our ")),o(i,{to:"/docs/web-client/prerequisites"},{default:s(()=>e[17]||(e[17]=[n("web client prerequisites documentation")])),_:1}),e[21]||(e[21]=n(" to install and configure the required software."))])])]),o(g(L),{severity:"caution",title:"Caution"},{default:s(()=>e[23]||(e[23]=[t("p",null,[n("If RAWeb is already installed, installing with this option will replace the existing configuration and installed files. Resources, policies, and other data in "),t("code",null,"/App_Data"),n(" with be preserved.")],-1)])),_:1}),e[40]||(e[40]=t("h3",{id:"install-unreleased-features"},"Install unreleased features",-1)),e[41]||(e[41]=t("p",null,"To install the latest version of the RAWeb, including features that may not have been released, follow these steps:",-1)),e[42]||(e[42]=t("ol",null,[t("li",null,[n("Download the "),t("a",{href:"https://github.com/kimmknight/raweb/archive/master.zip",target:"_blank",rel:"noopener noreferrer"},"latest RAWeb repository zip file"),n(".")]),t("li",null,[n("Extract the zip file and run "),t("strong",null,"Setup.ps1"),n(" in PowerShell as an administrator.")])],-1)),o(g(L),{severity:"caution",title:"Unstable code"},{default:s(()=>e[24]||(e[24]=[n(" Unreleased versions may contain unstable or experimental code that has not been fully tested. Use these versions at your own risk. ")])),_:1}),o(g(L),{title:"Note"},{default:s(()=>e[25]||(e[25]=[n(" Unreleased versions are not pre-built. Therefore, they require the .NET SDK to build the application before installation. "),t("p",null,"If you do not already have the .NET SDK installed, the setup script will download and install it for you.",-1)])),_:1}),e[43]||(e[43]=A('<h3 id="manual-installation-in-iis">Manual installation in IIS</h3><p><em>If you need to control user or group access to resources, want to configure RAWeb policies (application settings) via the web app, or plan to add RemoteApps and Desktops as a Workspace in the Windows App:</em></p><ol><li>Download and extract the latest pre-built RAWeb zip file from <a href="https://github.com/kimmknight/raweb/releases/latest" target="_blank" rel="noopener noreferrer">the latest release</a>.</li><li>Extract the contents of the zip file to a folder in your IIS website’s directory (default is <code>C:\\inetpub\\wwwroot</code>)</li><li>In IIS Manager, create a new application pool with the name <strong>raweb</strong> (all lowercase). Use <strong>.NET CLR Version v4.0.30319</strong> with <strong>Integrated</strong> pipeline mode.</li><li>In IIS, convert the folder to an application. Use the <strong>raweb</strong> application pool.</li><li>At the application level, edit Anonymous Authentication to use the application pool identity (raweb) instead of IUSR.</li><li>At the application level, enable Windows Authentication.</li><li>Disable permissions enheritance on the <code>RAWeb</code> directory. <ol><li>In <strong>IIS Manager</strong>, right click the application and choose <strong>Edit Permissions…</strong>.</li><li>Switch to the <strong>Security</strong> tab.</li><li>Click <strong>Advanced</strong>.</li><li>Click <strong>Disable inheritance</strong>.</li></ol></li><li>Update the permissions to the following:</li></ol><table><thead><tr><th>Type</th><th>Principal</th><th>Access</th><th>Applies to</th></tr></thead><tbody><tr><td>Allow</td><td>SYSTEM</td><td>Full Control</td><td>This folder, subfolders and files</td></tr><tr><td>Allow</td><td>Administrators</td><td>Full Control</td><td>This folder, subfolders and files</td></tr><tr><td>Allow</td><td>IIS AppPool\\raweb</td><td>Read</td><td>This folder, subfolders and files</td></tr></tbody></table><ol start="9"><li>Grant modify access to the <code>App_Data</code> folder for <strong>IIS AppPool\\raweb</strong>: <ol><li>Under the application in IIS Manager, right click <strong>App_Data</strong> and choose <strong>Edit Permissions…</strong>.</li><li>Switch to the <strong>Security</strong> tab.</li><li>Click <strong>Edit</strong>.</li><li>Select <strong>raweb</strong> and the check <strong>Modify</strong> in the <strong>Allow column</strong>. Click <strong>OK</strong>.</li></ol></li><li>Grant read access to <code>AppData\\resources</code> for <strong>Users</strong>.</li><li>Grant read and execute access to <code>bin\\SQLite.Interop.dll</code> for <strong>IIS AppPool\\raweb</strong></li><li>Install the management service: <ol><li>In Command Prompt or PowerShell, navigate to the <code>bin</code> folder. (for example: <code>cd C:\\inetpub\\wwwroot\\RAWeb\\bin</code>)</li><li>Then, run <code>.\\RAWeb.Server.Management.ServiceHost.exe install</code>.</li></ol></li></ol><p><em>If you only plan to use the web interface without authentication (some features will be disabled):</em></p><ol><li>Download and extract the latest pre-built RAWeb zip file from <a href="https://github.com/kimmknight/raweb/releases/latest" target="_blank" rel="noopener noreferrer">the latest release</a>.</li><li>Extract the contents of the zip file to a folder in your IIS website’s directory (default is <code>C:\\inetpub\\wwwroot</code>)</li><li>In IIS Manager, create a new application pool with the name <strong>raweb</strong> (all lowercase). Use <strong>.NET CLR Version v4.0.30319</strong> with <strong>Integrated</strong> pipeline mode.</li><li>In IIS, convert the folder to an application. Use the <strong>raweb</strong> application pool.</li><li>At the application level, edit Anonymous Authentication to use the application pool identity (raweb) instead of IUSR.</li><li>Ensure that the <strong>Users</strong> group has read and execute permissions for the application folder and its children.</li><li>Install the management service: <ol><li>In Command Prompt or PowerShell, navigate to the <code>bin</code> folder. (for example: <code>cd C:\\inetpub\\wwwroot\\RAWeb\\bin</code>)</li><li>Then, run <code>.\\RAWeb.Server.Management.ServiceHost.exe install</code>.</li></ol></li></ol><h3 id="install-development-branches">Install development branches</h3><p>To install a specific development branch of RAWeb, follow these steps:</p>',9)),t("ol",null,[e[27]||(e[27]=A('<li><p>Determine the branch you want to install. You can view work-in-progress branches on the <a href="https://github.com/kimmknight/raweb/pulls" target="_blank" rel="noopener noreferrer">pull requests page</a>. Branches are in the format <code>&lt;owner&gt;/&lt;branch&gt;</code>. For example: <code>kimmknight/branch-name</code> or <code>jackbuehner/branch-name</code>.</p></li><li><p><strong>Open PowerShell as an administrator.</strong> <br> Press the Windows key + X, then select PowerShell (Administrator) or Terminal (Administrator).</p></li>',2)),t("li",null,[e[26]||(e[26]=t("p",null,[t("strong",null,[n("Type the code below"),t("sup",{class:"footnote-ref"},[t("a",{href:"docs/installation/#fn1",id:"fnref1:2"},"[1:2]")]),n(", replacing the branch name, and then press enter.")])],-1)),o(h,{code:`iwr install.raweb.app/preview/<owner>/<branch> | iex
`})])]),o(g(L),{severity:"caution",title:"Unstable code"},{default:s(()=>e[28]||(e[28]=[n(" Unreleased versions may contain unstable or experimental code that has not been fully tested. Use these versions at your own risk. ")])),_:1}),o(g(L),{severity:"caution",title:"Caution"},{default:s(()=>e[29]||(e[29]=[t("p",null,[n("This will overwrite any existing RAWeb installation. Resources, policies, and other data in "),t("code",null,"/App_Data"),n(" with be preserved.")],-1)])),_:1}),o(g(L),{title:"Note"},{default:s(()=>e[30]||(e[30]=[n(" Unreleased versions are not pre-built. Therefore, they require the .NET SDK to build the application before installation. "),t("p",null,"If you do not already have the .NET SDK installed, the setup script will download and install it for you.",-1)])),_:1}),e[44]||(e[44]=A('<hr class="footnotes-sep"><section class="footnotes"><ol class="footnotes-list"><li id="fn1" class="footnote-item"><p>If you are attempting to install RAWeb on Windows Server 2016, you may need to enable TLS 1.2. In PowerShell, run <code>[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12</code>. <a href="docs/installation/#fnref1" class="footnote-backref">↩︎</a> <a href="docs/installation/#fnref1:1" class="footnote-backref">↩︎</a> <a href="docs/installation/#fnref1:2" class="footnote-backref">↩︎</a></p></li></ol></section>',2))])}}},Ht=Object.freeze(Object.defineProperty({__proto__:null,default:Wt,title:Ft},Symbol.toStringTag,{value:"Module"})),kt={class:"markdown-body"},Ut="$t{{ policies.App.Alerts.SignedInUser.title }}",Mt="User and group folders",Gt=["policies/App.Alerts.SignedInUser"],Bt={__name:"index",setup(l,{expose:r}){return r({frontmatter:{title:"$t{{ policies.App.Alerts.SignedInUser.title }}",nav_title:"User and group folders",redirects:["policies/App.Alerts.SignedInUser"]}}),(d,e)=>{const i=p("PolicyDetails");return a(),c("div",kt,[o(i,{translationKeyPrefix:"policies.App.Alerts.SignedInUser",open:""})])}}},xt=Object.freeze(Object.defineProperty({__proto__:null,default:Bt,nav_title:Mt,redirects:Gt,title:Ut},Symbol.toStringTag,{value:"Module"})),Yt="/deploy-preview/pr-215/lib/assets/windows-radc-blocked-DAgLB_-p.webp",Vt={class:"markdown-body"},jt="$t{{ policies.WorkspaceAuth.Block.title }}",_t="Block workspace authentication",qt=["policies/WorkspaceAuth.Block"],Jt={__name:"index",setup(l,{expose:r}){return r({frontmatter:{title:"$t{{ policies.WorkspaceAuth.Block.title }}",nav_title:"Block workspace authentication",redirects:["policies/WorkspaceAuth.Block"]}}),(d,e)=>{const i=p("RouterLink"),h=p("PolicyDetails");return a(),c("div",Vt,[e[3]||(e[3]=t("p",null,"Enable this policy to prevent workspace clients (such as Windows App) from authenticating to RAWeb. When enabled, users will be unable to add RAWeb’s resources to workspace clients or refresh them if they have already been added.",-1)),t("p",null,[e[1]||(e[1]=n("This policy is useful if you want to require multi-factor authentication (MFA) for all users accessing RAWeb’s resources. Workspace clients do not support MFA, so they must be blocked. For more information about using MFA with RAWeb, see ")),o(i,{to:"/docs/security/mfa"},{default:s(()=>e[0]||(e[0]=[n("Enable multi-factor authentication (MFA) for the web app")])),_:1}),e[2]||(e[2]=n("."))]),o(h,{translationKeyPrefix:"policies.WorkspaceAuth.Block"}),e[4]||(e[4]=t("p",null,"When this policy is enabled, the workspace URL section of the RAWeb settings page will be hidden.",-1)),e[5]||(e[5]=t("p",null,"When this policy is enabled, users will see an error message after attempting to authenticate via a workspace client. The error message will be a generic error message.",-1)),e[6]||(e[6]=t("p",null,[n("For example, on Windows, users may see the following error message:"),t("br"),t("img",{width:"580",src:Yt,height:"515",xmlns:"http://www.w3.org/1999/xhtml"})],-1))])}}},zt=Object.freeze(Object.defineProperty({__proto__:null,default:Jt,nav_title:_t,redirects:qt,title:jt},Symbol.toStringTag,{value:"Module"})),Kt={class:"markdown-body"},Xt="$t{{ policies.RegistryApps.Enabled.title }}",$t="Centralized publishing (registry)",Qt=["policies/RegistryApps.Enabled"],Zt={__name:"index",setup(l,{expose:r}){return r({frontmatter:{title:"$t{{ policies.RegistryApps.Enabled.title }}",nav_title:"Centralized publishing (registry)",redirects:["policies/RegistryApps.Enabled"]}}),(d,e)=>{const i=p("RouterLink"),h=p("PolicyDetails");return a(),c("div",Kt,[e[3]||(e[3]=t("p",null,"Enable this policy to store published RemoteApps and desktops in their own collection in the registry. If you have RAWeb and RDWeb running on the same server or multiple installations of RAWeb, enabling the policy ensures that visibility settings for RemoteApps and desktops do not conflict between the installations.",-1)),e[4]||(e[4]=t("p",null,"This policy defaults to enabled, but older versions of RAWeb may have it disabled by default. If you are upgrading from an older version of RAWeb, you must enable this policy manually.",-1)),e[5]||(e[5]=t("p",null,"This policy must be enabled for RDP file property customizations to be available.",-1)),e[6]||(e[6]=t("p",null,"The TSWebAccess option in RemoteApp Tool only works when this policy is disabled.",-1)),e[7]||(e[7]=t("p",null,"The option to show the system desktop in the web interface and in Workspace clients is only available when this policy is enabled.",-1)),t("p",null,[e[1]||(e[1]=n("For instructions on managing published RemoteApps and desktops, see ")),o(i,{to:"/docs/publish-resources/"},{default:s(()=>e[0]||(e[0]=[n("Publish RemoteApps and Desktops")])),_:1}),e[2]||(e[2]=n("."))]),o(h,{translationKeyPrefix:"policies.RegistryApps.Enabled",open:""})])}}},en=Object.freeze(Object.defineProperty({__proto__:null,default:Zt,nav_title:$t,redirects:Qt,title:Xt},Symbol.toStringTag,{value:"Module"})),tn="data:image/webp;base64,UklGRgwHAABXRUJQVlA4WAoAAAAIAAAAAwEAdwAAVlA4IA4GAACwJgCdASoEAXgAPzGWvFKnJaMhJnXp+OYmCelu3V/svHbf+6ATtU5nQsXk1d3k48Z065wF64/Ze921GlWg1DyhrST6JBkErJ8pvXtBLEgZgpMyd4hCYDS6oBwhP0lwiCiwSu2PzG9v+YR2V4lsjuxg413JwewALYrIz2KmL728B24G8gJ02zVaPlsmrGSJehA3AKESV1Q79PIEwXB+GCNabSAgveqfx+//LCO8oHHChMJuaaCxLPm9dJKgfyny/oBnrbJJ6OvAoQC66u6E2pony+GE/7QLJlUABkKjRVIS8zAEsJbK2yy/USNuD2bz4JF/2rtOVC02ES3SxFbLbKi/W8B5ninpGNC4URpVaupHErSYE+02SUCyz71UOTpf5A0DujeNjBQ1bdGZ7EWM7Mfx/Gm3eZAVYCZaAdkRhwAA/vBDD6uO1nzSe9B2iHDTXGPDFbPr7a6X/3q1g0VouFFXkbF8qYAVe1xrMza22DS93DSjbtWgo85Qa893RZ+zRyMHRhRA/tkEr9K+cZGIoowpGUGdhVbqm4oNdTxKRrgchYlqxReHxwXbQK0R4525NCanN8iJOwQF9KGj8B62M9DoYOFvNO54isSIjNh++UNtk2k66YN8FlYgOyMBlv+i5hh3rkAsFjZCbl/glicIaeuQeGbS1Z21zOjLKlhapS4h2uPrJSe+JTWFi03D0oEUUhYWC4Wokk/fDX7MgGsMgS5JX6QU/U9uUhM3tep/0ND2pW9JvPNHOlf5IB0sndc1Ymf8ENmbp9wcz+9Hl7NWO0cZjPOVjP4OWmtdo1eQB0Lo09FOIHUOZeUl0zfFyddUTJem7vR4RqesXzL/21p8hzJnNz8+4UgVTGKjoF4Q9s15JJUADnKlKJmPBpYm3k72F2sDf3JJJ1sNHkO9LZ6AKmx1duoUUy0tzbf5+jLHa60Pm+pcgrCHZYWAcKjPYPoTJau88uekoDh/CV0IsByzYlwnW4wI1uSUQiv0SkSNzzXkxOrO3h3PqhCvjBY/x3ErAQFp1h90QkUvdhB4tKqlRVf4WI1eq9HI8wFCJtCctiN/dJzuwuS1Ox858q41tGmQPhQVEBrEXtJ/ioZc/W/TLJpSTVMNMxnnpB6ogQ+8ph7yf0RUZ18To4SHEEZvOYp/xPjbeSKt15XErxW0Qm/V2+YOxkwpUou93Hk5CifiVI8dpVGudORh7Bc/DkamyUSCsOZLW7Yk/uuKjnRpEG0fqhkD4PkVbC8nwTuxnJU9hC5LqEpZUIz+RcvnAHVUAjwp7aP1vZrEbQa96eYb2vGHI9gtnqvxHCvDASuQLvPpOSfZWwIAHzIez3DgMAyOu62mPXB9bzlTsrH3cpDf+KNEKk0GHvh/gsySlpeA1r7eN7vRLXfxI2XIqK1ZdzC3sP8xuAx/dWsrSs/6KP9mY8PH0jKLrOFfkiGJu1NfVpsMXV3aQNRDOtPcyN9HkGh2hWEdnGClA9Onlvcsi7jxIrVHmp1swGIolFn1GeGhNYsTt/3meJ2Gqt5++iCA3HVSpc1m0ThSAuWXcHTu9Zmt0Hgy9FRMSdqn6SC7iE8j4tPAxH4YHIwoSoR/GVd7BsWEwHD+W7qpyxq5GhFztH7q9sIrUOLq7YweLG0W3zr2FKj7Anr7bafviQV1cwgO2V8uaG81PV8LdFIHsBdXwCxKtp+q+GqKiQ1s4JN5HkFqywwBXHJaG66iyEP3bTDe8Mc0ffu7bP4P0/DtCcd9ExDHs1hs+1marD1195vBfyGt0dTlnlCSSMNRKtLOQR3vo10cSqqZ6jPuYeLy91Dd5390KJnBSayredFWfr0aNuAGKMAeqUTGbJ46lvPyoLg5/ZCebnn5gV/Cpvqe3gvoqJv+/Y6zKnjmwwhHjaxbNGgFENpn88lCV6vRUGqnoRMesxpzX0l9c9r8r3qUSyKyVNHXvqpssLQOeGrBs+wuajDVLPWEBD1smrkmEHM1nPRAtEGL0N6Mlb88aFDb/2cKShoRunSEKr4iHQXmceUn/nIB0vbkm1WIix7rl9vF1OHqdorAzLFaVK0AAABEtKGA0GPu8AAAAEVYSUbYAAAASUkqAAgAAAAGABIBAwABAAAAAQAAABoBBQABAAAAVgAAABsBBQABAAAAXgAAACgBAwABAAAAAgAAADEBAgARAAAAZgAAAGmHBAABAAAAeAAAAAAAAABgAAAAAQAAAGAAAAABAAAAUGFpbnQuTkVUIDUuMS4xMQAABQAAkAcABAAAADAyMzABoAMAAQAAAAEAAAACoAQAAQAAAAQBAAADoAQAAQAAAHgAAAAFoAQAAQAAALoAAAAAAAAAAgABAAIABAAAAFI5OAACAAcABAAAADAxMDAAAAAA",nn="/deploy-preview/pr-215/lib/assets/sign-in-password-expired-AcEGZZMI.webp",on={class:"markdown-body"},rn="$t{{ policies.PasswordChange.Enabled.title }}",sn="Change passwords via RAWeb",an=["policies/PasswordChange.Enabled"],ln={__name:"index",setup(l,{expose:r}){return r({frontmatter:{title:"$t{{ policies.PasswordChange.Enabled.title }}",nav_title:"Change passwords via RAWeb",redirects:["policies/PasswordChange.Enabled"]}}),(d,e)=>{const i=p("PolicyDetails");return a(),c("div",on,[e[0]||(e[0]=t("p",null,"RAWeb allows users to change their passwords directly through the web interface. This feature can be enabled or disabled based on your organization’s policies.",-1)),e[1]||(e[1]=t("p",null,"Allowing password changes via RAWeb is particularly useful in environments where a user may have an expired password and is unable to sign in to RAWeb or connect to remote resources until they update their password.",-1)),o(i,{translationKeyPrefix:"policies.PasswordChange.Enabled"}),e[2]||(e[2]=t("h2",null,"Accessing the password change feature",-1)),e[3]||(e[3]=t("p",null,"When the password change feature is enabled, users will see the password change option in the following locations:",-1)),e[4]||(e[4]=t("h3",null,"Profile menu",-1)),e[5]||(e[5]=t("p",null,"Any signed in user can access the password change feature from their profile menu in the top-right corner of the RAWeb interface.",-1)),e[6]||(e[6]=t("img",{width:"260",src:tn,height:"120",xmlns:"http://www.w3.org/1999/xhtml"},null,-1)),e[7]||(e[7]=t("h3",null,"Sign in",-1)),e[8]||(e[8]=t("p",null,"If a user’s password has expired or an administrator has chosen to force a password change, they will be prompted to change their password directly from the sign-in screen.",-1)),e[9]||(e[9]=t("img",{width:"504",src:nn,height:"518",xmlns:"http://www.w3.org/1999/xhtml"},null,-1))])}}},dn=Object.freeze(Object.defineProperty({__proto__:null,default:ln,nav_title:sn,redirects:an,title:rn},Symbol.toStringTag,{value:"Module"})),cn={class:"markdown-body"},hn="$t{{ policies.App.CombineTerminalServersModeEnabled.title }}",un="Combine alike apps",pn=["policies/App.CombineTerminalServersModeEnabled"],mn={__name:"index",setup(l,{expose:r}){return r({frontmatter:{title:"$t{{ policies.App.CombineTerminalServersModeEnabled.title }}",nav_title:"Combine alike apps",redirects:["policies/App.CombineTerminalServersModeEnabled"]}}),(d,e)=>{const i=p("PolicyDetails");return a(),c("div",cn,[o(i,{translationKeyPrefix:"policies.App.CombineTerminalServersModeEnabled",open:""})])}}},fn=Object.freeze(Object.defineProperty({__proto__:null,default:mn,nav_title:un,redirects:pn,title:hn},Symbol.toStringTag,{value:"Module"})),An="/deploy-preview/pr-215/lib/assets/70185ca9-b89e-4137-b381-262960d102c0-DTgiFvTd.png",gn="/deploy-preview/pr-215/lib/assets/664409eb-6939-4101-a025-e07ea9ed141b-a1NswxUH.png",In="/deploy-preview/pr-215/lib/assets/edbfce9f-c9df-4c52-b353-9efaf027c639-DoypYOPV.png",Tn="/deploy-preview/pr-215/lib/assets/21a8dc0c-b148-4512-9901-93408de26f5e-BKXRSbPs.png",bn="/deploy-preview/pr-215/lib/assets/7528f048-07d8-420a-bc1d-7a16d93a39d3-B7VTEgC6.png",yn="/deploy-preview/pr-215/lib/assets/3afa8501-6057-433c-94a8-6b7cb6e26397-DcoSlUs8.png",Le="/deploy-preview/pr-215/lib/assets/iis-application-settings-5TEUsPqX.png",Ce="/deploy-preview/pr-215/lib/assets/iis-application-settings-add-DLHYjVCl.png",Rn={class:"markdown-body"},En="Configure hosting server and terminal server aliases",wn="Configure aliases",Sn=["policies/TerminalServerAliases"],On={__name:"index",setup(l,{expose:r}){return r({frontmatter:{title:"Configure hosting server and terminal server aliases",nav_title:"Configure aliases",redirects:["policies/TerminalServerAliases"]}}),(d,e)=>{const i=p("PolicyDetails");return a(),c("div",Rn,[e[0]||(e[0]=A('<p>If you want to customize the name of the hosting server that appears in RAWeb or any of the remote desktop clients, or you want to customize the names of the terminal servers for your remote apps and desktops, follow the instructions after the example section.</p><h1>Example (before and after)</h1><p><em>Using <code>&lt;add key=&quot;TerminalServerAliases&quot; value=&quot;WIN-SGPBICA0161=Win-RemoteApp;&quot; /&gt;</code></em></p><table><thead><tr><th>Before</th><th>After</th></tr></thead><tbody><tr><td><img src="'+An+'" alt="image"></td><td><img src="'+gn+'" alt="image"></td></tr><tr><td><img src="'+In+'" alt="image"></td><td><img src="'+Tn+'" alt="image"></td></tr><tr><td><img src="'+bn+'" alt="image"></td><td><img src="'+yn+'" alt="image"></td></tr></tbody></table><h1>Method 1: RAWeb web interface</h1><ol><li>Sign in to the web interface with an account that is a memeber of the Local Administrators group.</li><li>On the left navigation rail, click the <strong>Policies</strong> button.</li><li>Click <strong>Configure aliases for terminal servers</strong>.</li><li>In the dialog, set the <strong>State</strong> to <strong>Enabled</strong>. Under <strong>Options</strong>, click <strong>Add</strong> to add a new alias. For <strong>Key</strong>, specify the name of the server. For <strong>value</strong>, specify the alias you want to use. Click <strong>OK</strong> to save the alias(es).</li></ol>',6)),o(i,{translationKeyPrefix:"policies.TerminalServerAliases"}),e[1]||(e[1]=t("h1",null,"Method 2: IIS Manager",-1)),e[2]||(e[2]=t("ol",null,[t("li",null,[n("Once RAWeb is installed, open "),t("strong",null,"IIS Manager"),n(" and expand the tree in the "),t("strong",null,"Connections pane"),n(" on the left side until you can see the "),t("strong",null,"RAWeb"),n(" application. The default name is "),t("strong",null,"RAWeb"),n(", but it may have a different name if you performed a manual installation to a different folder. Click on the "),t("strong",null,"RAWeb"),n(" application.")]),t("li",null,[n("In the "),t("strong",null,"Features View"),n(", double click "),t("strong",null,"Application Settings"),n(),t("br"),t("img",{width:"860",src:Le,height:"471",xmlns:"http://www.w3.org/1999/xhtml"})]),t("li",null,[n("In the "),t("strong",null,"Actions pane"),n(", click "),t("strong",null,"Add"),n(" to open the "),t("strong",null,"Add Application Setting"),n(" dialog. "),t("br"),t("img",{width:"860",src:Ce,height:"471",xmlns:"http://www.w3.org/1999/xhtml"})]),t("li",null,[n("Specify the properties. For "),t("strong",null,"Name"),n(", use "),t("em",null,"TerminalServerAliases"),n(". For "),t("strong",null,"Value"),n(", specify the aliases with the format "),t("em",null,"ServerName1=Alias 1;"),n(". You can specify multiple aliases separated by semicolons. When you are finished, click "),t("strong",null,"OK"),n(".")])],-1)),e[3]||(e[3]=A("<h1>Method 3. Directly edit <code>appSettings.config</code>.</h1><ol><li>Open <strong>File Explorer</strong> and navigate to the RAWeb directory. The default installation directory is <code>C:\\inetpub\\RAWeb</code>.</li><li>Navigate to <code>App_Data</code>.</li><li>Open <code>appSettings.config</code> in a text editor.</li><li>Inside the <code>appSettings</code> element, add: <code>&lt;add key=&quot;TerminalServerAliases&quot; value=&quot;&quot; /&gt;</code></li><li>Edit the value attribute to specify the aliases. You can specify aliases with the format <em>ServerName1=Alias 1;ServerName2=Alias 2;</em>.</li><li>Save the file.</li></ol>",2))])}}},Nn=Object.freeze(Object.defineProperty({__proto__:null,default:On,nav_title:wn,redirects:Sn,title:En},Symbol.toStringTag,{value:"Module"})),vn="/deploy-preview/pr-215/lib/assets/rdpFile-method-BhPEwO9q.webp",ne="/deploy-preview/pr-215/lib/assets/no-connection-method-CurJzOdQ.webp",Ln={class:"markdown-body"},Cn="$t{{ policies.App.ConnectionMethod.RdpFileDownload.Enabled.title }}",Dn="RDP file connection method",Pn=["policies/App.ConnectionMethod.RdpFileDownload.Enabled"],Fn={__name:"index",setup(l,{expose:r}){return r({frontmatter:{title:"$t{{ policies.App.ConnectionMethod.RdpFileDownload.Enabled.title }}",nav_title:"RDP file connection method",redirects:["policies/App.ConnectionMethod.RdpFileDownload.Enabled"]}}),(d,e)=>{const i=p("PolicyDetails");return a(),c("div",Ln,[e[0]||(e[0]=t("p",null,"This policy controls whether the option to download an RDP file is available to users when connecting to resources.",-1)),e[1]||(e[1]=t("img",{width:"400",src:vn,height:"210",xmlns:"http://www.w3.org/1999/xhtml"},null,-1)),e[2]||(e[2]=t("p",null,"When enabled, users will see a “Download RDP File” button in the connection dialog, allowing them to download an RDP file configured to connect to the selected resource. Users can then use this file with their preferred RDP client application.",-1)),e[3]||(e[3]=t("p",null,"When disabled, the “Download RDP File” option will not be shown, preventing users from downloading RDP files for resource connections.",-1)),e[4]||(e[4]=t("p",null,"If no connection methods are enabled, users will be unable to connect to resources via the web app. Instead, they will see this following dialog:",-1)),e[5]||(e[5]=t("img",{width:"400",src:ne,height:"185",xmlns:"http://www.w3.org/1999/xhtml"},null,-1)),o(i,{translationKeyPrefix:"policies.App.ConnectionMethod.RdpFileDownload.Enabled"})])}}},Wn=Object.freeze(Object.defineProperty({__proto__:null,default:Fn,nav_title:Dn,redirects:Pn,title:Cn},Symbol.toStringTag,{value:"Module"})),Hn="/deploy-preview/pr-215/lib/assets/rdpProtocolUri-method-DqGWt4_i.webp",kn={class:"markdown-body"},Un="$t{{ policies.App.ConnectionMethod.RdpProtocol.Enabled.title }}",Mn="RDP URI connection method",Gn=["policies/App.ConnectionMethod.RdpProtocol.Enabled"],Bn={__name:"index",setup(l,{expose:r}){return r({frontmatter:{title:"$t{{ policies.App.ConnectionMethod.RdpProtocol.Enabled.title }}",nav_title:"RDP URI connection method",redirects:["policies/App.ConnectionMethod.RdpProtocol.Enabled"]}}),(d,e)=>{const i=p("PolicyDetails");return a(),c("div",kn,[e[0]||(e[0]=t("p",null,"This policy controls whether the option to launch a resources via its rdp:// URI is available to users when connecting to resources.",-1)),e[1]||(e[1]=t("img",{width:"400",src:Hn,height:"210",xmlns:"http://www.w3.org/1999/xhtml"},null,-1)),e[2]||(e[2]=A('<p>When enabled, users will see a “Launch via rdp://” button in the connection dialog, allowing them to directly launch a resource without downloading it first. On supported systems, this will open the resource in the user’s default RDP client application. See the table below for enabling support for rdp:// URIs on different platforms:</p><table><thead><tr><th>Platform</th><th>Required application</th></tr></thead><tbody><tr><td>Windows</td><td><a href="https://apps.microsoft.com/detail/9N1192WSCHV9?hl=en-us&amp;gl=US&amp;ocid=pdpshare" target="_blank" rel="noopener noreferrer">Remote Desktop Protocol Handler</a> from the Microsoft Store or from <a href="https://github.com/jackbuehner/rdp-protocol-handler/releases" target="_blank" rel="noopener noreferrer">jackbuehner/rdp-protocol-handler</a></td></tr><tr><td>macOS</td><td><a href="https://apps.apple.com/us/app/windows-app/id1295203466" target="_blank" rel="noopener noreferrer">Windows App</a> from the Mac App Store</td></tr><tr><td>iOS or iPadOS</td><td><a href="https://apps.apple.com/us/app/windows-app-mobile/id714464092" target="_blank" rel="noopener noreferrer">Windows App Mobile</a> from the App Store</td></tr><tr><td>Android</td><td>Not supported</td></tr></tbody></table><p>When disabled, the “Launch via rdp://” option will not be shown.</p><p>If no connection methods are enabled, users will be unable to connect to resources via the web app. Instead, they will see this following dialog:</p>',4)),e[3]||(e[3]=t("img",{width:"400",src:ne,height:"185",xmlns:"http://www.w3.org/1999/xhtml"},null,-1)),o(i,{translationKeyPrefix:"policies.App.ConnectionMethod.RdpProtocol.Enabled"})])}}},xn=Object.freeze(Object.defineProperty({__proto__:null,default:Bn,nav_title:Mn,redirects:Gn,title:Un},Symbol.toStringTag,{value:"Module"})),Yn={class:"markdown-body"},Vn="$t{{ policies.App.OpenConnectionsInNewWindowEnabled.title }}",jn="Open connections in new window",_n=["policies/App.OpenConnectionsInNewWindowEnabled"],qn={__name:"index",setup(l,{expose:r}){return r({frontmatter:{title:"$t{{ policies.App.OpenConnectionsInNewWindowEnabled.title }}",nav_title:"Open connections in new window",redirects:["policies/App.OpenConnectionsInNewWindowEnabled"]}}),(d,e)=>{const i=p("PolicyDetails");return a(),c("div",Yn,[o(i,{translationKeyPrefix:"policies.App.OpenConnectionsInNewWindowEnabled",open:""})])}}},Jn=Object.freeze(Object.defineProperty({__proto__:null,default:qn,nav_title:jn,redirects:_n,title:Vn},Symbol.toStringTag,{value:"Module"})),zn="/deploy-preview/pr-215/lib/assets/duo-auth-flow-BM7wueUI.webp",Kn="data:image/webp;base64,UklGRkQLAABXRUJQVlA4WAoAAAAIAAAAWgEAGAEAVlA4IEYKAABQTgCdASpbARkBPzGSvFSnJSghopUrYQYmCc3cLlIcwvL3umr5n3nOW3aD9IH+T3d3PA/gBrQHogdNZkR/k7a18twQdpfVT32+rzPxwBdYNqxK1vC9alg/of0P6H9D+h/Q/of0P5uZQAbNfWuvJtrJLoZK8diwF0TF3ZRso2KqhXQ/IMz7YWZRmuCRuxnou3tR6h7g7sfEOJW0l3+mSrNedwK8rqe1hwRPB4ELycvGZ2FQKMbSbYP6awqHxvW9PgwEaH1fwbqNLo9kcvaeySE8RIO5HcHjtiOZ8pK0ELeu/6rh1ks5IIfm7ylkDunB6IYvr68dQtvh4ZWaWHB8cjTz3IDdQATAc9QgdjDRI0svyNELzddHPY1P2iSEX1J4RmhppXj5ZfFDf03VE9uqK2dMUcGQJE0tneA3qE5R+DZalwrxJRI7gPPt2t51quEFZlPNNLsS91BF8ZAUnqsI4oxarg9NRso2O9I/2Ys3MM0RSN2M9I9aB43Yz0j1oHI08N471kSyyM+wDK4ErCp83/L98+evXr169evMvkwf7Yk2uWDHJRQ0594q1fECkTYcyo4qJma15UrVFz7iGds14+XOPYB8LK/cFsIoSWqKaI3jd2DgVrXkH53ZQyusA04Gq/CpQ2ov8kUJm7pne/y8wQ1IEPISDCBg/M4OTtLg2wke2ebinCKTfZ5uKcIpN9fbh5vllLoMTVoFPR9cS4D2LxyqfQrEgoJ2AGW1q/LgzJ5l7B6tQqFQxt7J5et6KXf0tOWkvKSEOFKNlGyjZQmzl2GhFPFM82GR3I7kdyH8QWw59w5c9h1Be5RuxnpHrQPG7GeketA8aue/gbkcAAD+/buQAAAKKfdVTKykvL2VNJRtixJyFe1s1ke61RZrYf+7O/93yAd+5f8yOtEeV38P2LfR8FzqLW8GwbgUWkL0eJCH+giCjMn/ylLvduDiOoNb18JPeZo2b8lcOTUeRufLFfvepxgBVr4eHN2V62iPuurTLP4jym+519jNGBSB/URYbSnen9pRgCCIpoZjHpsFtAo4saPxAkhyDgNQQ4GoWfI96Ws6lUWfcBpCgvLjREkVbTvGtcSvjHrK3UE5Xu4ekoJnDidA+MXlw5Og91wBFg7FWPpI/+uKqwp8Ej2rWnE45JtTlarD2kF68R/0zf4gWLPf6lCjA455MaIJAUk0laQHj5zKKPyRS5CEqmRpsQ2LCR2t8hc7MESqdmRP4n02kwgxAbsCNg5nZjMLyjWs5B8jf5PYQO9mRIHHgqhHXouCDg7yvYLcri+U+/FU2a96z+V77GvmelIqpzI86YTt2iC6JW5YDnv5QwjGWiB/XvRjF5JGxc9cKk1nXdx896THN+mtmNYBD9VLgAaO3YxKt28x+shFnSdFwXCuYOLNPNOmPhPizJkl0HzU+qvLnK+hqLXGB6vH5FiRKG7h3ygpRqF867i/C6mBzxnf+XEpaCelBCHjssykhmBt8YuzgdyaVN4JCy4hS5xYOM8RmIJWV+RdIlPUNA+5K39H0TbfNiQSib9twYCsJZXvCKRzC5z8NK2wptk1SziLwiZwIdgJMxBwZdG8Q3iOLyovOBoUcXrlLCMv7FgQ0xtFkHuWIyw8fzIYI4Ma9jW34SiT7knw3C9TJKhLzDoyvBvZZdbJL+MBpf8bRxOWZAG6T97iHpdRPZn6PSZUZ53G4H1NWiK+JeouwsB6WD07o2ows02qVBL8V5hF1/JVfjD1gGMRLyYnqb3nWcR3MUo/wbJ8fIXoCfyANu0i56Tiz42ApEN06qmBSHUIJrm0vomtaR3LkUmExzH4tLMRNJP2PvWUWSsDV4CYapHI0F9QS2VgnZhL8VyQ6twjRhX/Ii1sYqKiDKKXivWDWtb9gifxwxgKYsZxiIeong+Tf1Mm9v9UAHwJiRK9nhQi37dlPX5juxtbnQZHf6RzVpePdEf688JqKWTVZcX8HhzrCNpBquTfKLl3EmGmd8TzSTFJaDjZjpPnIzAI83tM5SORJppUH6LfUQh/3BBk02hQUUVdl5BJ0Wz21EAAEpqiVk7CC4f+1fMJEN9vyD8f9/yqz03eb1gm+pUnHXD2mHEHWr+3BWbkD526ffX2bkwF/GvGCnBWG43AlWepR1zls6J4DdwGWogDbZFVpqnmNV7G2Wn6aUydjJ4qmA0e8cwv3MiX2va2ZMqBIKBHvbzTlI8AL68No+9FeLd/oW3Cyv3+MbH9i3ePgrprCdY53a4GxeHFv88MAA2hj+MBJADcsnq5LEXEUhGFm8TTXNUct+8FkZIbt/hyYVrA/THy5ajjFA/LjsBkjyibBtkxzAABecv0SZ1w96UJ7qIzihVFPONais4ZvXXoLTF6tr15qDwUVgAgimLPc09MtmgCfn3Opc/DPtNUqrlZlxVGhT4IUPO3DfW6Gwd0tdJbnAWMSHun4RhBg7382T3bk+yrzLN12bb0oOuVP2Q7vSxfq7APrsA+uwD64eM8LkDB3j9Z510cppiCs1+/Juwo5u0PzYu0t2kjL1jMNuQMXb42G8l9TI3RzZe+/68ZAHq9U1snTRdHlJODwIZR0VoEemKOqxW3O1JHtnEnMZx86IPUtchHECg13AQm6XfDOLQDlrSAY0bThfKgJv5uiAasHnMOOatjF9zevJqHIY6jnoszJqc7sRNKcuTZbTYV04C+Azva6S3xzshqYw7SDcrP3J6uFQxBMaPHPZT2cNk0ER6Tiq/YTzwgRzxCu+vbZ0bNaNNC1ywaPAeam4Zy6ieh/UoOgBXuDv+YFRpt02kg83XKgqtUWKiJcO/Vuj3UaogPayULihcFItXBD9Xp9WshmktBbJMfSOwWgUjxH/22o9wTXvgVdQ7CHZKdUePr3ig58tP2WeNsCgGf+ryurKOkqu1GinrHiYQN+e29JDw9fF2jylNcli2JVcsZtSmzjrOUWqSgoSG3Q0YItfrOPbHoIKluS9+pTWuyiqNq54L4vEk0fvgQHteL8ZNCBM/af1h9k3+eXtONi8r3KgvTtFmdS7yvGD+xjvUuWyu+WDaNVXGU2yRfFi2QNJPUgttViCrIUPcqza5V/MIf/KmEPvDQkiav137jQAkJjr57opl9HjQVY/eFy7vUWm0h+TA9wxTwkAEFsIMFhIeOb91k0LlXxppNJKKZ9er8sfgh5HpGQflNlNa993nPuGphTF0FiQlUAVAbBCzQKr9uX4yLPyVaDYcFCo1LSouAA8qtiUPD3pY44R3pjSGJAIOD+wij88X2J0q3zVjg+kezWzZ2FInR2ZiMYBH75e7HXC9V1/bbGccIllyJxET/DnA4qdzPgpTFuxwxSCCrHtBfdEyC6CPKLsVULVNPbQ8MER8yC3V2AByvwnH1uEAgvUWK6zaRtp6JZQnrXQiuYEbDdIic8q7ANnerwAGl9XW3X14AADCElFe7VPj2IA/3wQIcatn5YWiLKufZBOa8rZWytRK/bUA42gUv6b03pvTem9agQFVkHRAFgRcOHhB9LAAAAEVYSUbYAAAASUkqAAgAAAAGABIBAwABAAAAAQAAABoBBQABAAAAVgAAABsBBQABAAAAXgAAACgBAwABAAAAAgAAADEBAgARAAAAZgAAAGmHBAABAAAAeAAAAAAAAABgAAAAAQAAAGAAAAABAAAAUGFpbnQuTkVUIDUuMS4xMQAABQAAkAcABAAAADAyMzABoAMAAQAAAAEAAAACoAQAAQAAAFsBAAADoAQAAQAAABkBAAAFoAQAAQAAALoAAAAAAAAAAgABAAIABAAAAFI5OAACAAcABAAAADAxMDAAAAAA",Xn="/deploy-preview/pr-215/lib/assets/catalog-2bmucMsX.webp",$n={class:"markdown-body"},Qn="$t{{ policies.App.Auth.MFA.Duo.title }}",Zn="Duo Universal Prompt",eo=["policies/App.Auth.MFA.Duo"],to={__name:"index",setup(l,{expose:r}){return r({frontmatter:{title:"$t{{ policies.App.Auth.MFA.Duo.title }}",nav_title:"Duo Universal Prompt",redirects:["policies/App.Auth.MFA.Duo"]}}),(d,e)=>{const i=p("RouterLink"),h=p("PolicyDetails"),y=p("InfoBar");return a(),c("div",$n,[e[5]||(e[5]=t("p",null,"Enable this policy to require users to provide a second factor of authentication via Duo Security’s Universal Prompt when signing in to RAWeb.",-1)),t("p",null,[e[1]||(e[1]=n("For alternative providers and MFA caveats, see ")),o(i,{to:"/docs/security/mfa"},{default:s(()=>e[0]||(e[0]=[n("Enable multi-factor authentication (MFA) for the web app")])),_:1}),e[2]||(e[2]=n("."))]),o(h,{translationKeyPrefix:"policies.App.Auth.MFA.Duo"}),e[6]||(e[6]=A('<p>Jump to a section:</p><ul><li><a href="docs/policies/duo-mfa/#auth-flow">Authentication flow</a></li><li><a href="docs/policies/duo-mfa/#about-duo">About Duo Security</a></li><li><a href="docs/policies/duo-mfa/#create-duo-app">Create RAWeb application in Duo</a></li><li><a href="docs/policies/duo-mfa/#configure-integration">Configure RAWeb to use Duo</a></li><li><a href="docs/policies/duo-mfa/#exclude-accounts">Exclude specific accounts from Duo MFA</a></li></ul><h2 id="auth-flow">Authentication flow</h2><p>When a user signs in to RAWeb with the Duo MFA policy enabled, the following flow occurs:</p><ol><li>The user enters their username and password in RAWeb’s sign-in form.</li><li>RAWeb verifies that the username and password are correct.</li><li>RAWeb updates its cache of the user’s details (if the user cache is enabled).</li><li>RAWeb checks if a Duo MFA policy is configured for the user’s domain. If no policy is found or the user’s account is excluded, the user is signed in without further prompts.</li><li>RAWeb instructs the web client to load Duo’s Universal Prompt.</li><li>The user selects their preferred second factor method in the Duo Universal Prompt and completes the authentication. If the user has not yet enrolled in Duo, they will be prompted to enroll up to two authentication factors.</li><li>Duo redirects to RAWeb.</li><li>If RAWeb receives a successful authentication response from Duo, the user is signed in to RAWeb. If the response indicates a failure or is missing, the sign-in attempt is rejected.</li></ol>',5)),e[7]||(e[7]=t("img",{width:"500",src:zn,height:"574",xmlns:"http://www.w3.org/1999/xhtml"},null,-1)),e[8]||(e[8]=A('<h2 id="about-duo">About Duo Security</h2><p><a href="https://duo.com/" target="_blank" rel="noopener noreferrer">Duo</a> provides <a href="https://duo.com/product/multi-factor-authentication-mfa" target="_blank" rel="noopener noreferrer">multi-factor authentication services</a> for a variety of applications and services. RAWeb integrates with Duo via the Duo Universal Prompt, which provides an interface for users to select their preferred second factor method during authentication.</p><p>Duo provides a free tier for up to 10 users. Larger teams can choose from several paid plans based on their needs. See <a href="https://duo.com/pricing" target="_blank" rel="noopener noreferrer">Duo’s pricing page</a> for more information. RAWeb’s integration only requires the MFA feature, which is included in all plans.</p><h2 id="create-duo-app">Create RAWeb application in Duo</h2>',4)),e[9]||(e[9]=t("ol",null,[t("li",null,[n("Sign in to "),t("a",{href:"https://admin.duosecurity.com/",target:"_blank",rel:"noopener noreferrer"},"admin.duosecurity.com"),n(" with your Duo account’s admin credentials. "),t("ul",null,[t("li",null,[n("If you do not have an account, you can start a free trial at "),t("a",{href:"https://signup.duo.com/",target:"_blank",rel:"noopener noreferrer"},"signup.duo.com"),n(". The trial will automatically switch to the free tier after 30 days.")])])]),t("li",null,[n("From the home page, click "),t("strong",null,"Add new…"),n(" and choose "),t("strong",null,"Application"),n("."),t("br"),t("img",{width:"200",src:Kn,height:"162",xmlns:"http://www.w3.org/1999/xhtml"})]),t("li",null,[n("Find the "),t("strong",null,"Partner WebSDK"),n(" application in the catalog and click "),t("strong",null,"Add"),n("."),t("br"),t("img",{width:"440",src:Xn,height:"317",xmlns:"http://www.w3.org/1999/xhtml"})]),t("li",null,[n("In the "),t("strong",null,"Basic Configuration"),n(" section, change the "),t("strong",null,"Application name"),n(" to RAWeb (or another name of your choice).")]),t("li",null,[n("In the "),t("strong",null,"Basic Configuration"),n(" section, set "),t("strong",null,"User access"),n(" to "),t("strong",null,"Enable for all users"),n(" (or another option of your choice).")]),t("li",null,[n("IN the "),t("strong",null,"Universal Prompt"),n(" section, set "),t("strong",null,"Activate Universal Prompt"),n(" to "),t("strong",null,"Show new Universal Prompt"),n(". RAWeb has not been tested with the classic prompt.")]),t("li",null,[n("In the "),t("strong",null,"Settings"),n(" section, set "),t("strong",null,"Voice greeting"),n(" to "),t("em",null,"Sign in to RAWeb"),n(" (or another greeting of your choice). This greeting will be played when users choose to authenticate via phone call.")]),t("li",null,[n("Click "),t("strong",null,"Save"),n(" to create the application.")])],-1)),e[10]||(e[10]=A('<h2 id="configure-integration">Configure RAWeb to use Duo</h2><ol><li>From the application’s page in Duo’s Admin panel, locate the <strong>Client ID</strong>, <strong>Client secret</strong>, and <strong>API hostname</strong> values in the <strong>Details</strong> section. You will need these values to configure RAWeb.</li><li>In RAWeb’s web interface, navigate to the <strong>Policies</strong> page.</li><li>Open the <strong>Configure Duo Universal Prompt multi-factor authentication (MFA)</strong> policy dialog.</li><li>Set the policy state to <strong>Enabled</strong>.</li><li>In the <strong>Options » Applications</strong> section, click <strong>Add new</strong>.</li><li>Enter the following values: <ul><li><strong>Client ID</strong>: Enter the client ID obtained from Duo.</li><li><strong>Client secret</strong>: Enter the client secret obtained from Duo.</li><li><strong>API hostname</strong>: Enter the API hostname obtained from Duo.</li><li><strong>Domains</strong>: Enter a comma-separated list of domains (e.g., <code>INTERNAL,TESTBOX,example.org</code>) for which this Duo configuration should be used. Use <code>*</code> to apply the connection to all domains. The domains specified here should match the domain part of the username used to sign in (e.g., for the username <code>INTERNAL\\alice</code>, the domain is <code>INTERNAL</code>). If a domain has a known FQDN (e.g., <code>example.org</code>), use it instead of the NetBIOS format domain (e.g. <code>EXAMPLE</code>).</li></ul></li><li>Click OK to save the policy.</li><li>Sign out of RAWeb and sign back in to test the configuration. After entering your credentials, you should be prompted to complete the second factor authentication via Duo’s Universal Prompt.</li></ol><p>If you need different Duo configurations for different domains, repeat steps 5-7 to add additional connections with the appropriate domains assigned to each Duo client ID, client secret, and API hostname.</p>',3)),o(y,null,{default:s(()=>e[3]||(e[3]=[t("p",null,[n("Wildcard domains ("),t("code",null,"*"),n(") will match any domain not explicitly listed in other connections.")],-1)])),_:1}),e[11]||(e[11]=A('<h2 id="exclude-accounts">Exclude specific accounts from Duo MFA</h2><p>To exclude specific user accounts from being prompted for Duo MFA, you can add their usernames to the <strong>Excluded account usernames</strong> field in the Duo MFA policy dialog. Usernames should be specified in the format <code>DOMAIN\\username</code> or <code>domain.tld\\username</code>. For local accounts, use <code>.\\username</code> or <code>MACHINE_NAME\\username</code>, where <code>MACHINE_NAME</code> is the name of the computer.</p>',2)),o(y,{severity:"caution",title:"Caution"},{default:s(()=>e[4]||(e[4]=[n(" The username is case-sensitive and must match exactly the username used during sign-in. The domain part is case-insensitive. "),t("div",{style:{"margin-top":"4px"}},null,-1),n(" RAWeb will automatically translate the username to the correct case based on the user's actual account information when they sign in. However, when adding usernames to the exclusion list, ensure that the case matches exactly. ")])),_:1}),e[12]||(e[12]=A("<ol><li>In RAWeb’s web interface, navigate to the <strong>Policies</strong> page.</li><li>Open the <strong>Configure Duo Universal Prompt multi-factor authentication (MFA)</strong> policy dialog.</li><li>In the <strong>Options » Excluded accounts</strong> section, click <strong>Add new</strong>.</li><li>Enter the username of a account to exclude in the format described above. To exclude multiple accounts, add each username as a separate entry.</li><li>Click OK to save the policy.</li><li>Sign out of RAWeb and sign back in with an excluded account to verify that the Duo MFA prompt is not shown.</li></ol>",1))])}}},no=Object.freeze(Object.defineProperty({__proto__:null,default:to,nav_title:Zn,redirects:eo,title:Qn},Symbol.toStringTag,{value:"Module"})),oo={class:"markdown-body"},io="$t{{ policies.App.FavoritesEnabled.title }}",ro="Favorites",so=["policies/App.FavoritesEnabled"],ao={__name:"index",setup(l,{expose:r}){return r({frontmatter:{title:"$t{{ policies.App.FavoritesEnabled.title }}",nav_title:"Favorites",redirects:["policies/App.FavoritesEnabled"]}}),(d,e)=>{const i=p("PolicyDetails");return a(),c("div",oo,[o(i,{translationKeyPrefix:"policies.App.FavoritesEnabled",open:""})])}}},lo=Object.freeze(Object.defineProperty({__proto__:null,default:ao,nav_title:ro,redirects:so,title:io},Symbol.toStringTag,{value:"Module"})),co={class:"markdown-body"},ho="$t{{ policies.App.FlatModeEnabled.title }}",uo="Flatten folders",po=["policies/App.FlatModeEnabled"],mo={__name:"index",setup(l,{expose:r}){return r({frontmatter:{title:"$t{{ policies.App.FlatModeEnabled.title }}",nav_title:"Flatten folders",redirects:["policies/App.FlatModeEnabled"]}}),(d,e)=>{const i=p("PolicyDetails");return a(),c("div",co,[o(i,{translationKeyPrefix:"policies.App.FlatModeEnabled",open:""})])}}},fo=Object.freeze(Object.defineProperty({__proto__:null,default:mo,nav_title:uo,redirects:po,title:ho},Symbol.toStringTag,{value:"Module"})),Ao={class:"markdown-body"},go="$t{{ policies.RegistryApps.FullAddressOverride.title }}",Io="Override full address",To=["policies/RegistryApps.FullAddressOverride"],bo={__name:"index",setup(l,{expose:r}){return r({frontmatter:{title:"$t{{ policies.RegistryApps.FullAddressOverride.title }}",nav_title:"Override full address",redirects:["policies/RegistryApps.FullAddressOverride"]}}),(d,e)=>{const i=p("PolicyDetails");return a(),c("div",Ao,[o(i,{translationKeyPrefix:"policies.RegistryApps.FullAddressOverride",open:""})])}}},yo=Object.freeze(Object.defineProperty({__proto__:null,default:bo,nav_title:Io,redirects:To,title:go},Symbol.toStringTag,{value:"Module"})),Ro={class:"markdown-body"},Eo="$t{{ policies.App.HidePortsEnabled.title }}",wo="Hide ports",So=["policies/App.HidePortsEnabled"],Oo={__name:"index",setup(l,{expose:r}){return r({frontmatter:{title:"$t{{ policies.App.HidePortsEnabled.title }}",nav_title:"Hide ports",redirects:["policies/App.HidePortsEnabled"]}}),(d,e)=>{const i=p("PolicyDetails");return a(),c("div",Ro,[o(i,{translationKeyPrefix:"policies.App.HidePortsEnabled",open:""})])}}},No=Object.freeze(Object.defineProperty({__proto__:null,default:Oo,nav_title:wo,redirects:So,title:Eo},Symbol.toStringTag,{value:"Module"})),vo={class:"markdown-body"},Lo="$t{{ policies.App.IconBackgroundsEnabled.title }}",Co="Icon backgrounds",Do=["policies/App.IconBackgroundsEnabled"],Po={__name:"index",setup(l,{expose:r}){return r({frontmatter:{title:"$t{{ policies.App.IconBackgroundsEnabled.title }}",nav_title:"Icon backgrounds",redirects:["policies/App.IconBackgroundsEnabled"]}}),(d,e)=>{const i=p("PolicyDetails");return a(),c("div",vo,[o(i,{translationKeyPrefix:"policies.App.IconBackgroundsEnabled",open:""})])}}},Fo=Object.freeze(Object.defineProperty({__proto__:null,default:Po,nav_title:Co,redirects:Do,title:Lo},Symbol.toStringTag,{value:"Module"})),Wo={class:"markdown-body"},Ho="$t{{ policies.RegistryApps.AdditionalProperties.title }}",ko="Additional RemoteApp properties",Uo=["policies/RegistryApps.AdditionalProperties"],Mo={__name:"index",setup(l,{expose:r}){return r({frontmatter:{title:"$t{{ policies.RegistryApps.AdditionalProperties.title }}",nav_title:"Additional RemoteApp properties",redirects:["policies/RegistryApps.AdditionalProperties"]}}),(d,e)=>{const i=p("PolicyDetails");return a(),c("div",Wo,[o(i,{translationKeyPrefix:"policies.RegistryApps.AdditionalProperties",open:""})])}}},Go=Object.freeze(Object.defineProperty({__proto__:null,default:Mo,nav_title:ko,redirects:Uo,title:Ho},Symbol.toStringTag,{value:"Module"})),Bo={class:"markdown-body"},xo="$t{{ policies.LogFiles.DiscardAgeDays.title }}",Yo="Log file retention",Vo=["policies/LogFiles.DiscardAgeDays"],jo={__name:"index",setup(l,{expose:r}){return r({frontmatter:{title:"$t{{ policies.LogFiles.DiscardAgeDays.title }}",nav_title:"Log file retention",redirects:["policies/LogFiles.DiscardAgeDays"]}}),(d,e)=>{const i=p("PolicyDetails");return a(),c("div",Bo,[e[0]||(e[0]=t("p",null,[n("RAWeb’s server will generate log files for select components to help with troubleshooting issues. They are generated and stored in the "),t("code",null,"App_Data\\logs"),n(" folder. (For a standard installation, the full path is "),t("code",null,"C:\\inetpub\\RAWeb\\App_Data\\logs"),n(".)")],-1)),e[1]||(e[1]=t("p",null,"The number of log files can grow over time, so it is recommended to configure a retention policy to automatically delete old log files. If you do not configure a retention policy, RAWeb will discard log files older than 3 days.",-1)),o(i,{translationKeyPrefix:"policies.LogFiles.DiscardAgeDays"}),e[2]||(e[2]=t("p",null,[n("To completely disable log file generation, set this policy to "),t("strong",null,"Disabled"),n(".")],-1)),e[3]||(e[3]=t("p",null,[n("To retain log files for a specific number of days, set this policy to "),t("strong",null,"Enabled"),n(" and specify the desired number of days. For example, setting it to 7 days will retain log files for one week before they are automatically deleted. There is no limit on the number of days you may retain log files, but keep in mind that retaining log files for longer periods will consume more disk space.")],-1))])}}},_o=Object.freeze(Object.defineProperty({__proto__:null,default:jo,nav_title:Yo,redirects:Vo,title:xo},Symbol.toStringTag,{value:"Module"})),qo={class:"markdown-body"},Jo="$t{{ policies.Workspace.ShowMultiuserResourcesUserAndGroupNames.title }}",zo="User and group folders",Ko=["policies/Workspace.ShowMultiuserResourcesUserAndGroupNames"],Xo={__name:"index",setup(l,{expose:r}){return r({frontmatter:{title:"$t{{ policies.Workspace.ShowMultiuserResourcesUserAndGroupNames.title }}",nav_title:"User and group folders",redirects:["policies/Workspace.ShowMultiuserResourcesUserAndGroupNames"]}}),(d,e)=>{const i=p("PolicyDetails");return a(),c("div",qo,[o(i,{translationKeyPrefix:"policies.Workspace.ShowMultiuserResourcesUserAndGroupNames",open:""})])}}},$o=Object.freeze(Object.defineProperty({__proto__:null,default:Xo,nav_title:zo,redirects:Ko,title:Jo},Symbol.toStringTag,{value:"Module"})),Qo={class:"markdown-body"},Zo="$t{{ policies.App.SimpleModeEnabled.title }}",ei="Simple mode",ti=["policies/App.SimpleModeEnabled"],ni={__name:"index",setup(l,{expose:r}){return r({frontmatter:{title:"$t{{ policies.App.SimpleModeEnabled.title }}",nav_title:"Simple mode",redirects:["policies/App.SimpleModeEnabled"]}}),(d,e)=>{const i=p("PolicyDetails");return a(),c("div",Qo,[o(i,{translationKeyPrefix:"policies.App.SimpleModeEnabled",open:""})])}}},oi=Object.freeze(Object.defineProperty({__proto__:null,default:ni,nav_title:ei,redirects:ti,title:Zo},Symbol.toStringTag,{value:"Module"})),ii={class:"markdown-body"},ri="Configure the user cache",si="User cache",ai=["policies/UserCache.Enabled","policies/UserCache.StaleWhileRevalidate"],li={__name:"index",setup(l,{expose:r}){return r({frontmatter:{title:"Configure the user cache",nav_title:"User cache",redirects:["policies/UserCache.Enabled","policies/UserCache.StaleWhileRevalidate"]}}),(d,e)=>{const i=p("PolicyDetails");return a(),c("div",ii,[e[0]||(e[0]=t("h2",null,"Enable the user cache",-1)),e[1]||(e[1]=t("p",null,"The user cache stores details about a user every time they sign in, and RAWeb will fall back to the details in the user cache if the domain controller cannot be reached. If RAWeb is unable to load group memberships from the domain, the group membership cached in the user cache is used instead.",-1)),e[2]||(e[2]=t("p",null,[n("For domain-joined Windows machines, when the user cache is enabled and the domain controller cannot be accessed, the authentication mechanism will populate RAWeb’s user cache with the "),t("a",{href:"https://learn.microsoft.com/en-us/troubleshoot/windows-server/user-profiles-and-logon/cached-domain-logon-information",target:"_blank",rel:"noopener noreferrer"},"cached domain logon information"),n(" stored by Windows, if available. By default, Windows caches the credentials of the last 10 users who have logged on to the machine.")],-1)),e[3]||(e[3]=t("p",null,"RAWeb’s mechanism for verifying user and group information may be unable to access user information if the machine with RAWeb is in a domain environment with one-way trust relationships. In such environments, if the domain controller for the user’s domain cannot be reached, RAWeb will only be able to populate the user cache upon initial logon.",-1)),e[4]||(e[4]=t("p",null,[n("When the "),t("code",null,"UserCache.Enabled"),n(" appSetting (policy) is enabled, a SQLite database is created in the App_Data folder that contains username, full name, domain, user sid, and group names and sids.")],-1)),o(i,{translationKeyPrefix:"policies.UserCache.Enabled"}),e[5]||(e[5]=t("h2",null,"Leverge the user cache for faster load times",-1)),e[6]||(e[6]=t("p",null,"The user cache also improves the time it takes for RAWeb to load user details. When the user cache is enabled, RAWeb will use the cached user details while it revalidates the details in the background. This can significantly improve performance in environments with a large number of groups or slow domain controllers.",-1)),e[7]||(e[7]=t("p",null,[n("By default, RAWeb will use the cached user details for up to 1 minute before requiring revalidation. This duration can be adjusted using the "),t("code",null,"UserCache.StaleWhileRevalidate"),n(" policy. "),t("br"),n(" If RAWeb is unable to revalidate the user details (for example, if the domain controller is unreachable), it will continue to use the cached details until revalidation is successful.")],-1)),o(i,{translationKeyPrefix:"policies.UserCache.StaleWhileRevalidate"})])}}},di=Object.freeze(Object.defineProperty({__proto__:null,default:li,nav_title:si,redirects:ai,title:ri},Symbol.toStringTag,{value:"Module"})),ci="/deploy-preview/pr-215/lib/assets/webGuacd-method-6fmSkezV.webp",hi={class:"markdown-body"},ui="$t{{ policies.GuacdWebClient.Address.title }}",pi="Web client connection method",mi=["policies/GuacdWebClient.Address"],fi={__name:"index",setup(l,{expose:r}){return r({frontmatter:{title:"$t{{ policies.GuacdWebClient.Address.title }}",nav_title:"Web client connection method",redirects:["policies/GuacdWebClient.Address"]}}),(d,e)=>{const i=p("RouterLink"),h=p("PolicyDetails");return a(),c("div",hi,[e[3]||(e[3]=t("p",null,"The web client allows users to access their desktops and applications through a web browser. This policy controls whether RAWeb will use a Guacamole daemon (guacd) as a remote desktop proxy to provide web client access.",-1)),e[4]||(e[4]=t("img",{width:"400",src:ci,height:"210",xmlns:"http://www.w3.org/1999/xhtml"},null,-1)),e[5]||(e[5]=t("p",null,"When enabled and properly configured, users will see a “Connect in browser” button in the connection dialog, allowing them to use the remote desktop connection proxy.",-1)),e[6]||(e[6]=t("p",null,"When disabled, the “Connect in browser” option will not be shown, preventing users from accessing the web client.",-1)),e[7]||(e[7]=t("p",null,"If no other connection methods are enabled, users will be unable to connect to resources via the web app. Instead, they will see this following dialog:",-1)),e[8]||(e[8]=t("img",{width:"400",src:ne,height:"185",xmlns:"http://www.w3.org/1999/xhtml"},null,-1)),e[9]||(e[9]=t("h2",null,"Prerequisites",-1)),e[10]||(e[10]=t("p",null,[n("The web client requires the RAWeb server to have access to a "),t("a",{href:"https://guacamole.apache.org/",target:"_blank",rel:"noopener noreferrer"},"Guacamole"),n(" daemon ("),t("a",{href:"https://hub.docker.com/r/guacamole/guacd/",target:"_blank",rel:"noopener noreferrer"},"guacd"),n("). There are two options for providing guacd to RAWeb:")],-1)),t("ul",null,[t("li",null,[o(i,{to:"/docs/web-client/prerequisites#opt1"},{default:s(()=>e[0]||(e[0]=[n("Option 1. Allow RAWeb to start its own guacd instance")])),_:1}),e[1]||(e[1]=n(" (recommended for most environments)"))]),t("li",null,[o(i,{to:"/docs/web-client/prerequisites#opt2"},{default:s(()=>e[2]||(e[2]=[n("Option 2. Provide an address to an existing guacd server")])),_:1})])]),e[11]||(e[11]=t("h2",null,"Configuration",-1)),o(h,{translationKeyPrefix:"policies.GuacdWebClient.Address",open:""})])}}},Ai=Object.freeze(Object.defineProperty({__proto__:null,default:fi,nav_title:pi,redirects:mi,title:ui},Symbol.toStringTag,{value:"Module"})),gi="/deploy-preview/pr-215/lib/assets/file-type-associations-button-CwZZsihZ.webp",Ii="/deploy-preview/pr-215/lib/assets/file-type-associations-dialog-CyUUQq9d.webp",Ti="/deploy-preview/pr-215/lib/assets/add-file-types-to-rdp-files-Cb1-AYUE.png",bi={class:"markdown-body"},yi="File type associations for RAWeb webfeed clients",Ri="File type associations",Ei={__name:"index",setup(l,{expose:r}){return r({frontmatter:{title:"File type associations for RAWeb webfeed clients",nav_title:"File type associations"}}),(d,e)=>{const i=p("CodeBlock");return a(),c("div",bi,[e[4]||(e[4]=A('<p>RAWeb can associate file types on clients to RemoteApps. For example, you can configure RAWeb to open a Microsoft Word RemoteApp when the client opens a <code>.docx</code> file.</p><h2>Requirements</h2><p>The following requirements must be met for file type associations to work:</p><ul><li>The client must be connected to RAWeb’s webfeed.</li><li>The client webfeed URL must be configured via Group Policy (or Local Policy).</li><li>RDP files need to have file types listed within.</li><li>Each RemoteApp on must be configured to allow command line parameters. This is the true for all RemoteApps created via RAWeb.</li></ul><p>File type associations <strong>will not work</strong> if you configure the webfeed URL via the RemoteApp and Desktop Connections control panel. You <strong>must</strong> configure it via policy. If you cannot configure the policy, you may be able to use <a href="https://www.cyberdrain.com/adding-remote-app-file-associations-via-powershell/" target="_blank" rel="noopener noreferrer">Kelbin Tegelaar’s workaround</a>.</p><h2 id="managed-resource-file-type-associations">Add file type associations to managed resources</h2>',6)),e[5]||(e[5]=t("ol",null,[t("li",null,[n("Navigate to "),t("strong",null,"Policies"),n(".")]),t("li",null,[n("At the top of the "),t("strong",null,"Policies"),n(" page, click "),t("strong",null,"Manage resources"),n(" to open the RemoteApps and desktops manager dialog.")]),t("li",null,"Click the RemoteApp for which you want to configure file type associations."),t("li",null,[n("In the "),t("strong",null,"Advanced"),n(" group, click the "),t("strong",null,"Configure file type associations"),n(" button."),t("br"),t("img",{width:"500",alt:"",src:gi,height:"650",xmlns:"http://www.w3.org/1999/xhtml"})]),t("li",null,[n("You will see a dialog where you can add, remove, and edit file type associations."),t("br"),n(" Additionally, for RemoteApps that belong to the terminal server that hosts RAWeb, you can select specific icons for each file type association. "),t("br"),n(" Click "),t("strong",null,"Add association"),n(" to add a new file type association. All file type associations must start with a dot and must not include an asterisk."),t("br"),t("img",{width:"430",alt:"",src:Ii,height:"558",xmlns:"http://www.w3.org/1999/xhtml"})]),t("li",null,[n("Click "),t("strong",null,"OK"),n(" to confirm the specified file type associations.")]),t("li",null,[n("Click "),t("strong",null,"OK"),n(" to save the RemoteApp details.")])],-1)),e[6]||(e[6]=t("h2",null,"Manually add file type associations to RDP files",-1)),t("ol",null,[e[2]||(e[2]=t("li",null,[t("p",null,"Open the RDP file in a text editor.")],-1)),t("li",null,[e[0]||(e[0]=t("p",null,"Add a new line:",-1)),o(i,{code:`remoteapplicationfileextensions:s:[file extensions separated by commas]
`}),e[1]||(e[1]=t("p",null,[t("img",{src:Ti,alt:""})],-1))])]),e[7]||(e[7]=A("<h3>Add icons for each file type</h3><p>To add icons for file type associations, specify an <code>.ico</code> or <code>.png</code> file with the same name as the <code>.rdp</code> file followed by a dot (.) and then the file extension.</p><p>For example, to set an icon for all <code>.docx</code> files which will open via <code>Word.rdp</code>, you would need an icon file named: <code>Word.docx.ico</code>.</p>",3)),o(g(L),null,{default:s(()=>e[3]||(e[3]=[n(" RAWeb will not include an icon in the workspace/webfeed unless the width and height are the same. ")])),_:1})])}}},wi=Object.freeze(Object.defineProperty({__proto__:null,default:Ei,nav_title:Ri,title:yi},Symbol.toStringTag,{value:"Module"})),ce="/deploy-preview/pr-215/lib/assets/apps%20manager-D2t3AexZ.webp",Si="/deploy-preview/pr-215/lib/assets/add%20new%20file%20resource-atqrccf4.webp",Oi="data:image/webp;base64,UklGRtQLAABXRUJQVlA4WAoAAAAIAAAAKQMAFgEAVlA4INYKAABQbACdASoqAxcBPzGYxVgnJSsmIhLIkWYmCelu4XVHKion/OH9r7ev9L4g16HkbHHyxHMLguyC1UFZU7H/qeTQeomkjHiNJGPEaSMeI0kY8RpIx4jSRjxGkjHiNJGPEaSMeI0kY8RpIx4jSRjxGkizYkPWDCsToo6atNesjVIGkLm+8jgtupJDjnUNTvqg4RdSSHHOoanfVBwi6kg2vURppRlJ1BAfbW3PAaHC1jHiNJGPEaSMeI0kY8RpIx4jij4UH/lYES1jHiNJGPEaSMeI0kY8RpIzTfmRtvlN14fUrYTCWFFDC+VOMXIARHulvU1J0UDKFinaryt2YSCVF83U6CxdtwCLN4fpQxa4o/mu6KY4533x5boaSMeI0kY8RpMLfI6WgDZjfrsQAHtBq62aUIFBkjIYPWrG28PdF9gqJ2iq+rW21xWBS3Zn2SKnfBP4L51OCLl3JhPw52/lAuP9K3x5boaSMeI0mFvkc+WEI/pC2dJWvq5Z5xpNaF7dOg+CW8vu7tDAr+FnZb2k3luhpIx4jSRjxGkkN8ezEMShHumHMKtd7YWBG7DsYrcc+A9/Rkve2NAv84PNW9rGhpIx4jSRjxGkjHiNJIb49lYPNSKIhf3O+RLVVxei11AeGqLSIRRPR6qffXP704NrEFbU4/0rfHluhpIx4jSRjxM2CL0aey/e5BYb20c11fc5bpPBr68QxhdDGveewRLWMeI0kY8RpIzTfmRbak3FnwpGaPMedrvhSAYORMlVpFVbgkOGb2ZffmqB6zDmGuHovmOjNyQdjHiNJGPEaSMeI0mFvkcomVVLQ/OwqLauMIAM20N0sBRYBI3qcZuekHYwhcloBQeRaVoLg/CADltpjoVDtjMLbLp47fk9J+NqNz1qe7gr4fk0FxFWNhYZku+jz5miRqZvIiKoHU1UonlChRNJGPEaSMeI0kZpvzIdylcskQe5BYb20PVI9MyilWb/SQ86gl+5nqEsgkXLXZAlFb48t0NJGPEaSMeJmwRkwzygxx87+C4/0rfHluhpIx4jSRjxM2B9QNNMUuUGWz8Zwi6kkOOdQ1O+qDhF1JIcc6hqd9S65WrIN8gPZJDj1OUDKF8O1O+qDhF1JIcc6hqd9UHCLqQSFE0lZYmkjHiNJGPEaSMeI0kY8RpIx4jLAAD+/s8AAAAABkski/xi1LQO8ssAKwDBIXY5Z/30tuOH0LFBLHrWPvJ5D188RxKWeVvY9t8W1lHcjuoReMiDftyP0FHHvLfEmxlTvSws6xIdVGL8HlN46nINf2poMerpICkB30K9OLn3HOkfoc1eT/mAiRqsBzAdv5nGaGUSfyWzcp1UnwAGQsspuUEWR9fUVyq0JFWHgWQhZhyruNEGungHy5nkH+sDp7rEwWsZEvsrE9kgAAAABIfYoqXvfXHBIxs7dIYojQTHmlTbZVbVhOsO4nd6I1zd6uLVI6UKozUndEI4kiqA2WSlx3lyxtUApHNmyIKS6pGtH1gL9r5yHuN34z9w2fxds5ljPFfocthn9SOvnIlpZY+z8cVj+ofizXW/VLiDuv1i1c078G7QtT4npU0x6Tzyt9voSpAbSH/nfZmeht741yGr1naU5j2EjGL7aVS5H5bWyBSk4mo2fhSjFOVwVY6yw7Kb6CAxq9xB6Y8Ql5gXAukipLp2YdR4bsv7IFM4J7EJdKAl7Op/pn3OxAaK3GEX9sVPTDU0k8rZ+8VUnULi9k1syT1VRNrdLvpkwvmgYPigfCNejdCmETBD8olTSmeaMj8yHD661OaD5TRv+wUTh8BAcLWnmDEeG/WjNQcWc635MwdA/ccqcicb7yC+clQShEa//0hKkvxSymHgEFrvwqZ+PUIDj1RMAvVwf+nox9XBGJEKP1EiXQQ2HGnch+T0LpQeFPY9eeAO9gTh7rq7NYeowVVc622J0dGY4c+glwCkvKkHN5hxBxjHa7ADHPw4cQAaOB7yuhw22EuDx+MzFmpdDg8GJTZGiwtJVcu6r5uFoOjHje0oVvUnM+Dq2BeoT0YlcV/6hqH/syKOLk4NI+mOml9dTcFKDTd4AWQjfqKc0uLXm9iqglLtU7r7XK39c+nkH1tRoa+gHjcox8FhQ8FRddz+9Qe16zfok1047Ebhx8dNS1fshBeyMaLBEwAUHLWY7wATyZ5Cw3Ro4KS5yeuGjggF5DB9PLS+gykNuR/0NZYGAARvRPI+z2t4t/WM5PL2EOBxJ5SjxdHwjmyuqO7lFG9nh69FvwnjBA95NylEUHpQuv1k2mE0C7rYRyhXV0KgSAmKR1HKpXgm8SNHst2qC+N3BfFZeibi/8jDEzNwQG1hJ9ffa4T3EgosnxcRMkpeRsYXeMH47rpCYDyld3quxAMK21yWueWXAntmdBAbzAB6WMRnBJDokuT28W5G8kYb5/E32rJowP4S+3qHAzO6DwtS2rjvj+00xCJI4hYS1akyVJLGDL7t8A1JeqN7Cxc9V7TpQ8jHDKBpayA1gjs6wqcpPVyYCRDtnmYYQhSvLWGeSYCFt3e8MrSvRzQ6oqDqg6oOo/5JazocellnB3i2Qagu/cz+zCGvmoVjLmb9B94nLNelbEGMYAAYJmIQ0SxkRtxxoEt+1YwW0NXq5byYzmjzOt5U7/zXFTt0FBi2eDw2ESUxJAY1Z1pn91bgSikN+dvTUWrHMDBgHN4uGveBjl0bbqmfc1xDNUqxiFDFsx4RbUQgMmr5w9rWkCpJFt0owiF3GVuLOpM3jqN8odPyK8X0QrgGMvGc1toHdB2gZkccniS/6JOQurcge4bmYgNyERBvkrRgxG6e9tX2CeraMPIHMHbFKX9Gs4/VBUKLK+SEkjG1QjM5i92cWYhuWLvfGathkqELXL6+FA1ocC79k9OwX50BOguZi1z42VyHfX5QnyCG0+p9yPTsQlEp4AeAFryU0VxWda+HDQ90uSXMJyEreW0bENniLfK1M6s9cApokQctq0S3xmQ+FX4KiL8/SYyrzXk7oEJKwUt5ktaTOyL+MYTecSLu3DXzhRnP5fR+KqwEpHqAw9CEXs5dghlykEUrzDUOMelZ8KB6Vhirk2SHEaoS37/V/4vR/0SgKx4nm3PSuSBkzXX31CTIZn/n6Nr+MJpKz0qxzToitEpCQq5/ssZ8qVfsxpjsgCpVEaM3tXNZG77QcEWnUGcrp2iiOhOtg6b1vy9dUxHTVT4r5W9KqwPMoRl52611c/mBsE/q3h7B6Nc9MGb7dsx+qcw1AbK4+aBybW4ESFJ6yQVVJ9ltJHSrSrCYNmBZMCS0gksiOCw+H7Un6WBi1PRllcDkzKKepVmn+bT/5H74N7fC/XjcDDoXco4BSC2D9zXB5rbZDN4Musiax+FORXP1ov4azWx2hEmGxmtygLKNCg8PoHO+BWKHl1Zf3BLxjM/vRLuZcm0+XJ0wEIKuTT12/D6ZIAisBmVm1Tu/D3JawEh+l6Z1WeKWYtVMach49FtilFhEYA8kkslY3hXD2uTx2rIkk3OFKIK0fyC4sOmcoCoPsTAwBAluOC2gYh4KhpzXIheudh0n/1VrBjqDvdlq+ZTh33BZ0NZZAv7J/AhqcAE+0BXuew0a8zFbFPC5keV4Yjf1yhq39oxJlYe14n6CvNtoggDQptkKXkLQABPyIaEwr8M/m4TIeJkJEAK2N0RcMyt2uik2zQAPQxkWQAAAAAAAAEVYSUbYAAAASUkqAAgAAAAGABIBAwABAAAAAQAAABoBBQABAAAAVgAAABsBBQABAAAAXgAAACgBAwABAAAAAgAAADEBAgARAAAAZgAAAGmHBAABAAAAeAAAAAAAAABgAAAAAQAAAGAAAAABAAAAUGFpbnQuTkVUIDUuMS4xMQAABQAAkAcABAAAADAyMzABoAMAAQAAAAEAAAACoAQAAQAAACoDAAADoAQAAQAAABcBAAAFoAQAAQAAALoAAAAAAAAAAgABAAIABAAAAFI5OAACAAcABAAAADAxMDAAAAAA",Ni="data:image/webp;base64,UklGRg4NAABXRUJQVlA4WAoAAAAIAAAAKQMAFgEAVlA4IBAMAABwcQCdASoqAxcBPzGYwVknJSmmINDI2TYmCelu4XYBGS1zz8bH/M7trnlNOP3arJiPK/+D7df9r01ob9AFH0y9YATvPeg8rD7f3xerWqgHX/9bydDXP9K3x5Xm6SMeI0kY8RpIx4jSRjxGkjHiNJGPEaSMeI0kY8RpIx4jSRjxGkjHiIpetKYCZS6Iajqs9OelO8yypQkiIMoGT72ZZ75UHCLqSQ451DU76oOEXUkG16/4A4NGO1ogJJu7hcAU1AeWsY8RpIx4jSRjxGkjHiNJIiXpy9c4/0rfHlebpIx4jSRjxGkjHiNJisrvSgeBmUyDwKptH0xVF2V07RYNwMSuwuWQ0KLjpF71SO59q/fqKGI3XgJawE72qTFz9qn8qxMWXLkL1jLSpRaxjxGkjHiNJGPEaSREvTRu8nwx/RUlDekP3ano53/MEdjQfk4gXUpfHk9N39NOF6QgtFADCP26DEbbiZlO/EPzGYjBSfJT32Tf0SsUs5IuP9K3x5Xm6SMeJpBj9DBHovC1ABz0DslkrkwPLja++tCOOQ7NHhd88MJyqbsuXr1pTA3Y3x5Xm6SMeI0kY8RpMVld8Dq9ErDVDaoJwUFWVhVX/k1iXRKn2cGOIIc974Wmemy3CtRCMj7MhK8Q+VPWQNZp5vK83SRjxGkjHiNJGa9Um5C29xjhwSnKEJ8svcsPkc8eempHePglCxZiTKC1IL/SFJsxpYKYfDBurM5awqEk8G82pGnn5CsLj/St8eV5ukjHiNJGa9Unmxezg/250eKQOjGJqj0J6aiogi+XpezJmJpIx4jSRjxGkjHiaQY/6PAZ0UPJPti2bErlczAeyyFURfrIz7AqrheKuY7LyqxZYlmFTp1M4agQSMeI0kY8RpIx4jTp0PllGO5oGacJVJigmAzytQgqXSgovJf2XdNrPCOLCoemg+rGwmEI8mriV4rx4Q+s96rt4efozDxXzoLTXur4d7FsXBaAJDph2CCpCJFzIAujFrQ8zvMQ4D5nqdDy1jHiNJGPEaSMgFCSEQRB+imPuGtxVW8y6t3L1GRh0xY7jd5Xm6SMeI0kY8Rp06Hy7l6OgFyVbKBk6eWsY8RpIx4jSRjxHGouoIhD4XPvZlnvlQcIupJDjnUNTvqg4RdSSHHOoams+EAT/A8tYx4jSRjxGkjHiNJGPEaSMeI0kY8RpIx4jSRjxGkjHiNJGPEaSMeI0kY8Ro2AAP7/IOWgCkAAYQBKJ7aPzQNJ4yVxkAMETJyU5wx0qbsAVFOTRdfsj5EKTS5MRA5HNFisNYHFFkMKyNx4lR+QdH7b55NKJ6IT9hThBkmfS+Oygg+MJ+SopeCzsqPP26g+bLaO1tmWOI80slS2UbOgktKwqxjS155BzWwl9v5+wM3DUxgVvEjph3c/uQBslUreM5HcHNG4iFEPeqYocrQhLGn+uLsBpyK+kA6HNCq+hcIi5qpx64QPMSJiv6SpAxgAA0NsAIb7B2ACezHr1LZOaUqGko+SheHIL4LN5qui2A41hnMzyEe5i4+IBMRMMHMBhbBTvwZyXClTqQWdmTmA1iNycNPdyjqPwa5BR1l46F6opOHQAq20QemYjC1FdtGe6Dopfi6ktXWZrds/OdylozWV/C3qB5Z+vLJoFRMQFYvxUZ30LgiNv7+N+1z4xvQb/z+NxVfJ1hZG/lFDdTxqPSzfqqW7xpF19l1be2nsq3HgyP8WrGrSDfyoAYJrwA5Az5zUEqPfimPzKVZhFkG9U2bmKrz5ty0pAob89w8pMKLduLR4jtWiPIw5/YSJTABUYOt7Z1wPDn3pnPGO3SPhYzW5nnUKwwYwvlwAkrg0CP2En8u7J14kyF4JpIa6lqx/u6ziVTVyfYEAuzl02guDzK9CBuDxlry+9LhcBmIiqtXDyFsAKfulrejECNTJn8gc0EVzgzo7GE8014y160GyYh9aWPd7sbRJvNsblE0fRiJbmXdqBNWYQRmi7t1AiexQVuBrSoDnaSvpzT9m8Ub/HMrfCrdrkiEPF8vrOZZId567pXZI1kSoG3IBipD1cfS8z6QfxKAfcDSVZIsZvlVvpIUxUqOt+ccRvL9aN9H142yOKBK/JqEbgWPlVQkib0N5b5UvC0H7zx49PH4ptwLEbK9vdoVLKVDWap/WOZMgky3Ah/jBRwjwm4LPa70K5Cr/1cbvu93jilTHJ73jMXj/xqn3Uwjp4jehOuFl+EuwiumuL9S1cZLzhs6HyBFEOXlp9E2ehm6AAQEfwBvw7IgNJ8X8x9+hAWba8ZIz9pTcI16rBo3DGmK3Qrrwolr7kVnIwe3bx0WlcdnRbtE0gSCRUw65p3L95xhI1GfZIKt89tHPhJ3bYEAku2+x/TQqTG8I5K4zvvtJ3go/v/AlS1UALrRbAEi4RhoptyaPw36XwFjjrfusHPLm6WaN9qJ+sNr4npAKBuxd2gpeSkoU3yUgRxkBkhMwPRXL/0u+zkGTWqEDM8xWvhMZSkAZpzfOlY5JD1/5lXnyT2yNiRVIwAkO3eaGu7t2rnCaLl0Re4aSgSsErJE8YJoW2GtZjJASel03eAl02MHbuSQEpgvW/cW5G63w6wZmITp+zpOl5590iCR03uw3DEFXr2V01sgHdSkE97Xkp7N0f924pUxX/t/1WmXvH0/pg1o6zDdx8tVqyKEYFDS20MGQD/A3GX0Cj21IOEXCYKi1QUzahGIUhufzXD5USShaEai1bT5kPjm4cEs4qWoIs9zFkhVdk9KOiF8D3/dDWj4IPG9fhpxuhH/aOwgr3xAKRCp+i0pPpOY2IoZEEifpDzzNPjhSQHwL1pMzMY1qNi3etxVO3q40iIykXWJk3tpUv0BAGRVVFnCgVdaydZnyvypXQLz7V+KriggzisR96+1qfsAIN2qh1LaQOkhNfzKuHs1/4ItSgd+Jxh0YZ+1yr3/RYXR5ioXaq7Qr+mh54EuoeGtDpOO9pCFhJTDs/Po4Jz4aIrXrL4MwXbk/6qJmjVuPZlMcIpwSyAFx6fV0OAZ+tUd5xIIy3HpUqdW89WDDlcKozvoUdy/o86YEiw6ieBNxIAPwCO+wyJ7g484/oH1ernsyjwHP077KfidF0bCaYTIez/+nzJ878js/3nWDtD41kMyrb9DKnAShhyReIFgbCWcT88dN844kjOtRMxWcculqwZclpvtAMes2KwVQrAG5dkUi2r2p37+n9Jv84BCo9MpWMRVESL0fu0fzkrg1hkoV8dnf/nOgmDtFli4nFn87bOaXOeRgBkRK02C94JpgbZyZt4rg7aAezGe2opyGqZw2ob+cOxFU21BZr7tcoGdo+/jjvKgLrGVtleiGb5UoX3j3oBijX5X234KmRCE71ftxtj3Pdij7X7eSPB6Fvtb8FQSGZ+iXNv6ib0E+tACCQ8hOOvC7wTz/5TnsNteiWiIYOkolsEyyLuNPrbFgOM9LMmeBC+VIbQmjTh1/cXbEYKk8Vd1eGiHsLN9ojLetJcPkzb8QUCKCRU9ek9gNQquj8pI6GVFL2BrCjxDucyauUsagpVyk/yu5qEaOj/9cUhaE/DhHYE7LI5HHJ/jZ6YtkzZuZ+qhaHVxPUtvj1z2EuTHRmajrVrjcRLDz+g0T2Kow2aBFXZhajJC4KoV6h4DAFNPPeo5ka+mJa9TmGrIQqPzRR4K3+hvaNhnnuAeiB7YIvF1DI52vubG/xn6QDO4uX86kT4sQu5nYUrJ/Q4mdyBXDpGdD3c7tIIC/IZTnNOr9Wm3rRSSXulI1UcR8DEYSTGOtw6Kov40ndSJO3izgLhh5JTde2DTJBzLdwtCmCRL1XgElEPrwU2X0I/BDBvjbe16h0Cr9A3E58EAbJ6FP9qHTKD0eim8ckT9O6G/YI+guNLPET0sRLp75Dpn/OczGiHcOKe6AjdUMRX8+xz5oIPmMfZwhnpqKIP5cytmDQhSnhyGiX6lL0HY7qAT2xFP+kkrv+rRzMo9MPeqxoY6BmfpfgvpbnMgR8xdf5p8M7IayyMTLlmVdWtllCJol3Fmx0xp5TA/VJPimnlij3poHpPT1kk5Td6Adt1SggxCTDfXIxK5tOB3AAAAAGByVyq5es5+FeApACSMZGBr9CmwAAAAAAAAARVhJRtgAAABJSSoACAAAAAYAEgEDAAEAAAABAAAAGgEFAAEAAABWAAAAGwEFAAEAAABeAAAAKAEDAAEAAAACAAAAMQECABEAAABmAAAAaYcEAAEAAAB4AAAAAAAAAGAAAAABAAAAYAAAAAEAAABQYWludC5ORVQgNS4xLjExAAAFAACQBwAEAAAAMDIzMAGgAwABAAAAAQAAAAKgBAABAAAAKgMAAAOgBAABAAAAFwEAAAWgBAABAAAAugAAAAAAAAACAAEAAgAEAAAAUjk4AAIABwAEAAAAMDEwMAAAAAA=",vi="/deploy-preview/pr-215/lib/assets/file-managed-resource--select-wallpaper-button-DRyTLr_K.webp",Li="/deploy-preview/pr-215/lib/assets/file-managed-resource--wallpaper-preview-ULrP1o2-.webp",he="/deploy-preview/pr-215/lib/assets/rdp-file-properties-editor-Cq2h0VZS.webp",ue="/deploy-preview/pr-215/lib/assets/delete-remoteapp-danger-CW9LqPoo.webp",Ci="/deploy-preview/pr-215/lib/assets/app%20discovery-DhYlZYS7.webp",Di="/deploy-preview/pr-215/lib/assets/add%20new%20remoteapp-DiRTv0vz.webp",Pi="/deploy-preview/pr-215/lib/assets/select-icon-button-DghRuThZ.webp",Fi="/deploy-preview/pr-215/lib/assets/select-icon-dialog-u2YybjXh.webp",Wi="/deploy-preview/pr-215/lib/assets/97a0db8c-768d-4f8c-89c6-5f597d1276ea-CXPnJxW0.png",Hi="/deploy-preview/pr-215/lib/assets/89e0db48-c585-4b08-8cd1-ab18fe0343f1-1Ao7T4vJ.png",ki="/deploy-preview/pr-215/lib/assets/apps-manager--system-desktop-focus-DlJz6BMe.webp",Ui="/deploy-preview/pr-215/lib/assets/system-desktop-properties-ChdQ-7tt.webp",Mi="/deploy-preview/pr-215/lib/assets/rdp-file-properties-editor--system-desktop-C5zrQqK0.webp",Gi="/deploy-preview/pr-215/lib/assets/28276875-8592-48f5-8db6-975d23136cff-i0taoQcn.png",Bi={class:"markdown-body"},xi="Publishing RemoteApps and Desktops",Yi="Publish RemoteApps and Desktops",Vi={__name:"index",setup(l,{expose:r}){return r({frontmatter:{title:"Publishing RemoteApps and Desktops",nav_title:"Publish RemoteApps and Desktops"}}),(d,e)=>{const i=p("InfoBar"),h=p("RouterLink");return a(),c("div",Bi,[e[93]||(e[93]=A('<p>By default, RAWeb will install to <strong>C:\\inetpub\\RAWeb</strong>. Parts of this guide assume that RAWeb is installed to the default location.</p><p>RAWeb can publish RDP files from any device. RAWeb can also publish RemoteApps specified in the registry.</p><p>Jump to a section:</p><ul><li><a href="docs/publish-resources/#managed-file-resources">Managed/uploaded RDP files</a></li><li><a href="docs/publish-resources/#managed-registry-resources">Registry RemoteApps and desktop</a></li><li><a href="docs/publish-resources/#remoteapp-tool">Registry RemoteApps via RemoteApp Tool (deprecated)</a></li><li><a href="docs/publish-resources/#host-system-desktop">Host system desktop</a></li><li><a href="docs/publish-resources/#standard-rdp-files">Standard RDP files</a></li></ul><h2 id="managed-file-resources">Managed/uploaded RDP files (managed file resources)</h2><p>RAWeb can publish any uploaded RDP file. The RDP file can point to any terminal server. These RemoteApps and desktops are called managed file resources and are stored in <code>C:\\inetpub\\RAWeb\\App_Data\\managed_resources</code>.</p><p>All uploaded RDP files must contain at least the <code>full address:s:</code> property.</p><p>An RDP file will be treated as a RemoteApp if it contains the <code>remoteapplicationmode:i:1</code> property. Otherwise, it will be treated as a desktop. RemoteApps must at least contain the <code>remoteapplicationprogram:s:</code> property.</p>',8)),o(i,{severity:"attention",title:"Secure context required"},{default:s(()=>e[0]||(e[0]=[n(" The resources manager requires a secure context (HTTPS). Make sure you access RAWeb's web interface via HTTPS in order to upload, edit, or delete managed file resources. "),t("br",null,null,-1),t("br",null,null,-1),n(" If you cannot access RAWeb via HTTPS, you may access RAWeb from "),t("code",null,"localhost",-1),n(" (http://localhost/RAWeb) via any browser based on Chromium or Firefox on the host server – they treat localhost as a secure context. ")])),_:1}),e[94]||(e[94]=t("p",null,"To upload an RDP file, sign in to RAWeb’s web interface with an administrator account and follow these steps:",-1)),e[95]||(e[95]=t("ol",null,[t("li",null,[n("Navigate to "),t("strong",null,"Policies"),n(".")]),t("li",null,[n("At the top of the "),t("strong",null,"Policies"),n(" page, click "),t("strong",null,"Manage resources"),n(" to open the RemoteApps and desktops manager dialog. "),t("br"),n(" You will see a list of resources currently managed by RAWeb. In addition to uploaded RDP files, this interface shows resources specified in the registry of the RAWeb host server. Uploaded managed file resources are denoted by a superscript lowercase greek letter "),t("em",null,"phi"),n(" (φ). "),t("br"),t("img",{width:"700",alt:"",src:ce,height:"649",xmlns:"http://www.w3.org/1999/xhtml"})]),t("li",null,[n("Click the dropdown arrow next to the "),t("strong",null,"Add new RemoteApp"),n(" button at the top left of the dialog. Select "),t("strong",null,"Add from RDP file"),n(" to open the RDP file upload dialog.")]),t("li",null,[n("Select an RDP file from your computer. The RDP file must contain at least the following properties: "),t("ul",null,[t("li",null,[t("code",null,"full address:s:")])])]),t("li",null,[n("Once RAWeb finishes processing the selected RDP file, you will see an "),t("strong",null,"Add new RemoteApp"),n(" or "),t("strong",null,"Add new Desktop"),n(" dialog that is populated with details from the RDP file."),t("br"),t("img",{width:"500",alt:"",src:Si,height:"650",xmlns:"http://www.w3.org/1999/xhtml"})]),t("li",null,[n("Configure the properties as desired. Make sure that "),t("strong",null,"Show in web interface and workspace feeds"),n(" is set to "),t("strong",null,"Yes"),n(". Click "),t("strong",null,"OK"),n(" to finish adding the resource.")])],-1)),e[96]||(e[96]=t("h3",null,"Change a RemoteApp’s icon",-1)),e[97]||(e[97]=t("p",null,"To change the icon for a managed file RemoteApp, you can upload any icon file that is supported by your browser. It will convert the icon to PNG format and store it in RAWeb’s managed resources folder. You will see a preview of the uploaded icon in the RemoteApp properties dialog before saving the changes.",-1)),e[98]||(e[98]=t("p",null,"Light mode and dark mode icons can be specified separately. If only a light mode icon is specified, RAWeb will also use it for dark mode. Most workspace clients only support light mode icons.",-1)),o(i,{severity:"attention",title:"Icon requirements"},{default:s(()=>e[1]||(e[1]=[n(" RemoteApp icons must have the same width and height. RAWeb may choose to ignore icons that do not meet this requirement. ")])),_:1}),e[99]||(e[99]=t("ol",null,[t("li",null,[n("Navigate to "),t("strong",null,"Policies"),n(".")]),t("li",null,[n("At the top of the "),t("strong",null,"Policies"),n(" page, click "),t("strong",null,"Manage resources"),n(" to open the RemoteApps and desktops manager dialog.")]),t("li",null,"Click the RemoteApp for which you want to change the icon."),t("li",null,[n("In the "),t("strong",null,"Icon"),n(" group, click the "),t("strong",null,"Select icon"),n(" button for either light mode or dark mode. The browser will show a prompt to upload an icon."),t("br"),t("img",{width:"500",alt:"",src:Oi,height:"172",xmlns:"http://www.w3.org/1999/xhtml"})]),t("li",null,[n("Review the new icon preview. To remove the icon, click the "),t("strong",null,"X"),n(" button next to the preview."),t("br"),t("img",{width:"500",alt:"",src:Ni,height:"172",xmlns:"http://www.w3.org/1999/xhtml"})]),t("li",null,[n("Click "),t("strong",null,"OK"),n(" to save the RemoteApp details, including the new icon(s).")])],-1)),e[100]||(e[100]=t("h3",null,"Change a Desktop’s wallpaper",-1)),e[101]||(e[101]=t("p",null,"To change the wallpaper for a desktop, you can upload any wallpaper file that is supported by your browser. It will convert the wallpaper to PNG format and store it in RAWeb’s managed resources folder. You will see a preview of the uploaded wallpaper in the desktop’s properties dialog before saving the changes.",-1)),e[102]||(e[102]=t("p",null,"Light mode and dark mode wallpaper can be specified separately. If only light mode wallpaper is specified, RAWeb will also use it for dark mode. Most workspace clients only support light mode.",-1)),e[103]||(e[103]=t("ol",null,[t("li",null,[n("Navigate to "),t("strong",null,"Policies"),n(".")]),t("li",null,[n("At the top of the "),t("strong",null,"Policies"),n(" page, click "),t("strong",null,"Manage resources"),n(" to open the RemoteApps and desktops manager dialog.")]),t("li",null,"Click the desktop for which you want to change the wallpaper."),t("li",null,[n("In the "),t("strong",null,"Wallpaper"),n(" group, click the "),t("strong",null,"Select wallpaper"),n(" button for either light mode or dark mode. The browser will show a prompt to upload an image."),t("br"),t("img",{width:"500",alt:"",src:vi,height:"269",xmlns:"http://www.w3.org/1999/xhtml"})]),t("li",null,[n("Review the new wallpaper preview. To remove the wallpaper, click the "),t("strong",null,"X"),n(" button next to the preview."),t("br"),t("img",{width:"500",alt:"",src:Li,height:"269",xmlns:"http://www.w3.org/1999/xhtml"})]),t("li",null,[n("Click "),t("strong",null,"OK"),n(" to save the desktop details, including the new wallpaper.")])],-1)),e[104]||(e[104]=t("h3",null,"Configure file type associations",-1)),t("p",null,[e[3]||(e[3]=n("See ")),o(h,{to:"/docs/publish-resources/file-type-associations/#managed-resource-file-type-associations"},{default:s(()=>e[2]||(e[2]=[n("Add file type associations to managed resources")])),_:1}),e[4]||(e[4]=n(" for instructions on how to configure file type associations for managed RemoteApps."))]),e[105]||(e[105]=t("h3",null,"Configure user and group access",-1)),t("p",null,[e[6]||(e[6]=n("See ")),o(h,{to:"/docs/publish-resources/resource-folder-permissions/#managed-resources"},{default:s(()=>e[5]||(e[5]=[n("Configuring user-based and group‐based access to resources")])),_:1}),e[7]||(e[7]=n(" for instructions on how to configure user and group access for managed RemoteApps."))]),e[106]||(e[106]=t("h3",null,"Customize individual RDP file properties",-1)),e[107]||(e[107]=t("p",null,"RAWeb allows you to customize most RDP file properties for managed resources. This allows you to optimize the experience for individual RemoteApps and desktops.",-1)),t("ol",null,[e[14]||(e[14]=A("<li>Navigate to <strong>Policies</strong>.</li><li>At the top of the <strong>Policies</strong> page, click <strong>Manage resources</strong> to open the RemoteApps and desktops manager dialog.</li><li>Click the resource for which you want to configure RDP file properties.</li><li>In the <strong>Advanced</strong> group, click the <strong>Edit RDP file</strong> button.</li>",4)),t("li",null,[e[9]||(e[9]=n("You will see a dialog where you can edit supported RDP file properties. Properties related to settings that are available in the main RemoteApp properties dialog are disabled in this dialog. If you want to test the properties before you save them, click the ")),e[10]||(e[10]=t("strong",null,"Download",-1)),e[11]||(e[11]=n(" button to download a test RDP file.")),e[12]||(e[12]=t("br",null,null,-1)),e[13]||(e[13]=t("img",{width:"580",alt:"",src:he,height:"531",xmlns:"http://www.w3.org/1999/xhtml"},null,-1)),o(i,{severity:"information",title:"Tip"},{default:s(()=>e[8]||(e[8]=[n(" Place your mouse cursor over each property label to view a description and possible values. ")])),_:1})]),e[15]||(e[15]=t("li",null,[n("After making your changes, click "),t("strong",null,"OK"),n(" to confirm the specified RDP file properties.")],-1)),e[16]||(e[16]=t("li",null,[n("Click "),t("strong",null,"OK"),n(" to save the RemoteApp or desktop details.")],-1))]),e[108]||(e[108]=t("h3",null,"Remove a managed file resource",-1)),e[109]||(e[109]=t("ol",null,[t("li",null,[n("Navigate to "),t("strong",null,"Policies"),n(".")]),t("li",null,[n("At the top of the "),t("strong",null,"Policies"),n(" page, click "),t("strong",null,"Manage resources"),n(" to open the RemoteApps and desktops manager dialog.")]),t("li",null,"Select the RemoteApp or desktop you want to delete."),t("li",null,[n("In the "),t("strong",null,"Danger zone"),n(" group, click the "),t("strong",null,"Remove RemoteApp"),n(" or "),t("strong",null,"Remove desktop"),n(" button."),t("br"),t("img",{width:"500",alt:"",src:ue,height:"650",xmlns:"http://www.w3.org/1999/xhtml"})])],-1)),e[110]||(e[110]=t("h2",{id:"managed-registry-resources"},"Registry RemoteApps (managed registry resources)",-1)),e[111]||(e[111]=t("p",null,[n("RAWeb can publish RDP files from "),t("code",null,"HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Terminal Server\\CentralPublishedResources"),n(". Only applications with the "),t("code",null,"ShowInPortal"),n(" DWORD set to "),t("code",null,"1"),n(" will be published.")],-1)),o(i,{severity:"attention",title:"Secure context required"},{default:s(()=>e[17]||(e[17]=[n(" The resources manager requires a secure context (HTTPS). Make sure you access RAWeb's web interface via HTTPS in order to upload, edit, or delete registry resources. "),t("br",null,null,-1),t("br",null,null,-1),n(" If you cannot access RAWeb via HTTPS, you may access RAWeb from "),t("code",null,"localhost",-1),n(" (http://localhost/RAWeb) via any browser based on Chromium or Firefox on the host server – they treat localhost as a secure context. ")])),_:1}),e[112]||(e[112]=t("p",null,"To add a new RemoteApp, sign in the RAWeb’s web interface with an administrator account and follow these steps:",-1)),e[113]||(e[113]=t("ol",null,[t("li",null,[n("Navigate to "),t("strong",null,"Policies"),n(".")]),t("li",null,[n("At the top of the "),t("strong",null,"Policies"),n(" page, click "),t("strong",null,"Manage resources"),n(" to open the RemoteApps and desktops manager dialog. "),t("br"),n(" You will see a list of RemoteApps currently listed in "),t("code",null,"HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Terminal Server\\TSAppAllowList\\Applications"),n(". By default, if an app is not listed here, it will not be possible to remotely connect to it."),t("br"),t("img",{width:"700",alt:"",src:ce,height:"649",xmlns:"http://www.w3.org/1999/xhtml"})]),t("li",null,[n("To add a new RemoteApp, click the "),t("strong",null,"Add new RemoteApp"),n(" button at the top left of the dialog to open the app discovery dialog."),t("br"),n(" You will see a list of apps that RAWeb was able to discover on the server. RAWeb lists all packaged apps and any shortcut included in the system-wide Start Menu folder."),t("br"),t("img",{width:"400",alt:"",src:Ci,height:"573",xmlns:"http://www.w3.org/1999/xhtml"})]),t("li",null,[n("Click the app you want to add. You will see a pre-populated "),t("strong",null,"Add new RemoteApp"),n(" dialog."),t("br"),t("img",{width:"500",alt:"",src:Di,height:"650",xmlns:"http://www.w3.org/1999/xhtml"})]),t("li",null,[n("Configure the properties as desired. Make sure that "),t("strong",null,"Show in web interface and workspace feeds"),n(" is set to "),t("strong",null,"Yes"),n(". Click "),t("strong",null,"OK"),n(" to save the RemoteApp details to the registry.")])],-1)),e[114]||(e[114]=A("<h3>Change the RemoteApp icon</h3><p>To change the icon for a registry RemoteApp, you need to know the path to an icon file on the terminal server. You can use any <code>.exe</code>, <code>.dll</code>, <code>.ico</code>, <code>.png</code>, <code>.jpg</code>, <code>.jpeg</code>, <code>.bmp</code>, or <code>.gif</code> source on the server.</p>",2)),e[115]||(e[115]=t("ol",null,[t("li",null,[n("Navigate to "),t("strong",null,"Policies"),n(".")]),t("li",null,[n("At the top of the "),t("strong",null,"Policies"),n(" page, click "),t("strong",null,"Manage resources"),n(" to open the RemoteApps and desktops manager dialog.")]),t("li",null,"Click the RemoteApp for which you want to change the icon."),t("li",null,[n("In the "),t("strong",null,"Icon"),n(" group, click the "),t("strong",null,"Select icon"),n(" button."),t("br"),t("img",{width:"500",alt:"",src:Pi,height:"650",xmlns:"http://www.w3.org/1999/xhtml"})]),t("li",null,[n("In the "),t("strong",null,"Select icon"),n(" dialog, enter the full path to the icon file on the server. Press Enter/Return on your keyboard to load icons at that path. If you specify an "),t("code",null,"exe"),n(", "),t("code",null,"dll"),n(", or "),t("code",null,"ico"),n(" file with multiple contained icons, you will see multiple icons. Click the icon you want to use."),t("br"),t("img",{width:"600",alt:"",src:Fi,height:"548",xmlns:"http://www.w3.org/1999/xhtml"})]),t("li",null,[n("Click "),t("strong",null,"OK"),n(" to save the RemoteApp details.")])],-1)),e[116]||(e[116]=t("h3",null,"Configure file type associations",-1)),t("p",null,[e[19]||(e[19]=n("See ")),o(h,{to:"/docs/publish-resources/file-type-associations/#managed-resource-file-type-associations"},{default:s(()=>e[18]||(e[18]=[n("Add file type associations to managed resources")])),_:1}),e[20]||(e[20]=n(" for instructions on how to configure file type associations for registry RemoteApps."))]),e[117]||(e[117]=t("h3",null,"Configure user and group access",-1)),t("p",null,[e[22]||(e[22]=n("See ")),o(h,{to:"/docs/publish-resources/resource-folder-permissions/#managed-resources"},{default:s(()=>e[21]||(e[21]=[n("Configuring user-based and group‐based access to resources")])),_:1}),e[23]||(e[23]=n(" for instructions on how to configure user and group access for registry RemoteApps."))]),e[118]||(e[118]=t("h3",null,"Customize individual RDP file properties",-1)),e[119]||(e[119]=t("p",null,"RAWeb allows you to customize most RDP file properties for managed resources. This allows you to optimize the experience for individual RemoteApps and desktops.",-1)),o(i,{severity:"caution"},{default:s(()=>[t("p",null,[e[25]||(e[25]=n("Properties will be ignored and possibly overwritten for any properties specified in the policy: ")),o(h,{to:"/docs/policies/inject-rdp-properties/"},{default:s(()=>e[24]||(e[24]=[n("Add additional RDP file properties to RemoteApps listed in the registry")])),_:1}),e[26]||(e[26]=n("."))])]),_:1}),t("ol",null,[e[39]||(e[39]=t("li",null,[t("p",null,[n("Navigate to "),t("strong",null,"Policies"),n(".")])],-1)),e[40]||(e[40]=t("li",null,[t("p",null,[n("At the top of the "),t("strong",null,"Policies"),n(" page, click "),t("strong",null,"Manage resources"),n(" to open the RemoteApps and desktops manager dialog.")])],-1)),e[41]||(e[41]=t("li",null,[t("p",null,"Click the RemoteApp for which you want to configure RDP file properties.")],-1)),t("li",null,[e[36]||(e[36]=t("p",null,[n("In the "),t("strong",null,"Advanced"),n(" group, click the "),t("strong",null,"Edit RDP file"),n(" button.")],-1)),o(i,{severity:"attention"},{default:s(()=>[t("p",null,[e[28]||(e[28]=n("If you do not see the ")),e[29]||(e[29]=t("strong",null,"Edit RDP file",-1)),e[30]||(e[30]=n(" button, make sure the ")),o(h,{to:"/docs/policies/centralized-publishing/"},{default:s(()=>e[27]||(e[27]=[n("Use a dedicated collection for RemoteApps in the registry instead of the global list")])),_:1}),e[31]||(e[31]=n(" policy is set to ")),e[32]||(e[32]=t("strong",null,"Disabled",-1)),e[33]||(e[33]=n(" or ")),e[34]||(e[34]=t("strong",null,"Not configured",-1)),e[35]||(e[35]=n("."))])]),_:1})]),t("li",null,[e[38]||(e[38]=t("p",null,[n("You will see a dialog where you can edit supported RDP file properties. Properties related to settings that are available in the main RemoteApp properties dialog are disabled in this dialog. If you want to test the properties before you save them, click the "),t("strong",null,"Download"),n(" button to download a test RDP file."),t("br"),t("img",{width:"580",alt:"",src:he,height:"531",xmlns:"http://www.w3.org/1999/xhtml"})],-1)),o(i,{severity:"information",title:"Tip"},{default:s(()=>e[37]||(e[37]=[n(" Place your mouse cursor over each property label to view a description and possible values. ")])),_:1})]),e[42]||(e[42]=t("li",null,[t("p",null,[n("After making your changes, click "),t("strong",null,"OK"),n(" to confirm the specified RDP file properties.")])],-1)),e[43]||(e[43]=t("li",null,[t("p",null,[n("Click "),t("strong",null,"OK"),n(" to save the RemoteApp details.")])],-1))]),e[120]||(e[120]=t("h3",null,"Remove a RemoteApp from the registry",-1)),e[121]||(e[121]=t("ol",null,[t("li",null,[n("Navigate to "),t("strong",null,"Policies"),n(".")]),t("li",null,[n("At the top of the "),t("strong",null,"Policies"),n(" page, click "),t("strong",null,"Manage resources"),n(" to open the RemoteApps and desktops manager dialog.")]),t("li",null,"Select the RemoteApp you want to delete."),t("li",null,[n("In the "),t("strong",null,"Danger zone"),n(" group, click the "),t("strong",null,"Remove RemoteApp"),n(" button."),t("br"),t("img",{width:"500",alt:"",src:ue,height:"650",xmlns:"http://www.w3.org/1999/xhtml"})])],-1)),e[122]||(e[122]=t("h2",{id:"remoteapp-tool"},"Registry RemoteApps via RemoteApp Tool (deprecated)",-1)),e[123]||(e[123]=t("p",null,[n("RAWeb can publish RDP files from "),t("code",null,"HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Terminal Server\\TSAppAllowList\\Applications"),n(". Only applications with the "),t("code",null,"ShowInTSWA"),n(" DWORD set to "),t("code",null,"1"),n(" will be published. This behavior is not the preferred method of adding registry RemoteApps, and support may be removed in a future release. Use the RemoteApps and desktops manager in RAWeb’s web interface instead.")],-1)),o(i,{severity:"attention",title:"Policy configuration required"},{default:s(()=>[t("p",null,[e[45]||(e[45]=n("You must set the ")),o(h,{to:"/docs/policies/centralized-publishing/"},{default:s(()=>e[44]||(e[44]=[n("Use a dedicated collection for RemoteApps in the registry instead of the global list")])),_:1}),e[46]||(e[46]=n(" policy to ")),e[47]||(e[47]=t("strong",null,"Disabled",-1)),e[48]||(e[48]=n(" in order for RAWeb to publish RemoteApps from the registry path ")),e[49]||(e[49]=t("code",null,"HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Terminal Server\\TSAppAllowList\\Applications",-1)),e[50]||(e[50]=n("."))])]),_:1}),e[124]||(e[124]=t("p",null,[n("Use "),t("a",{href:"https://github.com/kimmknight/remoteapptool",target:"_blank",rel:"noopener noreferrer"},"RemoteApp Tool"),n(" to add, remove, and configure RemoteApps in the registry.")],-1)),t("ol",null,[e[60]||(e[60]=t("li",null,[n("Open "),t("strong",null,"RemoteApp Tool"),n(".")],-1)),e[61]||(e[61]=t("li",null,[n("Click the green plus icon in the bottom-left corner to "),t("strong",null,"Add a new RemoteApp"),n(". Find the executable for the application you want to add."),t("br"),t("img",{width:"400",alt:"",src:Wi,height:"269",xmlns:"http://www.w3.org/1999/xhtml"})],-1)),e[62]||(e[62]=t("li",null,[n("The application you added should now appear in the list of applications. "),t("strong",null,"Double click"),n(" it in the list to configure the properties.")],-1)),t("li",null,[e[52]||(e[52]=n("Set ")),e[53]||(e[53]=t("strong",null,"TSWebAccess",-1)),e[54]||(e[54]=n(" to ")),e[55]||(e[55]=t("strong",null,"Yes",-1)),e[56]||(e[56]=n(". You may configure other options as well. Remember to click ")),e[57]||(e[57]=t("strong",null,"Save",-1)),e[58]||(e[58]=n(" when you are finished.")),o(i,null,{default:s(()=>e[51]||(e[51]=[n(" Make sure "),t("b",null,"Command line option",-1),n(" is set to "),t("b",null,"Optional",-1),n(" or "),t("b",null,"Enforced",-1),n(" to allow "),t("a",{href:"/docs/publish-resources/file-type-associations"},"file type associations",-1),n(" to work. ")])),_:1}),e[59]||(e[59]=t("img",{width:"400",alt:"image",src:Hi,height:"393",xmlns:"http://www.w3.org/1999/xhtml"},null,-1))])]),e[125]||(e[125]=t("p",null,"The application should now appear in RAWeb.",-1)),e[126]||(e[126]=t("h2",{id:"host-system-desktop"},"Host system desktop",-1)),e[127]||(e[127]=t("p",null,"RAWeb can also publish the host system’s desktop as a managed resource. This allows users to connect to the RAWeb host server’s desktop via RAWeb.",-1)),e[128]||(e[128]=t("p",null,"As an added benefit, because the desktop is on the host server, RAWeb can detect and use the host server’s wallpaper as the desktop wallpaper in RAWeb’s web interface and workspace clients. For users who have set a different wallpaper, chosen a solid background, or enabled Windows spotlight, RAWeb will use the chosen desktop background for that user.",-1)),e[129]||(e[129]=t("p",null,"Publishing the host system desktop also makes it easy to access any application that is not directly exposed as a RemoteApp.",-1)),o(i,{severity:"attention",title:"Secure context required"},{default:s(()=>e[63]||(e[63]=[n(" The resources manager requires a secure context (HTTPS). Make sure you access RAWeb's web interface via HTTPS in order to upload, edit, or delete resources. "),t("br",null,null,-1),t("br",null,null,-1),n(" If you cannot access RAWeb via HTTPS, you may access RAWeb from "),t("code",null,"localhost",-1),n(" (http://localhost/RAWeb) via any browser based on Chromium or Firefox on the host server – they treat localhost as a secure context. ")])),_:1}),o(i,{severity:"attention",title:"Policy configuration required"},{default:s(()=>[t("p",null,[e[65]||(e[65]=n("If you do not see the host system desktop in the resources manager, make sure the ")),o(h,{to:"/docs/policies/centralized-publishing/"},{default:s(()=>e[64]||(e[64]=[n("Use a dedicated collection for RemoteApps in the registry instead of the global list")])),_:1}),e[66]||(e[66]=n(" policy is set to ")),e[67]||(e[67]=t("strong",null,"Disabled",-1)),e[68]||(e[68]=n(" or ")),e[69]||(e[69]=t("strong",null,"Not configured",-1)),e[70]||(e[70]=n("."))])]),_:1}),e[130]||(e[130]=t("p",null,"To publishing the host system desktop, follow these steps:",-1)),e[131]||(e[131]=t("ol",null,[t("li",null,[n("Navigate to "),t("strong",null,"Policies"),n(".")]),t("li",null,[n("At the top of the "),t("strong",null,"Policies"),n(" page, click "),t("strong",null,"Manage resources"),n(" to open the RemoteApps and desktops manager dialog. "),t("br"),n(" You will see a list of resources currently managed by RAWeb. "),t("br"),t("img",{width:"700",alt:"",src:ki,height:"328",xmlns:"http://www.w3.org/1999/xhtml"})]),t("li",null,[n("Look for a desktop with the same name as the host system. In the above example, the host system is named "),t("em",null,"DC-CORE-1"),n(" and runs Windows Server 2025, so the desktop is named "),t("em",null,"DC-CORE-1"),n(" and shows the default Windows Server 2025 wallpaper. Click the desktop to open the desktop properties dialog. "),t("br"),t("img",{width:"500",alt:"",src:Ui,height:"510",xmlns:"http://www.w3.org/1999/xhtml"})]),t("li",null,[n("Configure the properties as desired. Make sure that "),t("strong",null,"Show in web interface and workspace feeds"),n(" is set to "),t("strong",null,"Yes"),n(". Click "),t("strong",null,"OK"),n(" to finish adding the resource.")])],-1)),e[132]||(e[132]=t("h3",null,"Configure user and group access",-1)),t("p",null,[e[72]||(e[72]=n("See ")),o(h,{to:"/docs/publish-resources/resource-folder-permissions/#managed-resources"},{default:s(()=>e[71]||(e[71]=[n("Configuring user-based and group‐based access to resources")])),_:1}),e[73]||(e[73]=n(" for instructions on how to configure user and group access for the system desktop. Review the section on managed resources."))]),e[133]||(e[133]=t("h3",null,"Customize individual RDP file properties",-1)),e[134]||(e[134]=t("p",null,"RAWeb allows you to customize most RDP file properties for the system desktop. This allows you to optimize the experience for clients connecting the the desktop.",-1)),t("ol",null,[e[80]||(e[80]=A("<li>Navigate to <strong>Policies</strong>.</li><li>At the top of the <strong>Policies</strong> page, click <strong>Manage resources</strong> to open the RemoteApps and desktops manager dialog.</li><li>Click the system desktop.</li><li>In the <strong>Advanced</strong> group, click the <strong>Edit RDP file</strong> button.</li>",4)),t("li",null,[e[75]||(e[75]=n("You will see a dialog where you can edit supported RDP file properties. Properties related to settings that are available in the main system desktop properties dialog are disabled in this dialog. If you want to test the properties before you save them, click the ")),e[76]||(e[76]=t("strong",null,"Download",-1)),e[77]||(e[77]=n(" button to download a test RDP file.")),e[78]||(e[78]=t("br",null,null,-1)),e[79]||(e[79]=t("img",{width:"580",alt:"",src:Mi,height:"470",xmlns:"http://www.w3.org/1999/xhtml"},null,-1)),o(i,{severity:"information",title:"Tip"},{default:s(()=>e[74]||(e[74]=[n(" Place your mouse cursor over each property label to view a description and possible values. ")])),_:1})]),e[81]||(e[81]=t("li",null,[n("After making your changes, click "),t("strong",null,"OK"),n(" to confirm the specified RDP file properties.")],-1)),e[82]||(e[82]=t("li",null,[n("Click "),t("strong",null,"OK"),n(" to save the system desktop details.")],-1))]),e[135]||(e[135]=A('<h2 id="standard-rdp-files">Standard RDP files</h2><p>RDP files should be placed in <strong>C:\\inetpub\\RAWeb\\App_Data\\resources</strong>. Any RDP file in this folder will be automatically published.</p><p>You can create subfolders to sort your RemoteApps and desktops into groups. RemoteApps and desktops are organized into sections on the RAWeb web interface based on subfolder name.</p><p>To add icons, specify a <strong>.ico</strong> or <strong>.png</strong> file in with the same name as the <strong>.rdp</strong> file.</p><ul><li>.ico and .png icons are the only file types supported.</li><li>For RemoteApps, RAWeb will not serve an icon unless the width and height are the same.</li><li>For desktops, if the icon width and height are not the same, RAWeb will assume that the icon file represents the destkop wallpaper. When an icon is needed for the desktop, RAWeb will place the wallpaper into the blue rectangle section of Windows 11’s This PC icon. RAWeb will directly use the wallpaper on the devices tab of the web interface when the display mode is set to card.</li><li>RAWeb’s interface can use dark mode icons and wallpapers. Add “-dark” to the end of the icon name to specify a dark-mode icon or wallpaper.</li></ul>',5)),e[136]||(e[136]=t("img",{width:"600",alt:"",src:Gi,height:"257",xmlns:"http://www.w3.org/1999/xhtml"},null,-1)),e[137]||(e[137]=t("br",null,null,-1)),e[138]||(e[138]=t("br",null,null,-1)),e[139]||(e[139]=n(" You can also configure RAWeb to restrict which users see certain RDP files. ")),e[140]||(e[140]=t("h3",null,"Configure security permissions",-1)),e[141]||(e[141]=t("p",null,[n("By default, the "),t("strong",null,"App_Data\\resources"),n(" folder can be read by any user in the "),t("strong",null,"Users"),n(" group.")],-1)),t("p",null,[e[84]||(e[84]=n("RAWeb uses standard Windows security descriptors when determining user access to files in the ")),e[85]||(e[85]=t("strong",null,"App_Data\\resources",-1)),e[86]||(e[86]=n(" folder. Configure security permissions via the security tab in the folder or files properties. For more information, see ")),o(h,{to:"/docs/publish-resources/resource-folder-permissions/#resource-folder-permissions"},{default:s(()=>e[83]||(e[83]=[n("Configuring user‐based access to resources in the resources folder")])),_:1}),e[87]||(e[87]=n("."))]),e[142]||(e[142]=t("h4",null,"Use folder-based permissions",-1)),t("p",null,[e[89]||(e[89]=n("You can optionally provide different RemoteApps and desktops to different users based on their username or group membership via ")),e[90]||(e[90]=t("strong",null,"App_Data\\multiuser-resources",-1)),e[91]||(e[91]=n(". See ")),o(h,{to:"/docs/publish-resources/resource-folder-permissions/#multiuser-resources"},{default:s(()=>e[88]||(e[88]=[n("Configuring user and group access via folder-based permissions")])),_:1}),e[92]||(e[92]=n("."))])])}}},ji=Object.freeze(Object.defineProperty({__proto__:null,default:Vi,nav_title:Yi,title:xi},Symbol.toStringTag,{value:"Module"})),_i="/deploy-preview/pr-215/lib/assets/reconnect-no-resources-BnvpPSrB.webp",qi={class:"markdown-body"},Ji="Automatic and manual reconnection via RAWeb/RDWebService.asmx endpoint (MS-RDWR)",zi="Reconnection (MS-RDWR)",Ki={__name:"index",setup(l,{expose:r}){return r({frontmatter:{title:"Automatic and manual reconnection via RAWeb/RDWebService.asmx endpoint (MS-RDWR)",nav_title:"Reconnection (MS-RDWR)"}}),(d,e)=>{const i=p("CodeBlock");return a(),c("div",qi,[e[0]||(e[0]=t("p",null,[t("a",{href:"https://learn.microsoft.com/en-us/openspecs/windows_protocols/ms-rdwr",target:"_blank",rel:"noopener noreferrer"},"Remote Desktop Workspace Runtime Protocol"),n(" (MS-RDWR) allows Windows to ask the server for RDP files to use to reconnect after RemoteApps lose their connection. When attempting to reconnect via the RemoteApp and Desktop Connections system tray icon, Windows will send post to RAWeb’s "),t("code",null,"RDWebService.asmx"),n(" endpoint, requesting the list of reconnectable resources for the user.")],-1)),e[1]||(e[1]=t("p",null,"RAWeb cannot support this feature because RAWeb does not track the resources a user has launched. Therefore, RAWeb cannot provide the list reconnectable resources when Windows requests them.",-1)),e[2]||(e[2]=t("p",null,[n("RAWeb will always respond to the "),t("code",null,"RDWebService.asmx"),n(" request with the following response, indicating that there are no reconnectable resources.")],-1)),o(i,{class:"language-xml",code:`<?xml version="1.0" encoding="utf-8"?>
<soap:Envelope xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/">
  <soap:Body>
    <GetRDPFilesResponse xmlns="http://schemas.microsoft.com/ts/2010/09/rdweb">
      <GetRDPFilesResult>
        <version>8.0</version>
        <wkspRC></wkspRC>
      </GetRDPFilesResult>
    </GetRDPFilesResponse>
  </soap:Body>
</soap:Envelope>
`}),e[3]||(e[3]=t("p",null,"When attempting to reconnect via the system tray icon, Windows RemoteApp and Desktop Connections will show the following error. This error is expected behavior when using RAWeb, and there is no workaround or way to suppress it. Please do not file bug reports about this behavior.",-1)),e[4]||(e[4]=t("img",{width:"440",alt:"RemoteApp and Desktop Connections Reconnection Error",src:_i,height:"377",xmlns:"http://www.w3.org/1999/xhtml"},null,-1))])}}},Xi=Object.freeze(Object.defineProperty({__proto__:null,default:Ki,nav_title:zi,title:Ji},Symbol.toStringTag,{value:"Module"})),$i="/deploy-preview/pr-215/lib/assets/security-dialog-button-B-9en8en.webp",Qi="/deploy-preview/pr-215/lib/assets/security-dialog-deny-DMH3CYD3.webp",Zi="/deploy-preview/pr-215/lib/assets/c9c532ff-e8d5-4ad5-af84-2fe041d2a702-r6VI_DMW.png",er="/deploy-preview/pr-215/lib/assets/fe9547ee-db4a-4b2f-a69d-a8ea2ab61498-CCbOcPfb.png",tr="/deploy-preview/pr-215/lib/assets/ff38c130-fae6-4302-a2bd-4f9420833368-DVr8Y7M-.png",nr="/deploy-preview/pr-215/lib/assets/df27a870-9830-42e9-b726-f0f413d5890e-CURbI1Wy.png",or="/deploy-preview/pr-215/lib/assets/6dfffa48-ce9c-4f8d-8aed-ceb6f9753983-Dx6pTKqD.png",ir="/deploy-preview/pr-215/lib/assets/f74bbe8c-b84d-4c86-80bc-98a55283e226-dbBcStEQ.png",rr="/deploy-preview/pr-215/lib/assets/c0ba7509-8dc4-41f0-9936-04a66a271a52-Bcd0jgVk.png",sr={class:"markdown-body"},ar="Configuring user-based and group‐based access to resources",lr="Security permissions",dr={__name:"index",setup(l,{expose:r}){return r({frontmatter:{title:"Configuring user-based and group‐based access to resources",nav_title:"Security permissions"}}),(d,e)=>(a(),c("div",sr,[e[1]||(e[1]=A('<p>RAWeb supports restricting visibility of managed resources to specific users or groups. Note that this does not prevent users from launching the RemoteApp or desktop if they know the name of the RemoteApp or desktop and how to modify RDP files. It only controls whether the RemoteApp or desktop is visible in RAWeb’s web interface and workspace feeds.</p><p>RAWeb offers different ways to configure access to RemoteApps and desktops for users and groups based on how the resources have been provided to RAWeb. There are four different locations where resources can be stored for RAWeb:</p><ul><li><code>App_Data\\managed_resources</code> (managed resources) - RDP files that have been provided to RAWeb via the RemoteApps and desktops manager.</li><li>Registry (managed resources) - RemoteApps and desktops on the RAWeb host that have been configured via the RemoteApps and desktops manager.</li><li><code>App_Data\\multiuser-resources</code> (multiuser resources) - RDP files that have been placed in subfolders to configure folder-based permissions.</li><li><code>App_Data\\resources</code> (resources) - RDP files that have been placed directly in the resources folder.</li></ul><h2 id="managed-resources">Managed resources</h2><p>If you do not configure any user or group restrictions for a managed resource, it will be visible to all Remote Desktop users or Administrators on the server.</p>',5)),e[2]||(e[2]=t("ol",null,[t("li",null,[n("Navigate to "),t("strong",null,"Policies"),n(".")]),t("li",null,[n("At the top of the "),t("strong",null,"Policies"),n(" page, click "),t("strong",null,"Manage resources"),n(" to open the RemoteApps and desktops manager dialog.")]),t("li",null,"Click the RemoteApp for which you want to configure user or group restrictions."),t("li",null,[n("In the "),t("strong",null,"Advanced"),n(" group, click the "),t("strong",null,"Manage user assignment"),n(" button."),t("br"),t("img",{width:"500",alt:"",src:$i,height:"650",xmlns:"http://www.w3.org/1999/xhtml"})]),t("li",null,[n("Click "),t("strong",null,"Add user or group"),n(" to open the "),t("strong",null,"Select Users or Groups"),n(" dialog.")]),t("li",null,[n("Enter the name of the user or group you want to add. Click "),t("strong",null,"Check Names"),n(" to verify the name. Click "),t("strong",null,"OK"),n(" to add the user or group.")]),t("li",null,[n("If you want to explicitly deny access to a user or group, click the shield icon next to the user or group name."),t("br"),t("img",{width:"460",alt:"",src:Qi,height:"597",xmlns:"http://www.w3.org/1999/xhtml"})]),t("li",null,[n("Click "),t("strong",null,"OK"),n(" to confirm the specified user and group restrictions.")]),t("li",null,[n("Click "),t("strong",null,"OK"),n(" to save the RemoteApp details.")])],-1)),e[3]||(e[3]=A('<h2 id="multiuser-resources">Resources in <code>App_Data\\multiuser-resources</code></h2><p>Inside the RAWeb folder, you will find a folder called <strong>App_Data\\multiuser-resources</strong>. If it does not exist, create it. This folder is used to store resources that are published to specific users or groups based on folder structure.</p><p>It contains the folders:</p><ul><li><p><strong>/user</strong> - Create folders in here for each user you wish to target (folder name = username). Drop rdp/image files into a user folder to publish them to the user.</p></li><li><p><strong>/group</strong> - Create folders in here for each group you wish to target (folder name = group name). Drop rdp/image files into a group folder to publish them to all users in the group.</p></li></ul>',4)),o(g(L),{title:"Note"},{default:s(()=>e[0]||(e[0]=[n(" Subfolders within user and group folders are supported. For clients that show folders, each subfolder will appear as a distinct section in the list of apps. ")])),_:1}),e[4]||(e[4]=A('<h2 id="resource-folder-permissions">Resources in <code>App_Data\\resources</code></h2><p>RAWeb uses standard Windows security descriptors when determining user access to files in the <strong>App_Data\\resources</strong> folder. Configure security permissions via the security tab in the folder or files properties. The following subsections describe how to configure security permissions for the <strong>App_Data\\resources</strong> folder and its contents.</p><p><strong>Section summary:</strong></p><ul><li>RAWeb users (or groups) should <em>only</em> have <strong>List folder contents</strong> permissions on the <strong>App_Data\\resources</strong> directory (disable inheritance).</li><li>Any user or group requiring access to a RemoteApp or desktop must have <strong>Read</strong> permission for the RDP file for the app or desktop. <ul><li>For icons to be visible, the user or group must also have <strong>Read</strong> permission for the icon(s) associated with the RDP file.</li></ul></li></ul><h3>Configure directory security permissions</h3><p>By default, the <strong>App_Data\\resources</strong> folder can be read by any user in the <strong>Users</strong> group. We need to change the permissions.</p>',6)),e[5]||(e[5]=t("ol",null,[t("li",null,[n("Open "),t("strong",null,"File Explorer"),n(" and navigate to the RAWeb directory. The default installation directory is "),t("code",null,"C:\\inetpub\\RAWeb"),n(".")]),t("li",null,[n("Navigate to "),t("code",null,"App_Data"),n(".")]),t("li",null,[n("Right click the "),t("code",null,"resources"),n(" folder and choose "),t("strong",null,"Properties"),n(" to open the properties window.")]),t("li",null,[n("Switch to the "),t("strong",null,"Security"),n(" tab and click "),t("strong",null,"Advanced"),n(" to open the "),t("strong",null,"Advanced Security Settings"),n(" dialog."),t("br"),t("img",{width:"400",src:Zi,height:"539",xmlns:"http://www.w3.org/1999/xhtml"})]),t("li",null,[n("In the list of "),t("strong",null,"Permissions entries"),n(", select "),t("strong",null,"Users"),n(". Then, click "),t("strong",null,"Edit"),n(". A "),t("strong",null,"Permission Entry"),n(" dialog will open."),t("br"),t("img",{width:"768",src:er,height:"531",xmlns:"http://www.w3.org/1999/xhtml"})]),t("li",null,[n("In the "),t("strong",null,"Permission Entry"),n(" dialog, click "),t("strong",null,"Show advanced permissions"),n(". Then, in the "),t("strong",null,"Advanced permissions"),n(" section, uncheck all permissions except "),t("em",null,"Traverse folder"),n(" and "),t("em",null,"List folder"),n(". Click "),t("strong",null,"OK"),n(" to close the dialog."),t("br"),t("img",{width:"918",src:tr,height:"607",xmlns:"http://www.w3.org/1999/xhtml"})]),t("li",null,[n("In the "),t("strong",null,"Advanced Security Settings"),n(" dialog, click "),t("strong",null,"OK"),n(" to apply the changes and close the dialog.")])],-1)),e[6]||(e[6]=t("h3",null,"Grant access to resources to specific users or groups",-1)),e[7]||(e[7]=t("p",null,[n("Use the following steps to grant access to a single resource for a user or group. These steps need to be repeated for each RDP file or icon/wallpaper file. Changes to security permissions affect access to resources from the web app and all workspace clients (e.g., Windows App). If you only need to grant access to a collection of resources to a single user or group, consider using "),t("a",{href:"docs/publish-resources/resource-folder-permissions/#folder-based-permissions"},"multiuser-resources for folder-based permissions"),n(".")],-1)),e[8]||(e[8]=t("ol",null,[t("li",null,[n("Navigate to the "),t("code",null,"resources"),n(" folder. In a standard installation, the path is "),t("code",null,"C:\\inetpub\\RAWeb\\App_Data\\resources"),n(".")]),t("li",null,[n("Right click a resource and choose "),t("strong",null,"Properties"),n(" to open the properties window.")]),t("li",null,[n("Switch to the "),t("strong",null,"Security"),n(" tab and click "),t("strong",null,"Edit"),n(" to open the "),t("strong",null,"Permissions"),n(" dialog."),t("br"),t("img",{width:"400",src:nr,height:"512",xmlns:"http://www.w3.org/1999/xhtml"})]),t("li",null,[n("Click "),t("strong",null,"Add"),n(" to open the "),t("strong",null,"Select Users or Groups"),n(" dialog."),t("br"),t("img",{width:"360",src:or,height:"454",xmlns:"http://www.w3.org/1999/xhtml"})]),t("li",null,[n("In the "),t("strong",null,"Select Users or Groups"),n(" dialog, specify the users or groups you want to add. When you are ready, click "),t("strong",null,"OK"),n("."),t("br"),t("img",{width:"458",src:ir,height:"257",xmlns:"http://www.w3.org/1999/xhtml"})]),t("li",null,[n("In the "),t("strong",null,"Permissions"),n(" dialog, confirm that only "),t("em",null,"Read"),n(" or "),t("em",null,"Read"),n(" and "),t("em",null,"Read and execute"),n(" are allowed. Click "),t("strong",null,"OK"),n(" to apply changes and close the dialog."),t("br"),t("img",{width:"360",src:rr,height:"454",xmlns:"http://www.w3.org/1999/xhtml"})]),t("li",null,[n("In the "),t("strong",null,"Properties"),n(" window, click "),t("strong",null,"OK"),n(".")])],-1))]))}},cr=Object.freeze(Object.defineProperty({__proto__:null,default:dr,nav_title:lr,title:ar},Symbol.toStringTag,{value:"Module"})),hr={class:"markdown-body"},ur="Reverse proxy via Nginx",pr={__name:"index",setup(l,{expose:r}){return r({frontmatter:{title:"Reverse proxy via Nginx"}}),(d,e)=>(a(),c("div",hr,e[0]||(e[0]=[t("p",null,"Nginx reverse proxy does not support proxying NTLM authentication by default. If you place RAWeb behind nginx, you will only be able to use the web interface.",-1),t("p",null,[n("Some recommendations on the internet suggest using "),t("code",null,"upstream"),n(" with "),t("code",null,"keepalive"),n(". This is "),t("em",null,[t("strong",null,"extremely dangerous")]),n(" because keepalive connections are shared between all connections, which means that one user’s NTLM authentication will apply to other users as well.")],-1),t("p",null,[n("You may be able to enable NTLM authentication with nginx via nginx plus or by using "),t("a",{href:"https://github.com/gabihodoroaga/nginx-ntlm-module",target:"_blank",rel:"noopener noreferrer"},"nginx-ntl-module"),n(".")],-1)])))}},mr=Object.freeze(Object.defineProperty({__proto__:null,default:pr,title:ur},Symbol.toStringTag,{value:"Module"})),fr="/deploy-preview/pr-215/lib/assets/sign-in-hybrid.dark-LEZ7iiqG.png",De="/deploy-preview/pr-215/lib/assets/sign-in-hybrid-BF61B-8t.png",Ar=fr,gr=De,Ir={class:"markdown-body"},Tr="Authentication modes",br={__name:"index",setup(l,{expose:r}){return r({frontmatter:{title:"Authentication modes"}}),(d,e)=>(a(),c("div",Ir,e[0]||(e[0]=[A("<p>RAWeb offers several authentication modes to control access to the application. This document describes the available modes and how to configure them.</p><h2>Modes</h2><h3>System authentication mode (recommended) (default)</h3><p>This mode requires users to sign in with a username and password before accessing RAWeb. User credentials are manged by Windows; any user with a valid Windows account on the server hosting RAWeb or the same domain as the server can sign in.</p><p>When this mode is enabled, anonymous authentication is never allowed.</p><h3>Anonymous mode</h3><p>In this mode, RAWeb does not perform any authentication, allowing anyone to access the application without signing in. It is not possible to sign in with credentials.</p><p>When this mode is enabled:</p><ul><li>The login page will automatically sign in as the anonymous user.</li><li>The web app will hide the option to sign out or change credentials.</li><li>The webfeed/workspace feature will work without authentication. A trusted certificate is still required.</li></ul><h3>Hybrid mode</h3><p>In hybrid mode, RAWeb allows both anonymous access and credential-based sign-in. Users can choose to sign in with their credentials or continue as an anonymous user.</p><p>When this mode is enabled, anonymous authentication is only allowed for the web interface; you must use credentials when access the RAWeb workspace/webfeed.</p><p>When this mode is enabled:</p><ul><li>The login page will show a <strong>Skip</strong> button, which signs in as the anonymous user.</li><li>Users can still sign in with their Windows credentials.</li><li>The webfeed/workspace feature still requires authentication with Windows credentials.</li><li>Resources can be restricted to the anonymous user by placing them in a folder in <code>App_Data/multiuser-resources</code> with the name <code>anonymous</code>. Internally, RAWeb assigns the anonymous user to the RAWEB virtual domain, the anonymous username, and the S-1-4-447-1 security identifier (SID).</li></ul><p><em>Note the skip button next to the continue button:</em></p>",15),t("picture",null,[t("source",{media:"(prefers-color-scheme: dark)",srcset:Ar}),t("source",{media:"(prefers-color-scheme: light)",srcset:gr}),t("img",{width:"400",src:De,alt:"A screenshot of the RAWeb sign in page when anonymous authentication is allowed",border:"1",height:"320",xmlns:"http://www.w3.org/1999/xhtml"})],-1),t("h2",null,"Configuration",-1),t("ol",null,[t("li",null,[n("Once RAWeb is installed, open "),t("strong",null,"IIS Manager"),n(" and expand the tree in the "),t("strong",null,"Connections pane"),n(" on the left side until you can see the "),t("strong",null,"RAWeb"),n(" application. The default name is "),t("strong",null,"RAWeb"),n(", but it may have a different name if you performed a manual installation to a different folder. Click on the "),t("strong",null,"RAWeb"),n(" application.")]),t("li",null,[n("In the "),t("strong",null,"Features View"),n(", double click "),t("strong",null,"Application Settings"),n(),t("br"),t("img",{width:"860",src:Le,height:"471",xmlns:"http://www.w3.org/1999/xhtml"})]),t("li",null,[n("In the "),t("strong",null,"Actions pane"),n(", click "),t("strong",null,"Add"),n(" to open the "),t("strong",null,"Add Application Setting"),n(" dialog. "),t("br"),t("img",{width:"860",src:Ce,height:"471",xmlns:"http://www.w3.org/1999/xhtml"})]),t("li",null,[n("Specify the properties. For "),t("strong",null,"Name"),n(", use "),t("code",null,"App.Auth.Anonymous"),n(". For "),t("strong",null,"Value"),n(", specify the value that corresponds to the desired authentication mode: "),t("ul",null,[t("li",null,[n("System authentication mode: "),t("code",null,"never")]),t("li",null,[n("Anonymous mode: "),t("code",null,"always")]),t("li",null,[n("Hybrid mode: "),t("code",null,"allow")])])]),t("li",null,[n("When you are finished, click "),t("strong",null,"OK"),n(".")])],-1)])))}},yr=Object.freeze(Object.defineProperty({__proto__:null,default:br,title:Tr},Symbol.toStringTag,{value:"Module"})),Rr={class:"markdown-body"},Er="Trusting the RAWeb server (Fix security error 5003)",wr="Trust (security error 5003)",Sr={__name:"index",setup(l,{expose:r}){return r({frontmatter:{title:"Trusting the RAWeb server (Fix security error 5003)",nav_title:"Trust (security error 5003)"}}),(d,e)=>{const i=p("CodeBlock");return a(),c("div",Rr,[e[0]||(e[0]=A('<p>As part of the RAWeb installation, it checks whether an SSL certificate is installed on the server and bound to the HTTPS binding in IIS. If it does not find a certificate, the installer will ask to install and bind a self-signed SSL certificate. If you did not accept that option, re-install RAWeb before continuing with this guide.</p><p>The following features require RAWeb to operate over HTTPS with a valid SSL certificate:</p><ul><li>RemoteApp and Desktop Connections (RADC) on Windows</li><li>Workspaces in Windows App (formerly Microsoft Remote Desktop) on macOS, Android, iOS, and iPadOS</li></ul><p>This guide shows you how to configure the RAWeb server and the client devices to operate over HTTPS with an SSL certificate. A limitation of using a self-signed certificate is that it must be installed on each client computer before it will be able to connect. If you wish to avoid this limitation, you must choose <a href="docs/security/error-5003/#option-2">Option 2. Use a certificate from a trusted certificate authority</a>.</p><h2>Option 1: Manually trust the self-signed certificate generated by the RAWeb installer</h2><p>Open PowerShell. Then, run the following script. It will prompt you for the full URL of your RAWeb installation. It will retrieve the SSL certificate from that URL and add it to the Trusted Root Certification Authorities certificate store for the current user.</p><p>After trusting the certificate on a device, some web browsers may still display a security error. In that case, you may need to restart the web browser or the device.</p>',7)),o(i,{class:"language-powershell",code:`$rawebUrl = Read-Host "Enter the full URL (include the protocol) to your installation of RAWeb:"

# bypass SSL/TLS certificate validation for the current session
[System.Net.ServicePointManager]::ServerCertificateValidationCallback = { $true }

try {
    $webRequest = [Net.WebRequest]::Create($rawebUrl)
    $webRequest.GetResponse() | Out-Null # we do not need the response, just the connection
    $cert = $webRequest.ServicePoint.Certificate

    if ($cert) {
        # get the current user's certificate store
        $store = New-Object System.Security.Cryptography.X509Certificates.X509Store("Root", [System.Security.Cryptography.X509Certificates.StoreLocation]::CurrentUser)

        try {
            $store.Open([System.Security.Cryptography.X509Certificates.OpenFlags]::ReadWrite)

            # Check if the certificate is already in the store (optional but good practice)
            $existingCert = $store.Certificates | Where-Object { $_.Thumbprint -eq $cert.Thumbprint }
            if ($existingCert) {
                Write-Host "Certificate is already in the Trusted Root Certification Authorities store for the current user."
            } else {
                $store.Add($cert)
                Write-Host "Certificate successfully installed to the Trusted Root Certification Authorities store for the current user."
            }
        }
        finally {
            $store.Close()
        }

    } else {
        Write-Error "Could not retrieve the certificate from the specified URL."
    }

} catch {
    Write-Error "An error occurred while trying to access the URL or process the certificate: $($_.Exception.Message)"
}
`}),e[1]||(e[1]=t("p",null,"If you need the .cer file for other devices, run the following script. It will prompt you to save the certificate file.",-1)),o(i,{class:"language-powershell",code:`Add-Type -AssemblyName System.Windows.Forms

# get the RAWeb URL first
$rawebUrl = Read-Host "Enter the full URL (include the protocol) to your installation of RAWeb:"

# bypass SSL/TLS certificate validation for the current session
[System.Net.ServicePointManager]::ServerCertificateValidationCallback = { $true }

try {
    $webRequest = [Net.WebRequest]::Create($rawebUrl)
    $webRequest.GetResponse() | Out-Null # We only need to establish the connection
    $cert = $webRequest.ServicePoint.Certificate

    if ($cert) {
        # create and configure a SaveFileDialog object
        $SaveFileDialog = New-Object System.Windows.Forms.SaveFileDialog
        $SaveFileDialog.InitialDirectory = [Environment]::GetFolderPath("Desktop") # Set initial directory (e.g., Desktop)
        $SaveFileDialog.Filter = "Certificate files (*.cer)|*.cer|All files (*.*)|*.*" # Set file filter
        $SaveFileDialog.FilterIndex = 1 # Set the default selected filter
        $SaveFileDialog.FileName = "raweb.cer" # Set a default file name
        $SaveFileDialog.Title = "Save RAWeb Certificate" # Set the dialog title
        $DialogResult = $SaveFileDialog.ShowDialog()

        # check if the user clicked OK
        if ($DialogResult -eq [System.Windows.Forms.DialogResult]::OK) {
            # get the selected file path
            $SavePath = $SaveFileDialog.FileName

            # export the certificate to bytes and save it to the path from the dialog
            $bytes = $cert.Export([Security.Cryptography.X509Certificates.X509ContentType]::Cert)
            set-content -value $bytes -encoding byte -path $SavePath

            Write-Host "Certificate successfully downloaded and saved to: $SavePath"

        } else {
            Write-Host "Certificate download canceled by user."
        }

        # clean up
        $SaveFileDialog.Dispose()

    } else {
        Write-Error "Could not retrieve the certificate from the specified URL."
    }

} catch {
    Write-Error "An error occurred while trying to access the URL or process the certificate: $($_.Exception.Message)"
}
`}),e[2]||(e[2]=A('<h2 id="option-2">Option 2: Use a certificate from a trusted certificate authority</h2><p>If you have a domain (e.g., example.com) or subdomain, you can obtain an SSL certificate from a trusted certificate authority. You can configure IIS to use the SSL certificate when accessing RAWeb via your domain or subdomain.</p><ol><li>Obtain a certificate in <code>.pfx</code> format from a trusted certificate authority for a domain you own. If you do not have one, you can obtain one for free from <a href="https://letsencrypt.org/getting-started/" target="_blank" rel="noopener noreferrer">Lets Encrypt</a>.</li><li>Open Internet Information Services (IIS) Manager.</li><li>Click the server’s name in the <strong>Connections</strong> pane.</li><li>In the <strong>Features View</strong>, double click <strong>Server Certificates</strong>.</li><li>In the <strong>Actions</strong> pane, click <strong>Import…</strong>.</li><li>Add the certificate file. If it has a password, specify it. Click <strong>OK</strong>.</li><li>In the <strong>Connections</strong> pane, navigate to <strong>Default Web Site</strong>.</li><li>In the <strong>Actions</strong> pane, click <strong>Bindings…</strong>.</li><li>In the <strong>Site Bindings</strong> dialog, click <strong>Add</strong>.</li><li>In the <strong>Add Site Binding</strong> dialog, set <strong>Type</strong> to <em>https</em> and <strong>SSL certificate</strong> to the certificate you imported. Click <strong>OK</strong>.</li><li>Configure your network to expose port 443 from the server or PC that hosts RAWeb.</li><li>In your domain’s DNS settings, configure an A record to point to the public IP address of the server or PC that hosts your installation of RAWeb.</li></ol>',3))])}}},Or=Object.freeze(Object.defineProperty({__proto__:null,default:Sr,nav_title:wr,title:Er},Symbol.toStringTag,{value:"Module"})),Nr={class:"markdown-body"},vr="Enable multi-factor authentication (MFA) for the web app",Lr="Multi-factor authentication (MFA)",Cr={__name:"index",setup(l,{expose:r}){return r({frontmatter:{title:"Enable multi-factor authentication (MFA) for the web app",nav_title:"Multi-factor authentication (MFA)"}}),(d,e)=>{const i=p("RouterLink");return a(),c("div",Nr,[e[4]||(e[4]=t("p",null,"RAWeb supports multi-factor authentication for the web app via external MFA providers. Visit the following policy documentation pages for instructions on configuring MFA with supported providers:",-1)),t("ul",null,[t("li",null,[o(i,{to:"/docs/policies/duo-mfa"},{default:s(()=>e[0]||(e[0]=[n("Duo Universal Prompt")])),_:1})])]),t("p",null,[e[2]||(e[2]=n("Windows RemoteApp and Desktop Connections and the Windows App provide no known mechanism for supporting MFA. Therefore, enabling MFA for the web app will not affect authentication for these clients. If you need to require MFA clients, consider disabling access to workspace clients with the ")),o(i,{to:"/docs/policies/block-workspace-auth"},{default:s(()=>e[1]||(e[1]=[n("Block workspace client authentication")])),_:1}),e[3]||(e[3]=n(" policy."))])])}}},Dr=Object.freeze(Object.defineProperty({__proto__:null,default:Cr,nav_title:Lr,title:vr},Symbol.toStringTag,{value:"Module"})),Pr={class:"markdown-body"},Fr="Supported server environments",Wr="Server environments",Hr={__name:"index",setup(l,{expose:r}){return r({frontmatter:{title:"Supported server environments",nav_title:"Server environments"}}),(d,e)=>(a(),c("div",Pr,[e[1]||(e[1]=A("<h2>Supported host machines</h2><p>RAWeb can be hosted on any modern 64-bit Windows device. The primary requirement is the device must support Internet Information Services (IIS) 10 and .NET Framework 4.6.2 or newer. Windows Server 2016 and Windows 10 Version 1607 are the first versions to meet these requirements.</p><h2>Authenticiation scenarios</h2><p>RAWeb can authenticate with local or domain credentials.</p><h3>Local credentials</h3><p>Local credentials should work in all scenarios.</p><h3>Domain credentials</h3><p>For domain credentials, the machine with the RAWeb installation must have permission to enumerate groups and their members. RAWeb uses group membership to restrict access to resources hosted on RAWeb. If RAWeb cannot search group memberships, resources will not load and authentication may fail. This is most likely an issue with complex domain forests that contain one-way trusts.</p><h4>Application pool configuration</h4><p>If necessary, you may change the credentials used by the RAWeb application in IIS so that it uses an account with permission to list groups and view group memberships. See the instructions below:</p><details><summary>Instructions</summary><ol><li>Open <strong>Internet Information Services (IIS) Manager</strong>.</li><li>In the <strong>Connections</strong> pane, click on <strong>Application Pools</strong>.</li><li>In the list of application pools, right click on <strong>raweb</strong> and choose <strong>Advanced Settings</strong>.</li><li>In the <strong>Process Model</strong> group, click on <strong>Identity</strong>. Then, click the button with the ellipsis (<strong>…</strong>) to open the <strong>Application Pool Identity</strong> dialog.</li><li>Choose <strong>Custom Account</strong>, and then click <strong>Set</strong> to provide the credentials for the account.</li><li>Click <strong>OK</strong> on all three dialogs. The RAWeb application will now use the credentials you proivided for its process.</li></ol></details><h4>User cache</h4>",12)),o(g(L),{severity:"caution",title:"Security consideration"},{default:s(()=>e[0]||(e[0]=[n(" Group membership will not automatically update when the user cache is enabled. ")])),_:1}),e[2]||(e[2]=A("<p>If there are cases where the domain controller may be unavailable to RAWeb, you may also want to enable the user cache. The user cache stores details about a user every time the sign in, and RAWeb will fall back to the details in the user cache if the domain controller cannot be reached. If RAWeb is unable to load group memberships from the domain, the group membership cached in the user cache will be used instead. When the user cache is enabled and the domain controller cannot be accessed, the authentication mechanism can also sign in using the cached domain credentials stored by the Windows machine with RAWeb installed. Instructions for enabling are below:</p><details><summary>Instructions</summary><p>If you are able to sign in to RAWeb as an administrative user:</p><ol><li>Open the RAWeb web app.</li><li>Navigate to the <strong>Policies</strong> page.</li><li>Set the <strong>Enable the user cache</strong> policy state to <strong>Enabled</strong>.</li><li>Click <strong>OK</strong> to apply the policy.</li></ol><p>Otherwise, enable the policy via IIS Manager:</p><ol><li>Open <strong>Internet Information Services (IIS) Manager</strong>.</li><li>In the <strong>Connections</strong> pane, find your installation of RAWeb and click it.</li><li>Open <strong>Application Settings</strong>.</li><li>In the <strong>Actions</strong> pane, click <strong>Add…</strong>.</li><li>For <strong>Name</strong>, specify <em>UserCache.Enabled</em>. For <strong>Value</strong>, specify <em>true</em>.</li><li>Click <strong>OK</strong> to apply the policy.</li></ol></details>",2))]))}},kr=Object.freeze(Object.defineProperty({__proto__:null,default:Hr,nav_title:Wr,title:Fr},Symbol.toStringTag,{value:"Module"})),Ur="/deploy-preview/pr-215/lib/assets/window-controls.dark-CnuJSFXc.webp",Pe="/deploy-preview/pr-215/lib/assets/window-controls-C6XL_qA2.webp",Mr=Ur,Gr=Pe,Br={class:"markdown-body"},xr="Supported web clients",Yr="Web clients",Vr={__name:"index",setup(l,{expose:r}){return r({frontmatter:{title:"Supported web clients",nav_title:"Web clients"}}),(d,e)=>(a(),c("div",Br,e[0]||(e[0]=[A('<p>RAWeb’s web interface utilizes modern web technologies to provide a responsive and user-friendly experience. To ensure optimal performance and compatibility, it is important to use a supported web browser. RAWeb hides or disables certain features when accessed from unsupported or partially-supported browsers.</p><table><thead><tr><th>Required web technology</th><th>Description</th><th>Supported browsers</th></tr></thead><tbody><tr><td><a href="https://developer.mozilla.org/en-US/docs/Web/CSS/Reference/At-rules/@media/prefers-color-scheme#browser_compatibility" target="_blank" rel="noopener noreferrer"><code>prefers-color-scheme</code></a> media query</td><td>Used to detect dark mode preference and adjust the interface accordingly.</td><td>Edge 79+, Chrome 76+, Firefox 67+, Safari 12.1+</td></tr><tr><td><a href="https://developer.mozilla.org/en-US/docs/Web/API/Crypto/subtle#browser_compatibility" target="_blank" rel="noopener noreferrer">Subtle crypto</a></td><td>Used for generating hashes for the resource manager.</td><td>Edge 12+, Chrome 37+, Firefox 34+, Safari 11+</td></tr><tr><td><a href="https://caniuse.com/css-anchor-positioning" target="_blank" rel="noopener noreferrer">CSS anchor positioning</a></td><td>Used for all dropdown and context menus in the web interface.</td><td>Edge 125+, Chrome 125+, Safari 26+</td></tr><tr><td><a href="https://developer.mozilla.org/en-US/docs/Web/API/HTMLDialogElement/requestClose#browser_compatibility" target="_blank" rel="noopener noreferrer"><code>dialog.requestClose()</code></a></td><td>Required for dialogs in the web interface.</td><td>Edge 134+, Chrome 134+, Firefox 139+, Safari 18.4+</td></tr><tr><td><a href="https://developer.mozilla.org/en-US/docs/Web/API/Service_Worker_API" target="_blank" rel="noopener noreferrer">Service Workers</a></td><td>Used for stale-while-revalidate caching logic, which allows RAWeb to load quickly after the first page load. The cache is discarded whenever a user signs out.</td><td>Edge 17+, Chrome 45+, Firefox 138+, Safari 11.1+</td></tr><tr><td><a href="https://caniuse.com/?search=window+controls+overlay" target="_blank" rel="noopener noreferrer">Window Controls Overlay</a></td><td>Allows RAWeb’s web interface to combine its titlebar with the OS titlebar when in PWA mode</td><td>Edge 105+, Chrome 105+</td></tr></tbody></table><h2>CSS anchor positioning</h2><p>Firefox does not currently support CSS anchor positioning.</p><p>The version of Safari released Fall 2025 is the first version to support CSS anchor positioning. As a result, users accessing RAWeb with Firefox or older versions of Safari will experience limited functionality in dropdown and context menus. Specifically, these menus will not be displayed.</p><ul><li>Menus for apps and devices will not appear.</li><li>It will not be possible to add or remove favorites.</li><li>The connection method dialog will not show an option to remember the selected method.</li><li>The menus in the resource manager will appear at the top-left corner of the screen instead of next to the menu source.</li></ul><h2>dialog.requestClose()</h2><p>Support for <code>&lt;dialog&gt;</code> and <code>dialog.requestClose()</code> are new features that were added to web browsers in 2025. As a result, users accessing RAWeb with older browsers will be unable to use dialogs in the web interface.</p><ul><li>The option to view properties for apps and devices will not appear.</li><li>The connection method dialog will not appear. Users will be connected using the default method without being prompted (usually RDP file download).</li><li>Dialogs on the policy page may not open correctly.</li><li>The resource manager functionality may be broken.</li></ul><h2>Window Controls Overlay</h2><p>On chromium-based browsers (e.g., Edge, Chrome), RAWeb can utilize the Window Controls Overlay feature when installed as a Progressive Web App (PWA). This allows RAWeb to integrate its titlebar with the operating system’s titlebar, providing a more seamless user experience.</p><p>To take advantage of this feature:</p><ol><li>Install RAWeb as a PWA by clicking the install icon in the address bar of your browser.</li><li>Launch RAWeb from your desktop or start menu.</li><li>Click the up chevron icon in the titlebar to toggle between integrated and standard titlebar modes.</li></ol><p><em>Note the RAWeb-provided buttons and browser-provided buttons in the shared titlebar:</em></p>',14),t("picture",null,[t("source",{media:"(prefers-color-scheme: dark)",srcset:Mr}),t("source",{media:"(prefers-color-scheme: light)",srcset:Gr}),t("img",{width:"900",src:Pe,alt:"A screenshot of the RAWeb sign in page when anonymous authentication is allowed",border:"1",height:"566",xmlns:"http://www.w3.org/1999/xhtml"})],-1)])))}},jr=Object.freeze(Object.defineProperty({__proto__:null,default:Vr,nav_title:Yr,title:xr},Symbol.toStringTag,{value:"Module"})),_r={class:"markdown-body"},qr="Uninstall RAWeb",Jr={__name:"index",setup(l,{expose:r}){return r({frontmatter:{title:"Uninstall RAWeb"}}),(d,e)=>{const i=p("CodeBlock");return a(),c("div",_r,[e[2]||(e[2]=A("<p><strong>Part 1. Remove from RAWeb virtual application</strong></p><ol><li>Open Internet Information Services (IIS) Manager.</li><li>In the <strong>Connections</strong> pane, expand <strong><em>Your device name</em> &gt; Sites &gt; Default Web Site</strong>.</li><li>Right click <strong>RAWeb</strong> and choose <strong>Remove</strong>.</li><li>In the <strong>Confirm Remove</strong> dialog, choose <strong>Yes</strong>.</li></ol><p><strong>Part 2. Remove installed files</strong></p><ol><li>Open <strong>File Explorer</strong>.</li><li>Navigate to <strong>C:\\inetpub</strong>.</li><li>Delete the <strong>RAWeb</strong> folder.</li></ol><p><strong>Part 3. Remove Internet Information Services Manager</strong></p><p><em>Only perform these steps if you do not have other IIS websites.</em></p><ol><li>Open PowerShell as an administrator <ul><li>Press the Windows key + X, then select PowerShell (Administrator) or Terminal (Administrator).</li></ul></li><li>Copy and paste the code below, then press enter.</li></ol>",7)),t("blockquote",null,[e[0]||(e[0]=t("p",null,"For Windows Server:",-1)),o(i,{code:`Uninstall-WindowsFeature -Name Web-Server, Web-Asp-Net45, Web-Windows-Auth, Web-Http-Redirect, Web-Mgmt-Console, Web-Basic-Auth
`})]),t("blockquote",null,[e[1]||(e[1]=t("p",null,"For other versions of Windows:",-1)),o(i,{code:`Disable-WindowsOptionalFeature -Online -FeatureName IIS-WebServerRole,IIS-WebServer,IIS-CommonHttpFeatures,IIS-HttpErrors,IIS-HttpRedirect,IIS-ApplicationDevelopment,IIS-Security,IIS-RequestFiltering,IIS-NetFxExtensibility45,IIS-HealthAndDiagnostics,IIS-HttpLogging,IIS-Performance,IIS-WebServerManagementTools,IIS-StaticContent,IIS-DefaultDocument,IIS-DirectoryBrowsing,IIS-ASPNET45,IIS-ISAPIExtensions,IIS-ISAPIFilter,IIS-HttpCompressionStatic,IIS-ManagementConsole,IIS-WindowsAuthentication,NetFx4-AdvSrvs,NetFx4Extended-ASPNET45,IIS-BasicAuthentication
`})])])}}},zr=Object.freeze(Object.defineProperty({__proto__:null,default:Jr,title:qr},Symbol.toStringTag,{value:"Module"})),Kr={class:"markdown-body"},Xr="Capabilities and considerations for the web client",$r="Capabilities and considerations",Qr={__name:"index",setup(l,{expose:r}){return r({frontmatter:{title:"Capabilities and considerations for the web client",nav_title:"Capabilities and considerations"}}),(d,e)=>{const i=p("CodeBlock");return a(),c("div",Kr,[e[31]||(e[31]=t("h2",null,"Making connections",-1)),e[32]||(e[32]=t("p",null,[n("Every RDP file contains an address for the terminal server. Normally, users who launch an RDP file must ensure that the terminal server is accessible from their device. However, when using the web client, users connect to the RAWeb server instead of the terminal server. The RAWeb server then forwards the connection to the terminal server on behalf of the user. This means that users can connect to their desktops and applications through the web client even if their devices do not have direct access to the terminal server, as long as they can access the RAWeb server. "),t("strong",null,"Therefore, you must ensure that the RAWeb server has access to the terminal servers that your RDP files reference"),n(".")],-1)),e[33]||(e[33]=t("h2",null,"Clipboard support",-1)),e[34]||(e[34]=t("p",null,"The web client supports synchronizing clipboard data between the client device and the remote desktop. At this time, only the plain text form of the clipboard data will be synchronized. If the clipboard data does not contain a plain text representation, it will not be synchronized. For example, if a user copies an image to their clipboard, that image will not be synchronized to the remote desktop because it does not have a plain text representation. If a user copies formatted text that contains both a plain text and a rich text representation, only the plain text portion will be synchronized to the remote desktop.",-1)),e[35]||(e[35]=t("p",null,[n("Some terminal servers may have an issue where clipboard data will stop synchronizing after a certain number of connections have occurred. If you encounter this issue, you can resolve it by restarting the "),t("code",null,"rdpclip.exe"),n(" process on the terminal server. To do this, sign in to the terminal server, launch a terminal (e.g. Command Prompt or PowerShell), and run the following commands:")],-1)),o(i,{code:`taskkill /IM rdpclip.exe /F
start rdpclip.exe
`}),e[36]||(e[36]=t("h2",null,"Copyright and license requirements",-1)),e[37]||(e[37]=t("p",null,"Web connections depend on guacd.wsl, which is a minimal version of the Guacamole daemon (guacd) built by RAWeb for use in WSL2. The following disclaimers and license reproductions are required for RAWeb to distribute guacd.wsl:",-1)),t("details",null,[e[0]||(e[0]=t("summary",null,"Disclaimers and licenses",-1)),e[1]||(e[1]=t("h3",null,"guacamole-server",-1)),o(i,{code:`Apache Guacamole
Copyright 2020 The Apache Software Foundation

This product includes software developed at
The Apache Software Foundation (http://www.apache.org/).
`}),o(i,{code:`
                                 Apache License
                           Version 2.0, January 2004
                        http://www.apache.org/licenses/

   TERMS AND CONDITIONS FOR USE, REPRODUCTION, AND DISTRIBUTION

   1. Definitions.

      "License" shall mean the terms and conditions for use, reproduction,
      and distribution as defined by Sections 1 through 9 of this document.

      "Licensor" shall mean the copyright owner or entity authorized by
      the copyright owner that is granting the License.

      "Legal Entity" shall mean the union of the acting entity and all
      other entities that control, are controlled by, or are under common
      control with that entity. For the purposes of this definition,
      "control" means (i) the power, direct or indirect, to cause the
      direction or management of such entity, whether by contract or
      otherwise, or (ii) ownership of fifty percent (50%) or more of the
      outstanding shares, or (iii) beneficial ownership of such entity.

      "You" (or "Your") shall mean an individual or Legal Entity
      exercising permissions granted by this License.

      "Source" form shall mean the preferred form for making modifications,
      including but not limited to software source code, documentation
      source, and configuration files.

      "Object" form shall mean any form resulting from mechanical
      transformation or translation of a Source form, including but
      not limited to compiled object code, generated documentation,
      and conversions to other media types.

      "Work" shall mean the work of authorship, whether in Source or
      Object form, made available under the License, as indicated by a
      copyright notice that is included in or attached to the work
      (an example is provided in the Appendix below).

      "Derivative Works" shall mean any work, whether in Source or Object
      form, that is based on (or derived from) the Work and for which the
      editorial revisions, annotations, elaborations, or other modifications
      represent, as a whole, an original work of authorship. For the purposes
      of this License, Derivative Works shall not include works that remain
      separable from, or merely link (or bind by name) to the interfaces of,
      the Work and Derivative Works thereof.

      "Contribution" shall mean any work of authorship, including
      the original version of the Work and any modifications or additions
      to that Work or Derivative Works thereof, that is intentionally
      submitted to Licensor for inclusion in the Work by the copyright owner
      or by an individual or Legal Entity authorized to submit on behalf of
      the copyright owner. For the purposes of this definition, "submitted"
      means any form of electronic, verbal, or written communication sent
      to the Licensor or its representatives, including but not limited to
      communication on electronic mailing lists, source code control systems,
      and issue tracking systems that are managed by, or on behalf of, the
      Licensor for the purpose of discussing and improving the Work, but
      excluding communication that is conspicuously marked or otherwise
      designated in writing by the copyright owner as "Not a Contribution."

      "Contributor" shall mean Licensor and any individual or Legal Entity
      on behalf of whom a Contribution has been received by Licensor and
      subsequently incorporated within the Work.

   2. Grant of Copyright License. Subject to the terms and conditions of
      this License, each Contributor hereby grants to You a perpetual,
      worldwide, non-exclusive, no-charge, royalty-free, irrevocable
      copyright license to reproduce, prepare Derivative Works of,
      publicly display, publicly perform, sublicense, and distribute the
      Work and such Derivative Works in Source or Object form.

   3. Grant of Patent License. Subject to the terms and conditions of
      this License, each Contributor hereby grants to You a perpetual,
      worldwide, non-exclusive, no-charge, royalty-free, irrevocable
      (except as stated in this section) patent license to make, have made,
      use, offer to sell, sell, import, and otherwise transfer the Work,
      where such license applies only to those patent claims licensable
      by such Contributor that are necessarily infringed by their
      Contribution(s) alone or by combination of their Contribution(s)
      with the Work to which such Contribution(s) was submitted. If You
      institute patent litigation against any entity (including a
      cross-claim or counterclaim in a lawsuit) alleging that the Work
      or a Contribution incorporated within the Work constitutes direct
      or contributory patent infringement, then any patent licenses
      granted to You under this License for that Work shall terminate
      as of the date such litigation is filed.

   4. Redistribution. You may reproduce and distribute copies of the
      Work or Derivative Works thereof in any medium, with or without
      modifications, and in Source or Object form, provided that You
      meet the following conditions:

      (a) You must give any other recipients of the Work or
          Derivative Works a copy of this License; and

      (b) You must cause any modified files to carry prominent notices
          stating that You changed the files; and

      (c) You must retain, in the Source form of any Derivative Works
          that You distribute, all copyright, patent, trademark, and
          attribution notices from the Source form of the Work,
          excluding those notices that do not pertain to any part of
          the Derivative Works; and

      (d) If the Work includes a "NOTICE" text file as part of its
          distribution, then any Derivative Works that You distribute must
          include a readable copy of the attribution notices contained
          within such NOTICE file, excluding those notices that do not
          pertain to any part of the Derivative Works, in at least one
          of the following places: within a NOTICE text file distributed
          as part of the Derivative Works; within the Source form or
          documentation, if provided along with the Derivative Works; or,
          within a display generated by the Derivative Works, if and
          wherever such third-party notices normally appear. The contents
          of the NOTICE file are for informational purposes only and
          do not modify the License. You may add Your own attribution
          notices within Derivative Works that You distribute, alongside
          or as an addendum to the NOTICE text from the Work, provided
          that such additional attribution notices cannot be construed
          as modifying the License.

      You may add Your own copyright statement to Your modifications and
      may provide additional or different license terms and conditions
      for use, reproduction, or distribution of Your modifications, or
      for any such Derivative Works as a whole, provided Your use,
      reproduction, and distribution of the Work otherwise complies with
      the conditions stated in this License.

   5. Submission of Contributions. Unless You explicitly state otherwise,
      any Contribution intentionally submitted for inclusion in the Work
      by You to the Licensor shall be under the terms and conditions of
      this License, without any additional terms or conditions.
      Notwithstanding the above, nothing herein shall supersede or modify
      the terms of any separate license agreement you may have executed
      with Licensor regarding such Contributions.

   6. Trademarks. This License does not grant permission to use the trade
      names, trademarks, service marks, or product names of the Licensor,
      except as required for reasonable and customary use in describing the
      origin of the Work and reproducing the content of the NOTICE file.

   7. Disclaimer of Warranty. Unless required by applicable law or
      agreed to in writing, Licensor provides the Work (and each
      Contributor provides its Contributions) on an "AS IS" BASIS,
      WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or
      implied, including, without limitation, any warranties or conditions
      of TITLE, NON-INFRINGEMENT, MERCHANTABILITY, or FITNESS FOR A
      PARTICULAR PURPOSE. You are solely responsible for determining the
      appropriateness of using or redistributing the Work and assume any
      risks associated with Your exercise of permissions under this License.

   8. Limitation of Liability. In no event and under no legal theory,
      whether in tort (including negligence), contract, or otherwise,
      unless required by applicable law (such as deliberate and grossly
      negligent acts) or agreed to in writing, shall any Contributor be
      liable to You for damages, including any direct, indirect, special,
      incidental, or consequential damages of any character arising as a
      result of this License or out of the use or inability to use the
      Work (including but not limited to damages for loss of goodwill,
      work stoppage, computer failure or malfunction, or any and all
      other commercial damages or losses), even if such Contributor
      has been advised of the possibility of such damages.

   9. Accepting Warranty or Additional Liability. While redistributing
      the Work or Derivative Works thereof, You may choose to offer,
      and charge a fee for, acceptance of support, warranty, indemnity,
      or other liability obligations and/or rights consistent with this
      License. However, in accepting such obligations, You may act only
      on Your own behalf and on Your sole responsibility, not on behalf
      of any other Contributor, and only if You agree to indemnify,
      defend, and hold each Contributor harmless for any liability
      incurred by, or claims asserted against, such Contributor by reason
      of your accepting any such warranty or additional liability.

   END OF TERMS AND CONDITIONS

   APPENDIX: How to apply the Apache License to your work.

      To apply the Apache License to your work, attach the following
      boilerplate notice, with the fields enclosed by brackets "[]"
      replaced with your own identifying information. (Don't include
      the brackets!)  The text should be enclosed in the appropriate
      comment syntax for the file format. We also recommend that a
      file or class name and description of purpose be included on the
      same "printed page" as the copyright notice for easier
      identification within third-party archives.

   Copyright [yyyy] [name of copyright owner]

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
`}),e[2]||(e[2]=t("h4",null,"brotli-libs",-1)),o(i,{code:`Copyright (c) 2009, 2010, 2013-2016 by the Brotli Authors.

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.  IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
`}),e[3]||(e[3]=t("h4",null,"cairo",-1)),o(i,{code:`Cairo is free software.

Every source file in the implementation[*] of cairo is available to be
redistributed and/or modified under the terms of either the GNU Lesser
General Public License (LGPL) version 2.1 or the Mozilla Public
License (MPL) version 1.1.  Some files are available under more
liberal terms, but we believe that in all cases, each file may be used
under either the LGPL or the MPL.

See the following files in this directory for the precise terms and
conditions of either license:

	COPYING-LGPL-2.1
	COPYING-MPL-1.1

Please see each file in the implementation for copyright and licensing
information, (in the opening comment of each file).

[*] The implementation of cairo is contained entirely within the "src"
directory of the cairo source distribution. There are other components
of the cairo source distribution (such as the "test", "util", and "perf")
that are auxiliary to the library itself. None of the source code in these
directories contributes to a build of the cairo library itself, (libcairo.so
or cairo.dll or similar).

These auxiliary components are also free software, but may be under
different license terms than cairo itself. For example, most of the
test cases in the perf and test directories are made available under
an MIT license to simplify any use of this code for reference purposes
in using cairo itself. Other files might be available under the GNU
General Public License (GPL), for example. Again, please see the COPYING
file under each directory and the opening comment of each file for copyright
and licensing information.
`}),o(i,{code:`
                  GNU LESSER GENERAL PUBLIC LICENSE
                       Version 2.1, February 1999

 Copyright (C) 1991, 1999 Free Software Foundation, Inc.
     51 Franklin Street, Suite 500, Boston, MA 02110-1335, USA
 Everyone is permitted to copy and distribute verbatim copies
 of this license document, but changing it is not allowed.

[This is the first released version of the Lesser GPL.  It also counts
 as the successor of the GNU Library Public License, version 2, hence
 the version number 2.1.]

                            Preamble

  The licenses for most software are designed to take away your
freedom to share and change it.  By contrast, the GNU General Public
Licenses are intended to guarantee your freedom to share and change
free software--to make sure the software is free for all its users.

  This license, the Lesser General Public License, applies to some
specially designated software packages--typically libraries--of the
Free Software Foundation and other authors who decide to use it.  You
can use it too, but we suggest you first think carefully about whether
this license or the ordinary General Public License is the better
strategy to use in any particular case, based on the explanations
below.

  When we speak of free software, we are referring to freedom of use,
not price.  Our General Public Licenses are designed to make sure that
you have the freedom to distribute copies of free software (and charge
for this service if you wish); that you receive source code or can get
it if you want it; that you can change the software and use pieces of
it in new free programs; and that you are informed that you can do
these things.

  To protect your rights, we need to make restrictions that forbid
distributors to deny you these rights or to ask you to surrender these
rights.  These restrictions translate to certain responsibilities for
you if you distribute copies of the library or if you modify it.

  For example, if you distribute copies of the library, whether gratis
or for a fee, you must give the recipients all the rights that we gave
you.  You must make sure that they, too, receive or can get the source
code.  If you link other code with the library, you must provide
complete object files to the recipients, so that they can relink them
with the library after making changes to the library and recompiling
it.  And you must show them these terms so they know their rights.

  We protect your rights with a two-step method: (1) we copyright the
library, and (2) we offer you this license, which gives you legal
permission to copy, distribute and/or modify the library.

  To protect each distributor, we want to make it very clear that
there is no warranty for the free library.  Also, if the library is
modified by someone else and passed on, the recipients should know
that what they have is not the original version, so that the original
author's reputation will not be affected by problems that might be
introduced by others.
\f
  Finally, software patents pose a constant threat to the existence of
any free program.  We wish to make sure that a company cannot
effectively restrict the users of a free program by obtaining a
restrictive license from a patent holder.  Therefore, we insist that
any patent license obtained for a version of the library must be
consistent with the full freedom of use specified in this license.

  Most GNU software, including some libraries, is covered by the
ordinary GNU General Public License.  This license, the GNU Lesser
General Public License, applies to certain designated libraries, and
is quite different from the ordinary General Public License.  We use
this license for certain libraries in order to permit linking those
libraries into non-free programs.

  When a program is linked with a library, whether statically or using
a shared library, the combination of the two is legally speaking a
combined work, a derivative of the original library.  The ordinary
General Public License therefore permits such linking only if the
entire combination fits its criteria of freedom.  The Lesser General
Public License permits more lax criteria for linking other code with
the library.

  We call this license the "Lesser" General Public License because it
does Less to protect the user's freedom than the ordinary General
Public License.  It also provides other free software developers Less
of an advantage over competing non-free programs.  These disadvantages
are the reason we use the ordinary General Public License for many
libraries.  However, the Lesser license provides advantages in certain
special circumstances.

  For example, on rare occasions, there may be a special need to
encourage the widest possible use of a certain library, so that it
becomes a de-facto standard.  To achieve this, non-free programs must
be allowed to use the library.  A more frequent case is that a free
library does the same job as widely used non-free libraries.  In this
case, there is little to gain by limiting the free library to free
software only, so we use the Lesser General Public License.

  In other cases, permission to use a particular library in non-free
programs enables a greater number of people to use a large body of
free software.  For example, permission to use the GNU C Library in
non-free programs enables many more people to use the whole GNU
operating system, as well as its variant, the GNU/Linux operating
system.

  Although the Lesser General Public License is Less protective of the
users' freedom, it does ensure that the user of a program that is
linked with the Library has the freedom and the wherewithal to run
that program using a modified version of the Library.

  The precise terms and conditions for copying, distribution and
modification follow.  Pay close attention to the difference between a
"work based on the library" and a "work that uses the library".  The
former contains code derived from the library, whereas the latter must
be combined with the library in order to run.
\f
                  GNU LESSER GENERAL PUBLIC LICENSE
   TERMS AND CONDITIONS FOR COPYING, DISTRIBUTION AND MODIFICATION

  0. This License Agreement applies to any software library or other
program which contains a notice placed by the copyright holder or
other authorized party saying it may be distributed under the terms of
this Lesser General Public License (also called "this License").
Each licensee is addressed as "you".

  A "library" means a collection of software functions and/or data
prepared so as to be conveniently linked with application programs
(which use some of those functions and data) to form executables.

  The "Library", below, refers to any such software library or work
which has been distributed under these terms.  A "work based on the
Library" means either the Library or any derivative work under
copyright law: that is to say, a work containing the Library or a
portion of it, either verbatim or with modifications and/or translated
straightforwardly into another language.  (Hereinafter, translation is
included without limitation in the term "modification".)

  "Source code" for a work means the preferred form of the work for
making modifications to it.  For a library, complete source code means
all the source code for all modules it contains, plus any associated
interface definition files, plus the scripts used to control
compilation and installation of the library.

  Activities other than copying, distribution and modification are not
covered by this License; they are outside its scope.  The act of
running a program using the Library is not restricted, and output from
such a program is covered only if its contents constitute a work based
on the Library (independent of the use of the Library in a tool for
writing it).  Whether that is true depends on what the Library does
and what the program that uses the Library does.

  1. You may copy and distribute verbatim copies of the Library's
complete source code as you receive it, in any medium, provided that
you conspicuously and appropriately publish on each copy an
appropriate copyright notice and disclaimer of warranty; keep intact
all the notices that refer to this License and to the absence of any
warranty; and distribute a copy of this License along with the
Library.

  You may charge a fee for the physical act of transferring a copy,
and you may at your option offer warranty protection in exchange for a
fee.
\f
  2. You may modify your copy or copies of the Library or any portion
of it, thus forming a work based on the Library, and copy and
distribute such modifications or work under the terms of Section 1
above, provided that you also meet all of these conditions:

    a) The modified work must itself be a software library.

    b) You must cause the files modified to carry prominent notices
    stating that you changed the files and the date of any change.

    c) You must cause the whole of the work to be licensed at no
    charge to all third parties under the terms of this License.

    d) If a facility in the modified Library refers to a function or a
    table of data to be supplied by an application program that uses
    the facility, other than as an argument passed when the facility
    is invoked, then you must make a good faith effort to ensure that,
    in the event an application does not supply such function or
    table, the facility still operates, and performs whatever part of
    its purpose remains meaningful.

    (For example, a function in a library to compute square roots has
    a purpose that is entirely well-defined independent of the
    application.  Therefore, Subsection 2d requires that any
    application-supplied function or table used by this function must
    be optional: if the application does not supply it, the square
    root function must still compute square roots.)

These requirements apply to the modified work as a whole.  If
identifiable sections of that work are not derived from the Library,
and can be reasonably considered independent and separate works in
themselves, then this License, and its terms, do not apply to those
sections when you distribute them as separate works.  But when you
distribute the same sections as part of a whole which is a work based
on the Library, the distribution of the whole must be on the terms of
this License, whose permissions for other licensees extend to the
entire whole, and thus to each and every part regardless of who wrote
it.

Thus, it is not the intent of this section to claim rights or contest
your rights to work written entirely by you; rather, the intent is to
exercise the right to control the distribution of derivative or
collective works based on the Library.

In addition, mere aggregation of another work not based on the Library
with the Library (or with a work based on the Library) on a volume of
a storage or distribution medium does not bring the other work under
the scope of this License.

  3. You may opt to apply the terms of the ordinary GNU General Public
License instead of this License to a given copy of the Library.  To do
this, you must alter all the notices that refer to this License, so
that they refer to the ordinary GNU General Public License, version 2,
instead of to this License.  (If a newer version than version 2 of the
ordinary GNU General Public License has appeared, then you can specify
that version instead if you wish.)  Do not make any other change in
these notices.
\f
  Once this change is made in a given copy, it is irreversible for
that copy, so the ordinary GNU General Public License applies to all
subsequent copies and derivative works made from that copy.

  This option is useful when you wish to copy part of the code of
the Library into a program that is not a library.

  4. You may copy and distribute the Library (or a portion or
derivative of it, under Section 2) in object code or executable form
under the terms of Sections 1 and 2 above provided that you accompany
it with the complete corresponding machine-readable source code, which
must be distributed under the terms of Sections 1 and 2 above on a
medium customarily used for software interchange.

  If distribution of object code is made by offering access to copy
from a designated place, then offering equivalent access to copy the
source code from the same place satisfies the requirement to
distribute the source code, even though third parties are not
compelled to copy the source along with the object code.

  5. A program that contains no derivative of any portion of the
Library, but is designed to work with the Library by being compiled or
linked with it, is called a "work that uses the Library".  Such a
work, in isolation, is not a derivative work of the Library, and
therefore falls outside the scope of this License.

  However, linking a "work that uses the Library" with the Library
creates an executable that is a derivative of the Library (because it
contains portions of the Library), rather than a "work that uses the
library".  The executable is therefore covered by this License.
Section 6 states terms for distribution of such executables.

  When a "work that uses the Library" uses material from a header file
that is part of the Library, the object code for the work may be a
derivative work of the Library even though the source code is not.
Whether this is true is especially significant if the work can be
linked without the Library, or if the work is itself a library.  The
threshold for this to be true is not precisely defined by law.

  If such an object file uses only numerical parameters, data
structure layouts and accessors, and small macros and small inline
functions (ten lines or less in length), then the use of the object
file is unrestricted, regardless of whether it is legally a derivative
work.  (Executables containing this object code plus portions of the
Library will still fall under Section 6.)

  Otherwise, if the work is a derivative of the Library, you may
distribute the object code for the work under the terms of Section 6.
Any executables containing that work also fall under Section 6,
whether or not they are linked directly with the Library itself.
\f
  6. As an exception to the Sections above, you may also combine or
link a "work that uses the Library" with the Library to produce a
work containing portions of the Library, and distribute that work
under terms of your choice, provided that the terms permit
modification of the work for the customer's own use and reverse
engineering for debugging such modifications.

  You must give prominent notice with each copy of the work that the
Library is used in it and that the Library and its use are covered by
this License.  You must supply a copy of this License.  If the work
during execution displays copyright notices, you must include the
copyright notice for the Library among them, as well as a reference
directing the user to the copy of this License.  Also, you must do one
of these things:

    a) Accompany the work with the complete corresponding
    machine-readable source code for the Library including whatever
    changes were used in the work (which must be distributed under
    Sections 1 and 2 above); and, if the work is an executable linked
    with the Library, with the complete machine-readable "work that
    uses the Library", as object code and/or source code, so that the
    user can modify the Library and then relink to produce a modified
    executable containing the modified Library.  (It is understood
    that the user who changes the contents of definitions files in the
    Library will not necessarily be able to recompile the application
    to use the modified definitions.)

    b) Use a suitable shared library mechanism for linking with the
    Library.  A suitable mechanism is one that (1) uses at run time a
    copy of the library already present on the user's computer system,
    rather than copying library functions into the executable, and (2)
    will operate properly with a modified version of the library, if
    the user installs one, as long as the modified version is
    interface-compatible with the version that the work was made with.

    c) Accompany the work with a written offer, valid for at least
    three years, to give the same user the materials specified in
    Subsection 6a, above, for a charge no more than the cost of
    performing this distribution.

    d) If distribution of the work is made by offering access to copy
    from a designated place, offer equivalent access to copy the above
    specified materials from the same place.

    e) Verify that the user has already received a copy of these
    materials or that you have already sent this user a copy.

  For an executable, the required form of the "work that uses the
Library" must include any data and utility programs needed for
reproducing the executable from it.  However, as a special exception,
the materials to be distributed need not include anything that is
normally distributed (in either source or binary form) with the major
components (compiler, kernel, and so on) of the operating system on
which the executable runs, unless that component itself accompanies
the executable.

  It may happen that this requirement contradicts the license
restrictions of other proprietary libraries that do not normally
accompany the operating system.  Such a contradiction means you cannot
use both them and the Library together in an executable that you
distribute.
\f
  7. You may place library facilities that are a work based on the
Library side-by-side in a single library together with other library
facilities not covered by this License, and distribute such a combined
library, provided that the separate distribution of the work based on
the Library and of the other library facilities is otherwise
permitted, and provided that you do these two things:

    a) Accompany the combined library with a copy of the same work
    based on the Library, uncombined with any other library
    facilities.  This must be distributed under the terms of the
    Sections above.

    b) Give prominent notice with the combined library of the fact
    that part of it is a work based on the Library, and explaining
    where to find the accompanying uncombined form of the same work.

  8. You may not copy, modify, sublicense, link with, or distribute
the Library except as expressly provided under this License.  Any
attempt otherwise to copy, modify, sublicense, link with, or
distribute the Library is void, and will automatically terminate your
rights under this License.  However, parties who have received copies,
or rights, from you under this License will not have their licenses
terminated so long as such parties remain in full compliance.

  9. You are not required to accept this License, since you have not
signed it.  However, nothing else grants you permission to modify or
distribute the Library or its derivative works.  These actions are
prohibited by law if you do not accept this License.  Therefore, by
modifying or distributing the Library (or any work based on the
Library), you indicate your acceptance of this License to do so, and
all its terms and conditions for copying, distributing or modifying
the Library or works based on it.

  10. Each time you redistribute the Library (or any work based on the
Library), the recipient automatically receives a license from the
original licensor to copy, distribute, link with or modify the Library
subject to these terms and conditions.  You may not impose any further
restrictions on the recipients' exercise of the rights granted herein.
You are not responsible for enforcing compliance by third parties with
this License.
\f
  11. If, as a consequence of a court judgment or allegation of patent
infringement or for any other reason (not limited to patent issues),
conditions are imposed on you (whether by court order, agreement or
otherwise) that contradict the conditions of this License, they do not
excuse you from the conditions of this License.  If you cannot
distribute so as to satisfy simultaneously your obligations under this
License and any other pertinent obligations, then as a consequence you
may not distribute the Library at all.  For example, if a patent
license would not permit royalty-free redistribution of the Library by
all those who receive copies directly or indirectly through you, then
the only way you could satisfy both it and this License would be to
refrain entirely from distribution of the Library.

If any portion of this section is held invalid or unenforceable under
any particular circumstance, the balance of the section is intended to
apply, and the section as a whole is intended to apply in other
circumstances.

It is not the purpose of this section to induce you to infringe any
patents or other property right claims or to contest validity of any
such claims; this section has the sole purpose of protecting the
integrity of the free software distribution system which is
implemented by public license practices.  Many people have made
generous contributions to the wide range of software distributed
through that system in reliance on consistent application of that
system; it is up to the author/donor to decide if he or she is willing
to distribute software through any other system and a licensee cannot
impose that choice.

This section is intended to make thoroughly clear what is believed to
be a consequence of the rest of this License.

  12. If the distribution and/or use of the Library is restricted in
certain countries either by patents or by copyrighted interfaces, the
original copyright holder who places the Library under this License
may add an explicit geographical distribution limitation excluding those
countries, so that distribution is permitted only in or among
countries not thus excluded.  In such case, this License incorporates
the limitation as if written in the body of this License.

  13. The Free Software Foundation may publish revised and/or new
versions of the Lesser General Public License from time to time.
Such new versions will be similar in spirit to the present version,
but may differ in detail to address new problems or concerns.

Each version is given a distinguishing version number.  If the Library
specifies a version number of this License which applies to it and
"any later version", you have the option of following the terms and
conditions either of that version or of any later version published by
the Free Software Foundation.  If the Library does not specify a
license version number, you may choose any version ever published by
the Free Software Foundation.
\f
  14. If you wish to incorporate parts of the Library into other free
programs whose distribution conditions are incompatible with these,
write to the author to ask for permission.  For software which is
copyrighted by the Free Software Foundation, write to the Free
Software Foundation; we sometimes make exceptions for this.  Our
decision will be guided by the two goals of preserving the free status
of all derivatives of our free software and of promoting the sharing
and reuse of software generally.

                            NO WARRANTY

  15. BECAUSE THE LIBRARY IS LICENSED FREE OF CHARGE, THERE IS NO
WARRANTY FOR THE LIBRARY, TO THE EXTENT PERMITTED BY APPLICABLE LAW.
EXCEPT WHEN OTHERWISE STATED IN WRITING THE COPYRIGHT HOLDERS AND/OR
OTHER PARTIES PROVIDE THE LIBRARY "AS IS" WITHOUT WARRANTY OF ANY
KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING, BUT NOT LIMITED TO, THE
IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR
PURPOSE.  THE ENTIRE RISK AS TO THE QUALITY AND PERFORMANCE OF THE
LIBRARY IS WITH YOU.  SHOULD THE LIBRARY PROVE DEFECTIVE, YOU ASSUME
THE COST OF ALL NECESSARY SERVICING, REPAIR OR CORRECTION.

  16. IN NO EVENT UNLESS REQUIRED BY APPLICABLE LAW OR AGREED TO IN
WRITING WILL ANY COPYRIGHT HOLDER, OR ANY OTHER PARTY WHO MAY MODIFY
AND/OR REDISTRIBUTE THE LIBRARY AS PERMITTED ABOVE, BE LIABLE TO YOU
FOR DAMAGES, INCLUDING ANY GENERAL, SPECIAL, INCIDENTAL OR
CONSEQUENTIAL DAMAGES ARISING OUT OF THE USE OR INABILITY TO USE THE
LIBRARY (INCLUDING BUT NOT LIMITED TO LOSS OF DATA OR DATA BEING
RENDERED INACCURATE OR LOSSES SUSTAINED BY YOU OR THIRD PARTIES OR A
FAILURE OF THE LIBRARY TO OPERATE WITH ANY OTHER SOFTWARE), EVEN IF
SUCH HOLDER OR OTHER PARTY HAS BEEN ADVISED OF THE POSSIBILITY OF SUCH
DAMAGES.

                     END OF TERMS AND CONDITIONS
\f
           How to Apply These Terms to Your New Libraries

  If you develop a new library, and you want it to be of the greatest
possible use to the public, we recommend making it free software that
everyone can redistribute and change.  You can do so by permitting
redistribution under these terms (or, alternatively, under the terms
of the ordinary General Public License).

  To apply these terms, attach the following notices to the library.
It is safest to attach them to the start of each source file to most
effectively convey the exclusion of warranty; and each file should
have at least the "copyright" line and a pointer to where the full
notice is found.


    <one line to give the library's name and a brief idea of what it does.>
    Copyright (C) <year>  <name of author>

    This library is free software; you can redistribute it and/or
    modify it under the terms of the GNU Lesser General Public
    License as published by the Free Software Foundation; either
    version 2.1 of the License, or (at your option) any later version.

    This library is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
    Lesser General Public License for more details.

    You should have received a copy of the GNU Lesser General Public
    License along with this library; if not, write to the Free Software
    Foundation, Inc., 51 Franklin Street, Suite 500, Boston, MA 02110-1335, USA

Also add information on how to contact you by electronic and paper mail.

You should also get your employer (if you work as a programmer) or
your school, if any, to sign a "copyright disclaimer" for the library,
if necessary.  Here is a sample; alter the names:

  Yoyodyne, Inc., hereby disclaims all copyright interest in the
  library \`Frob' (a library for tweaking knobs) written by James
  Random Hacker.

  <signature of Ty Coon>, 1 April 1990
  Ty Coon, President of Vice

That's all there is to it!
`}),e[4]||(e[4]=t("h4",null,"fontconfig",-1)),o(i,{code:`fontconfig/COPYING

Copyright © 2000,2001,2002,2003,2004,2006,2007 Keith Packard
Copyright © 2005 Patrick Lam
Copyright © 2007 Dwayne Bailey and Translate.org.za
Copyright © 2009 Roozbeh Pournader
Copyright © 2008,2009,2010,2011,2012,2013,2014,2015,2016,2017,2018,2019,2020 Red Hat, Inc.
Copyright © 2008 Danilo Šegan
Copyright © 2012 Google, Inc.


Permission to use, copy, modify, distribute, and sell this software and its
documentation for any purpose is hereby granted without fee, provided that
the above copyright notice appear in all copies and that both that
copyright notice and this permission notice appear in supporting
documentation, and that the name of the author(s) not be used in
advertising or publicity pertaining to distribution of the software without
specific, written prior permission.  The authors make no
representations about the suitability of this software for any purpose.  It
is provided "as is" without express or implied warranty.

THE AUTHOR(S) DISCLAIMS ALL WARRANTIES WITH REGARD TO THIS SOFTWARE,
INCLUDING ALL IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS, IN NO
EVENT SHALL THE AUTHOR(S) BE LIABLE FOR ANY SPECIAL, INDIRECT OR
CONSEQUENTIAL DAMAGES OR ANY DAMAGES WHATSOEVER RESULTING FROM LOSS OF USE,
DATA OR PROFITS, WHETHER IN AN ACTION OF CONTRACT, NEGLIGENCE OR OTHER
TORTIOUS ACTION, ARISING OUT OF OR IN CONNECTION WITH THE USE OR
PERFORMANCE OF THIS SOFTWARE.


--------------------------------------------------------------------------------
fontconfig/fc-case/CaseFolding.txt

© 2019 Unicode®, Inc.
Unicode and the Unicode Logo are registered trademarks of Unicode, Inc. in the U.S. and other countries.
For terms of use, see http://www.unicode.org/terms_of_use.html


--------------------------------------------------------------------------------
fontconfig/src/fcatomic.h

/*
 * Mutex operations.  Originally copied from HarfBuzz.
 *
 * Copyright © 2007  Chris Wilson
 * Copyright © 2009,2010  Red Hat, Inc.
 * Copyright © 2011,2012,2013  Google, Inc.
 *
 * Permission is hereby granted, without written agreement and without
 * license or royalty fees, to use, copy, modify, and distribute this
 * software and its documentation for any purpose, provided that the
 * above copyright notice and the following two paragraphs appear in
 * all copies of this software.
 *
 * IN NO EVENT SHALL THE COPYRIGHT HOLDER BE LIABLE TO ANY PARTY FOR
 * DIRECT, INDIRECT, SPECIAL, INCIDENTAL, OR CONSEQUENTIAL DAMAGES
 * ARISING OUT OF THE USE OF THIS SOFTWARE AND ITS DOCUMENTATION, EVEN
 * IF THE COPYRIGHT HOLDER HAS BEEN ADVISED OF THE POSSIBILITY OF SUCH
 * DAMAGE.
 *
 * THE COPYRIGHT HOLDER SPECIFICALLY DISCLAIMS ANY WARRANTIES, INCLUDING,
 * BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND
 * FITNESS FOR A PARTICULAR PURPOSE.  THE SOFTWARE PROVIDED HEREUNDER IS
 * ON AN "AS IS" BASIS, AND THE COPYRIGHT HOLDER HAS NO OBLIGATION TO
 * PROVIDE MAINTENANCE, SUPPORT, UPDATES, ENHANCEMENTS, OR MODIFICATIONS.
 *
 * Contributor(s):
 *	Chris Wilson <chris@chris-wilson.co.uk>
 * Red Hat Author(s): Behdad Esfahbod
 * Google Author(s): Behdad Esfahbod
 */


--------------------------------------------------------------------------------
fontconfig/src/fcfoundry.h

/*
  Copyright © 2002-2003 by Juliusz Chroboczek

  Permission is hereby granted, free of charge, to any person obtaining a copy
  of this software and associated documentation files (the "Software"), to deal
  in the Software without restriction, including without limitation the rights
  to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
  copies of the Software, and to permit persons to whom the Software is
  furnished to do so, subject to the following conditions:

  The above copyright notice and this permission notice shall be included in
  all copies or substantial portions of the Software.

  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.  IN NO EVENT SHALL THE
  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
  THE SOFTWARE.
*/


--------------------------------------------------------------------------------
fontconfig/src/fcmd5.h

/*
 * This code implements the MD5 message-digest algorithm.
 * The algorithm is due to Ron Rivest.  This code was
 * written by Colin Plumb in 1993, no copyright is claimed.
 * This code is in the public domain; do with it what you wish.
 *
 * Equivalent code is available from RSA Data Security, Inc.
 * This code has been tested against that, and is equivalent,
 * except that you don't need to include two pages of legalese
 * with every copy.
 *
 * To compute the message digest of a chunk of bytes, declare an
 * MD5Context structure, pass it to MD5Init, call MD5Update as
 * needed on buffers full of bytes, and then call MD5Final, which
 * will fill a supplied 16-byte array with the digest.
 */


--------------------------------------------------------------------------------
fontconfig/src/fcmutex.h

/*
 * Atomic int and pointer operations.  Originally copied from HarfBuzz.
 *
 * Copyright © 2007  Chris Wilson
 * Copyright © 2009,2010  Red Hat, Inc.
 * Copyright © 2011,2012,2013  Google, Inc.
 *
 * Permission is hereby granted, without written agreement and without
 * license or royalty fees, to use, copy, modify, and distribute this
 * software and its documentation for any purpose, provided that the
 * above copyright notice and the following two paragraphs appear in
 * all copies of this software.
 *
 * IN NO EVENT SHALL THE COPYRIGHT HOLDER BE LIABLE TO ANY PARTY FOR
 * DIRECT, INDIRECT, SPECIAL, INCIDENTAL, OR CONSEQUENTIAL DAMAGES
 * ARISING OUT OF THE USE OF THIS SOFTWARE AND ITS DOCUMENTATION, EVEN
 * IF THE COPYRIGHT HOLDER HAS BEEN ADVISED OF THE POSSIBILITY OF SUCH
 * DAMAGE.
 *
 * THE COPYRIGHT HOLDER SPECIFICALLY DISCLAIMS ANY WARRANTIES, INCLUDING,
 * BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND
 * FITNESS FOR A PARTICULAR PURPOSE.  THE SOFTWARE PROVIDED HEREUNDER IS
 * ON AN "AS IS" BASIS, AND THE COPYRIGHT HOLDER HAS NO OBLIGATION TO
 * PROVIDE MAINTENANCE, SUPPORT, UPDATES, ENHANCEMENTS, OR MODIFICATIONS.
 *
 * Contributor(s):
 *	Chris Wilson <chris@chris-wilson.co.uk>
 * Red Hat Author(s): Behdad Esfahbod
 * Google Author(s): Behdad Esfahbod
 */


--------------------------------------------------------------------------------
fontconfig/src/ftglue.[ch]

/* ftglue.c: Glue code for compiling the OpenType code from
 *           FreeType 1 using only the public API of FreeType 2
 *
 * By David Turner, The FreeType Project (www.freetype.org)
 *
 * This code is explicitely put in the public domain
 *
 * ==========================================================================
 *
 * the OpenType parser codes was originally written as an extension to
 * FreeType 1.x. As such, its source code was embedded within the library,
 * and used many internal FreeType functions to deal with memory and
 * stream i/o.
 *
 * When it was 'salvaged' for Pango and Qt, the code was "ported" to FreeType 2,
 * which basically means that some macro tricks were performed in order to
 * directly access FT2 _internal_ functions.
 *
 * these functions were never part of FT2 public API, and _did_ change between
 * various releases. This created chaos for many users: when they upgraded the
 * FreeType library on their system, they couldn't run Gnome anymore since
 * Pango refused to link.
 *
 * Very fortunately, it's possible to completely avoid this problem because
 * the FT_StreamRec and FT_MemoryRec structure types, which describe how
 * memory and stream implementations interface with the rest of the font
 * library, have always been part of the public API, and never changed.
 *
 * What we do thus is re-implement, within the OpenType parser, the few
 * functions that depend on them. This only adds one or two kilobytes of
 * code, and ensures that the parser can work with _any_ version
 * of FreeType installed on your system. How sweet... !
 *
 * Note that we assume that Pango doesn't use any other internal functions
 * from FreeType. It used to in old versions, but this should no longer
 * be the case. (crossing my fingers).
 *
 *  - David Turner
 *  - The FreeType Project  (www.freetype.org)
 *
 * PS: This "glue" code is explicitely put in the public domain
 */
`}),e[5]||(e[5]=t("h4",null,"freetype",-1)),o(i,{code:`FREETYPE LICENSES
-----------------

The FreeType  2 font  engine is  copyrighted work  and cannot  be used
legally without  a software  license.  In order  to make  this project
usable to  a vast majority of  developers, we distribute it  under two
mutually exclusive open-source licenses.

This means that *you* must choose  *one* of the two licenses described
below, then obey all its terms and conditions when using FreeType 2 in
any of your projects or products.

  - The FreeType License,  found in the file  \`docs/FTL.TXT\`, which is
    similar to the  original BSD license *with*  an advertising clause
    that forces  you to explicitly  cite the FreeType project  in your
    product's  documentation.  All  details are  in the  license file.
    This license is suited to products which don't use the GNU General
    Public License.

    Note that  this license  is compatible to  the GNU  General Public
    License version 3, but not version 2.

  - The   GNU   General   Public   License   version   2,   found   in
    \`docs/GPLv2.TXT\`  (any  later  version  can  be  used  also),  for
    programs  which  already  use  the  GPL.  Note  that  the  FTL  is
    incompatible with GPLv2 due to its advertisement clause.

The contributed  BDF and PCF  drivers come  with a license  similar to
that  of the  X Window  System.   It is  compatible to  the above  two
licenses (see files \`src/bdf/README\`  and \`src/pcf/README\`).  The same
holds   for   the   source    code   files   \`src/base/fthash.c\`   and
\`include/freetype/internal/fthash.h\`; they were part of the BDF driver
in earlier FreeType versions.

The gzip  module uses the  zlib license (see  \`src/gzip/zlib.h\`) which
too is compatible to the above two licenses.

The   files   \`src/autofit/ft-hb-ft.c\`,   \`src/autofit/ft-hb-decls.h\`,
\`src/autofit/ft-hb-types.h\`,     and    \`src/autofit/hb-script-list.h\`
contain code taken (almost) verbatim  from the HarfBuzz library, which
uses the 'Old MIT' license compatible to the above two licenses.

The  MD5 checksum  support  (only used  for  debugging in  development
builds) is in the public domain.


--- end of LICENSE.TXT ---
`}),o(i,{code:`                    The FreeType Project LICENSE
                    ----------------------------

                            2006-Jan-27

                    Copyright 1996-2002, 2006 by
          David Turner, Robert Wilhelm, and Werner Lemberg



Introduction
============

  The FreeType  Project is distributed in  several archive packages;
  some of them may contain, in addition to the FreeType font engine,
  various tools and  contributions which rely on, or  relate to, the
  FreeType Project.

  This  license applies  to all  files found  in such  packages, and
  which do not  fall under their own explicit  license.  The license
  affects  thus  the  FreeType   font  engine,  the  test  programs,
  documentation and makefiles, at the very least.

  This  license   was  inspired  by  the  BSD,   Artistic,  and  IJG
  (Independent JPEG  Group) licenses, which  all encourage inclusion
  and  use of  free  software in  commercial  and freeware  products
  alike.  As a consequence, its main points are that:

    o We don't promise that this software works. However, we will be
      interested in any kind of bug reports. (\`as is' distribution)

    o You can  use this software for whatever you  want, in parts or
      full form, without having to pay us. (\`royalty-free' usage)

    o You may not pretend that  you wrote this software.  If you use
      it, or  only parts of it,  in a program,  you must acknowledge
      somewhere  in  your  documentation  that  you  have  used  the
      FreeType code. (\`credits')

  We  specifically  permit  and  encourage  the  inclusion  of  this
  software, with  or without modifications,  in commercial products.
  We  disclaim  all warranties  covering  The  FreeType Project  and
  assume no liability related to The FreeType Project.


  Finally,  many  people  asked  us  for  a  preferred  form  for  a
  credit/disclaimer to use in compliance with this license.  We thus
  encourage you to use the following text:

   """
    Portions of this software are copyright © <year> The FreeType
    Project (https://freetype.org).  All rights reserved.
   """

  Please replace <year> with the value from the FreeType version you
  actually use.


Legal Terms
===========

0. Definitions
--------------

  Throughout this license,  the terms \`package', \`FreeType Project',
  and  \`FreeType  archive' refer  to  the  set  of files  originally
  distributed  by the  authors  (David Turner,  Robert Wilhelm,  and
  Werner Lemberg) as the \`FreeType Project', be they named as alpha,
  beta or final release.

  \`You' refers to  the licensee, or person using  the project, where
  \`using' is a generic term including compiling the project's source
  code as  well as linking it  to form a  \`program' or \`executable'.
  This  program is  referred to  as  \`a program  using the  FreeType
  engine'.

  This  license applies  to all  files distributed  in  the original
  FreeType  Project,   including  all  source   code,  binaries  and
  documentation,  unless  otherwise  stated   in  the  file  in  its
  original, unmodified form as  distributed in the original archive.
  If you are  unsure whether or not a particular  file is covered by
  this license, you must contact us to verify this.

  The FreeType  Project is copyright (C) 1996-2000  by David Turner,
  Robert Wilhelm, and Werner Lemberg.  All rights reserved except as
  specified below.

1. No Warranty
--------------

  THE FREETYPE PROJECT  IS PROVIDED \`AS IS' WITHOUT  WARRANTY OF ANY
  KIND, EITHER  EXPRESS OR IMPLIED,  INCLUDING, BUT NOT  LIMITED TO,
  WARRANTIES  OF  MERCHANTABILITY   AND  FITNESS  FOR  A  PARTICULAR
  PURPOSE.  IN NO EVENT WILL ANY OF THE AUTHORS OR COPYRIGHT HOLDERS
  BE LIABLE  FOR ANY DAMAGES CAUSED  BY THE USE OR  THE INABILITY TO
  USE, OF THE FREETYPE PROJECT.

2. Redistribution
-----------------

  This  license  grants  a  worldwide, royalty-free,  perpetual  and
  irrevocable right  and license to use,  execute, perform, compile,
  display,  copy,   create  derivative  works   of,  distribute  and
  sublicense the  FreeType Project (in  both source and  object code
  forms)  and  derivative works  thereof  for  any  purpose; and  to
  authorize others  to exercise  some or all  of the  rights granted
  herein, subject to the following conditions:

    o Redistribution of  source code  must retain this  license file
      (\`FTL.TXT') unaltered; any  additions, deletions or changes to
      the original  files must be clearly  indicated in accompanying
      documentation.   The  copyright   notices  of  the  unaltered,
      original  files must  be  preserved in  all  copies of  source
      files.

    o Redistribution in binary form must provide a  disclaimer  that
      states  that  the software is based in part of the work of the
      FreeType Team,  in  the  distribution  documentation.  We also
      encourage you to put an URL to the FreeType web page  in  your
      documentation, though this isn't mandatory.

  These conditions  apply to any  software derived from or  based on
  the FreeType Project,  not just the unmodified files.   If you use
  our work, you  must acknowledge us.  However, no  fee need be paid
  to us.

3. Advertising
--------------

  Neither the  FreeType authors and  contributors nor you  shall use
  the name of the  other for commercial, advertising, or promotional
  purposes without specific prior written permission.

  We suggest,  but do not require, that  you use one or  more of the
  following phrases to refer  to this software in your documentation
  or advertising  materials: \`FreeType Project',  \`FreeType Engine',
  \`FreeType library', or \`FreeType Distribution'.

  As  you have  not signed  this license,  you are  not  required to
  accept  it.   However,  as  the FreeType  Project  is  copyrighted
  material, only  this license, or  another one contracted  with the
  authors, grants you  the right to use, distribute,  and modify it.
  Therefore,  by  using,  distributing,  or modifying  the  FreeType
  Project, you indicate that you understand and accept all the terms
  of this license.

4. Contacts
-----------

  There are two mailing lists related to FreeType:

    o freetype@nongnu.org

      Discusses general use and applications of FreeType, as well as
      future and  wanted additions to the  library and distribution.
      If  you are looking  for support,  start in  this list  if you
      haven't found anything to help you in the documentation.

    o freetype-devel@nongnu.org

      Discusses bugs,  as well  as engine internals,  design issues,
      specific licenses, porting, etc.

  Our home page can be found at

    https://freetype.org


--- end of FTL.TXT ---
`}),e[6]||(e[6]=t("h1",null,"libbsd",-1)),o(i,{code:`Format: https://www.debian.org/doc/packaging-manuals/copyright-format/1.0/

Files:
 *
Copyright:
 Copyright © 2004-2024 Guillem Jover <guillem@hadrons.org>
License: BSD-3-clause

Files:
 include/bsd/err.h
 include/bsd/stdlib.h
 include/bsd/sys/param.h
 include/bsd/unistd.h
 src/bsd_getopt.c
 src/err.c
 src/fgetln.c
 src/progname.c
Copyright:
 Copyright © 2005, 2008-2012, 2019 Guillem Jover <guillem@hadrons.org>
 Copyright © 2005 Hector Garcia Alvarez
 Copyright © 2005 Aurelien Jarno
 Copyright © 2006 Robert Millan
 Copyright © 2018 Facebook, Inc.
License: BSD-3-clause

Files:
 include/bsd/netinet/ip_icmp.h
 include/bsd/sys/bitstring.h
 include/bsd/sys/queue.h
 include/bsd/sys/time.h
 include/bsd/timeconv.h
 include/bsd/vis.h
 man/bitstring.3bsd
 man/errc.3bsd
 man/explicit_bzero.3bsd
 man/fgetln.3bsd
 man/fgetwln.3bsd
 man/fpurge.3bsd
 man/funopen.3bsd
 man/getbsize.3bsd
 man/heapsort.3bsd
 man/nlist.3bsd
 man/pwcache.3bsd
 man/queue.3bsd
 man/radixsort.3bsd
 man/reallocarray.3bsd
 man/reallocf.3bsd
 man/setmode.3bsd
 man/strmode.3bsd
 man/strnstr.3bsd
 man/strtoi.3bsd
 man/strtou.3bsd
 man/unvis.3bsd
 man/vis.3bsd
 man/wcslcpy.3bsd
 src/getbsize.c
 src/heapsort.c
 src/merge.c
 src/nlist.c
 src/pwcache.c
 src/radixsort.c
 src/setmode.c
 src/strmode.c
 src/strnstr.c
 src/strtoi.c
 src/strtou.c
 src/unvis.c
Copyright:
 Copyright © 1980, 1982, 1986, 1989-1994
     The Regents of the University of California.  All rights reserved.
 Copyright © 1992 Keith Muller.
 Copyright © 2001 Mike Barcroft <mike@FreeBSD.org>
 .
 Some code is derived from software contributed to Berkeley by
 the American National Standards Committee X3, on Information
 Processing Systems.
 .
 Some code is derived from software contributed to Berkeley by
 Peter McIlroy.
 .
 Some code is derived from software contributed to Berkeley by
 Ronnie Kon at Mindcraft Inc., Kevin Lew and Elmer Yglesias.
 .
 Some code is derived from software contributed to Berkeley by
 Dave Borman at Cray Research, Inc.
 .
 Some code is derived from software contributed to Berkeley by
 Paul Vixie.
 .
 Some code is derived from software contributed to Berkeley by
 Chris Torek.
 .
 Copyright © UNIX System Laboratories, Inc.
 All or some portions of this file are derived from material licensed
 to the University of California by American Telephone and Telegraph
 Co. or Unix System Laboratories, Inc. and are reproduced herein with
 the permission of UNIX System Laboratories, Inc.
License: BSD-3-clause-Regents

Files:
 src/vis.c
Copyright:
 Copyright © 1989, 1993
     The Regents of the University of California.  All rights reserved.
 .
 Copyright © 1999, 2005 The NetBSD Foundation, Inc.
 All rights reserved.
License: BSD-3-clause-Regents and BSD-2-clause-NetBSD

Files:
 include/bsd/libutil.h
Copyright:
 Copyright © 1996  Peter Wemm <peter@FreeBSD.org>.
 All rights reserved.
 Copyright © 2002 Networks Associates Technology, Inc.
 All rights reserved.
License: BSD-3-clause-author

Files:
 man/timeradd.3bsd
Copyright:
 Copyright © 2009 Jukka Ruohonen <jruohonen@iki.fi>
 Copyright © 1999 Kelly Yancey <kbyanc@posi.net>
 All rights reserved.
License: BSD-3-clause-John-Birrell
 Redistribution and use in source and binary forms, with or without
 modification, are permitted provided that the following conditions
 are met:
 1. Redistributions of source code must retain the above copyright
    notice, this list of conditions and the following disclaimer.
 2. Redistributions in binary form must reproduce the above copyright
    notice, this list of conditions and the following disclaimer in the
    documentation and/or other materials provided with the distribution.
 3. Neither the name of the author nor the names of any co-contributors
    may be used to endorse or promote products derived from this software
    without specific prior written permission.
 .
 THIS SOFTWARE IS PROVIDED BY JOHN BIRRELL AND CONTRIBUTORS \`\`AS IS'' AND
 ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
 IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 ARE DISCLAIMED.  IN NO EVENT SHALL THE REGENTS OR CONTRIBUTORS BE LIABLE
 FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
 DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS
 OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION)
 HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT
 LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY
 OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF
 SUCH DAMAGE.

Files:
 man/setproctitle.3bsd
Copyright:
 Copyright © 1995 Peter Wemm <peter@FreeBSD.org>
 All rights reserved.
License: BSD-5-clause-Peter-Wemm
 Redistribution and use in source and binary forms, with or without
 modification, is permitted provided that the following conditions
 are met:
 1. Redistributions of source code must retain the above copyright
    notice immediately at the beginning of the file, without modification,
    this list of conditions, and the following disclaimer.
 2. Redistributions in binary form must reproduce the above copyright
    notice, this list of conditions and the following disclaimer in the
    documentation and/or other materials provided with the distribution.
 3. This work was done expressly for inclusion into FreeBSD.  Other use
    is permitted provided this notation is included.
 4. Absolutely no warranty of function or purpose is made by the author
    Peter Wemm.
 5. Modifications may be freely made to this file providing the above
    conditions are met.

Files:
 include/bsd/stringlist.h
 man/arc4random.3bsd
 man/fmtcheck.3bsd
 man/humanize_number.3bsd
 man/stringlist.3bsd
 man/timeval.3bsd
 src/fmtcheck.c
 src/humanize_number.c
 src/stringlist.c
 src/strtonum.c
Copyright:
 Copyright © 1994, 1997-2000, 2002, 2008, 2010, 2014
     The NetBSD Foundation, Inc.
 Copyright © 2013 John-Mark Gurney <jmg@FreeBSD.org>
 All rights reserved.
 .
 Copyright © 2014 The NetBSD Foundation, Inc.
 All rights reserved.
 .
 Some code was derived from software contributed to The NetBSD Foundation
 by Taylor R. Campbell.
 .
 Some code was contributed to The NetBSD Foundation by Allen Briggs.
 .
 Some code was contributed to The NetBSD Foundation by Luke Mewburn.
 .
 Some code is derived from software contributed to The NetBSD Foundation
 by Jason R. Thorpe of the Numerical Aerospace Simulation Facility,
 NASA Ames Research Center, by Luke Mewburn and by Tomas Svensson.
 .
 Some code is derived from software contributed to The NetBSD Foundation
 by Julio M. Merino Vidal, developed as part of Google's Summer of Code
 2005 program.
 .
 Some code is derived from software contributed to The NetBSD Foundation
 by Christos Zoulas.
 .
 Some code is derived from software contributed to The NetBSD Foundation
 by Jukka Ruohonen.
License: BSD-2-clause-NetBSD

Files:
 include/bsd/sys/endian.h
 man/byteorder.3bsd
 man/closefrom.3bsd
 man/expand_number.3bsd
 man/flopen.3bsd
 man/getpeereid.3bsd
 man/pidfile.3bsd
 src/expand_number.c
 src/pidfile.c
 src/reallocf.c
 src/timeconv.c
Copyright:
 Copyright © 1998, M. Warner Losh <imp@freebsd.org>
 All rights reserved.
 .
 Copyright © 2001 Dima Dorfman.
 All rights reserved.
 .
 Copyright © 2001 FreeBSD Inc.
 All rights reserved.
 .
 Copyright © 2002 Thomas Moestl <tmm@FreeBSD.org>
 All rights reserved.
 .
 Copyright © 2002 Mike Barcroft <mike@FreeBSD.org>
 All rights reserved.
 .
 Copyright © 2005 Pawel Jakub Dawidek <pjd@FreeBSD.org>
 All rights reserved.
 .
 Copyright © 2005 Colin Percival
 All rights reserved.
 .
 Copyright © 2007 Eric Anderson <anderson@FreeBSD.org>
 Copyright © 2007 Pawel Jakub Dawidek <pjd@FreeBSD.org>
 All rights reserved.
 .
 Copyright © 2007 Dag-Erling Coïdan Smørgrav
 All rights reserved.
 .
 Copyright © 2009 Advanced Computing Technologies LLC
 All rights reserved.
 .
 Copyright © 2011 Guillem Jover <guillem@hadrons.org>
License: BSD-2-clause

Files:
 src/flopen.c
Copyright:
 Copyright © 2007-2009 Dag-Erling Coïdan Smørgrav
 All rights reserved.
License: BSD-2-clause-verbatim
 Redistribution and use in source and binary forms, with or without
 modification, are permitted provided that the following conditions
 are met:
 1. Redistributions of source code must retain the above copyright
    notice, this list of conditions and the following disclaimer
    in this position and unchanged.
 2. Redistributions in binary form must reproduce the above copyright
    notice, this list of conditions and the following disclaimer in the
    documentation and/or other materials provided with the distribution.
 .
 THIS SOFTWARE IS PROVIDED BY THE AUTHOR AND CONTRIBUTORS \`\`AS IS'' AND
 ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
 IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 ARE DISCLAIMED.  IN NO EVENT SHALL THE AUTHOR OR CONTRIBUTORS BE LIABLE
 FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
 DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS
 OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION)
 HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT
 LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY
 OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF
 SUCH DAMAGE.

Files:
 include/bsd/sys/tree.h
 man/fparseln.3bsd
 man/tree.3bsd
 src/fparseln.c
Copyright:
 Copyright © 1997 Christos Zoulas.
 All rights reserved.
 .
 Copyright © 2002 Niels Provos <provos@citi.umich.edu>
 All rights reserved.
License: BSD-2-clause-author

Files:
 include/bsd/readpassphrase.h
 man/readpassphrase.3bsd
 man/strlcpy.3bsd
 man/strtonum.3bsd
 src/arc4random.c
 src/arc4random_linux.h
 src/arc4random_uniform.c
 src/arc4random_unix.h
 src/arc4random_win.h
 src/closefrom.c
 src/freezero.c
 src/getentropy_aix.c
 src/getentropy_bsd.c
 src/getentropy_hpux.c
 src/getentropy_hurd.c
 src/getentropy_linux.c
 src/getentropy_osx.c
 src/getentropy_solaris.c
 src/getentropy_win.c
 src/readpassphrase.c
 src/reallocarray.c
 src/recallocarray.c
 src/strlcat.c
 src/strlcpy.c
 test/explicit_bzero.c
 test/strtonum.c
Copyright:
 Copyright © 2004 Ted Unangst and Todd Miller
 All rights reserved.
 .
 Copyright © 1996 David Mazieres <dm@uun.org>
 Copyright © 1998, 2000-2002, 2004-2005, 2007, 2010, 2012-2015
     Todd C. Miller <Todd.Miller@courtesan.com>
 Copyright © 2004 Ted Unangst
 Copyright © 2004 Otto Moerbeek <otto@drijf.net>
 Copyright © 2008 Damien Miller <djm@openbsd.org>
 Copyright © 2008, 2010-2011, 2016-2017 Otto Moerbeek <otto@drijf.net>
 Copyright © 2013 Markus Friedl <markus@openbsd.org>
 Copyright © 2014 Bob Beck <beck@obtuse.com>
 Copyright © 2014 Brent Cook <bcook@openbsd.org>
 Copyright © 2014 Pawel Jakub Dawidek <pjd@FreeBSD.org>
 Copyright © 2014 Theo de Raadt <deraadt@openbsd.org>
 Copyright © 2014 Google Inc.
 Copyright © 2015 Michael Felt <aixtools@gmail.com>
 Copyright © 2015, 2022 Guillem Jover <guillem@hadrons.org>
License: ISC
 Permission to use, copy, modify, and distribute this software for any
 purpose with or without fee is hereby granted, provided that the above
 copyright notice and this permission notice appear in all copies.
 .
 THE SOFTWARE IS PROVIDED "AS IS" AND THE AUTHOR DISCLAIMS ALL WARRANTIES
 WITH REGARD TO THIS SOFTWARE INCLUDING ALL IMPLIED WARRANTIES OF
 MERCHANTABILITY AND FITNESS. IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR
 ANY SPECIAL, DIRECT, INDIRECT, OR CONSEQUENTIAL DAMAGES OR ANY DAMAGES
 WHATSOEVER RESULTING FROM LOSS OF USE, DATA OR PROFITS, WHETHER IN AN
 ACTION OF CONTRACT, NEGLIGENCE OR OTHER TORTIOUS ACTION, ARISING OUT OF
 OR IN CONNECTION WITH THE USE OR PERFORMANCE OF THIS SOFTWARE.

Files:
 src/inet_net_pton.c
Copyright:
 Copyright © 1996 by Internet Software Consortium.
License: ISC-Original
 Permission to use, copy, modify, and distribute this software for any
 purpose with or without fee is hereby granted, provided that the above
 copyright notice and this permission notice appear in all copies.
 .
 THE SOFTWARE IS PROVIDED "AS IS" AND INTERNET SOFTWARE CONSORTIUM DISCLAIMS
 ALL WARRANTIES WITH REGARD TO THIS SOFTWARE INCLUDING ALL IMPLIED WARRANTIES
 OF MERCHANTABILITY AND FITNESS. IN NO EVENT SHALL INTERNET SOFTWARE
 CONSORTIUM BE LIABLE FOR ANY SPECIAL, DIRECT, INDIRECT, OR CONSEQUENTIAL
 DAMAGES OR ANY DAMAGES WHATSOEVER RESULTING FROM LOSS OF USE, DATA OR
 PROFITS, WHETHER IN AN ACTION OF CONTRACT, NEGLIGENCE OR OTHER TORTIOUS
 ACTION, ARISING OUT OF OR IN CONNECTION WITH THE USE OR PERFORMANCE OF THIS
 SOFTWARE.

Files:
 src/setproctitle.c
Copyright:
 Copyright © 2010 William Ahern
 Copyright © 2012 Guillem Jover <guillem@hadrons.org>
License: Expat
 Permission is hereby granted, free of charge, to any person obtaining a
 copy of this software and associated documentation files (the
 "Software"), to deal in the Software without restriction, including
 without limitation the rights to use, copy, modify, merge, publish,
 distribute, sublicense, and/or sell copies of the Software, and to permit
 persons to whom the Software is furnished to do so, subject to the
 following conditions:
 .
 The above copyright notice and this permission notice shall be included
 in all copies or substantial portions of the Software.
 .
 THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS
 OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
 MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN
 NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM,
 DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
 OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE
 USE OR OTHER DEALINGS IN THE SOFTWARE.

Files:
 src/explicit_bzero.c
 src/chacha_private.h
Copyright:
 None
License: public-domain
 Public domain.

Files:
 man/mdX.3bsd
Copyright:
 None
License: Beerware
 "THE BEER-WARE LICENSE" (Revision 42):
 <phk@login.dkuug.dk> wrote this file.  As long as you retain this notice you
 can do whatever you want with this stuff. If we meet some day, and you think
 this stuff is worth it, you can buy me a beer in return.   Poul-Henning Kamp

License: BSD-3-clause-Regents
 Redistribution and use in source and binary forms, with or without
 modification, are permitted provided that the following conditions
 are met:
 1. Redistributions of source code must retain the above copyright
    notice, this list of conditions and the following disclaimer.
 2. Redistributions in binary form must reproduce the above copyright
    notice, this list of conditions and the following disclaimer in the
    documentation and/or other materials provided with the distribution.
 3. Neither the name of the University nor the names of its contributors
    may be used to endorse or promote products derived from this software
    without specific prior written permission.
 .
 THIS SOFTWARE IS PROVIDED BY THE REGENTS AND CONTRIBUTORS \`\`AS IS'' AND
 ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
 IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 ARE DISCLAIMED.  IN NO EVENT SHALL THE REGENTS OR CONTRIBUTORS BE LIABLE
 FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
 DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS
 OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION)
 HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT
 LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY
 OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF
 SUCH DAMAGE.

License: BSD-3-clause-author
 Redistribution and use in source and binary forms, with or without
 modification, is permitted provided that the following conditions
 are met:
 1. Redistributions of source code must retain the above copyright
    notice, this list of conditions and the following disclaimer.
 2. Redistributions in binary form must reproduce the above copyright
    notice, this list of conditions and the following disclaimer in the
    documentation and/or other materials provided with the distribution.
 3. The name of the author may not be used to endorse or promote
    products derived from this software without specific prior written
    permission.
 .
 THIS SOFTWARE IS PROVIDED BY THE AUTHOR AND CONTRIBUTORS \`\`AS IS'' AND
 ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
 IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 ARE DISCLAIMED.  IN NO EVENT SHALL THE AUTHOR OR CONTRIBUTORS BE LIABLE
 FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
 DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS
 OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION)
 HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT
 LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY
 OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF
 SUCH DAMAGE.

License: BSD-3-clause
 Redistribution and use in source and binary forms, with or without
 modification, are permitted provided that the following conditions
 are met:
 1. Redistributions of source code must retain the above copyright
    notice, this list of conditions and the following disclaimer.
 2. Redistributions in binary form must reproduce the above copyright
    notice, this list of conditions and the following disclaimer in the
    documentation and/or other materials provided with the distribution.
 3. The name of the author may not be used to endorse or promote products
    derived from this software without specific prior written permission.
 .
 THIS SOFTWARE IS PROVIDED \`\`AS IS'' AND ANY EXPRESS OR IMPLIED WARRANTIES,
 INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY
 AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED.  IN NO EVENT SHALL
 THE AUTHOR BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL,
 EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO,
 PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS;
 OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY,
 WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR
 OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF
 ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

License: BSD-2-clause-NetBSD
 Redistribution and use in source and binary forms, with or without
 modification, are permitted provided that the following conditions
 are met:
 1. Redistributions of source code must retain the above copyright
    notice, this list of conditions and the following disclaimer.
 2. Redistributions in binary form must reproduce the above copyright
    notice, this list of conditions and the following disclaimer in the
    documentation and/or other materials provided with the distribution.
 .
 THIS SOFTWARE IS PROVIDED BY THE NETBSD FOUNDATION, INC. AND CONTRIBUTORS
 \`\`AS IS'' AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED
 TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR
 PURPOSE ARE DISCLAIMED.  IN NO EVENT SHALL THE FOUNDATION OR CONTRIBUTORS
 BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
 CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
 SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
 INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
 CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
 ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
 POSSIBILITY OF SUCH DAMAGE.

License: BSD-2-clause-author
 Redistribution and use in source and binary forms, with or without
 modification, are permitted provided that the following conditions
 are met:
 1. Redistributions of source code must retain the above copyright
    notice, this list of conditions and the following disclaimer.
 2. Redistributions in binary form must reproduce the above copyright
    notice, this list of conditions and the following disclaimer in the
    documentation and/or other materials provided with the distribution.
 .
 THIS SOFTWARE IS PROVIDED BY THE AUTHOR \`\`AS IS'' AND ANY EXPRESS OR
 IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES
 OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED.
 IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR ANY DIRECT, INDIRECT,
 INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT
 NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE,
 DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY
 THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF
 THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

License: BSD-2-clause
 Redistribution and use in source and binary forms, with or without
 modification, are permitted provided that the following conditions
 are met:
 1. Redistributions of source code must retain the above copyright
    notice, this list of conditions and the following disclaimer.
 2. Redistributions in binary form must reproduce the above copyright
    notice, this list of conditions and the following disclaimer in the
    documentation and/or other materials provided with the distribution.
 .
 THIS SOFTWARE IS PROVIDED BY THE AUTHOR AND CONTRIBUTORS \`\`AS IS'' AND
 ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
 IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 ARE DISCLAIMED.  IN NO EVENT SHALL THE AUTHOR OR CONTRIBUTORS BE LIABLE
 FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
 DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS
 OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION)
 HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT
 LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY
 OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF
 SUCH DAMAGE.
`}),e[7]||(e[7]=t("h4",null,"libbz2",-1)),o(i,{code:`
--------------------------------------------------------------------------

This program, "bzip2", the associated library "libbzip2", and all
documentation, are copyright (C) 1996-2010 Julian R Seward.  All
rights reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions
are met:

1. Redistributions of source code must retain the above copyright
   notice, this list of conditions and the following disclaimer.

2. The origin of this software must not be misrepresented; you must
   not claim that you wrote the original software.  If you use this
   software in a product, an acknowledgment in the product
   documentation would be appreciated but is not required.

3. Altered source versions must be plainly marked as such, and must
   not be misrepresented as being the original software.

4. The name of the author may not be used to endorse or promote
   products derived from this software without specific prior written
   permission.

THIS SOFTWARE IS PROVIDED BY THE AUTHOR \`\`AS IS'' AND ANY EXPRESS
OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
ARE DISCLAIMED.  IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR ANY
DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE
GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY,
WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

Julian Seward, jseward@acm.org
bzip2/libbzip2 version 1.1.0 of 6 September 2010

--------------------------------------------------------------------------

`}),e[8]||(e[8]=t("h4",null,"libcrypto1.1",-1)),o(i,{code:`  LICENSE ISSUES
  ==============

  The OpenSSL toolkit stays under a double license, i.e. both the conditions of
  the OpenSSL License and the original SSLeay license apply to the toolkit.
  See below for the actual license texts.

  OpenSSL License
  ---------------

/* ====================================================================
 * Copyright (c) 1998-2019 The OpenSSL Project.  All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions
 * are met:
 *
 * 1. Redistributions of source code must retain the above copyright
 *    notice, this list of conditions and the following disclaimer.
 *
 * 2. Redistributions in binary form must reproduce the above copyright
 *    notice, this list of conditions and the following disclaimer in
 *    the documentation and/or other materials provided with the
 *    distribution.
 *
 * 3. All advertising materials mentioning features or use of this
 *    software must display the following acknowledgment:
 *    "This product includes software developed by the OpenSSL Project
 *    for use in the OpenSSL Toolkit. (http://www.openssl.org/)"
 *
 * 4. The names "OpenSSL Toolkit" and "OpenSSL Project" must not be used to
 *    endorse or promote products derived from this software without
 *    prior written permission. For written permission, please contact
 *    openssl-core@openssl.org.
 *
 * 5. Products derived from this software may not be called "OpenSSL"
 *    nor may "OpenSSL" appear in their names without prior written
 *    permission of the OpenSSL Project.
 *
 * 6. Redistributions of any form whatsoever must retain the following
 *    acknowledgment:
 *    "This product includes software developed by the OpenSSL Project
 *    for use in the OpenSSL Toolkit (http://www.openssl.org/)"
 *
 * THIS SOFTWARE IS PROVIDED BY THE OpenSSL PROJECT \`\`AS IS'' AND ANY
 * EXPRESSED OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
 * IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR
 * PURPOSE ARE DISCLAIMED.  IN NO EVENT SHALL THE OpenSSL PROJECT OR
 * ITS CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
 * SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT
 * NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION)
 * HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT,
 * STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
 * ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED
 * OF THE POSSIBILITY OF SUCH DAMAGE.
 * ====================================================================
 *
 * This product includes cryptographic software written by Eric Young
 * (eay@cryptsoft.com).  This product includes software written by Tim
 * Hudson (tjh@cryptsoft.com).
 *
 */

 Original SSLeay License
 -----------------------

/* Copyright (C) 1995-1998 Eric Young (eay@cryptsoft.com)
 * All rights reserved.
 *
 * This package is an SSL implementation written
 * by Eric Young (eay@cryptsoft.com).
 * The implementation was written so as to conform with Netscapes SSL.
 *
 * This library is free for commercial and non-commercial use as long as
 * the following conditions are aheared to.  The following conditions
 * apply to all code found in this distribution, be it the RC4, RSA,
 * lhash, DES, etc., code; not just the SSL code.  The SSL documentation
 * included with this distribution is covered by the same copyright terms
 * except that the holder is Tim Hudson (tjh@cryptsoft.com).
 *
 * Copyright remains Eric Young's, and as such any Copyright notices in
 * the code are not to be removed.
 * If this package is used in a product, Eric Young should be given attribution
 * as the author of the parts of the library used.
 * This can be in the form of a textual message at program startup or
 * in documentation (online or textual) provided with the package.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions
 * are met:
 * 1. Redistributions of source code must retain the copyright
 *    notice, this list of conditions and the following disclaimer.
 * 2. Redistributions in binary form must reproduce the above copyright
 *    notice, this list of conditions and the following disclaimer in the
 *    documentation and/or other materials provided with the distribution.
 * 3. All advertising materials mentioning features or use of this software
 *    must display the following acknowledgement:
 *    "This product includes cryptographic software written by
 *     Eric Young (eay@cryptsoft.com)"
 *    The word 'cryptographic' can be left out if the rouines from the library
 *    being used are not cryptographic related :-).
 * 4. If you include any Windows specific code (or a derivative thereof) from
 *    the apps directory (application code) you must include an acknowledgement:
 *    "This product includes software written by Tim Hudson (tjh@cryptsoft.com)"
 *
 * THIS SOFTWARE IS PROVIDED BY ERIC YOUNG \`\`AS IS'' AND
 * ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
 * IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 * ARE DISCLAIMED.  IN NO EVENT SHALL THE AUTHOR OR CONTRIBUTORS BE LIABLE
 * FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
 * DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS
 * OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION)
 * HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT
 * LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY
 * OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF
 * SUCH DAMAGE.
 *
 * The licence and distribution terms for any publically available version or
 * derivative of this code cannot be changed.  i.e. this code cannot simply be
 * copied and put under another distribution licence
 * [including the GNU Public Licence.]
 */
`}),e[9]||(e[9]=t("h4",null,"libexpat",-1)),o(i,{code:`Copyright (c) 1998-2000 Thai Open Source Software Center Ltd and Clark Cooper
Copyright (c) 2001-2025 Expat maintainers

Permission is hereby granted, free of charge, to any person obtaining
a copy of this software and associated documentation files (the
"Software"), to deal in the Software without restriction, including
without limitation the rights to use, copy, modify, merge, publish,
distribute, sublicense, and/or sell copies of the Software, and to
permit persons to whom the Software is furnished to do so, subject to
the following conditions:

The above copyright notice and this permission notice shall be included
in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
`}),e[10]||(e[10]=t("h4",null,"libjpeg-turbo",-1)),e[11]||(e[11]=t("p",null,"This software is based in part on the work of the Independent JPEG Group.",-1)),o(i,{code:`libjpeg-turbo Licenses
======================

libjpeg-turbo is covered by two compatible BSD-style open source licenses:

- The IJG (Independent JPEG Group) License, which is listed in
  [README.ijg](README.ijg)

  This license applies to the libjpeg API library and associated programs,
  including any code inherited from libjpeg and any modifications to that
  code.  Note that the libjpeg-turbo SIMD source code bears the
  [zlib License](https://opensource.org/licenses/Zlib), but in the context of
  the overall libjpeg API library, the terms of the zlib License are subsumed
  by the terms of the IJG License.

- The Modified (3-clause) BSD License, which is listed below

  This license applies to the TurboJPEG API library and associated programs, as
  well as the build system.  Note that the TurboJPEG API library wraps the
  libjpeg API library, so in the context of the overall TurboJPEG API library,
  both the terms of the IJG License and the terms of the Modified (3-clause)
  BSD License apply.


Complying with the libjpeg-turbo Licenses
=========================================

This section provides a roll-up of the libjpeg-turbo licensing terms, to the
best of our understanding.  This is not a license in and of itself.  It is
intended solely for clarification.

1.  If you are distributing a modified version of the libjpeg-turbo source,
    then:

    1.  You cannot alter or remove any existing copyright or license notices
        from the source.

        **Origin**
        - Clause 1 of the IJG License
        - Clause 1 of the Modified BSD License
        - Clauses 1 and 3 of the zlib License

    2.  You must add your own copyright notice to the header of each source
        file you modified, so others can tell that you modified that file.  (If
        there is not an existing copyright header in that file, then you can
        simply add a notice stating that you modified the file.)

        **Origin**
        - Clause 1 of the IJG License
        - Clause 2 of the zlib License

    3.  You must include the IJG README file, and you must not alter any of the
        copyright or license text in that file.

        **Origin**
        - Clause 1 of the IJG License

2.  If you are distributing only libjpeg-turbo binaries without the source, or
    if you are distributing an application that statically links with
    libjpeg-turbo, then:

    1.  Your product documentation must include a message stating:

        This software is based in part on the work of the Independent JPEG
        Group.

        **Origin**
        - Clause 2 of the IJG license

    2.  If your binary distribution includes or uses the TurboJPEG API, then
        your product documentation must include the text of the Modified BSD
        License (see below.)

        **Origin**
        - Clause 2 of the Modified BSD License

3.  You cannot use the name of the IJG or The libjpeg-turbo Project or the
    contributors thereof in advertising, publicity, etc.

    **Origin**
    - IJG License
    - Clause 3 of the Modified BSD License

4.  The IJG and The libjpeg-turbo Project do not warrant libjpeg-turbo to be
    free of defects, nor do we accept any liability for undesirable
    consequences resulting from your use of the software.

    **Origin**
    - IJG License
    - Modified BSD License
    - zlib License


The Modified (3-clause) BSD License
===================================

Copyright (C)2009-2025 D. R. Commander.  All Rights Reserved.<br>
Copyright (C)2015 Viktor Szathmáry.  All Rights Reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:

- Redistributions of source code must retain the above copyright notice,
  this list of conditions and the following disclaimer.
- Redistributions in binary form must reproduce the above copyright notice,
  this list of conditions and the following disclaimer in the documentation
  and/or other materials provided with the distribution.
- Neither the name of the libjpeg-turbo Project nor the names of its
  contributors may be used to endorse or promote products derived from this
  software without specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS",
AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
ARE DISCLAIMED.  IN NO EVENT SHALL THE COPYRIGHT HOLDERS OR CONTRIBUTORS BE
LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
POSSIBILITY OF SUCH DAMAGE.


Why Two Licenses?
=================

The zlib License could have been used instead of the Modified (3-clause) BSD
License, and since the IJG License effectively subsumes the distribution
conditions of the zlib License, this would have effectively placed
libjpeg-turbo binary distributions under the IJG License.  However, the IJG
License specifically refers to the Independent JPEG Group and does not extend
attribution and endorsement protections to other entities.  Thus, it was
desirable to choose a license that granted us the same protections for new code
that were granted to the IJG for code derived from their software.
`}),o(i,{code:`In plain English:

1. We don't promise that this software works.  (But if you find any bugs,
   please let us know!)
2. You can use this software for whatever you want.  You don't have to pay us.
3. You may not pretend that you wrote this software.  If you use it in a
   program, you must acknowledge somewhere in your documentation that
   you've used the IJG code.

In legalese:

The authors make NO WARRANTY or representation, either express or implied,
with respect to this software, its quality, accuracy, merchantability, or
fitness for a particular purpose.  This software is provided "AS IS", and you,
its user, assume the entire risk as to its quality and accuracy.

This software is copyright (C) 1991-2020, Thomas G. Lane, Guido Vollbeding.
All Rights Reserved except as specified below.

Permission is hereby granted to use, copy, modify, and distribute this
software (or portions thereof) for any purpose, without fee, subject to these
conditions:
(1) If any part of the source code for this software is distributed, then this
README file must be included, with this copyright and no-warranty notice
unaltered; and any additions, deletions, or changes to the original files
must be clearly indicated in accompanying documentation.
(2) If only executable code is distributed, then the accompanying
documentation must state that "this software is based in part on the work of
the Independent JPEG Group".
(3) Permission for use of this software is granted only if the user accepts
full responsibility for any undesirable consequences; the authors accept
NO LIABILITY for damages of any kind.

These conditions apply to any software derived from or based on the IJG code,
not just to the unmodified library.  If you use our work, you ought to
acknowledge us.

Permission is NOT granted for the use of any IJG author's name or company name
in advertising or publicity relating to this software or products derived from
it.  This software may be referred to only as "the Independent JPEG Group's
software".

We specifically permit and encourage the use of this software as the basis of
commercial products, provided that all warranty or liability claims are
assumed by the product vendor.
`}),e[12]||(e[12]=t("h4",null,"libmd",-1)),o(i,{code:`Format: https://www.debian.org/doc/packaging-manuals/copyright-format/1.0/

Files:
 *
Copyright:
 Copyright © 2009, 2011, 2016 Guillem Jover <guillem@hadrons.org>
License: BSD-3-clause
 Redistribution and use in source and binary forms, with or without
 modification, are permitted provided that the following conditions
 are met:
 1. Redistributions of source code must retain the above copyright
    notice, this list of conditions and the following disclaimer.
 2. Redistributions in binary form must reproduce the above copyright
    notice, this list of conditions and the following disclaimer in the
    documentation and/or other materials provided with the distribution.
 3. The name of the author may not be used to endorse or promote products
    derived from this software without specific prior written permission.
 .
 THIS SOFTWARE IS PROVIDED \`\`AS IS'' AND ANY EXPRESS OR IMPLIED WARRANTIES,
 INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY
 AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED.  IN NO EVENT SHALL
 THE AUTHOR BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL,
 EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO,
 PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS;
 OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY,
 WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR
 OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF
 ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

Files:
 include/sha2.h
 src/sha2.c
Copyright:
 Copyright © 2000-2001, Aaron D. Gifford
 All rights reserved.
License: BSD-3-clause-Aaron-D-Gifford
 Redistribution and use in source and binary forms, with or without
 modification, are permitted provided that the following conditions
 are met:
 1. Redistributions of source code must retain the above copyright
    notice, this list of conditions and the following disclaimer.
 2. Redistributions in binary form must reproduce the above copyright
    notice, this list of conditions and the following disclaimer in the
    documentation and/or other materials provided with the distribution.
 3. Neither the name of the copyright holder nor the names of contributors
    may be used to endorse or promote products derived from this software
    without specific prior written permission.
 .
 THIS SOFTWARE IS PROVIDED BY THE AUTHOR AND CONTRIBUTOR(S) \`\`AS IS'' AND
 ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
 IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 ARE DISCLAIMED.  IN NO EVENT SHALL THE AUTHOR OR CONTRIBUTOR(S) BE LIABLE
 FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
 DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS
 OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION)
 HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT
 LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY
 OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF
 SUCH DAMAGE.

Files:
 include/rmd160.h
 src/rmd160.c
Copyright:
 Copyright © 2001 Markus Friedl.  All rights reserved.
License: BSD-2-clause
 Redistribution and use in source and binary forms, with or without
 modification, are permitted provided that the following conditions
 are met:
 1. Redistributions of source code must retain the above copyright
    notice, this list of conditions and the following disclaimer.
 2. Redistributions in binary form must reproduce the above copyright
    notice, this list of conditions and the following disclaimer in the
    documentation and/or other materials provided with the distribution.
 .
 THIS SOFTWARE IS PROVIDED BY THE AUTHOR \`\`AS IS'' AND ANY EXPRESS OR
 IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES
 OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED.
 IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR ANY DIRECT, INDIRECT,
 INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT
 NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE,
 DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY
 THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF
 THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

Files:
 src/md2.c
Copyright:
 Copyright (c) 2001 The NetBSD Foundation, Inc.
 All rights reserved.
 .
 This code is derived from software contributed to The NetBSD Foundation
 by Andrew Brown.
License: BSD-2-clause-NetBSD
 Redistribution and use in source and binary forms, with or without
 modification, are permitted provided that the following conditions
 are met:
 1. Redistributions of source code must retain the above copyright
    notice, this list of conditions and the following disclaimer.
 2. Redistributions in binary form must reproduce the above copyright
    notice, this list of conditions and the following disclaimer in the
    documentation and/or other materials provided with the distribution.
 .
 THIS SOFTWARE IS PROVIDED BY THE NETBSD FOUNDATION, INC. AND CONTRIBUTORS
 \`\`AS IS'' AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED
 TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR
 PURPOSE ARE DISCLAIMED.  IN NO EVENT SHALL THE FOUNDATION OR CONTRIBUTORS
 BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
 CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
 SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
 INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
 CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
 ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
 POSSIBILITY OF SUCH DAMAGE.

Files:
 man/rmd160.3
 man/sha1.3
 man/sha2.3
Copyright:
 Copyright © 1997, 2003, 2004 Todd C. Miller <Todd.Miller@courtesan.com>
License: ISC
 Permission to use, copy, modify, and distribute this software for any
 purpose with or without fee is hereby granted, provided that the above
 copyright notice and this permission notice appear in all copies.
 .
 THE SOFTWARE IS PROVIDED "AS IS" AND THE AUTHOR DISCLAIMS ALL WARRANTIES
 WITH REGARD TO THIS SOFTWARE INCLUDING ALL IMPLIED WARRANTIES OF
 MERCHANTABILITY AND FITNESS. IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR
 ANY SPECIAL, DIRECT, INDIRECT, OR CONSEQUENTIAL DAMAGES OR ANY DAMAGES
 WHATSOEVER RESULTING FROM LOSS OF USE, DATA OR PROFITS, WHETHER IN AN
 ACTION OF CONTRACT, NEGLIGENCE OR OTHER TORTIOUS ACTION, ARISING OUT OF
 OR IN CONNECTION WITH THE USE OR PERFORMANCE OF THIS SOFTWARE.

Files:
 man/mdX.3
 src/helper.c
Copyright:
 Poul-Henning Kamp <phk@login.dkuug.dk>
License: Beerware
 "THE BEER-WARE LICENSE" (Revision 42):
 <phk@login.dkuug.dk> wrote this file.  As long as you retain this notice you
 can do whatever you want with this stuff. If we meet some day, and you think
 this stuff is worth it, you can buy me a beer in return.   Poul-Henning Kamp

Files:
 include/md4.h
 src/md4.c
Copyright:
 Colin Plumb
 Todd C. Miller
License: public-domain-md4
 This code implements the MD4 message-digest algorithm.
 The algorithm is due to Ron Rivest.  This code was
 written by Colin Plumb in 1993, no copyright is claimed.
 This code is in the public domain; do with it what you wish.
 Todd C. Miller modified the MD5 code to do MD4 based on RFC 1186.

Files:
 include/md5.h
 src/md5.c
Copyright:
 Colin Plumb
License: public-domain-md5
 This code implements the MD5 message-digest algorithm.
 The algorithm is due to Ron Rivest.  This code was
 written by Colin Plumb in 1993, no copyright is claimed.
 This code is in the public domain; do with it what you wish.

Files:
 include/sha1.h
 src/sha1.c
Copyright:
 Steve Reid <steve@edmweb.com>
License: public-domain-sha1
 100% Public Domain
`}),e[13]||(e[13]=t("h4",null,"libpng",-1)),o(i,{code:`COPYRIGHT NOTICE, DISCLAIMER, and LICENSE
=========================================

PNG Reference Library License version 2
---------------------------------------

 * Copyright (c) 1995-2025 The PNG Reference Library Authors.
 * Copyright (c) 2018-2025 Cosmin Truta.
 * Copyright (c) 2000-2002, 2004, 2006-2018 Glenn Randers-Pehrson.
 * Copyright (c) 1996-1997 Andreas Dilger.
 * Copyright (c) 1995-1996 Guy Eric Schalnat, Group 42, Inc.

The software is supplied "as is", without warranty of any kind,
express or implied, including, without limitation, the warranties
of merchantability, fitness for a particular purpose, title, and
non-infringement.  In no event shall the Copyright owners, or
anyone distributing the software, be liable for any damages or
other liability, whether in contract, tort or otherwise, arising
from, out of, or in connection with the software, or the use or
other dealings in the software, even if advised of the possibility
of such damage.

Permission is hereby granted to use, copy, modify, and distribute
this software, or portions hereof, for any purpose, without fee,
subject to the following restrictions:

 1. The origin of this software must not be misrepresented; you
    must not claim that you wrote the original software.  If you
    use this software in a product, an acknowledgment in the product
    documentation would be appreciated, but is not required.

 2. Altered source versions must be plainly marked as such, and must
    not be misrepresented as being the original software.

 3. This Copyright notice may not be removed or altered from any
    source or altered source distribution.


PNG Reference Library License version 1 (for libpng 0.5 through 1.6.35)
-----------------------------------------------------------------------

libpng versions 1.0.7, July 1, 2000, through 1.6.35, July 15, 2018 are
Copyright (c) 2000-2002, 2004, 2006-2018 Glenn Randers-Pehrson, are
derived from libpng-1.0.6, and are distributed according to the same
disclaimer and license as libpng-1.0.6 with the following individuals
added to the list of Contributing Authors:

    Simon-Pierre Cadieux
    Eric S. Raymond
    Mans Rullgard
    Cosmin Truta
    Gilles Vollant
    James Yu
    Mandar Sahastrabuddhe
    Google Inc.
    Vadim Barkov

and with the following additions to the disclaimer:

    There is no warranty against interference with your enjoyment of
    the library or against infringement.  There is no warranty that our
    efforts or the library will fulfill any of your particular purposes
    or needs.  This library is provided with all faults, and the entire
    risk of satisfactory quality, performance, accuracy, and effort is
    with the user.

Some files in the "contrib" directory and some configure-generated
files that are distributed with libpng have other copyright owners, and
are released under other open source licenses.

libpng versions 0.97, January 1998, through 1.0.6, March 20, 2000, are
Copyright (c) 1998-2000 Glenn Randers-Pehrson, are derived from
libpng-0.96, and are distributed according to the same disclaimer and
license as libpng-0.96, with the following individuals added to the
list of Contributing Authors:

    Tom Lane
    Glenn Randers-Pehrson
    Willem van Schaik

libpng versions 0.89, June 1996, through 0.96, May 1997, are
Copyright (c) 1996-1997 Andreas Dilger, are derived from libpng-0.88,
and are distributed according to the same disclaimer and license as
libpng-0.88, with the following individuals added to the list of
Contributing Authors:

    John Bowler
    Kevin Bracey
    Sam Bushell
    Magnus Holmgren
    Greg Roelofs
    Tom Tanner

Some files in the "scripts" directory have other copyright owners,
but are released under this license.

libpng versions 0.5, May 1995, through 0.88, January 1996, are
Copyright (c) 1995-1996 Guy Eric Schalnat, Group 42, Inc.

For the purposes of this copyright and license, "Contributing Authors"
is defined as the following set of individuals:

    Andreas Dilger
    Dave Martindale
    Guy Eric Schalnat
    Paul Schmidt
    Tim Wegner

The PNG Reference Library is supplied "AS IS".  The Contributing
Authors and Group 42, Inc. disclaim all warranties, expressed or
implied, including, without limitation, the warranties of
merchantability and of fitness for any purpose.  The Contributing
Authors and Group 42, Inc. assume no liability for direct, indirect,
incidental, special, exemplary, or consequential damages, which may
result from the use of the PNG Reference Library, even if advised of
the possibility of such damage.

Permission is hereby granted to use, copy, modify, and distribute this
source code, or portions hereof, for any purpose, without fee, subject
to the following restrictions:

 1. The origin of this source code must not be misrepresented.

 2. Altered versions must be plainly marked as such and must not
    be misrepresented as being the original source.

 3. This Copyright notice may not be removed or altered from any
    source or altered source distribution.

The Contributing Authors and Group 42, Inc. specifically permit,
without fee, and encourage the use of this source code as a component
to supporting the PNG file format in commercial products.  If you use
this source code in a product, acknowledgment is not required but would
be appreciated.
`}),e[14]||(e[14]=t("h4",null,"libssl1.1",-1)),e[15]||(e[15]=t("p",null,[n("This product includes software developed by the OpenSSL Project for use in the OpenSSL Toolkit ("),t("a",{href:"http://www.openssl.org/",target:"_blank",rel:"noopener noreferrer"},"http://www.openssl.org/"),n(")")],-1)),o(i,{code:`  LICENSE ISSUES
  ==============

  The OpenSSL toolkit stays under a double license, i.e. both the conditions of
  the OpenSSL License and the original SSLeay license apply to the toolkit.
  See below for the actual license texts.

  OpenSSL License
  ---------------

/* ====================================================================
 * Copyright (c) 1998-2019 The OpenSSL Project.  All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions
 * are met:
 *
 * 1. Redistributions of source code must retain the above copyright
 *    notice, this list of conditions and the following disclaimer.
 *
 * 2. Redistributions in binary form must reproduce the above copyright
 *    notice, this list of conditions and the following disclaimer in
 *    the documentation and/or other materials provided with the
 *    distribution.
 *
 * 3. All advertising materials mentioning features or use of this
 *    software must display the following acknowledgment:
 *    "This product includes software developed by the OpenSSL Project
 *    for use in the OpenSSL Toolkit. (http://www.openssl.org/)"
 *
 * 4. The names "OpenSSL Toolkit" and "OpenSSL Project" must not be used to
 *    endorse or promote products derived from this software without
 *    prior written permission. For written permission, please contact
 *    openssl-core@openssl.org.
 *
 * 5. Products derived from this software may not be called "OpenSSL"
 *    nor may "OpenSSL" appear in their names without prior written
 *    permission of the OpenSSL Project.
 *
 * 6. Redistributions of any form whatsoever must retain the following
 *    acknowledgment:
 *    "This product includes software developed by the OpenSSL Project
 *    for use in the OpenSSL Toolkit (http://www.openssl.org/)"
 *
 * THIS SOFTWARE IS PROVIDED BY THE OpenSSL PROJECT \`\`AS IS'' AND ANY
 * EXPRESSED OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
 * IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR
 * PURPOSE ARE DISCLAIMED.  IN NO EVENT SHALL THE OpenSSL PROJECT OR
 * ITS CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
 * SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT
 * NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION)
 * HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT,
 * STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
 * ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED
 * OF THE POSSIBILITY OF SUCH DAMAGE.
 * ====================================================================
 *
 * This product includes cryptographic software written by Eric Young
 * (eay@cryptsoft.com).  This product includes software written by Tim
 * Hudson (tjh@cryptsoft.com).
 *
 */

 Original SSLeay License
 -----------------------

/* Copyright (C) 1995-1998 Eric Young (eay@cryptsoft.com)
 * All rights reserved.
 *
 * This package is an SSL implementation written
 * by Eric Young (eay@cryptsoft.com).
 * The implementation was written so as to conform with Netscapes SSL.
 *
 * This library is free for commercial and non-commercial use as long as
 * the following conditions are aheared to.  The following conditions
 * apply to all code found in this distribution, be it the RC4, RSA,
 * lhash, DES, etc., code; not just the SSL code.  The SSL documentation
 * included with this distribution is covered by the same copyright terms
 * except that the holder is Tim Hudson (tjh@cryptsoft.com).
 *
 * Copyright remains Eric Young's, and as such any Copyright notices in
 * the code are not to be removed.
 * If this package is used in a product, Eric Young should be given attribution
 * as the author of the parts of the library used.
 * This can be in the form of a textual message at program startup or
 * in documentation (online or textual) provided with the package.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions
 * are met:
 * 1. Redistributions of source code must retain the copyright
 *    notice, this list of conditions and the following disclaimer.
 * 2. Redistributions in binary form must reproduce the above copyright
 *    notice, this list of conditions and the following disclaimer in the
 *    documentation and/or other materials provided with the distribution.
 * 3. All advertising materials mentioning features or use of this software
 *    must display the following acknowledgement:
 *    "This product includes cryptographic software written by
 *     Eric Young (eay@cryptsoft.com)"
 *    The word 'cryptographic' can be left out if the rouines from the library
 *    being used are not cryptographic related :-).
 * 4. If you include any Windows specific code (or a derivative thereof) from
 *    the apps directory (application code) you must include an acknowledgement:
 *    "This product includes software written by Tim Hudson (tjh@cryptsoft.com)"
 *
 * THIS SOFTWARE IS PROVIDED BY ERIC YOUNG \`\`AS IS'' AND
 * ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
 * IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 * ARE DISCLAIMED.  IN NO EVENT SHALL THE AUTHOR OR CONTRIBUTORS BE LIABLE
 * FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
 * DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS
 * OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION)
 * HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT
 * LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY
 * OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF
 * SUCH DAMAGE.
 *
 * The licence and distribution terms for any publically available version or
 * derivative of this code cannot be changed.  i.e. this code cannot simply be
 * copied and put under another distribution licence
 * [including the GNU Public Licence.]
 */
`}),e[16]||(e[16]=t("h4",null,"libuuid",-1)),o(i,{code:`                    GNU GENERAL PUBLIC LICENSE
                       Version 2, June 1991

 Copyright (C) 1989, 1991 Free Software Foundation, Inc.,
 51 Franklin Street, Fifth Floor, Boston, MA 02110-1301 USA
 Everyone is permitted to copy and distribute verbatim copies
 of this license document, but changing it is not allowed.

                            Preamble

  The licenses for most software are designed to take away your
freedom to share and change it.  By contrast, the GNU General Public
License is intended to guarantee your freedom to share and change free
software--to make sure the software is free for all its users.  This
General Public License applies to most of the Free Software
Foundation's software and to any other program whose authors commit to
using it.  (Some other Free Software Foundation software is covered by
the GNU Lesser General Public License instead.)  You can apply it to
your programs, too.

  When we speak of free software, we are referring to freedom, not
price.  Our General Public Licenses are designed to make sure that you
have the freedom to distribute copies of free software (and charge for
this service if you wish), that you receive source code or can get it
if you want it, that you can change the software or use pieces of it
in new free programs; and that you know you can do these things.

  To protect your rights, we need to make restrictions that forbid
anyone to deny you these rights or to ask you to surrender the rights.
These restrictions translate to certain responsibilities for you if you
distribute copies of the software, or if you modify it.

  For example, if you distribute copies of such a program, whether
gratis or for a fee, you must give the recipients all the rights that
you have.  You must make sure that they, too, receive or can get the
source code.  And you must show them these terms so they know their
rights.

  We protect your rights with two steps: (1) copyright the software, and
(2) offer you this license which gives you legal permission to copy,
distribute and/or modify the software.

  Also, for each author's protection and ours, we want to make certain
that everyone understands that there is no warranty for this free
software.  If the software is modified by someone else and passed on, we
want its recipients to know that what they have is not the original, so
that any problems introduced by others will not reflect on the original
authors' reputations.

  Finally, any free program is threatened constantly by software
patents.  We wish to avoid the danger that redistributors of a free
program will individually obtain patent licenses, in effect making the
program proprietary.  To prevent this, we have made it clear that any
patent must be licensed for everyone's free use or not licensed at all.

  The precise terms and conditions for copying, distribution and
modification follow.

                    GNU GENERAL PUBLIC LICENSE
   TERMS AND CONDITIONS FOR COPYING, DISTRIBUTION AND MODIFICATION

  0. This License applies to any program or other work which contains
a notice placed by the copyright holder saying it may be distributed
under the terms of this General Public License.  The "Program", below,
refers to any such program or work, and a "work based on the Program"
means either the Program or any derivative work under copyright law:
that is to say, a work containing the Program or a portion of it,
either verbatim or with modifications and/or translated into another
language.  (Hereinafter, translation is included without limitation in
the term "modification".)  Each licensee is addressed as "you".

Activities other than copying, distribution and modification are not
covered by this License; they are outside its scope.  The act of
running the Program is not restricted, and the output from the Program
is covered only if its contents constitute a work based on the
Program (independent of having been made by running the Program).
Whether that is true depends on what the Program does.

  1. You may copy and distribute verbatim copies of the Program's
source code as you receive it, in any medium, provided that you
conspicuously and appropriately publish on each copy an appropriate
copyright notice and disclaimer of warranty; keep intact all the
notices that refer to this License and to the absence of any warranty;
and give any other recipients of the Program a copy of this License
along with the Program.

You may charge a fee for the physical act of transferring a copy, and
you may at your option offer warranty protection in exchange for a fee.

  2. You may modify your copy or copies of the Program or any portion
of it, thus forming a work based on the Program, and copy and
distribute such modifications or work under the terms of Section 1
above, provided that you also meet all of these conditions:

    a) You must cause the modified files to carry prominent notices
    stating that you changed the files and the date of any change.

    b) You must cause any work that you distribute or publish, that in
    whole or in part contains or is derived from the Program or any
    part thereof, to be licensed as a whole at no charge to all third
    parties under the terms of this License.

    c) If the modified program normally reads commands interactively
    when run, you must cause it, when started running for such
    interactive use in the most ordinary way, to print or display an
    announcement including an appropriate copyright notice and a
    notice that there is no warranty (or else, saying that you provide
    a warranty) and that users may redistribute the program under
    these conditions, and telling the user how to view a copy of this
    License.  (Exception: if the Program itself is interactive but
    does not normally print such an announcement, your work based on
    the Program is not required to print an announcement.)

These requirements apply to the modified work as a whole.  If
identifiable sections of that work are not derived from the Program,
and can be reasonably considered independent and separate works in
themselves, then this License, and its terms, do not apply to those
sections when you distribute them as separate works.  But when you
distribute the same sections as part of a whole which is a work based
on the Program, the distribution of the whole must be on the terms of
this License, whose permissions for other licensees extend to the
entire whole, and thus to each and every part regardless of who wrote it.

Thus, it is not the intent of this section to claim rights or contest
your rights to work written entirely by you; rather, the intent is to
exercise the right to control the distribution of derivative or
collective works based on the Program.

In addition, mere aggregation of another work not based on the Program
with the Program (or with a work based on the Program) on a volume of
a storage or distribution medium does not bring the other work under
the scope of this License.

  3. You may copy and distribute the Program (or a work based on it,
under Section 2) in object code or executable form under the terms of
Sections 1 and 2 above provided that you also do one of the following:

    a) Accompany it with the complete corresponding machine-readable
    source code, which must be distributed under the terms of Sections
    1 and 2 above on a medium customarily used for software interchange; or,

    b) Accompany it with a written offer, valid for at least three
    years, to give any third party, for a charge no more than your
    cost of physically performing source distribution, a complete
    machine-readable copy of the corresponding source code, to be
    distributed under the terms of Sections 1 and 2 above on a medium
    customarily used for software interchange; or,

    c) Accompany it with the information you received as to the offer
    to distribute corresponding source code.  (This alternative is
    allowed only for noncommercial distribution and only if you
    received the program in object code or executable form with such
    an offer, in accord with Subsection b above.)

The source code for a work means the preferred form of the work for
making modifications to it.  For an executable work, complete source
code means all the source code for all modules it contains, plus any
associated interface definition files, plus the scripts used to
control compilation and installation of the executable.  However, as a
special exception, the source code distributed need not include
anything that is normally distributed (in either source or binary
form) with the major components (compiler, kernel, and so on) of the
operating system on which the executable runs, unless that component
itself accompanies the executable.

If distribution of executable or object code is made by offering
access to copy from a designated place, then offering equivalent
access to copy the source code from the same place counts as
distribution of the source code, even though third parties are not
compelled to copy the source along with the object code.

  4. You may not copy, modify, sublicense, or distribute the Program
except as expressly provided under this License.  Any attempt
otherwise to copy, modify, sublicense or distribute the Program is
void, and will automatically terminate your rights under this License.
However, parties who have received copies, or rights, from you under
this License will not have their licenses terminated so long as such
parties remain in full compliance.

  5. You are not required to accept this License, since you have not
signed it.  However, nothing else grants you permission to modify or
distribute the Program or its derivative works.  These actions are
prohibited by law if you do not accept this License.  Therefore, by
modifying or distributing the Program (or any work based on the
Program), you indicate your acceptance of this License to do so, and
all its terms and conditions for copying, distributing or modifying
the Program or works based on it.

  6. Each time you redistribute the Program (or any work based on the
Program), the recipient automatically receives a license from the
original licensor to copy, distribute or modify the Program subject to
these terms and conditions.  You may not impose any further
restrictions on the recipients' exercise of the rights granted herein.
You are not responsible for enforcing compliance by third parties to
this License.

  7. If, as a consequence of a court judgment or allegation of patent
infringement or for any other reason (not limited to patent issues),
conditions are imposed on you (whether by court order, agreement or
otherwise) that contradict the conditions of this License, they do not
excuse you from the conditions of this License.  If you cannot
distribute so as to satisfy simultaneously your obligations under this
License and any other pertinent obligations, then as a consequence you
may not distribute the Program at all.  For example, if a patent
license would not permit royalty-free redistribution of the Program by
all those who receive copies directly or indirectly through you, then
the only way you could satisfy both it and this License would be to
refrain entirely from distribution of the Program.

If any portion of this section is held invalid or unenforceable under
any particular circumstance, the balance of the section is intended to
apply and the section as a whole is intended to apply in other
circumstances.

It is not the purpose of this section to induce you to infringe any
patents or other property right claims or to contest validity of any
such claims; this section has the sole purpose of protecting the
integrity of the free software distribution system, which is
implemented by public license practices.  Many people have made
generous contributions to the wide range of software distributed
through that system in reliance on consistent application of that
system; it is up to the author/donor to decide if he or she is willing
to distribute software through any other system and a licensee cannot
impose that choice.

This section is intended to make thoroughly clear what is believed to
be a consequence of the rest of this License.

  8. If the distribution and/or use of the Program is restricted in
certain countries either by patents or by copyrighted interfaces, the
original copyright holder who places the Program under this License
may add an explicit geographical distribution limitation excluding
those countries, so that distribution is permitted only in or among
countries not thus excluded.  In such case, this License incorporates
the limitation as if written in the body of this License.

  9. The Free Software Foundation may publish revised and/or new versions
of the General Public License from time to time.  Such new versions will
be similar in spirit to the present version, but may differ in detail to
address new problems or concerns.

Each version is given a distinguishing version number.  If the Program
specifies a version number of this License which applies to it and "any
later version", you have the option of following the terms and conditions
either of that version or of any later version published by the Free
Software Foundation.  If the Program does not specify a version number of
this License, you may choose any version ever published by the Free Software
Foundation.

  10. If you wish to incorporate parts of the Program into other free
programs whose distribution conditions are different, write to the author
to ask for permission.  For software which is copyrighted by the Free
Software Foundation, write to the Free Software Foundation; we sometimes
make exceptions for this.  Our decision will be guided by the two goals
of preserving the free status of all derivatives of our free software and
of promoting the sharing and reuse of software generally.

                            NO WARRANTY

  11. BECAUSE THE PROGRAM IS LICENSED FREE OF CHARGE, THERE IS NO WARRANTY
FOR THE PROGRAM, TO THE EXTENT PERMITTED BY APPLICABLE LAW.  EXCEPT WHEN
OTHERWISE STATED IN WRITING THE COPYRIGHT HOLDERS AND/OR OTHER PARTIES
PROVIDE THE PROGRAM "AS IS" WITHOUT WARRANTY OF ANY KIND, EITHER EXPRESSED
OR IMPLIED, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF
MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE.  THE ENTIRE RISK AS
TO THE QUALITY AND PERFORMANCE OF THE PROGRAM IS WITH YOU.  SHOULD THE
PROGRAM PROVE DEFECTIVE, YOU ASSUME THE COST OF ALL NECESSARY SERVICING,
REPAIR OR CORRECTION.

  12. IN NO EVENT UNLESS REQUIRED BY APPLICABLE LAW OR AGREED TO IN WRITING
WILL ANY COPYRIGHT HOLDER, OR ANY OTHER PARTY WHO MAY MODIFY AND/OR
REDISTRIBUTE THE PROGRAM AS PERMITTED ABOVE, BE LIABLE TO YOU FOR DAMAGES,
INCLUDING ANY GENERAL, SPECIAL, INCIDENTAL OR CONSEQUENTIAL DAMAGES ARISING
OUT OF THE USE OR INABILITY TO USE THE PROGRAM (INCLUDING BUT NOT LIMITED
TO LOSS OF DATA OR DATA BEING RENDERED INACCURATE OR LOSSES SUSTAINED BY
YOU OR THIRD PARTIES OR A FAILURE OF THE PROGRAM TO OPERATE WITH ANY OTHER
PROGRAMS), EVEN IF SUCH HOLDER OR OTHER PARTY HAS BEEN ADVISED OF THE
POSSIBILITY OF SUCH DAMAGES.

                     END OF TERMS AND CONDITIONS

            How to Apply These Terms to Your New Programs

  If you develop a new program, and you want it to be of the greatest
possible use to the public, the best way to achieve this is to make it
free software which everyone can redistribute and change under these terms.

  To do so, attach the following notices to the program.  It is safest
to attach them to the start of each source file to most effectively
convey the exclusion of warranty; and each file should have at least
the "copyright" line and a pointer to where the full notice is found.

    <one line to give the program's name and a brief idea of what it does.>
    Copyright (C) <year>  <name of author>

    This program is free software; you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation; either version 2 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License along
    with this program; if not, write to the Free Software Foundation, Inc.,
    51 Franklin Street, Fifth Floor, Boston, MA 02110-1301 USA.

Also add information on how to contact you by electronic and paper mail.

If the program is interactive, make it output a short notice like this
when it starts in an interactive mode:

    Gnomovision version 69, Copyright (C) year name of author
    Gnomovision comes with ABSOLUTELY NO WARRANTY; for details type \`show w'.
    This is free software, and you are welcome to redistribute it
    under certain conditions; type \`show c' for details.

The hypothetical commands \`show w' and \`show c' should show the appropriate
parts of the General Public License.  Of course, the commands you use may
be called something other than \`show w' and \`show c'; they could even be
mouse-clicks or menu items--whatever suits your program.

You should also get your employer (if you work as a programmer) or your
school, if any, to sign a "copyright disclaimer" for the program, if
necessary.  Here is a sample; alter the names:

  Yoyodyne, Inc., hereby disclaims all copyright interest in the program
  \`Gnomovision' (which makes passes at compilers) written by James Hacker.

  <signature of Ty Coon>, 1 April 1989
  Ty Coon, President of Vice

This General Public License does not permit incorporating your program into
proprietary programs.  If your program is a subroutine library, you may
consider it more useful to permit linking proprietary applications with the
library.  If this is what you want to do, use the GNU Lesser General
Public License instead of this License.
`}),e[17]||(e[17]=t("h4",null,"libwebp",-1)),o(i,{code:`                    GNU GENERAL PUBLIC LICENSE
                       Version 2, June 1991

 Copyright (C) 1989, 1991 Free Software Foundation, Inc.,
 51 Franklin Street, Fifth Floor, Boston, MA 02110-1301 USA
 Everyone is permitted to copy and distribute verbatim copies
 of this license document, but changing it is not allowed.

                            Preamble

  The licenses for most software are designed to take away your
freedom to share and change it.  By contrast, the GNU General Public
License is intended to guarantee your freedom to share and change free
software--to make sure the software is free for all its users.  This
General Public License applies to most of the Free Software
Foundation's software and to any other program whose authors commit to
using it.  (Some other Free Software Foundation software is covered by
the GNU Lesser General Public License instead.)  You can apply it to
your programs, too.

  When we speak of free software, we are referring to freedom, not
price.  Our General Public Licenses are designed to make sure that you
have the freedom to distribute copies of free software (and charge for
this service if you wish), that you receive source code or can get it
if you want it, that you can change the software or use pieces of it
in new free programs; and that you know you can do these things.

  To protect your rights, we need to make restrictions that forbid
anyone to deny you these rights or to ask you to surrender the rights.
These restrictions translate to certain responsibilities for you if you
distribute copies of the software, or if you modify it.

  For example, if you distribute copies of such a program, whether
gratis or for a fee, you must give the recipients all the rights that
you have.  You must make sure that they, too, receive or can get the
source code.  And you must show them these terms so they know their
rights.

  We protect your rights with two steps: (1) copyright the software, and
(2) offer you this license which gives you legal permission to copy,
distribute and/or modify the software.

  Also, for each author's protection and ours, we want to make certain
that everyone understands that there is no warranty for this free
software.  If the software is modified by someone else and passed on, we
want its recipients to know that what they have is not the original, so
that any problems introduced by others will not reflect on the original
authors' reputations.

  Finally, any free program is threatened constantly by software
patents.  We wish to avoid the danger that redistributors of a free
program will individually obtain patent licenses, in effect making the
program proprietary.  To prevent this, we have made it clear that any
patent must be licensed for everyone's free use or not licensed at all.

  The precise terms and conditions for copying, distribution and
modification follow.

                    GNU GENERAL PUBLIC LICENSE
   TERMS AND CONDITIONS FOR COPYING, DISTRIBUTION AND MODIFICATION

  0. This License applies to any program or other work which contains
a notice placed by the copyright holder saying it may be distributed
under the terms of this General Public License.  The "Program", below,
refers to any such program or work, and a "work based on the Program"
means either the Program or any derivative work under copyright law:
that is to say, a work containing the Program or a portion of it,
either verbatim or with modifications and/or translated into another
language.  (Hereinafter, translation is included without limitation in
the term "modification".)  Each licensee is addressed as "you".

Activities other than copying, distribution and modification are not
covered by this License; they are outside its scope.  The act of
running the Program is not restricted, and the output from the Program
is covered only if its contents constitute a work based on the
Program (independent of having been made by running the Program).
Whether that is true depends on what the Program does.

  1. You may copy and distribute verbatim copies of the Program's
source code as you receive it, in any medium, provided that you
conspicuously and appropriately publish on each copy an appropriate
copyright notice and disclaimer of warranty; keep intact all the
notices that refer to this License and to the absence of any warranty;
and give any other recipients of the Program a copy of this License
along with the Program.

You may charge a fee for the physical act of transferring a copy, and
you may at your option offer warranty protection in exchange for a fee.

  2. You may modify your copy or copies of the Program or any portion
of it, thus forming a work based on the Program, and copy and
distribute such modifications or work under the terms of Section 1
above, provided that you also meet all of these conditions:

    a) You must cause the modified files to carry prominent notices
    stating that you changed the files and the date of any change.

    b) You must cause any work that you distribute or publish, that in
    whole or in part contains or is derived from the Program or any
    part thereof, to be licensed as a whole at no charge to all third
    parties under the terms of this License.

    c) If the modified program normally reads commands interactively
    when run, you must cause it, when started running for such
    interactive use in the most ordinary way, to print or display an
    announcement including an appropriate copyright notice and a
    notice that there is no warranty (or else, saying that you provide
    a warranty) and that users may redistribute the program under
    these conditions, and telling the user how to view a copy of this
    License.  (Exception: if the Program itself is interactive but
    does not normally print such an announcement, your work based on
    the Program is not required to print an announcement.)

These requirements apply to the modified work as a whole.  If
identifiable sections of that work are not derived from the Program,
and can be reasonably considered independent and separate works in
themselves, then this License, and its terms, do not apply to those
sections when you distribute them as separate works.  But when you
distribute the same sections as part of a whole which is a work based
on the Program, the distribution of the whole must be on the terms of
this License, whose permissions for other licensees extend to the
entire whole, and thus to each and every part regardless of who wrote it.

Thus, it is not the intent of this section to claim rights or contest
your rights to work written entirely by you; rather, the intent is to
exercise the right to control the distribution of derivative or
collective works based on the Program.

In addition, mere aggregation of another work not based on the Program
with the Program (or with a work based on the Program) on a volume of
a storage or distribution medium does not bring the other work under
the scope of this License.

  3. You may copy and distribute the Program (or a work based on it,
under Section 2) in object code or executable form under the terms of
Sections 1 and 2 above provided that you also do one of the following:

    a) Accompany it with the complete corresponding machine-readable
    source code, which must be distributed under the terms of Sections
    1 and 2 above on a medium customarily used for software interchange; or,

    b) Accompany it with a written offer, valid for at least three
    years, to give any third party, for a charge no more than your
    cost of physically performing source distribution, a complete
    machine-readable copy of the corresponding source code, to be
    distributed under the terms of Sections 1 and 2 above on a medium
    customarily used for software interchange; or,

    c) Accompany it with the information you received as to the offer
    to distribute corresponding source code.  (This alternative is
    allowed only for noncommercial distribution and only if you
    received the program in object code or executable form with such
    an offer, in accord with Subsection b above.)

The source code for a work means the preferred form of the work for
making modifications to it.  For an executable work, complete source
code means all the source code for all modules it contains, plus any
associated interface definition files, plus the scripts used to
control compilation and installation of the executable.  However, as a
special exception, the source code distributed need not include
anything that is normally distributed (in either source or binary
form) with the major components (compiler, kernel, and so on) of the
operating system on which the executable runs, unless that component
itself accompanies the executable.

If distribution of executable or object code is made by offering
access to copy from a designated place, then offering equivalent
access to copy the source code from the same place counts as
distribution of the source code, even though third parties are not
compelled to copy the source along with the object code.

  4. You may not copy, modify, sublicense, or distribute the Program
except as expressly provided under this License.  Any attempt
otherwise to copy, modify, sublicense or distribute the Program is
void, and will automatically terminate your rights under this License.
However, parties who have received copies, or rights, from you under
this License will not have their licenses terminated so long as such
parties remain in full compliance.

  5. You are not required to accept this License, since you have not
signed it.  However, nothing else grants you permission to modify or
distribute the Program or its derivative works.  These actions are
prohibited by law if you do not accept this License.  Therefore, by
modifying or distributing the Program (or any work based on the
Program), you indicate your acceptance of this License to do so, and
all its terms and conditions for copying, distributing or modifying
the Program or works based on it.

  6. Each time you redistribute the Program (or any work based on the
Program), the recipient automatically receives a license from the
original licensor to copy, distribute or modify the Program subject to
these terms and conditions.  You may not impose any further
restrictions on the recipients' exercise of the rights granted herein.
You are not responsible for enforcing compliance by third parties to
this License.

  7. If, as a consequence of a court judgment or allegation of patent
infringement or for any other reason (not limited to patent issues),
conditions are imposed on you (whether by court order, agreement or
otherwise) that contradict the conditions of this License, they do not
excuse you from the conditions of this License.  If you cannot
distribute so as to satisfy simultaneously your obligations under this
License and any other pertinent obligations, then as a consequence you
may not distribute the Program at all.  For example, if a patent
license would not permit royalty-free redistribution of the Program by
all those who receive copies directly or indirectly through you, then
the only way you could satisfy both it and this License would be to
refrain entirely from distribution of the Program.

If any portion of this section is held invalid or unenforceable under
any particular circumstance, the balance of the section is intended to
apply and the section as a whole is intended to apply in other
circumstances.

It is not the purpose of this section to induce you to infringe any
patents or other property right claims or to contest validity of any
such claims; this section has the sole purpose of protecting the
integrity of the free software distribution system, which is
implemented by public license practices.  Many people have made
generous contributions to the wide range of software distributed
through that system in reliance on consistent application of that
system; it is up to the author/donor to decide if he or she is willing
to distribute software through any other system and a licensee cannot
impose that choice.

This section is intended to make thoroughly clear what is believed to
be a consequence of the rest of this License.

  8. If the distribution and/or use of the Program is restricted in
certain countries either by patents or by copyrighted interfaces, the
original copyright holder who places the Program under this License
may add an explicit geographical distribution limitation excluding
those countries, so that distribution is permitted only in or among
countries not thus excluded.  In such case, this License incorporates
the limitation as if written in the body of this License.

  9. The Free Software Foundation may publish revised and/or new versions
of the General Public License from time to time.  Such new versions will
be similar in spirit to the present version, but may differ in detail to
address new problems or concerns.

Each version is given a distinguishing version number.  If the Program
specifies a version number of this License which applies to it and "any
later version", you have the option of following the terms and conditions
either of that version or of any later version published by the Free
Software Foundation.  If the Program does not specify a version number of
this License, you may choose any version ever published by the Free Software
Foundation.

  10. If you wish to incorporate parts of the Program into other free
programs whose distribution conditions are different, write to the author
to ask for permission.  For software which is copyrighted by the Free
Software Foundation, write to the Free Software Foundation; we sometimes
make exceptions for this.  Our decision will be guided by the two goals
of preserving the free status of all derivatives of our free software and
of promoting the sharing and reuse of software generally.

                            NO WARRANTY

  11. BECAUSE THE PROGRAM IS LICENSED FREE OF CHARGE, THERE IS NO WARRANTY
FOR THE PROGRAM, TO THE EXTENT PERMITTED BY APPLICABLE LAW.  EXCEPT WHEN
OTHERWISE STATED IN WRITING THE COPYRIGHT HOLDERS AND/OR OTHER PARTIES
PROVIDE THE PROGRAM "AS IS" WITHOUT WARRANTY OF ANY KIND, EITHER EXPRESSED
OR IMPLIED, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF
MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE.  THE ENTIRE RISK AS
TO THE QUALITY AND PERFORMANCE OF THE PROGRAM IS WITH YOU.  SHOULD THE
PROGRAM PROVE DEFECTIVE, YOU ASSUME THE COST OF ALL NECESSARY SERVICING,
REPAIR OR CORRECTION.

  12. IN NO EVENT UNLESS REQUIRED BY APPLICABLE LAW OR AGREED TO IN WRITING
WILL ANY COPYRIGHT HOLDER, OR ANY OTHER PARTY WHO MAY MODIFY AND/OR
REDISTRIBUTE THE PROGRAM AS PERMITTED ABOVE, BE LIABLE TO YOU FOR DAMAGES,
INCLUDING ANY GENERAL, SPECIAL, INCIDENTAL OR CONSEQUENTIAL DAMAGES ARISING
OUT OF THE USE OR INABILITY TO USE THE PROGRAM (INCLUDING BUT NOT LIMITED
TO LOSS OF DATA OR DATA BEING RENDERED INACCURATE OR LOSSES SUSTAINED BY
YOU OR THIRD PARTIES OR A FAILURE OF THE PROGRAM TO OPERATE WITH ANY OTHER
PROGRAMS), EVEN IF SUCH HOLDER OR OTHER PARTY HAS BEEN ADVISED OF THE
POSSIBILITY OF SUCH DAMAGES.

                     END OF TERMS AND CONDITIONS

            How to Apply These Terms to Your New Programs

  If you develop a new program, and you want it to be of the greatest
possible use to the public, the best way to achieve this is to make it
free software which everyone can redistribute and change under these terms.

  To do so, attach the following notices to the program.  It is safest
to attach them to the start of each source file to most effectively
convey the exclusion of warranty; and each file should have at least
the "copyright" line and a pointer to where the full notice is found.

    <one line to give the program's name and a brief idea of what it does.>
    Copyright (C) <year>  <name of author>

    This program is free software; you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation; either version 2 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License along
    with this program; if not, write to the Free Software Foundation, Inc.,
    51 Franklin Street, Fifth Floor, Boston, MA 02110-1301 USA.

Also add information on how to contact you by electronic and paper mail.

If the program is interactive, make it output a short notice like this
when it starts in an interactive mode:

    Gnomovision version 69, Copyright (C) year name of author
    Gnomovision comes with ABSOLUTELY NO WARRANTY; for details type \`show w'.
    This is free software, and you are welcome to redistribute it
    under certain conditions; type \`show c' for details.

The hypothetical commands \`show w' and \`show c' should show the appropriate
parts of the General Public License.  Of course, the commands you use may
be called something other than \`show w' and \`show c'; they could even be
mouse-clicks or menu items--whatever suits your program.

You should also get your employer (if you work as a programmer) or your
school, if any, to sign a "copyright disclaimer" for the program, if
necessary.  Here is a sample; alter the names:

  Yoyodyne, Inc., hereby disclaims all copyright interest in the program
  \`Gnomovision' (which makes passes at compilers) written by James Hacker.

  <signature of Ty Coon>, 1 April 1989
  Ty Coon, President of Vice

This General Public License does not permit incorporating your program into
proprietary programs.  If your program is a subroutine library, you may
consider it more useful to permit linking proprietary applications with the
library.  If this is what you want to do, use the GNU Lesser General
Public License instead of this License.
`}),e[18]||(e[18]=t("h4",null,"libx11",-1)),o(i,{code:`The following is the 'standard copyright' agreed upon by most contributors,
and is currently the canonical license preferred by the X.Org Foundation.
This is a slight variant of the common MIT license form published by the
Open Source Initiative at http://www.opensource.org/licenses/mit-license.php

Copyright holders of new code should use this license statement where
possible, and insert their name to this list.  Please sort by surname
for people, and by the full name for other entities (e.g.  Juliusz
Chroboczek sorts before Intel Corporation sorts before Daniel Stone).

See each individual source file or directory for the license that applies
to that file.

Copyright (C) 2003-2006,2008 Jamey Sharp, Josh Triplett
Copyright © 2009 Red Hat, Inc.
Copyright (c) 1990-1992, 1999, 2000, 2004, 2009, 2010, 2015, 2017, Oracle and/or its affiliates.

Permission is hereby granted, free of charge, to any person obtaining a
copy of this software and associated documentation files (the "Software"),
to deal in the Software without restriction, including without limitation
the rights to use, copy, modify, merge, publish, distribute, sublicense,
and/or sell copies of the Software, and to permit persons to whom the
Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice (including the next
paragraph) shall be included in all copies or substantial portions of the
Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.  IN NO EVENT SHALL
THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
DEALINGS IN THE SOFTWARE.

 ----------------------------------------------------------------------

The following licenses are 'legacy' - usually MIT/X11 licenses with the name
of the copyright holder(s) in the license statement:

Copyright 1984-1994, 1998 The Open Group

Permission to use, copy, modify, distribute, and sell this software and its
documentation for any purpose is hereby granted without fee, provided that
the above copyright notice appear in all copies and that both that
copyright notice and this permission notice appear in supporting
documentation.

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.  IN NO EVENT SHALL THE
OPEN GROUP BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN
AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

Except as contained in this notice, the name of The Open Group shall not be
used in advertising or otherwise to promote the sale, use or other dealings
in this Software without prior written authorization from The Open Group.

X Window System is a trademark of The Open Group.

		----------------------------------------

Copyright 1985, 1986, 1987, 1988, 1989, 1990, 1991, 1994, 1996 X Consortium
Copyright 2000 The XFree86 Project, Inc.

Permission is hereby granted, free of charge, to any person obtaining
a copy of this software and associated documentation files (the
"Software"), to deal in the Software without restriction, including
without limitation the rights to use, copy, modify, merge, publish,
distribute, sublicense, and/or sell copies of the Software, and to
permit persons to whom the Software is furnished to do so, subject to
the following conditions:

The above copyright notice and this permission notice shall be included
in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS
OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
IN NO EVENT SHALL THE X CONSORTIUM BE LIABLE FOR ANY CLAIM, DAMAGES OR
OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE,
ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
OTHER DEALINGS IN THE SOFTWARE.

Except as contained in this notice, the name of the X Consortium shall
not be used in advertising or otherwise to promote the sale, use or
other dealings in this Software without prior written authorization
from the X Consortium.

Copyright 1985, 1986, 1987, 1988, 1989, 1990, 1991 by
Digital Equipment Corporation

Portions Copyright 1990, 1991 by Tektronix, Inc.

Permission to use, copy, modify and distribute this documentation for
any purpose and without fee is hereby granted, provided that the above
copyright notice appears in all copies and that both that copyright notice
and this permission notice appear in all copies, and that the names of
Digital and Tektronix not be used in in advertising or publicity pertaining
to this documentation without specific, written prior permission.
Digital and Tektronix makes no representations about the suitability
of this documentation for any purpose.
It is provided \`\`as is'' without express or implied warranty.

		----------------------------------------

Copyright (c) 1999-2000  Free Software Foundation, Inc.

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.  IN NO EVENT SHALL THE
FREE SOFTWARE FOUNDATION BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR
IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

Except as contained in this notice, the name of the Free Software Foundation
shall not be used in advertising or otherwise to promote the sale, use or
other dealings in this Software without prior written authorization from the
Free Software Foundation.

		----------------------------------------

Code and supporting documentation (c) Copyright 1990 1991 Tektronix, Inc.
	All Rights Reserved

This file is a component of an X Window System-specific implementation
of Xcms based on the TekColor Color Management System.  TekColor is a
trademark of Tektronix, Inc.  The term "TekHVC" designates a particular
color space that is the subject of U.S. Patent No. 4,985,853 (equivalent
foreign patents pending).  Permission is hereby granted to use, copy,
modify, sell, and otherwise distribute this software and its
documentation for any purpose and without fee, provided that:

1. This copyright, permission, and disclaimer notice is reproduced in
   all copies of this software and any modification thereof and in
   supporting documentation;
2. Any color-handling application which displays TekHVC color
   cooordinates identifies these as TekHVC color coordinates in any
   interface that displays these coordinates and in any associated
   documentation;
3. The term "TekHVC" is always used, and is only used, in association
   with the mathematical derivations of the TekHVC Color Space,
   including those provided in this file and any equivalent pathways and
   mathematical derivations, regardless of digital (e.g., floating point
   or integer) representation.

Tektronix makes no representation about the suitability of this software
for any purpose.  It is provided "as is" and with all faults.

TEKTRONIX DISCLAIMS ALL WARRANTIES APPLICABLE TO THIS SOFTWARE,
INCLUDING THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A
PARTICULAR PURPOSE.  IN NO EVENT SHALL TEKTRONIX BE LIABLE FOR ANY
SPECIAL, INDIRECT OR CONSEQUENTIAL DAMAGES OR ANY DAMAGES WHATSOEVER
RESULTING FROM LOSS OF USE, DATA, OR PROFITS, WHETHER IN AN ACTION OF
CONTRACT, NEGLIGENCE, OR OTHER TORTIOUS ACTION, ARISING OUT OF OR IN
CONNECTION WITH THE USE OR THE PERFORMANCE OF THIS SOFTWARE.

		----------------------------------------

(c) Copyright 1995 FUJITSU LIMITED
This is source code modified by FUJITSU LIMITED under the Joint
Development Agreement for the CDE/Motif PST.

		----------------------------------------

Copyright 1992 by Oki Technosystems Laboratory, Inc.
Copyright 1992 by Fuji Xerox Co., Ltd.

Permission to use, copy, modify, distribute, and sell this software
and its documentation for any purpose is hereby granted without fee,
provided that the above copyright notice appear in all copies and
that both that copyright notice and this permission notice appear
in supporting documentation, and that the name of Oki Technosystems
Laboratory and Fuji Xerox not be used in advertising or publicity
pertaining to distribution of the software without specific, written
prior permission.
Oki Technosystems Laboratory and Fuji Xerox make no representations
about the suitability of this software for any purpose.  It is provided
"as is" without express or implied warranty.

OKI TECHNOSYSTEMS LABORATORY AND FUJI XEROX DISCLAIM ALL WARRANTIES
WITH REGARD TO THIS SOFTWARE, INCLUDING ALL IMPLIED WARRANTIES OF
MERCHANTABILITY AND FITNESS, IN NO EVENT SHALL OKI TECHNOSYSTEMS
LABORATORY AND FUJI XEROX BE LIABLE FOR ANY SPECIAL, INDIRECT OR
CONSEQUENTIAL DAMAGES OR ANY DAMAGES WHATSOEVER RESULTING FROM LOSS
OF USE, DATA OR PROFITS, WHETHER IN AN ACTION OF CONTRACT, NEGLIGENCE
OR OTHER TORTIOUS ACTION, ARISING OUT OF OR IN CONNECTION WITH THE USE
OR PERFORMANCE OF THIS SOFTWARE.

		----------------------------------------

Copyright 1990, 1991, 1992, 1993, 1994 by FUJITSU LIMITED

Permission to use, copy, modify, distribute, and sell this software
and its documentation for any purpose is hereby granted without fee,
provided that the above copyright notice appear in all copies and
that both that copyright notice and this permission notice appear
in supporting documentation, and that the name of FUJITSU LIMITED
not be used in advertising or publicity pertaining to distribution
of the software without specific, written prior permission.
FUJITSU LIMITED makes no representations about the suitability of
this software for any purpose.
It is provided "as is" without express or implied warranty.

FUJITSU LIMITED DISCLAIM ALL WARRANTIES WITH REGARD TO THIS SOFTWARE,
INCLUDING ALL IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS, IN NO
EVENT SHALL FUJITSU LIMITED BE LIABLE FOR ANY SPECIAL, INDIRECT OR
CONSEQUENTIAL DAMAGES OR ANY DAMAGES WHATSOEVER RESULTING FROM LOSS OF
USE, DATA OR PROFITS, WHETHER IN AN ACTION OF CONTRACT, NEGLIGENCE OR
OTHER TORTIOUS ACTION, ARISING OUT OF OR IN CONNECTION WITH THE USE OR
PERFORMANCE OF THIS SOFTWARE.

		----------------------------------------

Copyright 1990, 1991 by OMRON Corporation

Permission to use, copy, modify, distribute, and sell this software and its
documentation for any purpose is hereby granted without fee, provided that
the above copyright notice appear in all copies and that both that
copyright notice and this permission notice appear in supporting
documentation, and that the name OMRON not be used in
advertising or publicity pertaining to distribution of the software without
specific, written prior permission.  OMRON makes no representations
about the suitability of this software for any purpose.  It is provided
"as is" without express or implied warranty.

OMRON DISCLAIMS ALL WARRANTIES WITH REGARD TO THIS SOFTWARE,
INCLUDING ALL IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS, IN NO
EVENT SHALL OMRON BE LIABLE FOR ANY SPECIAL, INDIRECT OR
CONSEQUENTIAL DAMAGES OR ANY DAMAGES WHATSOEVER RESULTING FROM LOSS OF USE,
DATA OR PROFITS, WHETHER IN AN ACTION OF CONTRACT, NEGLIGENCE OR OTHER
TORTUOUS ACTION, ARISING OUT OF OR IN CONNECTION WITH THE USE OR
PERFORMANCE OF THIS SOFTWARE.

		----------------------------------------

Copyright 1985, 1986, 1987, 1988, 1989, 1990, 1991 by
Digital Equipment Corporation

Portions Copyright 1990, 1991 by Tektronix, Inc

Rewritten for X.org by Chris Lee <clee@freedesktop.org>

Permission to use, copy, modify, distribute, and sell this documentation
for any purpose and without fee is hereby granted, provided that the above
copyright notice and this permission notice appear in all copies.
Chris Lee makes no representations about the suitability for any purpose
of the information in this document.  It is provided \\\`\\\`as-is'' without
express or implied warranty.

		----------------------------------------

Copyright 1993 by Digital Equipment Corporation, Maynard, Massachusetts,
Copyright 1994 by FUJITSU LIMITED
Copyright 1994 by Sony Corporation

                        All Rights Reserved

Permission to use, copy, modify, and distribute this software and its
documentation for any purpose and without fee is hereby granted,
provided that the above copyright notice appear in all copies and that
both that copyright notice and this permission notice appear in
supporting documentation, and that the names of Digital, FUJITSU
LIMITED and Sony Corporation not be used in advertising or publicity
pertaining to distribution of the software without specific, written
prior permission.

DIGITAL, FUJITSU LIMITED AND SONY CORPORATION DISCLAIMS ALL WARRANTIES
WITH REGARD TO THIS SOFTWARE, INCLUDING ALL IMPLIED WARRANTIES OF
MERCHANTABILITY AND FITNESS, IN NO EVENT SHALL DIGITAL, FUJITSU LIMITED
AND SONY CORPORATION BE LIABLE FOR ANY SPECIAL, INDIRECT OR
CONSEQUENTIAL DAMAGES OR ANY DAMAGES WHATSOEVER RESULTING FROM LOSS OF
USE, DATA OR PROFITS, WHETHER IN AN ACTION OF CONTRACT, NEGLIGENCE OR
OTHER TORTIOUS ACTION, ARISING OUT OF OR IN CONNECTION WITH THE USE OR
PERFORMANCE OF THIS SOFTWARE.

		----------------------------------------


Copyright 1991 by the Open Software Foundation

Permission to use, copy, modify, distribute, and sell this software and its
documentation for any purpose is hereby granted without fee, provided that
the above copyright notice appear in all copies and that both that
copyright notice and this permission notice appear in supporting
documentation, and that the name of Open Software Foundation
not be used in advertising or publicity pertaining to distribution of the
software without specific, written prior permission.  Open Software
Foundation makes no representations about the suitability of this
software for any purpose.  It is provided "as is" without express or
implied warranty.

OPEN SOFTWARE FOUNDATION DISCLAIMS ALL WARRANTIES WITH REGARD TO
THIS SOFTWARE, INCLUDING ALL IMPLIED WARRANTIES OF MERCHANTABILITY AND
FITNESS, IN NO EVENT SHALL OPEN SOFTWARE FOUNDATIONN BE
LIABLE FOR ANY SPECIAL, INDIRECT OR CONSEQUENTIAL DAMAGES OR ANY DAMAGES
WHATSOEVER RESULTING FROM LOSS OF USE, DATA OR PROFITS, WHETHER IN AN
ACTION OF CONTRACT, NEGLIGENCE OR OTHER TORTIOUS ACTION, ARISING OUT OF
OR IN CONNECTION WITH THE USE OR PERFORMANCE OF THIS SOFTWARE.

		----------------------------------------

Copyright 1990, 1991, 1992,1993, 1994 by FUJITSU LIMITED
Copyright 1993, 1994                  by Sony Corporation

Permission to use, copy, modify, distribute, and sell this software and
its documentation for any purpose is hereby granted without fee, provided
that the above copyright notice appear in all copies and that both that
copyright notice and this permission notice appear in supporting
documentation, and that the name of FUJITSU LIMITED and Sony Corporation
not be used in advertising or publicity pertaining to distribution of the
software without specific, written prior permission.  FUJITSU LIMITED and
Sony Corporation makes no representations about the suitability of this
software for any purpose.  It is provided "as is" without express or
implied warranty.

FUJITSU LIMITED AND SONY CORPORATION DISCLAIMS ALL WARRANTIES WITH REGARD
TO THIS SOFTWARE, INCLUDING ALL IMPLIED WARRANTIES OF MERCHANTABILITY AND
FITNESS, IN NO EVENT SHALL FUJITSU LIMITED OR SONY CORPORATION BE LIABLE
FOR ANY SPECIAL, INDIRECT OR CONSEQUENTIAL DAMAGES OR ANY DAMAGES WHATSOEVER
RESULTING FROM LOSS OF USE, DATA OR PROFITS, WHETHER IN AN ACTION OF CONTRACT,
NEGLIGENCE OR OTHER TORTIOUS ACTION, ARISING OUT OF OR IN CONNECTION WITH THE
USE OR PERFORMANCE OF THIS SOFTWARE.

		----------------------------------------

Copyright (c) 1993, 1995 by Silicon Graphics Computer Systems, Inc.

Permission to use, copy, modify, and distribute this
software and its documentation for any purpose and without
fee is hereby granted, provided that the above copyright
notice appear in all copies and that both that copyright
notice and this permission notice appear in supporting
documentation, and that the name of Silicon Graphics not be
used in advertising or publicity pertaining to distribution
of the software without specific prior written permission.
Silicon Graphics makes no representation about the suitability
of this software for any purpose. It is provided "as is"
without any express or implied warranty.

SILICON GRAPHICS DISCLAIMS ALL WARRANTIES WITH REGARD TO THIS
SOFTWARE, INCLUDING ALL IMPLIED WARRANTIES OF MERCHANTABILITY
AND FITNESS FOR A PARTICULAR PURPOSE. IN NO EVENT SHALL SILICON
GRAPHICS BE LIABLE FOR ANY SPECIAL, INDIRECT OR CONSEQUENTIAL
DAMAGES OR ANY DAMAGES WHATSOEVER RESULTING FROM LOSS OF USE,
DATA OR PROFITS, WHETHER IN AN ACTION OF CONTRACT, NEGLIGENCE
OR OTHER TORTIOUS ACTION, ARISING OUT OF OR IN CONNECTION  WITH
THE USE OR PERFORMANCE OF THIS SOFTWARE.

		----------------------------------------

Copyright 1991, 1992, 1993, 1994 by FUJITSU LIMITED
Copyright 1993 by Digital Equipment Corporation

Permission to use, copy, modify, distribute, and sell this software
and its documentation for any purpose is hereby granted without fee,
provided that the above copyright notice appear in all copies and that
both that copyright notice and this permission notice appear in
supporting documentation, and that the name of FUJITSU LIMITED and
Digital Equipment Corporation not be used in advertising or publicity
pertaining to distribution of the software without specific, written
prior permission.  FUJITSU LIMITED and Digital Equipment Corporation
makes no representations about the suitability of this software for
any purpose.  It is provided "as is" without express or implied
warranty.

FUJITSU LIMITED AND DIGITAL EQUIPMENT CORPORATION DISCLAIM ALL
WARRANTIES WITH REGARD TO THIS SOFTWARE, INCLUDING ALL IMPLIED
WARRANTIES OF MERCHANTABILITY AND FITNESS, IN NO EVENT SHALL
FUJITSU LIMITED AND DIGITAL EQUIPMENT CORPORATION BE LIABLE FOR
ANY SPECIAL, INDIRECT OR CONSEQUENTIAL DAMAGES OR ANY DAMAGES
WHATSOEVER RESULTING FROM LOSS OF USE, DATA OR PROFITS, WHETHER
IN AN ACTION OF CONTRACT, NEGLIGENCE OR OTHER TORTIOUS ACTION,
ARISING OUT OF OR IN CONNECTION WITH THE USE OR PERFORMANCE OF
THIS SOFTWARE.

		----------------------------------------

Copyright 1992, 1993 by FUJITSU LIMITED
Copyright 1993 by Fujitsu Open Systems Solutions, Inc.
Copyright 1994 by Sony Corporation

Permission to use, copy, modify, distribute and sell this software
and its documentation for any purpose is hereby granted without fee,
provided that the above copyright notice appear in all copies and
that both that copyright notice and this permission notice appear
in supporting documentation, and that the name of FUJITSU LIMITED,
Fujitsu Open Systems Solutions, Inc. and Sony Corporation  not be
used in advertising or publicity pertaining to distribution of the
software without specific, written prior permission.
FUJITSU LIMITED, Fujitsu Open Systems Solutions, Inc. and
Sony Corporation make no representations about the suitability of
this software for any purpose.  It is provided "as is" without
express or implied warranty.

FUJITSU LIMITED, FUJITSU OPEN SYSTEMS SOLUTIONS, INC. AND SONY
CORPORATION DISCLAIM ALL WARRANTIES WITH REGARD TO THIS SOFTWARE,
INCLUDING ALL IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS,
IN NO EVENT SHALL FUJITSU OPEN SYSTEMS SOLUTIONS, INC., FUJITSU LIMITED
AND SONY CORPORATION BE LIABLE FOR ANY SPECIAL, INDIRECT OR
CONSEQUENTIAL DAMAGES OR ANY DAMAGES WHATSOEVER RESULTING FROM LOSS
OF USE, DATA OR PROFITS, WHETHER IN AN ACTION OF CONTRACT, NEGLIGENCE
OR OTHER TORTIOUS ACTION, ARISING OUT OF OR IN CONNECTION WITH THE USE
OR PERFORMANCE OF THIS SOFTWARE.

		----------------------------------------

Copyright 1987, 1988, 1990, 1993 by Digital Equipment Corporation,
Maynard, Massachusetts,

                        All Rights Reserved

Permission to use, copy, modify, and distribute this software and its
documentation for any purpose and without fee is hereby granted,
provided that the above copyright notice appear in all copies and that
both that copyright notice and this permission notice appear in
supporting documentation, and that the name of Digital not be
used in advertising or publicity pertaining to distribution of the
software without specific, written prior permission.

DIGITAL DISCLAIMS ALL WARRANTIES WITH REGARD TO THIS SOFTWARE, INCLUDING
ALL IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS, IN NO EVENT SHALL
DIGITAL BE LIABLE FOR ANY SPECIAL, INDIRECT OR CONSEQUENTIAL DAMAGES OR
ANY DAMAGES WHATSOEVER RESULTING FROM LOSS OF USE, DATA OR PROFITS,
WHETHER IN AN ACTION OF CONTRACT, NEGLIGENCE OR OTHER TORTIOUS ACTION,
ARISING OUT OF OR IN CONNECTION WITH THE USE OR PERFORMANCE OF THIS
SOFTWARE.

		----------------------------------------

Copyright 1993 by SunSoft, Inc.
Copyright 1999-2000 by Bruno Haible

Permission to use, copy, modify, distribute, and sell this software
and its documentation for any purpose is hereby granted without fee,
provided that the above copyright notice appear in all copies and
that both that copyright notice and this permission notice appear
in supporting documentation, and that the names of SunSoft, Inc. and
Bruno Haible not be used in advertising or publicity pertaining to
distribution of the software without specific, written prior
permission.  SunSoft, Inc. and Bruno Haible make no representations
about the suitability of this software for any purpose.  It is
provided "as is" without express or implied warranty.

SunSoft Inc. AND Bruno Haible DISCLAIM ALL WARRANTIES WITH REGARD
TO THIS SOFTWARE, INCLUDING ALL IMPLIED WARRANTIES OF MERCHANTABILITY
AND FITNESS, IN NO EVENT SHALL SunSoft, Inc. OR Bruno Haible BE LIABLE
FOR ANY SPECIAL, INDIRECT OR CONSEQUENTIAL DAMAGES OR ANY DAMAGES
WHATSOEVER RESULTING FROM LOSS OF USE, DATA OR PROFITS, WHETHER IN AN
ACTION OF CONTRACT, NEGLIGENCE OR OTHER TORTIOUS ACTION, ARISING OUT
OF OR IN CONNECTION WITH THE USE OR PERFORMANCE OF THIS SOFTWARE.

		----------------------------------------

Copyright 1991 by the Open Software Foundation
Copyright 1993 by the TOSHIBA Corp.

Permission to use, copy, modify, distribute, and sell this software and its
documentation for any purpose is hereby granted without fee, provided that
the above copyright notice appear in all copies and that both that
copyright notice and this permission notice appear in supporting
documentation, and that the names of Open Software Foundation and TOSHIBA
not be used in advertising or publicity pertaining to distribution of the
software without specific, written prior permission.  Open Software
Foundation and TOSHIBA make no representations about the suitability of this
software for any purpose.  It is provided "as is" without express or
implied warranty.

OPEN SOFTWARE FOUNDATION AND TOSHIBA DISCLAIM ALL WARRANTIES WITH REGARD TO
THIS SOFTWARE, INCLUDING ALL IMPLIED WARRANTIES OF MERCHANTABILITY AND
FITNESS, IN NO EVENT SHALL OPEN SOFTWARE FOUNDATIONN OR TOSHIBA BE
LIABLE FOR ANY SPECIAL, INDIRECT OR CONSEQUENTIAL DAMAGES OR ANY DAMAGES
WHATSOEVER RESULTING FROM LOSS OF USE, DATA OR PROFITS, WHETHER IN AN
ACTION OF CONTRACT, NEGLIGENCE OR OTHER TORTIOUS ACTION, ARISING OUT OF
OR IN CONNECTION WITH THE USE OR PERFORMANCE OF THIS SOFTWARE.

		----------------------------------------

Copyright 1988 by Wyse Technology, Inc., San Jose, Ca.,

                        All Rights Reserved

Permission to use, copy, modify, and distribute this software and its
documentation for any purpose and without fee is hereby granted,
provided that the above copyright notice appear in all copies and that
both that copyright notice and this permission notice appear in
supporting documentation, and that the name Wyse not be
used in advertising or publicity pertaining to distribution of the
software without specific, written prior permission.

WYSE DISCLAIMS ALL WARRANTIES WITH REGARD TO THIS SOFTWARE, INCLUDING
ALL IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS, IN NO EVENT SHALL
DIGITAL BE LIABLE FOR ANY SPECIAL, INDIRECT OR CONSEQUENTIAL DAMAGES OR
ANY DAMAGES WHATSOEVER RESULTING FROM LOSS OF USE, DATA OR PROFITS,
WHETHER IN AN ACTION OF CONTRACT, NEGLIGENCE OR OTHER TORTIOUS ACTION,
ARISING OUT OF OR IN CONNECTION WITH THE USE OR PERFORMANCE OF THIS
SOFTWARE.

		----------------------------------------


Copyright 1991 by the Open Software Foundation
Copyright 1993, 1994 by the Sony Corporation

Permission to use, copy, modify, distribute, and sell this software and its
documentation for any purpose is hereby granted without fee, provided that
the above copyright notice appear in all copies and that both that
copyright notice and this permission notice appear in supporting
documentation, and that the names of Open Software Foundation and
Sony Corporation not be used in advertising or publicity pertaining to
distribution of the software without specific, written prior permission.
Open Software Foundation and Sony Corporation make no
representations about the suitability of this software for any purpose.
It is provided "as is" without express or implied warranty.

OPEN SOFTWARE FOUNDATION AND SONY CORPORATION DISCLAIM ALL
WARRANTIES WITH REGARD TO THIS SOFTWARE, INCLUDING ALL IMPLIED
WARRANTIES OF MERCHANTABILITY AND FITNESS, IN NO EVENT SHALL OPEN
SOFTWARE FOUNDATIONN OR SONY CORPORATION BE LIABLE FOR ANY SPECIAL,
INDIRECT OR CONSEQUENTIAL DAMAGES OR ANY DAMAGES WHATSOEVER RESULTING FROM
LOSS OF USE, DATA OR PROFITS, WHETHER IN AN ACTION OF CONTRACT, NEGLIGENCE
OR OTHER TORTIOUS ACTION, ARISING OUT OF OR IN CONNECTION WITH THE USE OR
PERFORMANCE OF THIS SOFTWARE.

		----------------------------------------

Copyright 1992, 1993 by FUJITSU LIMITED
Copyright 1993 by Fujitsu Open Systems Solutions, Inc.

Permission to use, copy, modify, distribute and sell this software
and its documentation for any purpose is hereby granted without fee,
provided that the above copyright notice appear in all copies and
that both that copyright notice and this permission notice appear
in supporting documentation, and that the name of FUJITSU LIMITED and
Fujitsu Open Systems Solutions, Inc. not be used in advertising or
publicity pertaining to distribution of the software without specific,
written prior permission.
FUJITSU LIMITED and Fujitsu Open Systems Solutions, Inc. makes no
representations about the suitability of this software for any purpose.
It is provided "as is" without express or implied warranty.

FUJITSU LIMITED AND FUJITSU OPEN SYSTEMS SOLUTIONS, INC. DISCLAIMS ALL
WARRANTIES WITH REGARD TO THIS SOFTWARE, INCLUDING ALL IMPLIED WARRANTIES
OF MERCHANTABILITY AND FITNESS, IN NO EVENT SHALL FUJITSU OPEN SYSTEMS
SOLUTIONS, INC. AND FUJITSU LIMITED BE LIABLE FOR ANY SPECIAL, INDIRECT
OR CONSEQUENTIAL DAMAGES OR ANY DAMAGES WHATSOEVER RESULTING FROM LOSS OF
USE, DATA OR PROFITS, WHETHER IN AN ACTION OF CONTRACT, NEGLIGENCE OR OTHER
TORTIOUS ACTION, ARISING OUT OF OR IN CONNECTION WITH THE USE OR PERFORMANCE
OF THIS SOFTWARE.

		----------------------------------------

Copyright 1993, 1994 by Sony Corporation

Permission to use, copy, modify, distribute, and sell this software
and its documentation for any purpose is hereby granted without fee,
provided that the above copyright notice appear in all copies and
that both that copyright notice and this permission notice appear
in supporting documentation, and that the name of Sony Corporation
not be used in advertising or publicity pertaining to distribution
of the software without specific, written prior permission.
Sony Corporation makes no representations about the suitability of
this software for any purpose. It is provided "as is" without
express or implied warranty.

SONY CORPORATION DISCLAIMS ALL WARRANTIES WITH REGARD TO THIS SOFTWARE,
INCLUDING ALL IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS, IN NO
EVENT SHALL SONY CORPORATION BE LIABLE FOR ANY SPECIAL, INDIRECT OR
CONSEQUENTIAL DAMAGES OR ANY DAMAGES WHATSOEVER RESULTING FROM LOSS OF
USE, DATA OR PROFITS, WHETHER IN AN ACTION OF CONTRACT, NEGLIGENCE OR
OTHER TORTIOUS ACTION, ARISING OUT OF OR IN CONNECTION WITH THE USE OR
PERFORMANCE OF THIS SOFTWARE.

		----------------------------------------

Copyright 1986, 1998  The Open Group
Copyright (c) 2000  The XFree86 Project, Inc.

Permission to use, copy, modify, distribute, and sell this software and its
documentation for any purpose is hereby granted without fee, provided that
the above copyright notice appear in all copies and that both that
copyright notice and this permission notice appear in supporting
documentation.

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.  IN NO EVENT SHALL THE
X CONSORTIUM OR THE XFREE86 PROJECT BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.

Except as contained in this notice, the name of the X Consortium or of the
XFree86 Project shall not be used in advertising or otherwise to promote the
sale, use or other dealings in this Software without prior written
authorization from the X Consortium and the XFree86 Project.

		----------------------------------------

Copyright 1990, 1991 by OMRON Corporation, NTT Software Corporation,
                     and Nippon Telegraph and Telephone Corporation
Copyright 1991 by the Open Software Foundation
Copyright 1993 by the FUJITSU LIMITED

Permission to use, copy, modify, distribute, and sell this software and its
documentation for any purpose is hereby granted without fee, provided that
the above copyright notice appear in all copies and that both that
copyright notice and this permission notice appear in supporting
documentation, and that the names of OMRON, NTT Software, NTT, and
Open Software Foundation not be used in advertising or publicity
pertaining to distribution of the software without specific,
written prior permission. OMRON, NTT Software, NTT, and Open Software
Foundation make no representations about the suitability of this
software for any purpose.  It is provided "as is" without express or
implied warranty.

OMRON, NTT SOFTWARE, NTT, AND OPEN SOFTWARE FOUNDATION
DISCLAIM ALL WARRANTIES WITH REGARD TO THIS SOFTWARE, INCLUDING
ALL IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS, IN NO EVENT
SHALL OMRON, NTT SOFTWARE, NTT, OR OPEN SOFTWARE FOUNDATION BE
LIABLE FOR ANY SPECIAL, INDIRECT OR CONSEQUENTIAL DAMAGES OR ANY DAMAGES
WHATSOEVER RESULTING FROM LOSS OF USE, DATA OR PROFITS, WHETHER IN AN
ACTION OF CONTRACT, NEGLIGENCE OR OTHER TORTIOUS ACTION, ARISING OUT OF
OR IN CONNECTION WITH THE USE OR PERFORMANCE OF THIS SOFTWARE.

		----------------------------------------

Copyright 1988 by Wyse Technology, Inc., San Jose, Ca,
Copyright 1987 by Digital Equipment Corporation, Maynard, Massachusetts,

                        All Rights Reserved

Permission to use, copy, modify, and distribute this software and its
documentation for any purpose and without fee is hereby granted,
provided that the above copyright notice appear in all copies and that
both that copyright notice and this permission notice appear in
supporting documentation, and that the name Digital not be
used in advertising or publicity pertaining to distribution of the
software without specific, written prior permission.

DIGITAL AND WYSE DISCLAIM ALL WARRANTIES WITH REGARD TO THIS SOFTWARE,
INCLUDING ALL IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS, IN NO
EVENT SHALL DIGITAL OR WYSE BE LIABLE FOR ANY SPECIAL, INDIRECT OR
CONSEQUENTIAL DAMAGES OR ANY DAMAGES WHATSOEVER RESULTING FROM LOSS OF
USE, DATA OR PROFITS, WHETHER IN AN ACTION OF CONTRACT, NEGLIGENCE OR
OTHER TORTIOUS ACTION, ARISING OUT OF OR IN CONNECTION WITH THE USE OR
PERFORMANCE OF THIS SOFTWARE.

		----------------------------------------


Copyright 1991, 1992 by Fuji Xerox Co., Ltd.
Copyright 1992, 1993, 1994 by FUJITSU LIMITED

Permission to use, copy, modify, distribute, and sell this software
and its documentation for any purpose is hereby granted without fee,
provided that the above copyright notice appear in all copies and
that both that copyright notice and this permission notice appear
in supporting documentation, and that the name of Fuji Xerox,
FUJITSU LIMITED not be used in advertising or publicity pertaining
to distribution of the software without specific, written prior
permission. Fuji Xerox, FUJITSU LIMITED make no representations
about the suitability of this software for any purpose.
It is provided "as is" without express or implied warranty.

FUJI XEROX, FUJITSU LIMITED DISCLAIM ALL WARRANTIES WITH
REGARD TO THIS SOFTWARE, INCLUDING ALL IMPLIED WARRANTIES OF
MERCHANTABILITY AND FITNESS, IN NO EVENT SHALL FUJI XEROX,
FUJITSU LIMITED BE LIABLE FOR ANY SPECIAL, INDIRECT OR CONSEQUENTIAL
DAMAGES OR ANY DAMAGES WHATSOEVER RESULTING FROM LOSS OF USE, DATA
OR PROFITS, WHETHER IN AN ACTION OF CONTRACT, NEGLIGENCE OR OTHER
TORTIOUS ACTION, ARISING OUT OF OR IN CONNECTION WITH THE USE OR
PERFORMANCE OF THIS SOFTWARE.

		----------------------------------------

Copyright 2006 Josh Triplett

Permission is hereby granted, free of charge, to any person obtaining
a copy of this software and associated documentation files (the
"Software"), to deal in the Software without restriction, including
without limitation the rights to use, copy, modify, merge, publish,
distribute, sublicense, and/or sell copies of the Software, and to
permit persons to whom the Software is furnished to do so, subject to
the following conditions:

The above copyright notice and this permission notice shall be included
in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS
OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
IN NO EVENT SHALL THE X CONSORTIUM BE LIABLE FOR ANY CLAIM, DAMAGES OR
OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE,
ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
OTHER DEALINGS IN THE SOFTWARE.

		----------------------------------------

(c) Copyright 1996 by Sebastien Marineau and Holger Veit
			<marineau@genie.uottawa.ca>
                     <Holger.Veit@gmd.de>

Permission is hereby granted, free of charge, to any person obtaining a
copy of this software and associated documentation files (the "Software"),
to deal in the Software without restriction, including without limitation
the rights to use, copy, modify, merge, publish, distribute, sublicense,
and/or sell copies of the Software, and to permit persons to whom the
Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.  IN NO EVENT SHALL
HOLGER VEIT  BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF
OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.

Except as contained in this notice, the name of Sebastien Marineau or Holger Veit
shall not be used in advertising or otherwise to promote the sale, use or other
dealings in this Software without prior written authorization from Holger Veit or
Sebastien Marineau.

		----------------------------------------

Copyright 1990, 1991 by OMRON Corporation, NTT Software Corporation,
                     and Nippon Telegraph and Telephone Corporation
Copyright 1991 by the Open Software Foundation
Copyright 1993 by the TOSHIBA Corp.
Copyright 1993, 1994 by Sony Corporation
Copyright 1993, 1994 by the FUJITSU LIMITED

Permission to use, copy, modify, distribute, and sell this software and its
documentation for any purpose is hereby granted without fee, provided that
the above copyright notice appear in all copies and that both that
copyright notice and this permission notice appear in supporting
documentation, and that the names of OMRON, NTT Software, NTT, Open
Software Foundation, and Sony Corporation not be used in advertising
or publicity pertaining to distribution of the software without specific,
written prior permission. OMRON, NTT Software, NTT, Open Software
Foundation, and Sony Corporation  make no representations about the
suitability of this software for any purpose.  It is provided "as is"
without express or implied warranty.

OMRON, NTT SOFTWARE, NTT, OPEN SOFTWARE FOUNDATION, AND SONY
CORPORATION DISCLAIM ALL WARRANTIES WITH REGARD TO THIS SOFTWARE, INCLUDING
ALL IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS, IN NO EVENT
SHALL OMRON, NTT SOFTWARE, NTT, OPEN SOFTWARE FOUNDATION, OR SONY
CORPORATION BE LIABLE FOR ANY SPECIAL, INDIRECT OR CONSEQUENTIAL DAMAGES OR
ANY DAMAGES WHATSOEVER RESULTING FROM LOSS OF USE, DATA OR PROFITS, WHETHER
IN AN ACTION OF CONTRACT, NEGLIGENCE OR OTHER TORTIOUS ACTION, ARISING OUT
OF OR IN CONNECTION WITH THE USE OR PERFORMANCE OF THIS SOFTWARE.

		----------------------------------------

Copyright 2000 by Bruno Haible

Permission to use, copy, modify, distribute, and sell this software
and its documentation for any purpose is hereby granted without fee,
provided that the above copyright notice appear in all copies and
that both that copyright notice and this permission notice appear
in supporting documentation, and that the name of Bruno Haible not
be used in advertising or publicity pertaining to distribution of the
software without specific, written prior permission.  Bruno Haible
makes no representations about the suitability of this software for
any purpose.  It is provided "as is" without express or implied
warranty.

Bruno Haible DISCLAIMS ALL WARRANTIES WITH REGARD TO THIS SOFTWARE,
INCLUDING ALL IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS, IN
NO EVENT SHALL Bruno Haible BE LIABLE FOR ANY SPECIAL, INDIRECT OR
CONSEQUENTIAL DAMAGES OR ANY DAMAGES WHATSOEVER RESULTING FROM LOSS
OF USE, DATA OR PROFITS, WHETHER IN AN ACTION OF CONTRACT, NEGLIGENCE
OR OTHER TORTIOUS ACTION, ARISING OUT OF OR IN CONNECTION WITH THE USE
OR PERFORMANCE OF THIS SOFTWARE.

		----------------------------------------

Copyright © 2003 Keith Packard

Permission to use, copy, modify, distribute, and sell this software and its
documentation for any purpose is hereby granted without fee, provided that
the above copyright notice appear in all copies and that both that
copyright notice and this permission notice appear in supporting
documentation, and that the name of Keith Packard not be used in
advertising or publicity pertaining to distribution of the software without
specific, written prior permission.  Keith Packard makes no
representations about the suitability of this software for any purpose.  It
is provided "as is" without express or implied warranty.

KEITH PACKARD DISCLAIMS ALL WARRANTIES WITH REGARD TO THIS SOFTWARE,
INCLUDING ALL IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS, IN NO
EVENT SHALL KEITH PACKARD BE LIABLE FOR ANY SPECIAL, INDIRECT OR
CONSEQUENTIAL DAMAGES OR ANY DAMAGES WHATSOEVER RESULTING FROM LOSS OF USE,
DATA OR PROFITS, WHETHER IN AN ACTION OF CONTRACT, NEGLIGENCE OR OTHER
TORTIOUS ACTION, ARISING OUT OF OR IN CONNECTION WITH THE USE OR
PERFORMANCE OF THIS SOFTWARE.

		----------------------------------------

Copyright (c) 2007-2009, Troy D. Hanson
All rights reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:

Redistributions of source code must retain the above copyright
notice, this list of conditions and the following disclaimer.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS
IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED
TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A
PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER
OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL,
EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO,
PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR
PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF
LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

		----------------------------------------

Copyright 1992, 1993 by TOSHIBA Corp.

Permission to use, copy, modify, and distribute this software and its
documentation for any purpose and without fee is hereby granted, provided
that the above copyright notice appear in all copies and that both that
copyright notice and this permission notice appear in supporting
documentation, and that the name of TOSHIBA not be used in advertising
or publicity pertaining to distribution of the software without specific,
written prior permission. TOSHIBA make no representations about the
suitability of this software for any purpose.  It is provided "as is"
without express or implied warranty.

TOSHIBA DISCLAIM ALL WARRANTIES WITH REGARD TO THIS SOFTWARE, INCLUDING
ALL IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS, IN NO EVENT SHALL
TOSHIBA BE LIABLE FOR ANY SPECIAL, INDIRECT OR CONSEQUENTIAL DAMAGES OR
ANY DAMAGES WHATSOEVER RESULTING FROM LOSS OF USE, DATA OR PROFITS,
WHETHER IN AN ACTION OF CONTRACT, NEGLIGENCE OR OTHER TORTIOUS ACTION,
ARISING OUT OF OR IN CONNECTION WITH THE USE OR PERFORMANCE OF THIS
SOFTWARE.


		----------------------------------------

Copyright IBM Corporation 1993

All Rights Reserved

License to use, copy, modify, and distribute this software and its
documentation for any purpose and without fee is hereby granted,
provided that the above copyright notice appear in all copies and that
both that copyright notice and this permission notice appear in
supporting documentation, and that the name of IBM not be
used in advertising or publicity pertaining to distribution of the
software without specific, written prior permission.

IBM DISCLAIMS ALL WARRANTIES WITH REGARD TO THIS SOFTWARE, INCLUDING
ALL IMPLIED WARRANTIES OF MERCHANTABILITY, FITNESS, AND
NONINFRINGEMENT OF THIRD PARTY RIGHTS, IN NO EVENT SHALL
IBM BE LIABLE FOR ANY SPECIAL, INDIRECT OR CONSEQUENTIAL DAMAGES OR
ANY DAMAGES WHATSOEVER RESULTING FROM LOSS OF USE, DATA OR PROFITS,
WHETHER IN AN ACTION OF CONTRACT, NEGLIGENCE OR OTHER TORTIOUS ACTION,
ARISING OUT OF OR IN CONNECTION WITH THE USE OR PERFORMANCE OF THIS
SOFTWARE.

		----------------------------------------

Copyright 1990, 1991 by OMRON Corporation, NTT Software Corporation,
                     and Nippon Telegraph and Telephone Corporation

Permission to use, copy, modify, distribute, and sell this software and its
documentation for any purpose is hereby granted without fee, provided that
the above copyright notice appear in all copies and that both that
copyright notice and this permission notice appear in supporting
documentation, and that the names of OMRON, NTT Software, and NTT
not be used in advertising or publicity pertaining to distribution of the
software without specific, written prior permission. OMRON, NTT Software,
and NTT make no representations about the suitability of this
software for any purpose.  It is provided "as is" without express or
implied warranty.

OMRON, NTT SOFTWARE, AND NTT, DISCLAIM ALL WARRANTIES WITH REGARD
TO THIS SOFTWARE, INCLUDING ALL IMPLIED WARRANTIES OF MERCHANTABILITY
AND FITNESS, IN NO EVENT SHALL OMRON, NTT SOFTWARE, OR NTT, BE
LIABLE FOR ANY SPECIAL, INDIRECT OR CONSEQUENTIAL DAMAGES OR ANY DAMAGES
WHATSOEVER RESULTING FROM LOSS OF USE, DATA OR PROFITS, WHETHER IN AN
ACTION OF CONTRACT, NEGLIGENCE OR OTHER TORTIOUS ACTION, ARISING OUT OF
OR IN CONNECTION WITH THE USE OR PERFORMANCE OF THIS SOFTWARE.

		----------------------------------------

Copyright (c) 2008 Otto Moerbeek <otto@drijf.net>

Permission to use, copy, modify, and distribute this software for any
purpose with or without fee is hereby granted, provided that the above
copyright notice and this permission notice appear in all copies.

THE SOFTWARE IS PROVIDED "AS IS" AND THE AUTHOR DISCLAIMS ALL WARRANTIES
WITH REGARD TO THIS SOFTWARE INCLUDING ALL IMPLIED WARRANTIES OF
MERCHANTABILITY AND FITNESS. IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR
ANY SPECIAL, DIRECT, INDIRECT, OR CONSEQUENTIAL DAMAGES OR ANY DAMAGES
WHATSOEVER RESULTING FROM LOSS OF USE, DATA OR PROFITS, WHETHER IN AN
ACTION OF CONTRACT, NEGLIGENCE OR OTHER TORTIOUS ACTION, ARISING OUT OF
OR IN CONNECTION WITH THE USE OR PERFORMANCE OF THIS SOFTWARE.

`}),e[19]||(e[19]=t("h4",null,"libxau",-1)),o(i,{code:`Copyright 1988, 1993, 1994, 1998  The Open Group

Permission to use, copy, modify, distribute, and sell this software and its
documentation for any purpose is hereby granted without fee, provided that
the above copyright notice appear in all copies and that both that
copyright notice and this permission notice appear in supporting
documentation.

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.  IN NO EVENT SHALL THE
OPEN GROUP BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN
AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

Except as contained in this notice, the name of The Open Group shall not be
used in advertising or otherwise to promote the sale, use or other dealings
in this Software without prior written authorization from The Open Group.
`}),e[20]||(e[20]=t("h4",null,"libxcb",-1)),o(i,{code:`Copyright (C) 2001-2006 Bart Massey, Jamey Sharp, and Josh Triplett.
All Rights Reserved.

Permission is hereby granted, free of charge, to any person
obtaining a copy of this software and associated
documentation files (the "Software"), to deal in the
Software without restriction, including without limitation
the rights to use, copy, modify, merge, publish, distribute,
sublicense, and/or sell copies of the Software, and to
permit persons to whom the Software is furnished to do so,
subject to the following conditions:

The above copyright notice and this permission notice shall
be included in all copies or substantial portions of the
Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY
KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS
BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER
IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
OTHER DEALINGS IN THE SOFTWARE.

Except as contained in this notice, the names of the authors
or their institutions shall not be used in advertising or
otherwise to promote the sale, use or other dealings in this
Software without prior written authorization from the
authors.
`}),e[21]||(e[21]=t("h4",null,"libxdmcp",-1)),o(i,{code:`Copyright 1989, 1998  The Open Group

Permission to use, copy, modify, distribute, and sell this software and its
documentation for any purpose is hereby granted without fee, provided that
the above copyright notice appear in all copies and that both that
copyright notice and this permission notice appear in supporting
documentation.

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.  IN NO EVENT SHALL THE
OPEN GROUP BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN
AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

Except as contained in this notice, the name of The Open Group shall not be
used in advertising or otherwise to promote the sale, use or other dealings
in this Software without prior written authorization from The Open Group.

Author:  Keith Packard, MIT X Consortium

`}),e[22]||(e[22]=t("h4",null,"libxext",-1)),o(i,{code:`Copyright 1986, 1987, 1988, 1989, 1994, 1998  The Open Group

Permission to use, copy, modify, distribute, and sell this software and its
documentation for any purpose is hereby granted without fee, provided that
the above copyright notice appear in all copies and that both that
copyright notice and this permission notice appear in supporting
documentation.

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.  IN NO EVENT SHALL THE
OPEN GROUP BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN
AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

Except as contained in this notice, the name of The Open Group shall not be
used in advertising or otherwise to promote the sale, use or other dealings
in this Software without prior written authorization from The Open Group.

Copyright (c) 1996 Digital Equipment Corporation, Maynard, Massachusetts.

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software.

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.  IN NO EVENT SHALL
DIGITAL EQUIPMENT CORPORATION BE LIABLE FOR ANY CLAIM, DAMAGES, INCLUDING,
BUT NOT LIMITED TO CONSEQUENTIAL OR INCIDENTAL DAMAGES, OR OTHER LIABILITY,
WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR
IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

Except as contained in this notice, the name of Digital Equipment Corporation
shall not be used in advertising or otherwise to promote the sale, use or other
dealings in this Software without prior written authorization from Digital
Equipment Corporation.

Copyright (c) 1997 by Silicon Graphics Computer Systems, Inc.
Permission to use, copy, modify, and distribute this
software and its documentation for any purpose and without
fee is hereby granted, provided that the above copyright
notice appear in all copies and that both that copyright
notice and this permission notice appear in supporting
documentation, and that the name of Silicon Graphics not be
used in advertising or publicity pertaining to distribution
of the software without specific prior written permission.
Silicon Graphics makes no representation about the suitability
of this software for any purpose. It is provided "as is"
without any express or implied warranty.
SILICON GRAPHICS DISCLAIMS ALL WARRANTIES WITH REGARD TO THIS
SOFTWARE, INCLUDING ALL IMPLIED WARRANTIES OF MERCHANTABILITY
AND FITNESS FOR A PARTICULAR PURPOSE. IN NO EVENT SHALL SILICON
GRAPHICS BE LIABLE FOR ANY SPECIAL, INDIRECT OR CONSEQUENTIAL
DAMAGES OR ANY DAMAGES WHATSOEVER RESULTING FROM LOSS OF USE,
DATA OR PROFITS, WHETHER IN AN ACTION OF CONTRACT, NEGLIGENCE
OR OTHER TORTIOUS ACTION, ARISING OUT OF OR IN CONNECTION  WITH
THE USE OR PERFORMANCE OF THIS SOFTWARE.

Copyright 1992 Network Computing Devices

Permission to use, copy, modify, distribute, and sell this software and its
documentation for any purpose is hereby granted without fee, provided that
the above copyright notice appear in all copies and that both that
copyright notice and this permission notice appear in supporting
documentation, and that the name of NCD. not be used in advertising or
publicity pertaining to distribution of the software without specific,
written prior permission.  NCD. makes no representations about the
suitability of this software for any purpose.  It is provided "as is"
without express or implied warranty.

NCD. DISCLAIMS ALL WARRANTIES WITH REGARD TO THIS SOFTWARE, INCLUDING ALL
IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS, IN NO EVENT SHALL NCD.
BE LIABLE FOR ANY SPECIAL, INDIRECT OR CONSEQUENTIAL DAMAGES OR ANY DAMAGES
WHATSOEVER RESULTING FROM LOSS OF USE, DATA OR PROFITS, WHETHER IN AN ACTION
OF CONTRACT, NEGLIGENCE OR OTHER TORTIOUS ACTION, ARISING OUT OF OR IN
CONNECTION WITH THE USE OR PERFORMANCE OF THIS SOFTWARE.

Copyright 1991,1993 by Digital Equipment Corporation, Maynard, Massachusetts,
and Olivetti Research Limited, Cambridge, England.

                        All Rights Reserved

Permission to use, copy, modify, and distribute this software and its
documentation for any purpose and without fee is hereby granted,
provided that the above copyright notice appear in all copies and that
both that copyright notice and this permission notice appear in
supporting documentation, and that the names of Digital or Olivetti
not be used in advertising or publicity pertaining to distribution of the
software without specific, written prior permission.

DIGITAL AND OLIVETTI DISCLAIM ALL WARRANTIES WITH REGARD TO THIS
SOFTWARE, INCLUDING ALL IMPLIED WARRANTIES OF MERCHANTABILITY AND
FITNESS, IN NO EVENT SHALL THEY BE LIABLE FOR ANY SPECIAL, INDIRECT OR
CONSEQUENTIAL DAMAGES OR ANY DAMAGES WHATSOEVER RESULTING FROM LOSS OF
USE, DATA OR PROFITS, WHETHER IN AN ACTION OF CONTRACT, NEGLIGENCE OR
OTHER TORTIOUS ACTION, ARISING OUT OF OR IN CONNECTION WITH THE USE OR
PERFORMANCE OF THIS SOFTWARE.

Copyright 1986, 1987, 1988 by Hewlett-Packard Corporation

Permission to use, copy, modify, and distribute this
software and its documentation for any purpose and without
fee is hereby granted, provided that the above copyright
notice appear in all copies and that both that copyright
notice and this permission notice appear in supporting
documentation, and that the name of Hewlett-Packard not be used in
advertising or publicity pertaining to distribution of the
software without specific, written prior permission.

Hewlett-Packard makes no representations about the
suitability of this software for any purpose.  It is provided
"as is" without express or implied warranty.

This software is not subject to any license of the American
Telephone and Telegraph Company or of the Regents of the
University of California.

Copyright (c) 1994, 1995  Hewlett-Packard Company

Permission is hereby granted, free of charge, to any person obtaining
a copy of this software and associated documentation files (the
"Software"), to deal in the Software without restriction, including
without limitation the rights to use, copy, modify, merge, publish,
distribute, sublicense, and/or sell copies of the Software, and to
permit persons to whom the Software is furnished to do so, subject to
the following conditions:

The above copyright notice and this permission notice shall be included
in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS
OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
IN NO EVENT SHALL HEWLETT-PACKARD COMPANY BE LIABLE FOR ANY CLAIM,
DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR
THE USE OR OTHER DEALINGS IN THE SOFTWARE.

Except as contained in this notice, the name of the Hewlett-Packard
Company shall not be used in advertising or otherwise to promote the
sale, use or other dealings in this Software without prior written
authorization from the Hewlett-Packard Company.

Copyright Digital Equipment Corporation, 1996

Permission to use, copy, modify, distribute, and sell this
documentation for any purpose is hereby granted without fee,
provided that the above copyright notice and this permission
notice appear in all copies.  Digital Equipment Corporation
makes no representations about the suitability for any purpose
of the information in this document.  This documentation is
provided \`\`as is'' without express or implied warranty.

Copyright (c) 1999, 2005, 2006, 2013, 2015, Oracle and/or its affiliates.
Copyright © 2007-2008 Peter Hutterer

Permission is hereby granted, free of charge, to any person obtaining a
copy of this software and associated documentation files (the "Software"),
to deal in the Software without restriction, including without limitation
the rights to use, copy, modify, merge, publish, distribute, sublicense,
and/or sell copies of the Software, and to permit persons to whom the
Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice (including the next
paragraph) shall be included in all copies or substantial portions of the
Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.  IN NO EVENT SHALL
THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
DEALINGS IN THE SOFTWARE.

Copyright (c) 1989 X Consortium, Inc. and Digital Equipment Corporation.
Copyright (c) 1992 X Consortium, Inc. and Intergraph Corporation.
Copyright (c) 1993 X Consortium, Inc. and Silicon Graphics, Inc.
Copyright (c) 1994, 1995 X Consortium, Inc. and Hewlett-Packard Company.

Permission to use, copy, modify, and distribute this documentation for
any purpose and without fee is hereby granted, provided that the above
copyright notice and this permission notice appear in all copies.
Digital Equipment Corporation, Intergraph Corporation, Silicon
Graphics, Hewlett-Packard, and the X Consortium make no
representations about the suitability for any purpose of the
information in this document.  This documentation is provided \`\`as is''
without express or implied warranty.

Copyright (c) 2008 Otto Moerbeek <otto@drijf.net>

Permission to use, copy, modify, and distribute this software for any
purpose with or without fee is hereby granted, provided that the above
copyright notice and this permission notice appear in all copies.

THE SOFTWARE IS PROVIDED "AS IS" AND THE AUTHOR DISCLAIMS ALL WARRANTIES
WITH REGARD TO THIS SOFTWARE INCLUDING ALL IMPLIED WARRANTIES OF
MERCHANTABILITY AND FITNESS. IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR
ANY SPECIAL, DIRECT, INDIRECT, OR CONSEQUENTIAL DAMAGES OR ANY DAMAGES
WHATSOEVER RESULTING FROM LOSS OF USE, DATA OR PROFITS, WHETHER IN AN
ACTION OF CONTRACT, NEGLIGENCE OR OTHER TORTIOUS ACTION, ARISING OUT OF
OR IN CONNECTION WITH THE USE OR PERFORMANCE OF THIS SOFTWARE.

`}),e[23]||(e[23]=t("h4",null,"libxrender",-1)),o(i,{code:`
Copyright © 2001,2003 Keith Packard

Permission to use, copy, modify, distribute, and sell this software and its
documentation for any purpose is hereby granted without fee, provided that
the above copyright notice appear in all copies and that both that
copyright notice and this permission notice appear in supporting
documentation, and that the name of Keith Packard not be used in
advertising or publicity pertaining to distribution of the software without
specific, written prior permission.  Keith Packard makes no
representations about the suitability of this software for any purpose.  It
is provided "as is" without express or implied warranty.

KEITH PACKARD DISCLAIMS ALL WARRANTIES WITH REGARD TO THIS SOFTWARE,
INCLUDING ALL IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS, IN NO
EVENT SHALL KEITH PACKARD BE LIABLE FOR ANY SPECIAL, INDIRECT OR
CONSEQUENTIAL DAMAGES OR ANY DAMAGES WHATSOEVER RESULTING FROM LOSS OF USE,
DATA OR PROFITS, WHETHER IN AN ACTION OF CONTRACT, NEGLIGENCE OR OTHER
TORTIOUS ACTION, ARISING OUT OF OR IN CONNECTION WITH THE USE OR
PERFORMANCE OF THIS SOFTWARE.

Copyright © 2000 SuSE, Inc.

Permission to use, copy, modify, distribute, and sell this software and its
documentation for any purpose is hereby granted without fee, provided that
the above copyright notice appear in all copies and that both that
copyright notice and this permission notice appear in supporting
documentation, and that the name of SuSE not be used in advertising or
publicity pertaining to distribution of the software without specific,
written prior permission.  SuSE makes no representations about the
suitability of this software for any purpose.  It is provided "as is"
without express or implied warranty.

SuSE DISCLAIMS ALL WARRANTIES WITH REGARD TO THIS SOFTWARE, INCLUDING ALL
IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS, IN NO EVENT SHALL SuSE
BE LIABLE FOR ANY SPECIAL, INDIRECT OR CONSEQUENTIAL DAMAGES OR ANY DAMAGES
WHATSOEVER RESULTING FROM LOSS OF USE, DATA OR PROFITS, WHETHER IN AN ACTION
OF CONTRACT, NEGLIGENCE OR OTHER TORTIOUS ACTION, ARISING OUT OF OR IN
CONNECTION WITH THE USE OR PERFORMANCE OF THIS SOFTWARE.

`}),e[24]||(e[24]=t("h4",null,"musl",-1)),o(i,{code:`musl as a whole is licensed under the following standard MIT license:

----------------------------------------------------------------------
Copyright © 2005-2020 Rich Felker, et al.

Permission is hereby granted, free of charge, to any person obtaining
a copy of this software and associated documentation files (the
"Software"), to deal in the Software without restriction, including
without limitation the rights to use, copy, modify, merge, publish,
distribute, sublicense, and/or sell copies of the Software, and to
permit persons to whom the Software is furnished to do so, subject to
the following conditions:

The above copyright notice and this permission notice shall be
included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
----------------------------------------------------------------------

Authors/contributors include:

A. Wilcox
Ada Worcester
Alex Dowad
Alex Suykov
Alexander Monakov
Andre McCurdy
Andrew Kelley
Anthony G. Basile
Aric Belsito
Arvid Picciani
Bartosz Brachaczek
Benjamin Peterson
Bobby Bingham
Boris Brezillon
Brent Cook
Chris Spiegel
Clément Vasseur
Daniel Micay
Daniel Sabogal
Daurnimator
David Carlier
David Edelsohn
Denys Vlasenko
Dmitry Ivanov
Dmitry V. Levin
Drew DeVault
Emil Renner Berthing
Fangrui Song
Felix Fietkau
Felix Janda
Gianluca Anzolin
Hauke Mehrtens
He X
Hiltjo Posthuma
Isaac Dunham
Jaydeep Patil
Jens Gustedt
Jeremy Huntwork
Jo-Philipp Wich
Joakim Sindholt
John Spencer
Julien Ramseier
Justin Cormack
Kaarle Ritvanen
Khem Raj
Kylie McClain
Leah Neukirchen
Luca Barbato
Luka Perkov
Lynn Ochs
M Farkas-Dyck (Strake)
Mahesh Bodapati
Markus Wichmann
Masanori Ogino
Michael Clark
Michael Forney
Mikhail Kremnyov
Natanael Copa
Nicholas J. Kain
orc
Pascal Cuoq
Patrick Oppenlander
Petr Hosek
Petr Skocik
Pierre Carrier
Reini Urban
Rich Felker
Richard Pennington
Ryan Fairfax
Samuel Holland
Segev Finer
Shiz
sin
Solar Designer
Stefan Kristiansson
Stefan O'Rear
Szabolcs Nagy
Timo Teräs
Trutz Behn
Will Dietz
William Haddon
William Pitcock

Portions of this software are derived from third-party works licensed
under terms compatible with the above MIT license:

The TRE regular expression implementation (src/regex/reg* and
src/regex/tre*) is Copyright © 2001-2008 Ville Laurikari and licensed
under a 2-clause BSD license (license text in the source files). The
included version has been heavily modified by Rich Felker in 2012, in
the interests of size, simplicity, and namespace cleanliness.

Much of the math library code (src/math/* and src/complex/*) is
Copyright © 1993,2004 Sun Microsystems or
Copyright © 2003-2011 David Schultz or
Copyright © 2003-2009 Steven G. Kargl or
Copyright © 2003-2009 Bruce D. Evans or
Copyright © 2008 Stephen L. Moshier or
Copyright © 2017-2018 Arm Limited
and labelled as such in comments in the individual source files. All
have been licensed under extremely permissive terms.

The ARM memcpy code (src/string/arm/memcpy.S) is Copyright © 2008
The Android Open Source Project and is licensed under a two-clause BSD
license. It was taken from Bionic libc, used on Android.

The AArch64 memcpy and memset code (src/string/aarch64/*) are
Copyright © 1999-2019, Arm Limited.

The implementation of DES for crypt (src/crypt/crypt_des.c) is
Copyright © 1994 David Burren. It is licensed under a BSD license.

The implementation of blowfish crypt (src/crypt/crypt_blowfish.c) was
originally written by Solar Designer and placed into the public
domain. The code also comes with a fallback permissive license for use
in jurisdictions that may not recognize the public domain.

The smoothsort implementation (src/stdlib/qsort.c) is Copyright © 2011
Lynn Ochs and is licensed under an MIT-style license.

The x86_64 port was written by Nicholas J. Kain and is licensed under
the standard MIT terms.

The mips and microblaze ports were originally written by Richard
Pennington for use in the ellcc project. The original code was adapted
by Rich Felker for build system and code conventions during upstream
integration. It is licensed under the standard MIT terms.

The mips64 port was contributed by Imagination Technologies and is
licensed under the standard MIT terms.

The powerpc port was also originally written by Richard Pennington,
and later supplemented and integrated by John Spencer. It is licensed
under the standard MIT terms.

All other files which have no copyright comments are original works
produced specifically for use as part of this library, written either
by Rich Felker, the main author of the library, or by one or more
contibutors listed above. Details on authorship of individual files
can be found in the git version control history of the project. The
omission of copyright and license comments in each file is in the
interest of source tree size.

In addition, permission is hereby granted for all public header files
(include/* and arch/*/bits/*) and crt files intended to be linked into
applications (crt/*, ldso/dlstart.c, and arch/*/crt_arch.h) to omit
the copyright notice and permission notice otherwise required by the
license, and to use these files without any requirement of
attribution. These files include substantial contributions from:

Bobby Bingham
John Spencer
Nicholas J. Kain
Rich Felker
Richard Pennington
Stefan Kristiansson
Szabolcs Nagy

all of whom have explicitly granted such permission.

This file previously contained text expressing a belief that most of
the files covered by the above exception were sufficiently trivial not
to be subject to copyright, resulting in confusion over whether it
negated the permissions granted in the license. In the spirit of
permissive licensing, and of not having licensing issues being an
obstacle to adoption, that text has been removed.
`}),e[25]||(e[25]=t("h4",null,"pixman",-1)),o(i,{code:`The following is the MIT license, agreed upon by most contributors.
Copyright holders of new code should use this license statement where
possible. They may also add themselves to the list below.

/*
 * Copyright 1987, 1988, 1989, 1998  The Open Group
 * Copyright 1987, 1988, 1989 Digital Equipment Corporation
 * Copyright 1999, 2004, 2008 Keith Packard
 * Copyright 2000 SuSE, Inc.
 * Copyright 2000 Keith Packard, member of The XFree86 Project, Inc.
 * Copyright 2004, 2005, 2007, 2008, 2009, 2010 Red Hat, Inc.
 * Copyright 2004 Nicholas Miell
 * Copyright 2005 Lars Knoll & Zack Rusin, Trolltech
 * Copyright 2005 Trolltech AS
 * Copyright 2007 Luca Barbato
 * Copyright 2008 Aaron Plattner, NVIDIA Corporation
 * Copyright 2008 Rodrigo Kumpera
 * Copyright 2008 André Tupinambá
 * Copyright 2008 Mozilla Corporation
 * Copyright 2008 Frederic Plourde
 * Copyright 2009, Oracle and/or its affiliates. All rights reserved.
 * Copyright 2009, 2010 Nokia Corporation
 *
 * Permission is hereby granted, free of charge, to any person obtaining a
 * copy of this software and associated documentation files (the "Software"),
 * to deal in the Software without restriction, including without limitation
 * the rights to use, copy, modify, merge, publish, distribute, sublicense,
 * and/or sell copies of the Software, and to permit persons to whom the
 * Software is furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice (including the next
 * paragraph) shall be included in all copies or substantial portions of the
 * Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.  IN NO EVENT SHALL
 * THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
 * FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
 * DEALINGS IN THE SOFTWARE.
 */

`}),e[26]||(e[26]=t("h4",null,"zlib",-1)),o(i,{code:`Copyright notice:

 (C) 1995-2025 Jean-loup Gailly and Mark Adler

  This software is provided 'as-is', without any express or implied
  warranty.  In no event will the authors be held liable for any damages
  arising from the use of this software.

  Permission is granted to anyone to use this software for any purpose,
  including commercial applications, and to alter it and redistribute it
  freely, subject to the following restrictions:

  1. The origin of this software must not be misrepresented; you must not
     claim that you wrote the original software. If you use this software
     in a product, an acknowledgment in the product documentation would be
     appreciated but is not required.
  2. Altered source versions must be plainly marked as such, and must not be
     misrepresented as being the original software.
  3. This notice may not be removed or altered from any source distribution.

  Jean-loup Gailly        Mark Adler
  jloup@gzip.org          madler@alumni.caltech.edu
`}),e[27]||(e[27]=t("h4",null,"ca-certificates",-1)),o(i,{code:`https://gitlab.alpinelinux.org/alpine/aports/-/blob/3.18-stable/main/ca-certificates/APKBUILD
`}),e[28]||(e[28]=t("h4",null,"netcat-openbsd",-1)),o(i,{code:`Format: https://www.debian.org/doc/packaging-manuals/copyright-format/1.0/
Source: http://www.openbsd.org/cgi-bin/cvsweb/src/usr.bin/nc/
Upstream-Name: netcat

Files: netcat.c
Copyright: 2001 Eric Jackson <ericj@monkey.org>
License: BSD-3-Clause

Files: nc.1
Copyright: 1996 David Sacerdote
License: BSD-3-Clause

Files: atomicio.*
Copyright: 2006 Damien Miller
           2005 Anil Madhavapeddy
           1995,1999 Theo de Raadt
License: BSD-2-Clause

Files: socks.c
Copyright: 1999 Niklas Hallqvist
           2004, 2005 Damien Miller
License: BSD-2-Clause

Files: Makefile
Copyright: The OpenBSD project
License: BSD-3-Clause

Files: debian/*
Copyright: 2008, 2009, 2010 Decklin Foster <decklin@red-bean.com>
           2008, 2009, 2010 Soren Hansen <soren@ubuntu.com>
           2012 Aron Xu <aron@debian.org>
           2016-2022 Guilhem Moulin <guilhem@debian.org>
License: BSD-3-Clause

Files: debian/checks/* debian/tests/*
Copyright: 2021-2022 Guilhem Moulin <guilhem@debian.org>
License: BSD-3-Clause

License: BSD-2-Clause
 Redistribution and use in source and binary forms, with or without
 modification, are permitted provided that the following conditions
 are met:
 .
 1. Redistributions of source code must retain the above copyright
    notice, this list of conditions and the following disclaimer.
 2. Redistributions in binary form must reproduce the above copyright
    notice, this list of conditions and the following disclaimer in the
    documentation and/or other materials provided with the distribution.
 .
 THIS SOFTWARE IS PROVIDED BY THE AUTHOR \`\`AS IS'' AND ANY EXPRESS OR
 IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES
 OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED.
 IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR ANY DIRECT, INDIRECT,
 INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT
 NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE,
 DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY
 THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF
 THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

License: BSD-3-Clause
 Redistribution and use in source and binary forms, with or without
 modification, are permitted provided that the following conditions
 are met:
 .
 1. Redistributions of source code must retain the above copyright
    notice, this list of conditions and the following disclaimer.
 2. Redistributions in binary form must reproduce the above copyright
    notice, this list of conditions and the following disclaimer in the
    documentation and/or other materials provided with the distribution.
 3. The name of the author may not be used to endorse or promote products
    derived from this software without specific prior written permission.
 .
 THIS SOFTWARE IS PROVIDED BY THE AUTHOR \`\`AS IS'' AND ANY EXPRESS OR
 IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES
 OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED.
 IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR ANY DIRECT, INDIRECT,
 INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT
 NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE,
 DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY
 THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF
 THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
`}),e[29]||(e[29]=t("h4",null,"shadow",-1)),o(i,{code:`SPDX-License-Identifier: BSD-3-Clause

All files under this project either

1. fall under the BSD 3 clause license (by default).

2. carry an SPDX header declaring what license applies.

or

3. list a full custom license

This software is originally

 * Copyright (c) 1989 - 1994, Julianne Frances Haugh

 * All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions
 * are met:
 * 1. Redistributions of source code must retain the above copyright
 *    notice, this list of conditions and the following disclaimer.
 * 2. Redistributions in binary form must reproduce the above copyright
 *    notice, this list of conditions and the following disclaimer in the
 *    documentation and/or other materials provided with the distribution.
 * 3. The name of the copyright holders or contributors may not be used to
 *    endorse or promote products derived from this software without
 *    specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
 * \`\`AS IS'' AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
 * LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A
 * PARTICULAR PURPOSE ARE DISCLAIMED.  IN NO EVENT SHALL THE COPYRIGHT
 * HOLDERS OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
 * SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT
 * LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE,
 * DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY
 * THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
 * OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
`}),e[30]||(e[30]=t("h4",null,"util-linux-login",-1)),o(i,{code:`                    GNU GENERAL PUBLIC LICENSE
                       Version 2, June 1991

 Copyright (C) 1989, 1991 Free Software Foundation, Inc.,
 51 Franklin Street, Fifth Floor, Boston, MA 02110-1301 USA
 Everyone is permitted to copy and distribute verbatim copies
 of this license document, but changing it is not allowed.

                            Preamble

  The licenses for most software are designed to take away your
freedom to share and change it.  By contrast, the GNU General Public
License is intended to guarantee your freedom to share and change free
software--to make sure the software is free for all its users.  This
General Public License applies to most of the Free Software
Foundation's software and to any other program whose authors commit to
using it.  (Some other Free Software Foundation software is covered by
the GNU Lesser General Public License instead.)  You can apply it to
your programs, too.

  When we speak of free software, we are referring to freedom, not
price.  Our General Public Licenses are designed to make sure that you
have the freedom to distribute copies of free software (and charge for
this service if you wish), that you receive source code or can get it
if you want it, that you can change the software or use pieces of it
in new free programs; and that you know you can do these things.

  To protect your rights, we need to make restrictions that forbid
anyone to deny you these rights or to ask you to surrender the rights.
These restrictions translate to certain responsibilities for you if you
distribute copies of the software, or if you modify it.

  For example, if you distribute copies of such a program, whether
gratis or for a fee, you must give the recipients all the rights that
you have.  You must make sure that they, too, receive or can get the
source code.  And you must show them these terms so they know their
rights.

  We protect your rights with two steps: (1) copyright the software, and
(2) offer you this license which gives you legal permission to copy,
distribute and/or modify the software.

  Also, for each author's protection and ours, we want to make certain
that everyone understands that there is no warranty for this free
software.  If the software is modified by someone else and passed on, we
want its recipients to know that what they have is not the original, so
that any problems introduced by others will not reflect on the original
authors' reputations.

  Finally, any free program is threatened constantly by software
patents.  We wish to avoid the danger that redistributors of a free
program will individually obtain patent licenses, in effect making the
program proprietary.  To prevent this, we have made it clear that any
patent must be licensed for everyone's free use or not licensed at all.

  The precise terms and conditions for copying, distribution and
modification follow.

                    GNU GENERAL PUBLIC LICENSE
   TERMS AND CONDITIONS FOR COPYING, DISTRIBUTION AND MODIFICATION

  0. This License applies to any program or other work which contains
a notice placed by the copyright holder saying it may be distributed
under the terms of this General Public License.  The "Program", below,
refers to any such program or work, and a "work based on the Program"
means either the Program or any derivative work under copyright law:
that is to say, a work containing the Program or a portion of it,
either verbatim or with modifications and/or translated into another
language.  (Hereinafter, translation is included without limitation in
the term "modification".)  Each licensee is addressed as "you".

Activities other than copying, distribution and modification are not
covered by this License; they are outside its scope.  The act of
running the Program is not restricted, and the output from the Program
is covered only if its contents constitute a work based on the
Program (independent of having been made by running the Program).
Whether that is true depends on what the Program does.

  1. You may copy and distribute verbatim copies of the Program's
source code as you receive it, in any medium, provided that you
conspicuously and appropriately publish on each copy an appropriate
copyright notice and disclaimer of warranty; keep intact all the
notices that refer to this License and to the absence of any warranty;
and give any other recipients of the Program a copy of this License
along with the Program.

You may charge a fee for the physical act of transferring a copy, and
you may at your option offer warranty protection in exchange for a fee.

  2. You may modify your copy or copies of the Program or any portion
of it, thus forming a work based on the Program, and copy and
distribute such modifications or work under the terms of Section 1
above, provided that you also meet all of these conditions:

    a) You must cause the modified files to carry prominent notices
    stating that you changed the files and the date of any change.

    b) You must cause any work that you distribute or publish, that in
    whole or in part contains or is derived from the Program or any
    part thereof, to be licensed as a whole at no charge to all third
    parties under the terms of this License.

    c) If the modified program normally reads commands interactively
    when run, you must cause it, when started running for such
    interactive use in the most ordinary way, to print or display an
    announcement including an appropriate copyright notice and a
    notice that there is no warranty (or else, saying that you provide
    a warranty) and that users may redistribute the program under
    these conditions, and telling the user how to view a copy of this
    License.  (Exception: if the Program itself is interactive but
    does not normally print such an announcement, your work based on
    the Program is not required to print an announcement.)

These requirements apply to the modified work as a whole.  If
identifiable sections of that work are not derived from the Program,
and can be reasonably considered independent and separate works in
themselves, then this License, and its terms, do not apply to those
sections when you distribute them as separate works.  But when you
distribute the same sections as part of a whole which is a work based
on the Program, the distribution of the whole must be on the terms of
this License, whose permissions for other licensees extend to the
entire whole, and thus to each and every part regardless of who wrote it.

Thus, it is not the intent of this section to claim rights or contest
your rights to work written entirely by you; rather, the intent is to
exercise the right to control the distribution of derivative or
collective works based on the Program.

In addition, mere aggregation of another work not based on the Program
with the Program (or with a work based on the Program) on a volume of
a storage or distribution medium does not bring the other work under
the scope of this License.

  3. You may copy and distribute the Program (or a work based on it,
under Section 2) in object code or executable form under the terms of
Sections 1 and 2 above provided that you also do one of the following:

    a) Accompany it with the complete corresponding machine-readable
    source code, which must be distributed under the terms of Sections
    1 and 2 above on a medium customarily used for software interchange; or,

    b) Accompany it with a written offer, valid for at least three
    years, to give any third party, for a charge no more than your
    cost of physically performing source distribution, a complete
    machine-readable copy of the corresponding source code, to be
    distributed under the terms of Sections 1 and 2 above on a medium
    customarily used for software interchange; or,

    c) Accompany it with the information you received as to the offer
    to distribute corresponding source code.  (This alternative is
    allowed only for noncommercial distribution and only if you
    received the program in object code or executable form with such
    an offer, in accord with Subsection b above.)

The source code for a work means the preferred form of the work for
making modifications to it.  For an executable work, complete source
code means all the source code for all modules it contains, plus any
associated interface definition files, plus the scripts used to
control compilation and installation of the executable.  However, as a
special exception, the source code distributed need not include
anything that is normally distributed (in either source or binary
form) with the major components (compiler, kernel, and so on) of the
operating system on which the executable runs, unless that component
itself accompanies the executable.

If distribution of executable or object code is made by offering
access to copy from a designated place, then offering equivalent
access to copy the source code from the same place counts as
distribution of the source code, even though third parties are not
compelled to copy the source along with the object code.

  4. You may not copy, modify, sublicense, or distribute the Program
except as expressly provided under this License.  Any attempt
otherwise to copy, modify, sublicense or distribute the Program is
void, and will automatically terminate your rights under this License.
However, parties who have received copies, or rights, from you under
this License will not have their licenses terminated so long as such
parties remain in full compliance.

  5. You are not required to accept this License, since you have not
signed it.  However, nothing else grants you permission to modify or
distribute the Program or its derivative works.  These actions are
prohibited by law if you do not accept this License.  Therefore, by
modifying or distributing the Program (or any work based on the
Program), you indicate your acceptance of this License to do so, and
all its terms and conditions for copying, distributing or modifying
the Program or works based on it.

  6. Each time you redistribute the Program (or any work based on the
Program), the recipient automatically receives a license from the
original licensor to copy, distribute or modify the Program subject to
these terms and conditions.  You may not impose any further
restrictions on the recipients' exercise of the rights granted herein.
You are not responsible for enforcing compliance by third parties to
this License.

  7. If, as a consequence of a court judgment or allegation of patent
infringement or for any other reason (not limited to patent issues),
conditions are imposed on you (whether by court order, agreement or
otherwise) that contradict the conditions of this License, they do not
excuse you from the conditions of this License.  If you cannot
distribute so as to satisfy simultaneously your obligations under this
License and any other pertinent obligations, then as a consequence you
may not distribute the Program at all.  For example, if a patent
license would not permit royalty-free redistribution of the Program by
all those who receive copies directly or indirectly through you, then
the only way you could satisfy both it and this License would be to
refrain entirely from distribution of the Program.

If any portion of this section is held invalid or unenforceable under
any particular circumstance, the balance of the section is intended to
apply and the section as a whole is intended to apply in other
circumstances.

It is not the purpose of this section to induce you to infringe any
patents or other property right claims or to contest validity of any
such claims; this section has the sole purpose of protecting the
integrity of the free software distribution system, which is
implemented by public license practices.  Many people have made
generous contributions to the wide range of software distributed
through that system in reliance on consistent application of that
system; it is up to the author/donor to decide if he or she is willing
to distribute software through any other system and a licensee cannot
impose that choice.

This section is intended to make thoroughly clear what is believed to
be a consequence of the rest of this License.

  8. If the distribution and/or use of the Program is restricted in
certain countries either by patents or by copyrighted interfaces, the
original copyright holder who places the Program under this License
may add an explicit geographical distribution limitation excluding
those countries, so that distribution is permitted only in or among
countries not thus excluded.  In such case, this License incorporates
the limitation as if written in the body of this License.

  9. The Free Software Foundation may publish revised and/or new versions
of the General Public License from time to time.  Such new versions will
be similar in spirit to the present version, but may differ in detail to
address new problems or concerns.

Each version is given a distinguishing version number.  If the Program
specifies a version number of this License which applies to it and "any
later version", you have the option of following the terms and conditions
either of that version or of any later version published by the Free
Software Foundation.  If the Program does not specify a version number of
this License, you may choose any version ever published by the Free Software
Foundation.

  10. If you wish to incorporate parts of the Program into other free
programs whose distribution conditions are different, write to the author
to ask for permission.  For software which is copyrighted by the Free
Software Foundation, write to the Free Software Foundation; we sometimes
make exceptions for this.  Our decision will be guided by the two goals
of preserving the free status of all derivatives of our free software and
of promoting the sharing and reuse of software generally.

                            NO WARRANTY

  11. BECAUSE THE PROGRAM IS LICENSED FREE OF CHARGE, THERE IS NO WARRANTY
FOR THE PROGRAM, TO THE EXTENT PERMITTED BY APPLICABLE LAW.  EXCEPT WHEN
OTHERWISE STATED IN WRITING THE COPYRIGHT HOLDERS AND/OR OTHER PARTIES
PROVIDE THE PROGRAM "AS IS" WITHOUT WARRANTY OF ANY KIND, EITHER EXPRESSED
OR IMPLIED, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF
MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE.  THE ENTIRE RISK AS
TO THE QUALITY AND PERFORMANCE OF THE PROGRAM IS WITH YOU.  SHOULD THE
PROGRAM PROVE DEFECTIVE, YOU ASSUME THE COST OF ALL NECESSARY SERVICING,
REPAIR OR CORRECTION.

  12. IN NO EVENT UNLESS REQUIRED BY APPLICABLE LAW OR AGREED TO IN WRITING
WILL ANY COPYRIGHT HOLDER, OR ANY OTHER PARTY WHO MAY MODIFY AND/OR
REDISTRIBUTE THE PROGRAM AS PERMITTED ABOVE, BE LIABLE TO YOU FOR DAMAGES,
INCLUDING ANY GENERAL, SPECIAL, INCIDENTAL OR CONSEQUENTIAL DAMAGES ARISING
OUT OF THE USE OR INABILITY TO USE THE PROGRAM (INCLUDING BUT NOT LIMITED
TO LOSS OF DATA OR DATA BEING RENDERED INACCURATE OR LOSSES SUSTAINED BY
YOU OR THIRD PARTIES OR A FAILURE OF THE PROGRAM TO OPERATE WITH ANY OTHER
PROGRAMS), EVEN IF SUCH HOLDER OR OTHER PARTY HAS BEEN ADVISED OF THE
POSSIBILITY OF SUCH DAMAGES.

                     END OF TERMS AND CONDITIONS

            How to Apply These Terms to Your New Programs

  If you develop a new program, and you want it to be of the greatest
possible use to the public, the best way to achieve this is to make it
free software which everyone can redistribute and change under these terms.

  To do so, attach the following notices to the program.  It is safest
to attach them to the start of each source file to most effectively
convey the exclusion of warranty; and each file should have at least
the "copyright" line and a pointer to where the full notice is found.

    <one line to give the program's name and a brief idea of what it does.>
    Copyright (C) <year>  <name of author>

    This program is free software; you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation; either version 2 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License along
    with this program; if not, write to the Free Software Foundation, Inc.,
    51 Franklin Street, Fifth Floor, Boston, MA 02110-1301 USA.

Also add information on how to contact you by electronic and paper mail.

If the program is interactive, make it output a short notice like this
when it starts in an interactive mode:

    Gnomovision version 69, Copyright (C) year name of author
    Gnomovision comes with ABSOLUTELY NO WARRANTY; for details type \`show w'.
    This is free software, and you are welcome to redistribute it
    under certain conditions; type \`show c' for details.

The hypothetical commands \`show w' and \`show c' should show the appropriate
parts of the General Public License.  Of course, the commands you use may
be called something other than \`show w' and \`show c'; they could even be
mouse-clicks or menu items--whatever suits your program.

You should also get your employer (if you work as a programmer) or your
school, if any, to sign a "copyright disclaimer" for the program, if
necessary.  Here is a sample; alter the names:

  Yoyodyne, Inc., hereby disclaims all copyright interest in the program
  \`Gnomovision' (which makes passes at compilers) written by James Hacker.

  <signature of Ty Coon>, 1 April 1989
  Ty Coon, President of Vice

This General Public License does not permit incorporating your program into
proprietary programs.  If your program is a subroutine library, you may
consider it more useful to permit linking proprietary applications with the
library.  If this is what you want to do, use the GNU Lesser General
Public License instead of this License.
`})])])}}},Zr=Object.freeze(Object.defineProperty({__proto__:null,default:Qr,nav_title:$r,title:Xr},Symbol.toStringTag,{value:"Module"})),es={class:"markdown-body"},ts="Common web client errors",ns=["wsl2"],os={__name:"index",setup(l,{expose:r}){return r({frontmatter:{title:"Common web client errors",redirects:["wsl2"]}}),(d,e)=>{const i=p("RouterLink"),h=p("CodeBlock");return a(),c("div",es,[e[15]||(e[15]=A('<p>This section describes some common errors that may be encountered when using the RAWeb web client, along with their possible causes and solutions.</p><h2 id="code516">The requested resource was not found</h2><p>This error indicates that the requested RemoteApp or desktop resource could not be found. This is most likely to occur if the resource was renamed or deleted after the user accessed the RAWeb web client. To resolve this issue, ensure that the resource exists and that the user has permission to access it.</p><h2 id="code403">You are not authorized to access this resource</h2><p>This error indicates that the user does not have permission to access the requested RemoteApp or desktop resource. To resolve this issue, ensure that the user has been granted access to the resource in RAWeb.</p><h2 id="code10001">The RDP file is missing the full address property</h2><p>This error indicates that RAWeb was unable to find a valid address for the requested RemoteApp or desktop resource. This error appears when the resource does not have the <em>full address</em> or <em>alternate full address</em> properties set in its RDP file. To resolve this issue, ensure that the resource’s RDP file includes a valid <em>full address</em> or <em>alternate full address</em> property.</p><h2 id="code10010">The specified remote host could not be reached.</h2><p>This error indicates that the RAWeb server was unable to connect to the remote host specified in the requested RemoteApp or desktop resource’s RDP file. This may be due to network connectivity issues, firewall settings, or incorrect address information in the RDP file. To resolve this issue, verify that the RAWeb server can reach the remote host and that the address information in the RDP file is correct.</p><h2 id="code10027">The specified remote host refused the connection.</h2><p>The remote host is likely behind a reverse proxy that is blocking connections or a docker container that is offline or not exposed on the specified port.</p><p>See also: <a href="docs/web-client/errors/#code10010">The specified remote host could not be reached</a>.</p><h2 id="code10009">Error checking server certificate</h2><p>RAWeb encountered an error while attempting to validate the server certificate presented by the remote host. Review the latest log file in <code>C:\\inetpub\\RAWeb\\logs</code> that starts with <code>guacd-tunnel-</code> for more details about the specific error encountered.</p><h2 id="code10026">Timeout while checking server certificate</h2><p>See <a href="docs/web-client/errors/#code10009">Error checking server certificate</a>.</p><h2 id="code10032">Failed to resolve hostname to an IPv4 address</h2><p>This error indicates that RAWeb was unable to resolve the hostname specified in the requested RemoteApp or desktop resource’s RDP file to an IPv4 address. This may be due to DNS configuration issues or an incorrect hostname in the RDP file. To resolve this issue, verify that the RAWeb server can resolve the hostname and that the hostname in the RDP file is correct.</p><p>A hostname must resolve to an IPv4 address when <em>full address</em> or <em>alternate full address</em> properties in the RDP file use a hostname rather than an IP address. RAWeb does not currently support using IPv6 addresses for these properties.</p><h2 id="code10005">Domain, username, and password must be provided.</h2><p>This error indicates that the RAWeb server did not receive the necessary credentials to authenticate the user to the remote host. To resolve this issue, ensure that the user provides a valid domain, username, and password when connecting to the RemoteApp or desktop resource via the web client.</p><h2 id="code10008">Gateway username and password must be provided</h2><p>When a resource’s RDP file includes the <code>gatewayhostname:s:</code> property, RAWeb will connect to the resource via the gateway rather than connecting directly. This error indicates that RAWeb was unable to obtain the necessary gateway credentials from the web client.</p><p>To resolve this issue, ensure that the user provides valid gateway credentials when connecting to the RemoteApp or desktop resource via the web client. The web client will prompt the user for their gateway credentials after they provide their credentials for the terminal server.</p><h2 id="code10017">Failed to install the remote desktop proxy service</h2><p>This error occurs when RAWeb is configured to host its own instance of guacd using WSL2, but RAWeb is unable to install the remote desktop proxy service within WSL2. This may be due to issues with the WSL2 installation or configuration on the RAWeb server.</p>',26)),t("p",null,[e[1]||(e[1]=n("To resolve the error, ensure that WSL2 is properly installed and configured on the RAWeb server. Refer to the ")),o(i,{to:"/docs/web-client/prerequisites"},{default:s(()=>e[0]||(e[0]=[n("Web client prerequisites documentation")])),_:1}),e[2]||(e[2]=n(" for more information on setting up WSL2 for RAWeb."))]),e[16]||(e[16]=t("p",null,[n("For specific details about the error encountered, review the latest log file in "),t("code",null,"C:\\inetpub\\RAWeb\\logs"),n(" that starts with "),t("code",null,"guacd-"),n(".")],-1)),e[17]||(e[17]=t("h2",{id:"code10014"},"The remote desktop proxy service did not start in time",-1)),e[18]||(e[18]=t("p",null,"This error occurs if the remote desktop proxy service within WSL2 fails to start within 30 seconds. Your host system may be under heavy load, preventing WSL2 from starting the service in a timely manner.",-1)),e[19]||(e[19]=t("h2",{id:"code10015"},"The remote desktop proxy service is not installed on the server",-1)),t("p",null,[e[4]||(e[4]=n("This error indicates that RAWeb is configured to use its own instance of guacd via WSL2, but the remote desktop proxy service has not been installed within WSL2. To resolve this issue, ensure that WSL2 is properly installed and configured on the RAWeb server. Refer to the ")),o(i,{to:"/docs/web-client/prerequisites"},{default:s(()=>e[3]||(e[3]=[n("Web client prerequisites documentation")])),_:1}),e[5]||(e[5]=n(" for more information on setting up WSL2 for RAWeb."))]),e[20]||(e[20]=t("h2",{id:"code10016"},"The Windows Subsystem for Linux is not installed on the server",-1)),t("p",null,[e[7]||(e[7]=n("This error indicates that RAWeb is configured to use its own instance of guacd via WSL2, but WSL2 is not installed on the RAWeb server. To resolve this issue, install and configure WSL2 on the RAWeb server. Refer to the ")),o(i,{to:"/docs/web-client/prerequisites"},{default:s(()=>e[6]||(e[6]=[n("Web client prerequisites documentation")])),_:1}),e[8]||(e[8]=n(" for more information on setting up WSL2 for RAWeb."))]),e[21]||(e[21]=t("h2",{id:"code10022"},"The remote desktop proxy service failed to install",-1)),e[22]||(e[22]=t("p",null,"This error indicates that RAWeb was unable to install the remote desktop proxy service within WSL2. This may be due to issues with the WSL2 installation or configuration on the RAWeb server.",-1)),e[23]||(e[23]=t("p",null,[n("For specific details about the error encountered, review the latest log file in "),t("code",null,"C:\\inetpub\\RAWeb\\logs"),n(" that starts with "),t("code",null,"guacd-"),n(".")],-1)),e[24]||(e[24]=t("h2",{id:"code10023"},"The Windows Subsystem for Linux optional component is not installed on the server",-1)),e[25]||(e[25]=t("p",null,"This error indicates that RAWeb is configured to use its own instance of guacd via WSL2, but the “Windows Subsystem for Linux” optional Windows component is not enabled on the RAWeb server.",-1)),e[26]||(e[26]=t("p",null,"To resolve this issue, run the following command in PowerShell as an administrator on the RAWeb server:",-1)),o(h,{class:"language-powershell",code:`wsl.exe --install --no-distribution
`}),e[27]||(e[27]=t("h2",{id:"code10024"},"The Virtual Machine Platform optional component is not installed on the server",-1)),e[28]||(e[28]=t("p",null,"This error indicates that RAWeb is configured to use its own instance of guacd via WSL2, but the “Virtual Machine Platform” optional Windows component is not enabled on the RAWeb server.",-1)),e[29]||(e[29]=t("p",null,"To resolve this issue, run the following command in PowerShell as an administrator on the RAWeb server:",-1)),o(h,{class:"language-powershell",code:`Enable-WindowsOptionalFeature -Online -FeatureName VirtualMachinePlatform -All
`}),e[30]||(e[30]=t("h2",{id:"code10028"},"The Virtual Machine Platform is unavailable",-1)),e[31]||(e[31]=t("p",null,"This error indicates that RAWeb is configured to use its own instance of guacd via WSL2, but the “Virtual Machine Platform” feature is unavailable. This may occur if the RAWeb server is running within a virtual machine that does not have nested virtualization enabled.",-1)),t("p",null,[e[10]||(e[10]=n("See the ")),o(i,{to:"/docs/web-client/prerequisites#wsl-hyperv"},{default:s(()=>e[9]||(e[9]=[n("Web client prerequisites documentation")])),_:1}),e[11]||(e[11]=n(" for more information on enabling nested virtualization for virtual machines."))]),e[32]||(e[32]=A('<h2 id="code10025">An error with the Windows Subsystem for Linux prevented the remote desktop proxy service from installing or starting</h2><p>This error indicates that RAWeb encountered an error with WSL2 while attempting to install or start the remote desktop proxy service. This may be due to issues with the WSL2 installation or configuration on the RAWeb server.</p><p>Review the latest log file in <code>C:\\inetpub\\RAWeb\\logs</code> that starts with <code>guacd-</code> for more details about the specific error encountered.</p><h2 id="code10013">The remote desktop proxy service failed to start</h2><p>This error occurs when RAWeb is configured to host its own instance of guacd using WSL2, but RAWeb is unable to start the remote desktop proxy service within WSL2. This may be due to issues with the WSL2 installation or configuration on the RAWeb server.</p><p>Review the latest log file in <code>C:\\inetpub\\RAWeb\\logs</code> that starts with <code>guacd-tunnel-</code> for more details about the specific error encountered.</p><h2 id="code10011">Guacd address is not properly configured</h2><p>This error occurs when RAWeb is configured to use an external guacd server, but the address provided is invalid or unreachable.</p>',8)),t("p",null,[e[13]||(e[13]=n("To resolve this issue, edit the “Allow the web client connection method” policy in RAWeb to provide a valid and reachable guacd server address. Refer to the ")),o(i,{to:"/docs/web-client/prerequisites#opt2"},{default:s(()=>e[12]||(e[12]=[n("Option 2. Provide an address to an existing guacd server")])),_:1}),e[14]||(e[14]=n(" for specific instructions on configuring RAWeb to use an external guacd server."))]),e[33]||(e[33]=A('<h2 id="code10033">The web client is using an unsupported Guacamole protocol version</h2><p>The web client is using a version of the Guacamole protocol that is not supported by the remote desktop proxy service. This may occur if the web client is outdated.</p><p>To resolve this issue, ensure that the web client is up to date. The web client can be force updated by clearing the browser cache or performing a hard refresh (Ctrl + F5) multiple times while on the RAWeb web app.</p><h2 id="code10018">The specified connection file must not specify a file to open on the terminal server</h2><p>This error indicates that the RDP file for the requested RemoteApp resource includes the <code>remoteapplicationmode:i:1</code> value and the <code>remoteapplicationfile:s:</code> property. The <code>remoteapplicationfile:s:</code> property is not supported for RemoteApp resources connected via the RAWeb web client.</p><p>To resolve this issue, remove the <code>remoteapplicationfile:s:</code> property from the resource’s RDP file.</p><h2 id="code10019">The specified connection file must specify a program to open on the terminal server</h2><p>This error indicates that the RDP file for the requested RemoteApp resource includes the <code>remoteapplicationmode:i:1</code> value but does not include the <code>remoteapplicationprogram:s:</code> property. The <code>remoteapplicationprogram:s:</code> property is required for RemoteApp resources.</p><p>To resolve this issue, ensure that the resource’s RDP file includes the <code>remoteapplicationprogram:s:</code> property with a valid program path.</p><h2 id="code10020">The specified connection file must not expand the command line parameters on the terminal server</h2><p>This error indicates that the RDP file for the requested RemoteApp resource includes the <code>remoteapplicationmode:i:1</code> value and the <code>remoteapplicationexpandcmdline:i:1</code> property. The <code>remoteapplicationexpandcmdline:i:1</code> property is not supported for RemoteApp resources connected via the RAWeb web client.</p><p>To resolve this issue, remove the <code>remoteapplicationexpandcmdline:i:1</code> property from the resource’s RDP file.</p><h2 id="code10021">Connections to packaged applications must connect via C:\\Windows\\explorer.exe</h2><p>This error indicates that the RDP file for the requested packaged application resource does not specify <code>C:\\Windows\\explorer.exe</code> as the program to launch on the terminal server. Packaged applications must launch via <code>C:\\Windows\\explorer.exe</code> to ensure proper functionality.</p><p>Packaged applications are identified by the presence of the <code>shell:AppsFolder</code> in the <code>remoteapplicationcmdline:s:</code> property value of the resource’s RDP file.</p><p>To resolve this issue, ensure that the resource’s RDP file specifies <code>C:\\Windows\\explorer.exe</code> as the program to launch on the terminal server. RemoteApps added via the RAWeb management interface will automatically be configured to use <code>C:\\Windows\\explorer.exe</code> for packaged applications.</p><h2 id="code10029">The guacd server could not be reached</h2><p>This error indicates that RAWeb is configured to use an external guacd server, but RAWeb was unable to connect to the specified guacd server. This may be due to network connectivity issues, firewall settings, or incorrect address information.</p><p>To resolve this issue, verify that the RAWeb server can reach the guacd server and that the address information is correct. Additionally, verify that the guacd server is running and accepting connections.</p><h2 id="code10030">The guacd server refused the connection</h2><p>See <a href="docs/web-client/errors/#code10029">The guacd server could not be reached</a>.</p><h2 id="code10031">An unexpected error occurred when attempting to connect to the guacd server</h2><p>This error indicates that an unexpected error occurred while RAWeb was attempting to connect to the guacd server. This may be due to network connectivity issues, firewall settings, or other unforeseen problems.</p><p>Review the latest log file in <code>C:\\inetpub\\RAWeb\\logs</code> that starts with <code>guacd-tunnel-</code> for more details about the error.</p><h2 id="code10034">An unexpected error occurred during the remote desktop session</h2><p>This error indicates that an unexpected error occurred during the remote desktop session between the web client and the remote host.</p><p>Review the latest log file in <code>C:\\inetpub\\RAWeb\\logs</code> that starts with <code>guacd-tunnel-</code> for more details about the error.</p><h2 id="code10035">The remote desktop proxy service is stopping and cannot be started at this time</h2><p>This error indicates that RAWeb is configured to host its own instance of guacd using WSL2, but the remote desktop proxy service within WSL2 is currently stopping and cannot be started at this time.</p><p>Specifically, this error occurs when RAWeb has been instructed to stop its instance of guacd, but then a new web client connection attempt is made before guacd has fully stopped. This error is only shown if it has been more than 20 seconds since the stop request was made.</p><p>This may occur if the RAWeb server is under heavy load or if there are issues with the WSL2 installation or configuration.</p><p>Check the latest log file in <code>C:\\inetpub\\RAWeb\\logs</code> that starts with <code>guacd-</code> for an error message indicating that the guacd WSK instance failed to terminate.</p>',32))])}}},is=Object.freeze(Object.defineProperty({__proto__:null,default:os,redirects:ns,title:ts},Symbol.toStringTag,{value:"Module"})),rs={class:"markdown-body"},ss="Install web client software prerequisites",as="Required software",ls=["wsl2-install"],ds={__name:"index",setup(l,{expose:r}){return r({frontmatter:{title:"Install web client software prerequisites",nav_title:"Required software",redirects:["wsl2-install"]}}),(d,e)=>{const i=p("InfoBar"),h=p("CodeBlock"),y=p("RouterLink");return a(),c("div",rs,[e[16]||(e[16]=t("p",null,[n("The web client requires the RAWeb server to have access to a "),t("a",{href:"https://guacamole.apache.org/",target:"_blank",rel:"noopener noreferrer"},"Guacamole"),n(" daemon ("),t("a",{href:"https://hub.docker.com/r/guacamole/guacd/",target:"_blank",rel:"noopener noreferrer"},"guacd"),n("). There are two options for providing guacd to RAWeb:")],-1)),e[17]||(e[17]=t("ul",null,[t("li",null,[t("a",{href:"docs/web-client/prerequisites/#opt1"},"Option 1. Allow RAWeb to start its own guacd instance"),n(" (recommended for most environments)")]),t("li",null,[t("a",{href:"docs/web-client/prerequisites/#opt2"},"Option 2. Provide an address to an existing guacd server")])],-1)),o(i,{severity:"attention",title:"You only need to follow these instructions once."},{default:s(()=>e[0]||(e[0]=[n(" When you upgrade RAWeb, you do not need to repeat these steps unless you are switching to a different option for providing guacd. ")])),_:1}),o(i,null,{default:s(()=>e[1]||(e[1]=[n(" These prerequisites are only necessary if you plan to use the web client connection method. ")])),_:1}),e[18]||(e[18]=t("h1",{id:"opt1"},"Option 1. Allow RAWeb to start its own guacd instance",-1)),e[19]||(e[19]=t("p",null,"RAWeb can start its own guacd instance when a user first accesses the web client. This option requires a system-wide installation of Windows Subsystem for Linux 2 (WSL2) to be available on the RAWeb server. Use the following steps to install WSL2 and ensure it is ready for RAWeb to use.",-1)),o(i,null,{default:s(()=>e[2]||(e[2]=[n(" On Windows 11 or Windows Server 2022 Desktop Experience or newer, you may be able to skip the first two steps. ")])),_:1}),t("ol",null,[e[4]||(e[4]=A('<li><p><strong>Download the latest system-wide installer for Windows Subsystem for Linux from <a href="https://github.com/microsoft/WSL/releases/latest" target="_blank" rel="noopener noreferrer">https://github.com/microsoft/WSL/releases/latest.</a></strong> <br> Be sure to choose the <em>msi</em> installer, not the <em>msixbundle</em>.</p></li><li><p><strong>Run the installer.</strong> <br> When it completes, it will automatically close the installation window.</p></li><li><p><strong>Open PowerShell as an administrator.</strong> <br> Press the Windows key + X, then select PowerShell (Administrator) or Terminal (Administrator).</p></li>',3)),t("li",null,[e[3]||(e[3]=t("p",null,[t("strong",null,"Copy and paste the following command, and then press enter."),n(),t("br"),n(" This command will enable the “Windows Subsystem for Linux” and “Virtual Machine Platform” optional Windows components if they are not already enabled.")],-1)),o(h,{class:"language-powershell",code:`wsl.exe --install --no-distribution
`})]),e[5]||(e[5]=t("li",null,[t("p",null,[t("strong",null,"Restart the server or PC if prompted."),n(),t("br"),n(" Enabling the virtual machine platform requires a restart to actually enable the feature.")])],-1))]),o(i,{title:"Storage consideration"},{default:s(()=>e[6]||(e[6]=[t("p",null,[n("The "),t("code",null,"guacd"),n(" image used by RAWeb consumes 30-40 megabytes of disk space. If you choose this option, ensure that the RAWeb server has sufficient disk space to accommodate the image.")],-1)])),_:1}),o(i,{severity:"caution",title:"Virtual Machine Platform requirement"},{default:s(()=>[e[7]||(e[7]=n(' If the "Windows Subsystem for Linux" optional Windows component was already enabled before step 4, you must manually enable the "Virtual Machine Platform" optional Windows component. To do this, run the following command in PowerShell as an administrator: ')),o(h,{class:"language-powershell",code:`Enable-WindowsOptionalFeature -Online -FeatureName VirtualMachinePlatform -All
`}),e[8]||(e[8]=t("p",null,"After running the command, restart the server or PC to apply the changes.",-1))]),_:1}),t("p",null,[e[10]||(e[10]=n("Review the ")),o(y,{to:"/docs/web-client/about/"},{default:s(()=>e[9]||(e[9]=[n("capabilities and considerations")])),_:1}),e[11]||(e[11]=n(" page for additional information about the web client and guacd."))]),e[20]||(e[20]=t("h2",{id:"wsl-hyperv"},"If RAWeb is running within a virtual machine",-1)),e[21]||(e[21]=t("p",null,"If you are running RAWeb within a Hyper-V virtual machine, you must also enable nested virtualization for the VM. Without nested virtualization, WSL2 will not be able to start, preventing RAWeb from starting guacd. To enable nested virtualization, shut down the VM and run the following command in PowerShell as an administrator on the Hyper-V host:",-1)),o(h,{code:`Set-VMProcessor -VMName <VMName> -ExposeVirtualizationExtensions $true
`}),o(i,{severity:"caution",title:"AMD processor limitation"},{default:s(()=>e[12]||(e[12]=[n(" Windows versions prior to Windows 11 and Windows Server 2025 do not support nested virtualization on AMD processors. ")])),_:1}),e[22]||(e[22]=A('<p>If you are using a different hypervisor, refer to its documentation for instructions on enabling nested virtualization.</p><h1 id="opt2">Option 2. Provide an address to an existing guacd server</h1><p>You can provide RAWeb the address of an existing guacd server. Be cautious when using this option; guacd does not have built-in authentication, so if the guacd server is accessible to unauthorized users, they could potentially access desktops and applications through it.</p><p>To provide RAWeb with the address of an existing guacd server, follow these steps:</p><ol><li>Open the RAWeb web interface.</li><li>Sign in to RAWeb as an administrator.</li><li>Navigate to <strong>Policies</strong>.</li><li>Select the <strong>Allow the web client connection method</strong> policy.</li><li>If you see a choice between “Use an RAWeb-managed guacd container” and “Use an externally-managed guacd instance”, select <strong>Use an externally-managed guacd instance</strong> option.</li><li>Enter the hostname or IP address and port of the guacd server in the <strong>External address</strong> fields.</li><li>Click <strong>OK</strong> to apply the policy changes.</li></ol>',5)),t("p",null,[e[14]||(e[14]=n("Review the ")),o(y,{to:"/docs/web-client/about/"},{default:s(()=>e[13]||(e[13]=[n("capabilities and considerations")])),_:1}),e[15]||(e[15]=n(" page for additional information about the web client and guacd."))])])}}},cs=Object.freeze(Object.defineProperty({__proto__:null,default:ds,nav_title:as,redirects:ls,title:ss},Symbol.toStringTag,{value:"Module"})),hs={class:"markdown-body"},us="Inject custom content into RAWeb",ps="Content injection",ms={__name:"index",setup(l,{expose:r}){return r({frontmatter:{title:"Inject custom content into RAWeb",nav_title:"Content injection"}}),(d,e)=>{const i=p("InfoBar"),h=p("CodeBlock");return a(),c("div",hs,[e[1]||(e[1]=t("p",null,[n("RAWeb allows administrators to inject custom CSS and JavaScript into the web application by placing files in the "),t("code",null,"App_Data/inject"),n(" folder of the RAWeb server installation. Any static file placed in this folder will be served by RAWeb, and specific files can be used to customize the appearance and behavior of the web application.")],-1)),o(i,{title:"Unsupported feature",severity:"caution"},{default:s(()=>e[0]||(e[0]=[n(" Custom content injection is made available for advanced users and administrators as way to test potential new features for RAWeb or implement customizatations that are not otherwise provided by RAWeb. It is not officially supported and may break in future releases. Use at your own risk. ")])),_:1}),e[2]||(e[2]=A("<p>To inject custom content, create the following files in the <code>App_Data/inject</code> folder:</p><ul><li><code>index.css</code>: This file can contain custom CSS styles that will be applied to the RAWeb web application.</li><li><code>index.js</code>: This file can contain custom JavaScript code that will be executed in the context of the RAWeb web application.</li></ul><p>Once these files are placed in the <code>App_Data/inject</code> folder, RAWeb will automatically include them in the web application. This allows administrators to customize the appearance and behavior of RAWeb to better fit their organization’s needs.</p><p>For example, you could use <code>index.css</code> to change the color scheme of the RAWeb interface, or use <code>index.js</code> to add custom functionality such as additional logging or user interface enhancements.</p><h2>Example: Hide the wiki button from the navigation bar</h2><p>To hide the “Docs” button from the navigation bar, you can create an <code>index.css</code> file in the <code>App_Data/inject</code> folder with the following content:</p>",6)),o(h,{class:"language-css",code:`.nav-rail a[href="/docs"] {
  display: none;
}
`}),e[3]||(e[3]=t("p",null,[n("This CSS rule targets the anchor element that links to the documentation page and sets its display property to "),t("code",null,"none"),n(", effectively hiding it from view.")],-1)),e[4]||(e[4]=t("h2",null,"Example: Use iris spring accent color",-1)),e[5]||(e[5]=t("p",null,[n("To change the accent color of RAWeb to match the iris spring theme, create an "),t("code",null,"index.css"),n(" file in the "),t("code",null,"App_Data/inject"),n(" folder with the following content:")],-1)),o(h,{class:"language-css",code:`@media (prefers-color-scheme: light) {
  :root {
    --wui-accent-default: hsl(265, 40%, 43%);
    --wui-accent-secondary: hsl(265, 33%, 48%);
    --wui-accent-tertiary: hsl(266, 29%, 53%);
    --wui-accent-selected-text-background: hsl(265, 37%, 48%);
    --wui-accent-text-primary: hsl(259, 50%, 31%);
    --wui-accent-text-secondary: hsl(255, 71%, 19%);
    --wui-accent-text-tertiary: hsl(265, 40%, 43%);
  }
}

@media (prefers-color-scheme: dark) {
  :root {
    --wui-accent-default: hsl(272, 39%, 72%);
    --wui-accent-secondary: hsl(272, 29%, 66%);
    --wui-accent-tertiary: hsl(272, 22%, 60%);
    --wui-accent-selected-text-background: hsl(265, 37%, 48%);
    --wui-accent-text-primary: hsl(282, 53%, 87%);
    --wui-accent-text-secondary: hsl(282, 53%, 87%);
    --wui-accent-text-tertiary: hsl(272, 39%, 72%);
  }
}
`}),e[6]||(e[6]=t("h2",null,"Example: Add Google Analytics tracking",-1)),e[7]||(e[7]=t("p",null,[n("To add Google Analytics tracking to RAWeb, create an "),t("code",null,"index.js"),n(" file in the "),t("code",null,"App_Data/inject"),n(" folder with the following content, replacing "),t("code",null,"G-XXXXXXXXXX"),n(" with your actual Google Analytics tag ID:")],-1)),o(h,{class:"language-javascript",code:`/**
 * Inject Google Analytics tracking script
 * @param {string} tag - The Google Analytics tag ID
 */
function injectGoogleAnalytics(tag) {
  if (!tag) {
    console.error('Google Analytics tag ID is not provided.');
    alert('Google Analytics tag ID is not provided.');
    return;
  }

  // create and inject the gtag script
  const gtagScript = document.createElement('script');
  gtagScript.async = true;
  gtagScript.src = \`https://www.googletagmanager.com/gtag/js?id=\${tag}\`;
  document.head.appendChild(gtagScript);

  // initialize dataLayer and gtag function
  window.dataLayer = window.dataLayer || [];
  function gtag() {
    dataLayer.push(arguments);
  }
  gtag('js', new Date());
  gtag('config', tag);
}

injectGoogleAnalytics('G-XXXXXXXXXX');
`}),e[8]||(e[8]=t("h2",null,"Example: Replace the titlebar logo",-1)),e[9]||(e[9]=t("p",null,[n("To replace the RAWeb logo in the titlebar with a custom logo, create an "),t("code",null,"index.css"),n(" file in the "),t("code",null,"App_Data/inject"),n(" folder with the following content, replacing the URL with the path to your custom logo image:")],-1)),o(h,{class:"language-css",code:`:root {
  --logo-url: url("/lib/assets/default.ico");
}

/* replace header logo */
.app-header img.logo {
  display: none;
}
.app-header .left::before {
  content: "";
  display: inline-block;
  width: 16px;
  height: 16px;
  padding: 0 8px;
  background-image: var(--logo-url);
  background-size: contain;
  background-repeat: no-repeat;
  background-position: center;
}

/* replace splash screen logo */
:where(svg.root-splash-app-logo, svg.splash-app-logo) > * {
  display: none;
}
:where(svg.root-splash-app-logo, svg.splash-app-logo) {
  background-image: var(--logo-url);
  background-size: contain;
  background-repeat: no-repeat;
  background-position: center;
}

`})])}}},fs=Object.freeze(Object.defineProperty({__proto__:null,default:ms,nav_title:ps,title:us},Symbol.toStringTag,{value:"Module"})),As="/deploy-preview/pr-215/lib/assets/client-video-B07BLuIt.webp",gs="/deploy-preview/pr-215/lib/assets/connection-open-setting-Dr5m4sVH.webp",Is={class:"markdown-body"},Ts="Access resources via the web client",bs="Using the web client",ys={__name:"index",setup(l,{expose:r}){return r({frontmatter:{title:"Access resources via the web client",nav_title:"Using the web client"}}),(d,e)=>{const i=p("InfoBar");return a(),c("div",Is,[e[4]||(e[4]=t("p",null,"If your administrator has enabled the built-in web client, you may access all devices and RemoteApps directly from a web browser. No additional software is required on the client device.",-1)),o(i,{severity:"attention"},{default:s(()=>e[0]||(e[0]=[n(" The web client is a newer addition to RAWeb and is still considered experimental. While it is functional for many use cases, some features may be missing or infeasible to implement. For users who require a more stable experience, we recommend using a dedicated remote desktop client instead. ")])),_:1}),e[5]||(e[5]=t("img",{src:As,width:"824",alt:"",style:{border:"1px solid var(--wui-card-stroke-default)","border-radius":"var(--wui-control-corner-radius)"},height:"584",xmlns:"http://www.w3.org/1999/xhtml"},null,-1)),e[6]||(e[6]=A('<p>Jump to a section:</p><ul><li><a href="docs/web-client/#access-the-web-client">Access the web client</a></li><li><a href="docs/web-client/#features-of-the-web-client">Features of the web client</a><ul><li><a href="docs/web-client/#not-supported">Unsupported features</a></li></ul></li><li><a href="docs/web-client/#control-where-connections-open">Control where connections open</a></li><li><a href="docs/web-client/#troubleshooting">Troubleshooting</a></li></ul><h2 id="access-the-web-client">Access the web client</h2><ol><li>To access the web client, open a web browser and navigate to the RAWeb server’s URL.</li><li>If necessary, sign in with your credentials.</li><li>Navigate the <strong>devices</strong> list or <strong>apps</strong> list to find the resource you wish to access.</li><li>Click on the device or app to start a connection. <ul><li>For some views, you may need to click the <strong>Connect</strong> button instead of clicking anywhere on the device or app.</li><li>By default, the web client will launch the connection in a new window.</li></ul></li></ol>',4)),o(i,{title:"Pop-up blockers"},{default:s(()=>e[1]||(e[1]=[n(" If your browser has a pop-up blocker enabled, you may need to allow pop-ups from the RAWeb server's URL for connections to open in a new window. ")])),_:1}),e[7]||(e[7]=A('<h2 id="features-of-the-web-client">Features of the web client</h2><ul><li><strong>No client software required</strong><ul><li>Access resources from any modern web browser without installing additional software.</li></ul></li><li><strong>Automatically adjust display size</strong><ul><li>The web client automatically adjusts the display resolution to fit your browser window when you resize it.</li></ul></li><li><strong>Connect to RemoteApps and full desktops</strong></li><li><strong>Basic clipboard support</strong><ul><li>Copy and paste text between your local device and the remote session.</li></ul></li><li><strong>Speaker output redirection</strong></li><li><strong>Access connections from anywhere</strong><ul><li>The web client is a proxy between your browser and the terminal server. As long as the RAWeb server can access the terminal server, you can connect from any location where you can reach the RAWeb server.</li></ul></li></ul><h3 id="not-supported">Not supported</h3><ul><li><strong>File transfer</strong>: The web client does not support file transfer between the local device and the remote session.</li><li><strong>Microphone redirection</strong>: The web client does not support microphone input from the local device to the remote session.</li><li><strong>USB redirection</strong>: The web client does not support USB device redirection.</li><li><strong>Advanced clipboard features</strong>: The web client only supports basic text clipboard functionality and does not support images or files.</li><li><strong>Insecure connections</strong>: The web client can only connect to terminal servers with Network-Level Authentication (NLA) enabled.</li><li><strong>Multiple monitors</strong>: The web client does not support multiple monitor setups.</li></ul><h2 id="control-where-connections-open">Control where connections open</h2><p>By default, connections launched from the web client will open in a new browser window. You can change this behavior to open connections in the same window instead.</p><ol><li>On the web app, navigate to the <strong>Settings</strong> page. <ul><li>This is usually accessible via the gear icon present in the navigation rail. If you do not have a navigation rail, look for the icon in the titlebar.</li></ul></li><li>In the <strong>Web client display method</strong> section, toggle the switch for <strong>Open web client connections in a new window</strong>.</li></ol>',7)),o(i,{severity:"attention"},{default:s(()=>e[2]||(e[2]=[n(" If the option is disabled, contact your administrator. They may have enforced the setting via a policy. ")])),_:1}),o(i,{title:"Pop-up blockers"},{default:s(()=>e[3]||(e[3]=[n(" If your browser has a pop-up blocker enabled, you may need to allow pop-ups from the RAWeb server's URL for connections to open in a new window. ")])),_:1}),e[8]||(e[8]=t("img",{src:gs,width:"824",alt:"Web client setting to control whether connections open in a new window or the same window.",style:{border:"1px solid var(--wui-card-stroke-default)","border-radius":"var(--wui-control-corner-radius)"},height:"583",xmlns:"http://www.w3.org/1999/xhtml"},null,-1)),e[9]||(e[9]=t("h2",{id:"troubleshooting"},"Troubleshooting",-1)),e[10]||(e[10]=t("p",null,"If you encounter issues using the web client, consider the following troubleshooting steps:",-1)),e[11]||(e[11]=t("ul",null,[t("li",null,[t("strong",null,"Ensure your browser is supported"),n(": The web client requires a modern web browser that supports WebSockets and HTML5 features. Supported browsers include the latest versions of Google Chrome, Mozilla Firefox, Microsoft Edge, and Safari.")]),t("li",null,[t("strong",null,"Download the RDP file and check if you can connect using a different RDP client"),n(": If you are unable to connect using the web client, try downloading the RDP file for the resource and opening it with a different RDP client, such as the Microsoft Remote Desktop app. This can help determine if the issue is specific to the web client or a broader connectivity problem. If you normally must connect to a VPN before you can reach a terminal server on your local device, note that your administrator must configure the device that hosts RAWeb to connect to the VPN as well.")])],-1)),e[12]||(e[12]=t("p",null,[n("The web client is a newer addition to RAWeb, so there may be occasional issues. If you continue to experience problems, contact your administrator for further assistance, and work with them to "),t("a",{href:"https://github.com/kimmknight/raweb/issues",target:"_blank",rel:"noopener noreferrer"},"file a bug report"),n(" if necessary.")],-1))])}}},Rs=Object.freeze(Object.defineProperty({__proto__:null,default:ys,nav_title:bs,title:Ts},Symbol.toStringTag,{value:"Module"})),Es="/deploy-preview/pr-215/lib/assets/c5501233-6ef0-48b4-b10d-026139d90c0f-sfGg2XBD.png",ws="/deploy-preview/pr-215/lib/assets/f4551b21-bea4-42bd-9bf0-be728d3d2d39-f-5tD707.png",Ss="/deploy-preview/pr-215/lib/assets/6d3eccd5-eb60-4573-9d09-ec178aa95dbc-CoTG_lda.png",Os="/deploy-preview/pr-215/lib/assets/f3f997d2-3bbe-4965-b3ec-675a5565111c-CkA7oWqf.png",Ns="/deploy-preview/pr-215/lib/assets/bde8567d-dbff-47d7-81f1-695de517d01e-BixqZTW4.png",vs="/deploy-preview/pr-215/lib/assets/d69d8074-78a2-4774-8ba0-9e1a08d083f0-Bl6qCYae.png",Ls="/deploy-preview/pr-215/lib/assets/e84cb7e8-48ff-4d82-bfee-d49c682a2fd6-DXc5-NN1.png",Cs="/deploy-preview/pr-215/lib/assets/12967053-a025-4ec8-ac56-ebd4d5da109c-DiB2bNLw.png",Ds="/deploy-preview/pr-215/lib/assets/3b838293-83f5-4016-b828-761beefb3179-C0GA9lgq.png",Ps="/deploy-preview/pr-215/lib/assets/e3584a4e-ebdf-4707-a299-5604538af954-CQObtZEO.png",Fs="/deploy-preview/pr-215/lib/assets/3594cd9d-d108-46fc-bade-f535449746cc-BCNH5cpm.png",Ws={class:"markdown-body"},Hs="Access RAWeb resources as a workspace",ks="Access via Windows App",Us={__name:"index",setup(l,{expose:r}){return r({frontmatter:{title:"Access RAWeb resources as a workspace",nav_title:"Access via Windows App"}}),(d,e)=>{const i=p("RouterLink"),h=p("InfoBar");return a(),c("div",Ws,[e[11]||(e[11]=t("p",null,"In addition to accessing resources from the RAWeb web interface, you can also access resources via:",-1)),e[12]||(e[12]=t("ul",null,[t("li",null,"RemoteApp and Desktop Connections (RADC) on Windows"),t("li",null,"Workspaces in Windows App (formerly Microsoft Remote Desktop) on macOS, Android, iOS, and iPadOS")],-1)),o(h,{severity:"caution",title:"Caution"},{default:s(()=>[e[2]||(e[2]=n(" This feature will only work if RAWeb is using an SSL certificate that is trusted on every device that attempts to access the resources in RAWeb. ")),e[3]||(e[3]=t("br",null,null,-1)),e[4]||(e[4]=n(" Refer to ")),o(i,{to:"/docs/security/error-5003"},{default:s(()=>e[0]||(e[0]=[n("Trusting the RAWeb server")])),_:1}),e[5]||(e[5]=n(" for more details and instructions for using a trusted SSL certificate. ")),e[6]||(e[6]=t("br",null,null,-1)),e[7]||(e[7]=n(" We recommend ")),o(i,{to:"/docs/security/error-5003#option-2"},{default:s(()=>e[1]||(e[1]=[n("using a certificate from a globally trusted certificate authority")])),_:1}),e[8]||(e[8]=n(". "))]),_:1}),e[13]||(e[13]=A('<h2 id="workspace-url">Identify your workspace URL or email address</h2><p>Before you can add RAWeb’s resources to RADC or Windows App, you need to know the URL for the workspace. Follow these instructions for finding your workspace URL.</p><ol><li>Navigate to your RAWeb installation’s web interface from the device with RADC or Windows App. <em>This step is important; If you cannot access the web interface from the device with RADC or Windows App, your workspace URL will not work.</em></li><li>Sign in to RAWeb.</li><li>Navigate to RAWeb settings. <ul><li>For most users, access settings by clicking or tapping <strong>Settings</strong> in the bottom-left corner of the screen.</li><li>If you or your administrator have configured RAWeb to use <em>simple mode</em>, click or tap the settings icon next to you username in the top-right area of the titlebar.</li></ul></li><li>In the <strong>Workspace URL</strong> section, click or tap <strong>Copy workspace URL</strong> or <strong>Copy workspace email</strong>. Use this URL or email address when adding a workspace. <br> <em>For information about email-based workspace discovery, refer to <a href="https://learn.microsoft.com/en-us/windows-server/remote/remote-desktop-services/rds-email-discovery" target="_blank" rel="noopener noreferrer">the documentaiton on Microsoft Learn</a> and <a href="https://github.com/kimmknight/raweb/pull/129" target="_blank" rel="noopener noreferrer">PR#129</a>.</em><ul><li>If you do not see the <strong>Workspace URL</strong> section, your administrator disabled it via policy. Contact your administrator for assistance.</li></ul></li></ol><p>Now, jump to one of the follow sections based on which device you are using:</p><ul><li><a href="docs/workspaces/#windows-radc">Windows via RemoteApp and Desktop Connections</a></li><li><a href="docs/workspaces/#macos">macOS via Windows App</a></li><li><a href="docs/workspaces/#android">Android via Windows App</a></li><li><a href="docs/workspaces/#ios-and-ipados">iOS and iPadOS via Windows App</a></li></ul>',5)),o(h,{severity:"attention",title:"Note"},{default:s(()=>e[9]||(e[9]=[n(" Windows App on Windows does not support adding workspaces via URL or email address. "),t("br",null,null,-1),n(" Instead, use RemoteApp and Desktop Connections. ")])),_:1}),e[14]||(e[14]=t("h2",{id:"windows-radc"},"Windows via RemoteApp and Desktop Connections",-1)),e[15]||(e[15]=t("ol",null,[t("li",null,[n("Right click the Start menu (or press the Windows key + X) and choose "),t("strong",null,"Run"),n(".")]),t("li",null,[n("In the "),t("strong",null,"Run"),n(" dialog, type "),t("em",null,"control.exe"),n(". Click "),t("strong",null,"OK"),n(". "),t("strong",null,"Control Panel"),n(" will open."),t("br"),t("img",{width:"380",src:Es,height:"225",xmlns:"http://www.w3.org/1999/xhtml"})]),t("li",null,[n("If needed, change the view from "),t("strong",null,"Category"),n(" to "),t("strong",null,"Small icons"),n(" or "),t("strong",null,"Large icons"),n("."),t("br"),t("img",{width:"830",src:ws,height:"442",xmlns:"http://www.w3.org/1999/xhtml"})]),t("li",null,[n("Click "),t("strong",null,"RemoteApp and Desktop Connections"),n(" in the list."),t("br"),t("img",{width:"830",src:Ss,height:"489",xmlns:"http://www.w3.org/1999/xhtml"})]),t("li",null,[n("On the left side, click "),t("strong",null,"Access RemoteApp and desktops"),n("."),t("br"),t("img",{width:"830",src:Os,height:"236",xmlns:"http://www.w3.org/1999/xhtml"})]),t("li",null,[n("In the "),t("strong",null,"Access RemoteApp and desktops"),n(" window, enter the "),t("a",{href:"docs/workspaces/#workspace-url"},"workspace URL or email address"),n(". Click "),t("strong",null,"Next"),n(" to continue to the next step."),t("br"),t("img",{width:"586",src:Ns,height:"521",xmlns:"http://www.w3.org/1999/xhtml"})]),t("li",null,[n("Review the information. Then, click "),t("strong",null,"Next"),n(" to connect.")]),t("li",null,[n("You will see an "),t("strong",null,"Adding connection resources"),n(" message. During this step, resources and icons are downloaded from RAWeb. Depending on the number of resources, this may take a while. "),t("ul",null,[t("li",null,[n("If you see a "),t("strong",null,"Windows Security"),n(" dialog with the message "),t("em",null,"Your credentials did not work"),n(", enter the credentials you use to sign in to the RAWeb web interface."),t("br"),t("img",{width:"586",src:vs,height:"521",xmlns:"http://www.w3.org/1999/xhtml"}),t("br"),t("img",{width:"456",src:Ls,height:"380",xmlns:"http://www.w3.org/1999/xhtml"})])])]),t("li",null,[n("If the connection succeeded, you will see a message indicating the connection name and URL and the programs and desktops that have been added to the Start menu."),t("br"),n("Windows will periodically update the connection. You may also manually force the connection to update via the control panel."),t("br"),t("img",{width:"586",src:Cs,height:"521",xmlns:"http://www.w3.org/1999/xhtml"})])],-1)),o(h,{title:"Note"},{default:s(()=>e[10]||(e[10]=[t("p",null,[n("By default, Windows will update the connection at 12:00 AM every day for any user currently sign in to the device. You can increase the frequency or add custom triggers (e.g. on device unlock) by editing the task in "),t("strong",null,"Task Scheduler"),n(":")],-1),t("ol",null,[t("li",null,[n("Open "),t("strong",null,"Task Scheduler"),n(" on the client device.")]),t("li",null,[n("Expand "),t("strong",null,"Task Scheduler Library » Microsoft » Windows » RemoteApp and Desktop Connections Update » <username>"),n(".")]),t("li",null,[n("Double click the "),t("strong",null,"Update connections"),n(" task")]),t("li",null,[n("Switch to the "),t("strong",null,"Triggers"),n(" tab.")]),t("li",null,"Create and save a new trigger.")],-1)])),_:1}),e[16]||(e[16]=t("h2",{id:"macos"},"macOS via Windows App",-1)),e[17]||(e[17]=t("ol",null,[t("li",null,[n("Install "),t("a",{href:"https://apps.apple.com/us/app/windows-app/id1295203466",target:"_blank",rel:"noopener noreferrer"},"Windows App from the App Store"),n(".")]),t("li",null,[n("Open "),t("strong",null,"Windows App"),n(".")]),t("li",null,[n("In the menu bar, choose "),t("strong",null,"Connections > Add Workspace…"),n("."),t("br"),t("img",{width:"477",src:Ds,height:"326",xmlns:"http://www.w3.org/1999/xhtml"})]),t("li",null,[n("In the "),t("strong",null,"Add Workspace"),n(" sheet, enter the "),t("a",{href:"docs/workspaces/#workspace-url"},"workspace URL or email address"),n(". Change "),t("strong",null,"Credentials"),n(" to the credentials you use when you sign in to the RAWeb web interface. Click "),t("strong",null,"Add"),n(" to add the workspace."),t("br"),t("img",{width:"500",src:Ps,height:"401",xmlns:"http://www.w3.org/1999/xhtml"})]),t("li",null,[n("You will see a "),t("strong",null,"Setting up workspace…"),n(" message. During this step, resources and icons are downloaded from RAWeb. Depending on the number of resources, this may take a while."),t("br"),t("img",{width:"500",src:Fs,height:"164",xmlns:"http://www.w3.org/1999/xhtml"})])],-1)),e[18]||(e[18]=A('<p>If the connection succeeeded, you will see your apps and devices included in Windows App.</p><h2 id="android">Android via Windows App</h2><ol><li>Install <a href="https://play.google.com/store/apps/details?id=com.microsoft.rdc.androidx" target="_blank" rel="noopener noreferrer">Windows App from the Play Store</a>.</li><li>Open <strong>Windows App</strong>.</li><li>Tap the <strong>+</strong> button in the top-right corner of the app.</li><li>Choose <strong>Workspace</strong>.</li><li>In the <strong>Add Workspace</strong> dialog, enter the <a href="docs/workspaces/#workspace-url">workspace URL or email address</a>. Change <strong>User account</strong> to the credentials you use when you sign in to the RAWeb web interface. Tap <strong>Next</strong> to add the workspace.</li><li>You will see a <strong>Setting up workspace…</strong> message. During this step, resources and icons are downloaded from RAWeb. Depending on the number of resources, this may take a while.</li></ol><h2 id="ios-and-ipados">iOS and iPadOS via Windows App</h2><ol><li>Install <a href="https://apps.apple.com/my/app/windows-app-mobile/id714464092" target="_blank" rel="noopener noreferrer">Windows App Mobile from the App Store</a>.</li><li>Open <strong>Windows App</strong>.</li><li>Tap the <strong>+</strong> button in the top-right corner of the app.</li><li>Choose <strong>Workspace</strong>.</li><li>In the <strong>Add Workspace</strong> sheet, enter the <a href="docs/workspaces/#workspace-url">workspace URL or email address</a>. Change <strong>Credentials</strong> to the credentials you use when you sign in to the RAWeb web interface. Tap <strong>Next</strong> to add the workspace.</li><li>You will see a <strong>Setting up workspace…</strong> message. During this step, resources and icons are downloaded from RAWeb. Depending on the number of resources, this may take a while.</li></ol>',5))])}}},Ms=Object.freeze(Object.defineProperty({__proto__:null,default:Us,nav_title:ks,title:Hs},Symbol.toStringTag,{value:"Module"})),Gs={class:"markdown-body"},Bs="Get started with RAWeb",xs="Get started",Ys={__name:"index",setup(l,{expose:r}){return r({frontmatter:{title:"Get started with RAWeb",nav_title:"Get started"}}),(d,e)=>{const i=p("RouterLink"),h=p("CodeBlock");return a(),c("div",Gs,[t("p",null,[e[1]||(e[1]=n("The easiest way to get started with RAWeb is to install it with our installation script. Before you install RAWeb, review our ")),o(i,{to:"/docs/supported-environments"},{default:s(()=>e[0]||(e[0]=[n("supported environments documentation")])),_:1}),e[2]||(e[2]=n(". Follow the steps below:"))]),t("ol",null,[e[12]||(e[12]=t("li",null,[t("p",null,[t("strong",null,"Open PowerShell as an administrator"),t("br"),n(" Press the Windows key + X, then select PowerShell (Administrator) or Terminal (Administrator).")])],-1)),t("li",null,[e[3]||(e[3]=t("p",null,[t("strong",null,[n("Copy and paste the code below"),t("sup",{class:"footnote-ref"},[t("a",{href:"docs/get-started/#fn1",id:"fnref1"},"[1]")]),n(", then press enter.")])],-1)),o(h,{code:`irm https://github.com/kimmknight/raweb/releases/latest/download/install.ps1 | iex
`})]),t("li",null,[e[6]||(e[6]=t("p",null,[t("strong",null,"Follow the prompts.")],-1)),o(g(L),{severity:"caution",title:"Caution"},{default:s(()=>e[4]||(e[4]=[n(" The installer will retrieve the pre-built version of RAWeb from the latest release and install it to "),t("code",null,"C:\\inetpub\\RAWeb",-1),n(". "),t("br",null,null,-1),n(" Refer to "),t("a",{href:"https://github.com/kimmknight/raweb/releases/latest",target:"_blank",rel:"noopener noreferrer"},"the release page",-1),n(" for more details. ")])),_:1}),o(g(L),{severity:"attention",title:"Note"},{default:s(()=>e[5]||(e[5]=[n(" If Internet Information Services (IIS) or other required components are not already installed, the RAWeb installer will retreive and install them. ")])),_:1})]),t("li",null,[t("p",null,[e[8]||(e[8]=t("strong",null,"Install web client prerequisites.",-1)),e[9]||(e[9]=t("br",null,null,-1)),e[10]||(e[10]=n(" If you plan to use the web client connection method, follow the instructions in our ")),o(i,{to:"/docs/web-client/prerequisites"},{default:s(()=>e[7]||(e[7]=[n("web client prerequisites documentation")])),_:1}),e[11]||(e[11]=n(" to install and configure the required software."))])])]),e[16]||(e[16]=t("p",null,[n("To install other versions, visit the "),t("a",{href:"https://github.com/kimmknight/raweb/releases",target:"_blank",rel:"noopener noreferrer"},"the releases page"),n(" on GitHub.")],-1)),e[17]||(e[17]=t("h2",null,"Using RAWeb",-1)),e[18]||(e[18]=t("p",null,[n("By default, RAWeb is available at "),t("a",{href:"https://127.0.0.1/RAWeb",target:"_blank",rel:"noopener noreferrer"},"https://127.0.0.1/RAWeb"),n(". To access RAWeb from other computers on your local network, replace 127.0.0.1 with your host PC or server’s name. To access RAWeb from outside your local network, expose port 443 and replace 127.0.0.1 with your public IP address.")],-1)),t("p",null,[e[14]||(e[14]=n("To add resources to the RAWeb interface, ")),o(i,{to:"/docs/publish-resources"},{default:s(()=>e[13]||(e[13]=[n("refer to Publishing RemoteApps and Desktops")])),_:1}),e[15]||(e[15]=n("."))]),e[19]||(e[19]=A('<p>Refer to the guides in this wiki’s sidebar for more information about using RAWeb.</p><hr class="footnotes-sep"><section class="footnotes"><ol class="footnotes-list"><li id="fn1" class="footnote-item"><p>If you are attempting to install RAWeb on Windows Server 2016, you may need to enable TLS 1.2. In PowerShell, run <code>[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12</code>. <a href="docs/get-started/#fnref1" class="footnote-backref">↩︎</a></p></li></ol></section>',3))])}}},Vs=Object.freeze(Object.defineProperty({__proto__:null,default:Ys,nav_title:xs,title:Bs},Symbol.toStringTag,{value:"Module"})),js=Be,_s=pe,qs=xe,Js=me,zs=Ye,Ks=fe,Xs=Ve,$s=Ae,Qs=je,Zs=ge,ea=_e,ta=Ie,na={class:"markdown-body"},oa="RAWeb",ia={__name:"index",setup(l,{expose:r}){return r({frontmatter:{title:"RAWeb"}}),(d,e)=>{const i=p("RouterLink");return a(),c("div",na,[e[6]||(e[6]=t("p",null,"A web interface and workspace provider for viewing and managing your RemoteApps and Desktops hosted on Windows 10, 11, and Server.",-1)),e[7]||(e[7]=t("p",null,[n("To set up RemoteApps on your PC without RAWeb, try "),t("a",{href:"https://github.com/kimmknight/remoteapptool",target:"_blank",rel:"noopener noreferrer"},"RemoteApp Tool"),t("sup",{class:"footnote-ref"},[t("a",{href:"docs/#fn1",id:"fnref1"},"[1]")]),n(".")],-1)),e[8]||(e[8]=t("picture",null,[t("source",{media:"(prefers-color-scheme: dark)",srcset:js}),t("source",{media:"(prefers-color-scheme: light)",srcset:_s}),t("img",{src:pe,alt:"A screenshot of the favorites page in RAWeb",height:"532",xmlns:"http://www.w3.org/1999/xhtml"})],-1)),e[9]||(e[9]=A('<h2>Features</h2><ul><li>A web interface for viewing and managing your RemoteApp and Desktop RDP connections <ul><li>Search the list of apps and devices</li><li>Favorite your most-used apps and devices for easy access</li><li>Sort apps and desktops by name, date modifed, and terminal server</li><li>Stale-while-revalidate caching for fast load times</li><li>Progressive web app with <a href="https://github.com/WICG/window-controls-overlay/blob/main/explainer.md" target="_blank" rel="noopener noreferrer">window controls overlay</a> support</li><li>Download RDP files for your apps and devices, or directly launch them in Windows App or mstsc.exe<sup class="footnote-ref"><a href="docs/#fn2" id="fnref2">[2]</a></sup></li><li>Add, edit, and remove RemoteApps and desktops directly from the web interface.</li><li>Follows the style and layout of Fluent 2 (WinUI 3)</li></ul></li><li>Fully-compliant Workspace (webfeed) feature to place your RemoteApps and desktop connections in: <ul><li>The Start Menu of Windows clients</li><li>The Android/iOS/iPadOS/MacOS Windows app</li></ul></li><li>File type associations on webfeed clients</li><li>Different RemoteApps for different users and groups</li><li>A setup script for easy installation</li></ul><h2>Get started &amp; installation</h2>',3)),t("p",null,[e[1]||(e[1]=n("Refer to out ")),o(i,{to:"/docs/get-started"},{default:s(()=>e[0]||(e[0]=[n("get started guide")])),_:1}),e[2]||(e[2]=n(" for the easiest way to start using RAWeb."))]),t("p",null,[e[4]||(e[4]=n("Refer to our ")),o(i,{to:"/docs/installation#installation"},{default:s(()=>e[3]||(e[3]=[n("installation documentation")])),_:1}),e[5]||(e[5]=n(" for detailed instructions on installing RAWeb, including different installation methods such as non-interactive installation and manual installation in IIS."))]),e[10]||(e[10]=t("h2",null,"Screenshots",-1)),e[11]||(e[11]=t("p",null,"A web interface for your RemoteApps and desktops:",-1)),e[12]||(e[12]=t("picture",null,[t("source",{media:"(prefers-color-scheme: dark)",srcset:qs}),t("source",{media:"(prefers-color-scheme: light)",srcset:Js}),t("img",{src:me,alt:"A screenshot of the apps page in RAWeb",height:"532",xmlns:"http://www.w3.org/1999/xhtml"})],-1)),e[13]||(e[13]=t("picture",null,[t("source",{media:"(prefers-color-scheme: dark)",srcset:zs}),t("source",{media:"(prefers-color-scheme: light)",srcset:Ks}),t("img",{src:fe,alt:"A screenshot of the devices page in RAWeb",height:"532",xmlns:"http://www.w3.org/1999/xhtml"})],-1)),e[14]||(e[14]=t("picture",null,[t("source",{media:"(prefers-color-scheme: dark)",srcset:Xs}),t("source",{media:"(prefers-color-scheme: light)",srcset:$s}),t("img",{src:Ae,alt:"A screenshot of the settings page in RAWeb",height:"532",xmlns:"http://www.w3.org/1999/xhtml"})],-1)),e[15]||(e[15]=t("picture",null,[t("source",{media:"(prefers-color-scheme: dark)",srcset:Qs}),t("source",{media:"(prefers-color-scheme: light)",srcset:Zs}),t("img",{src:ge,alt:"A screenshot of the termninal server picker dialog in RAWeb, which appears when selecting an app that exists on multiple hosts",height:"532",xmlns:"http://www.w3.org/1999/xhtml"})],-1)),e[16]||(e[16]=t("picture",null,[t("source",{media:"(prefers-color-scheme: dark)",srcset:ea}),t("source",{media:"(prefers-color-scheme: light)",srcset:ta}),t("img",{src:Ie,alt:"A screenshot of the properties dialog in RAWeb, which shows the contents of the RDP file",height:"532",xmlns:"http://www.w3.org/1999/xhtml"})],-1)),e[17]||(e[17]=A('<p>Webfeed puts RemoteApps in Windows client Start Menu:</p><p><img src="https://github.com/kimmknight/raweb/wiki/images/screenshots/windows-webfeed-sm.png" alt=""></p><p>Android RD Client app subscribed to the webfeed/workspace:</p><p><img src="https://github.com/kimmknight/raweb/wiki/images/screenshots/android-workspace-sm.jpg" alt=""></p><h2>Translations</h2><p>Please follow the instructions at <a href="https://github.com/kimmknight/raweb/blob/master/TRANSLATING.md" target="_blank" rel="noopener noreferrer">TRANSLATING.md</a> to add or update translations.</p><hr class="footnotes-sep"><section class="footnotes"><ol class="footnotes-list"><li id="fn1" class="footnote-item"><p>If RemoteApp Tool is on the same device as RAWeb, enable TSWebAccess for each app that should appear in RAWeb. If on a different device, export RDP files and icons and follow <a href="https://raweb.app/docs/publish-resources/" target="_blank" rel="noopener noreferrer">the instructions</a> to add them to RAWeb. <a href="docs/#fnref1" class="footnote-backref">↩︎</a></p></li><li id="fn2" class="footnote-item"><p>Directly launching apps and devices requires additional software. <br> On <strong>Windows</strong>, install the <a href="https://apps.microsoft.com/detail/9N1192WSCHV9?hl=en-us&amp;gl=US&amp;ocid=pdpshare" target="_blank" rel="noopener noreferrer">Remote Desktop Protocol Handler</a> app from the Microsoft Store or install it with WinGet (<code>winget install &quot;RDP Protocol Handler&quot; --source msstore</code>). <br> On <strong>macOS</strong>, install <a href="https://apps.apple.com/us/app/windows-app/id1295203466" target="_blank" rel="noopener noreferrer">Windows App</a> from the Mac App Store. <br> On <strong>iOS</strong> or <strong>iPadOS</strong>, install <a href="https://apps.apple.com/us/app/windows-app-mobile/id714464092" target="_blank" rel="noopener noreferrer">Windows App Mobile</a> from the App Store. <br> Not supported on <strong>Android</strong>. <a href="docs/#fnref2" class="footnote-backref">↩︎</a></p></li></ol></section>',8))])}}},ra=Object.freeze(Object.defineProperty({__proto__:null,default:ia,title:oa},Symbol.toStringTag,{value:"Module"})),sa={id:"appContent"},aa={class:"search-box-container"},la={key:0,class:"search-box-results"},da={key:1,class:"search-box-result center"},ca={key:2,class:"search-box-result center"},ha={key:3,class:"search-box-result center"},ua={key:5,style:{height:"3px"}},pa={id:"page","data-pagefind-body":""},ma=Te({__name:"Documentation",setup(l){const r=F(!1);async function u(f){if(f.data.type==="fetch-queue"){const m=f.data.backgroundFetchQueueLength>0;r.value=m}}const d=F(!1);Z(()=>{qe(u).then(f=>{f==="SSL_ERROR"&&(d.value=!0)})});const e=F(!1);be.then(()=>{e.value=!0});const{t:i}=ye(),h=Je(()=>e.value),y=F(!1);K(()=>{h.value&&setTimeout(()=>{ze().then(()=>{y.value=!0})},300)});function S(f){const m=[];function I(E,w,v){const[Y,...Q]=E;if(!Y)return;let V=w.find(Ge=>Ge.label===Y);V||(V={label:Y,children:[]},w.push(V)),Q.length===0?Object.assign(V,v,{label:Y,children:V.children}):I(Q,V.children,v)}for(const E of f){if(!E.name)continue;const w=String(E.name).replace(/\/+$/,"").split("/").filter(Boolean);I(w,m,E)}return m}function O(f,m){if(!m)return;f.preventDefault();const I=new URL(m,window.location.origin),E=new URL(window.location.href);if(I.pathname===E.pathname){const w=document.querySelector("#app main > #page");Oe(w).then(async()=>{document.location.hash=I.hash,document.querySelector("#app main")?.scrollTo(0,0),await Ne(w)})}else W.push(m)}function C(f){return f.map(m=>({name:b[m.label]?.label||m.label,type:"category",children:m.children.map(I=>{const E=[{name:String(I.meta?.nav_title||I.meta?.title||I.name),href:I.path,onClick:w=>O(w,I.path)},...I.children.map(w=>({name:String(w.meta?.nav_title||w.meta?.title||w.name),href:w.path,onClick:v=>O(v,w.path)})).filter(se)].filter(w=>w.name!=="undefined");return E.length===1?{name:E[0].name,icon:b[I.label]?.icon,href:E[0].href,onClick:E[0].onClick}:{name:b[I.label]?.label||I.label,icon:b[I.label]?.icon,type:"expander",children:E}}).sort((I,E)=>I.name.localeCompare(E.name))})).sort((m,I)=>m.name.localeCompare(I.name)).sort(m=>m.name==="User Guide"?-1:1)}const W=Re(),B=W.getRoutes(),P=B.find(f=>f.name==="index"),J=B.filter(f=>f.name?.toString().startsWith("(welcome)/")),$=[P,...J].filter(se),T=B.filter(f=>!$.some(m=>m.name===f.name)),R=S(T)||[],b={"(user-guide)":{label:"User Guide"},"(administration)":{label:"Administration"},"(development)":{label:"Development"},workspaces:{label:"Workspaces",icon:ae},"publish-resources":{label:"Publishing Resources",icon:mt},policies:{label:"Policies",icon:pt},security:{label:"Security",icon:ut},"reverse-proxy":{label:"Reverse proxies",icon:ht},deployment:{label:"Deployment",icon:ae},uninstall:{label:"Uninstall RAWeb",icon:ct},installation:{label:"Install RAWeb",icon:dt},"get-started":{label:"Get started",icon:lt},"supported-environments":{label:"Supported Environments",icon:at},"custom-content":{label:"Custom Content",icon:st},"web-client":{label:"Web client",icon:`<svg viewBox="0 0 192 192" xmlns="http://www.w3.org/2000/svg" fill="none" style="fill: none !important;">
                <path
                  stroke="currentColor"
                  stroke-linecap="round"
                  stroke-linejoin="round"
                  stroke-width="12"
                  d="M96 170c40.869 0 74-33.131 74-74 0-40.87-33.131-74-74-74-40.87 0-74 33.13-74 74 0 40.869 33.13 74 74 74Z"
                />
                <path
                  stroke="currentColor"
                  stroke-linecap="round"
                  stroke-linejoin="round"
                  stroke-width="12"
                  d="M126 52 98 80l28 28M66 84l28 28-28 28"
                />
              </svg>`}},N=C(S(J)),U=C(R),oe=[{name:"Home",href:P?.path,icon:Ke},...N[0]?.children||[],{name:"hr"},...U,{name:"footer",type:"navigation",children:[{name:"hr"},{name:"View on GitHub",href:"https://github.com/kimmknight/raweb",icon:'<svg width="98" height="96" viewBox="0 0 96 96" xmlns="http://www.w3.org/2000/svg"><path fill-rule="evenodd" clip-rule="evenodd" d="M48.854 0C21.839 0 0 22 0 49.217c0 21.756 13.993 40.172 33.405 46.69 2.427.49 3.316-1.059 3.316-2.362 0-1.141-.08-5.052-.08-9.127-13.59 2.934-16.42-5.867-16.42-5.867-2.184-5.704-5.42-7.17-5.42-7.17-4.448-3.015.324-3.015.324-3.015 4.934.326 7.523 5.052 7.523 5.052 4.367 7.496 11.404 5.378 14.235 4.074.404-3.178 1.699-5.378 3.074-6.6-10.839-1.141-22.243-5.378-22.243-24.283 0-5.378 1.94-9.778 5.014-13.2-.485-1.222-2.184-6.275.486-13.038 0 0 4.125-1.304 13.426 5.052a46.97 46.97 0 0 1 12.214-1.63c4.125 0 8.33.571 12.213 1.63 9.302-6.356 13.427-5.052 13.427-5.052 2.67 6.763.97 11.816.485 13.038 3.155 3.422 5.015 7.822 5.015 13.2 0 18.905-11.404 23.06-22.324 24.283 1.78 1.548 3.316 4.481 3.316 9.126 0 6.6-.08 11.897-.08 13.526 0 1.304.89 2.853 3.316 2.364 19.412-6.52 33.405-24.935 33.405-46.691C97.707 22 75.788 0 48.854 0z" fill="currentColor"/></svg>'},{name:"Submit a bug report",href:"https://github.com/kimmknight/raweb/issues",icon:Xe}]}],z=F(te?window.innerWidth:0);function M(){z.value=window.innerWidth}Z(()=>{window.addEventListener("resize",M)}),$e(()=>{window.removeEventListener("resize",M)}),Z(()=>{if(!window.location.hash)return;const m=document.querySelector(window.location.hash);m&&m instanceof HTMLElement&&document.querySelector("#app main")?.scrollTo(0,m.offsetTop-32)}),Qe(()=>y.value,()=>{const f=window.location.hash;f&&requestAnimationFrame(()=>{document.querySelectorAll(f).forEach(I=>{I.classList.add("router-target")})})});const D=F(""),H=F([]),x=F(!1);K(f=>{if(D.value,!te||!window.pagefind)return;const m=D.value.trim();if(!m){H.value=[],x.value=!1;return}let I=!1;f(()=>{I=!0}),x.value=!0,window.pagefind.debouncedSearch(m).then(async E=>{if(I)return;const w=await Promise.all((E?.results||[]).slice(0,5).map(v=>v.data()));H.value=w}).finally(()=>{I||(x.value=!1)})});const G=F(!1);function ie(){G.value=!0}function Fe(f){(!f.relatedTarget||!(f.relatedTarget instanceof HTMLElement)||!f.currentTarget||!(f.currentTarget instanceof HTMLElement)||!f.currentTarget.contains(f.relatedTarget))&&(G.value=!1)}function We(){const f=document.activeElement;if(f){const m=f.previousElementSibling;m?(f.setAttribute("tabindex","-1"),m.focus(),m.setAttribute("tabindex","0")):re()}}function He(){const f=document.activeElement;if(f){let m=f.nextElementSibling;for(;m&&!m.hasAttribute("tabindex");)m=m.nextElementSibling;m&&(f.setAttribute("tabindex","-1"),m.focus(),m.setAttribute("tabindex","0"))}}function ke(){const f=document.querySelector(".search-box-result");f&&(f.focus(),f.setAttribute("tabindex","0"))}function re(){document.querySelector(".search-box-container input")?.focus()}function Ue(f){f.length>0&&(W.push("/docs/search?q="+encodeURIComponent(f)),G.value=!1,D.value="")}function Me(f){f(),setTimeout(()=>{G.value=!0,re()},120)}return(f,m)=>{const I=p("router-view");return a(),c(X,null,[o(g(Ze),{forceVisible:"",loading:r.value,hideProfileMenu:""},null,8,["loading"]),t("div",sa,[o(g(it),{"show-back-arrow":!1,variant:z.value<800?"leftCompact":"left",stateId:"docs-nav","menu-items":oe},{custom:s(({collapsed:E,toggleCollapse:w})=>[E?(a(),_(g(ot),{key:1,onClick:v=>Me(w),class:"search-area-icon"},{icon:s(()=>m[4]||(m[4]=[t("svg",{width:"24",height:"24",fill:"none",viewBox:"0 0 24 24",xmlns:"http://www.w3.org/2000/svg"},[t("path",{d:"M10 2.75a7.25 7.25 0 0 1 5.63 11.819l4.9 4.9a.75.75 0 0 1-.976 1.134l-.084-.073-4.901-4.9A7.25 7.25 0 1 1 10 2.75Zm0 1.5a5.75 5.75 0 1 0 0 11.5 5.75 5.75 0 0 0 0-11.5Z",fill:"currentColor"})],-1)])),_:2},1032,["onClick"])):(a(),c("div",{key:0,class:"search-area",onFocusin:ie,onFocusout:Fe},[t("div",aa,[o(g(et),{value:D.value,"onUpdate:value":m[0]||(m[0]=v=>D.value=v),placeholder:g(i)("docs.search.placeholder"),onKeydown:m[1]||(m[1]=ee(v=>ke(),["down"])),onSubmit:Ue,showSubmitButton:""},null,8,["value","placeholder"])]),G.value?(a(),c("div",la,[H.value.length>0?(a(!0),c(X,{key:0},Ee(H.value,(v,Y)=>(a(),_(g(tt),{class:"search-box-result",href:v.raw_url,key:v.raw_url,onClick:we(Q=>{g(W).push(v.raw_url||"/docs/"),D.value="",G.value=!1},["prevent"]),tabindex:Y===0?"0":"-1",onKeydown:[m[2]||(m[2]=ee(()=>We(),["up"])),m[3]||(m[3]=ee(()=>He(),["down"]))]},{default:s(()=>[n(k(v.meta.nav_title||v.meta.title),1)]),_:2},1032,["href","onClick","tabindex"]))),128)):q("",!0),x.value&&H.value.length===0?(a(),c("div",da,[o(g(Se),{size:16}),n(" "+k(g(i)("docs.search.searching")),1)])):H.value.length===0&&!D.value?(a(),c("div",ca,k(g(i)("docs.search.typeToSearch")),1)):H.value.length===0?(a(),c("div",ha,k(g(i)("docs.search.noResults")),1)):q("",!0),x.value?(a(),_(g(nt),{key:4})):(a(),c("div",ua))])):q("",!0)],32))]),_:1},8,["variant"]),t("main",null,[t("div",pa,[o(I,null,{default:s(({Component:E})=>[o(g(j),{variant:"title",tag:"h1",class:"page-title","data-pagefind-meta":"title"},{default:s(()=>[n(k(g(W).currentRoute.value.meta.title),1)]),_:1}),(a(),_(rt(E)))]),_:1})])])])],64)}}}),fa=ve(ma,[["__scopeId","data-v-dca4f983"]]),Aa={key:0,class:"please-wait"},ga=["href","onClick"],Ia={class:"result"},Ta=Te({__name:"DocumentationSearchResults",setup(l){const{t:r}=ye(),u=Re();function d(y){return y.replace(/[.*+?^${}()|[\]\\]/g,"\\$&")}function e(y,S){if(!S)return y;const O=new RegExp(`(${d(S)})`,"gi");return y.replace(O,"<mark>$1</mark>")}const i=F([]),h=F(!1);return K(y=>{if(!te||!window.pagefind||!u.currentRoute.value.query.q||typeof u.currentRoute.value.query.q!="string")return;const S=u.currentRoute.value.query.q.trim();if(!S){i.value=[],h.value=!1;return}let O=!1;y(()=>{O=!0}),h.value=!0,window.pagefind.debouncedSearch(S).then(async C=>{if(O)return;const B=(await Promise.all((C?.results||[]).slice(0,10).map(P=>P.data()))).map(P=>(P.excerpt=ft.sanitize(P.excerpt.replaceAll("&gt;",">").replaceAll("&lt;","<").replaceAll("<mark>","").replaceAll("</mark>",""),{ALLOWED_TAGS:["mark"]}),P.excerpt=e(P.excerpt,u.currentRoute.value.query.q),P));i.value=B}).finally(()=>{O||(h.value=!1)})}),K(()=>{i.value.length>0&&At(()=>{const y=document.querySelector("a.result-link");y&&y.focus()})}),(y,S)=>(a(),c(X,null,[h.value?(a(),c("div",Aa,[o(g(Se)),o(g(j),{variant:"bodyStrong"},{default:s(()=>[n(k(g(r)("pleaseWait")),1)]),_:1})])):q("",!0),h.value?q("",!0):(a(),_(g(j),{key:1,variant:"title",tag:"h1",class:"page-title",block:""},{default:s(()=>[n(k(g(r)("docs.search.title",{query:g(u).currentRoute.value.query.q})),1)]),_:1})),(a(!0),c(X,null,Ee(i.value,O=>(a(),c("a",{href:O.raw_url,onClick:we(C=>g(u).push(O.raw_url||"/docs/"),["prevent"]),class:"result-link"},[t("article",Ia,[o(g(j),{tag:"h1",variant:"subtitle",block:""},{default:s(()=>[n(k(O.meta.title||O.meta.nav_title),1)]),_:2},1024),o(g(j),{innerHTML:O.excerpt},null,8,["innerHTML"])])],8,ga))),256)),!h.value&&i.value.length===0?(a(),_(g(j),{key:2,variant:"body"},{default:s(()=>[n(k(g(r)("docs.search.noResults")),1)]),_:1})):q("",!0)],64))}}),ba=ve(Ta,[["__scopeId","data-v-bdbda936"]]);async function ya({ssr:l=!1,initialPath:r}={}){const d=gt((l===!0?It:Tt)(fa)),e=await be,i=Object.assign({"../docs/(administration)/deployment/index.md":Dt,"../docs/(administration)/installation/index.md":Ht,"../docs/(administration)/policies/alert-banners/index.md":xt,"../docs/(administration)/policies/block-workspace-auth/index.md":zt,"../docs/(administration)/policies/centralized-publishing/index.md":en,"../docs/(administration)/policies/change-password/index.md":dn,"../docs/(administration)/policies/combine-alike-apps/index.md":fn,"../docs/(administration)/policies/configure-aliases/index.md":Nn,"../docs/(administration)/policies/connection-method-rdpFile/index.md":Wn,"../docs/(administration)/policies/connection-method-rdpProtocolUrl/index.md":xn,"../docs/(administration)/policies/connection-window/index.md":Jn,"../docs/(administration)/policies/duo-mfa/index.md":no,"../docs/(administration)/policies/favorites/index.md":lo,"../docs/(administration)/policies/flatten-folders/index.md":fo,"../docs/(administration)/policies/fulladdress-override/index.md":yo,"../docs/(administration)/policies/hide-ports/index.md":No,"../docs/(administration)/policies/icon-backgrounds/index.md":Fo,"../docs/(administration)/policies/inject-rdp-properties/index.md":Go,"../docs/(administration)/policies/log-files/index.md":_o,"../docs/(administration)/policies/show-multiuser-names/index.md":$o,"../docs/(administration)/policies/simple-mode/index.md":oi,"../docs/(administration)/policies/user-cache/index.md":di,"../docs/(administration)/policies/web-client/index.md":Ai,"../docs/(administration)/publish-resources/file-type-associations/index.md":wi,"../docs/(administration)/publish-resources/index.md":ji,"../docs/(administration)/publish-resources/reconnection/index.md":Xi,"../docs/(administration)/publish-resources/resource-folder-permissions/index.md":cr,"../docs/(administration)/reverse-proxy/nginx/index.md":mr,"../docs/(administration)/security/authentication/index.md":yr,"../docs/(administration)/security/error-5003/index.md":Or,"../docs/(administration)/security/mfa/index.md":Dr,"../docs/(administration)/supported-environments/index.md":kr,"../docs/(administration)/supported-environments/web-client-support/index.md":jr,"../docs/(administration)/uninstall/index.md":zr,"../docs/(administration)/web-client/about/index.md":Zr,"../docs/(administration)/web-client/errors/index.md":is,"../docs/(administration)/web-client/prerequisites/index.md":cs,"../docs/(development)/custom-content/index.md":fs,"../docs/(user-guide)/web-client/index.md":Rs,"../docs/(user-guide)/workspaces/index.md":Ms,"../docs/(welcome)/get-started/index.md":Vs,"../docs/index.md":ra}),h=await Promise.all(Object.entries(i).map(async([T,{default:R,...b}])=>{let N=T.replace("../docs/","").replace("/index.md","/").toLowerCase();if(N==="index.md"&&(N="index"),b)for(const[M,D]of Object.entries(b)){if(!D||typeof D!="string"||!D.includes("$t{{"))continue;const H=/\$t\{\{\s*([^\}]+)\s*\}\}/g;b[M]=D.replaceAll(H,(x,G)=>e(G.trim(),{lng:"en-US"}))}const U={path:"/docs/"+(N==="index"?"":N.replace("(user-guide)/","").replace("(administration)/","").replace("(welcome)/","").replace("(development)/","")),name:N,meta:{...b},component:R||le},z=(Array.isArray(b.redirects)&&b.redirects.every(M=>typeof M=="string")?b.redirects:[]).map(M=>({path:`/docs/${M.replace(/^\//,"")}`,redirect:U.path}));return[U,...z]})).then(T=>T.flat()),y=l===!0?bt():yt(),S=Rt({history:y,routes:[{path:"/:pathMatch(.*)*",component:le,props:{variant:"docs"}},{path:"/:pathMatch(.*[^/])",redirect:T=>`/${T.params.pathMatch}/`},{path:"/docs/search/",component:ba},...h],strict:!0,scrollBehavior(T,R){if(l)return;const b=document.querySelector("#app main");if(b){if(C.restoreScrollRequested){const N=O.get(T.fullPath);if(N)return b.scrollTo(N.left,N.top),C.restoreScrollRequested=!1,!1}if(T.hash)return new Promise(N=>{requestAnimationFrame(()=>{const U=document.querySelector(T.hash);U&&U instanceof HTMLElement&&b.scrollTo(0,U.offsetTop-32),N(!1)})});b.scrollTo(0,0)}}}),O=new Map;l||S.beforeEach((T,R)=>{const b=document.querySelector("#app main");b&&R.fullPath&&O.set(R.fullPath,{left:b.scrollLeft,top:b.scrollTop})}),l||y.listen(()=>{C.restoreScrollRequested=!0});const C=Et({animating:!1,restoreScrollRequested:!1});l||(S.beforeEach(async(T,R)=>{if(!(T.path!==R.path))return;const N=document.querySelector("#app main > #page");C.animating=!0,await Oe(N)}),S.afterEach(async(T,R)=>{const b=document.querySelector("#app main > #page");await Ne(b),C.animating=!1})),l||(S.afterEach(T=>{T.hash&&requestAnimationFrame(()=>{document.querySelectorAll(T.hash).forEach(b=>{b.classList.add("router-target")})})}),S.beforeEach(()=>{document.querySelectorAll(".router-target").forEach(R=>{R.classList.remove("router-target")})})),l||S.afterEach(T=>{document.title=T.meta.title?`${T.meta.title} - RAWeb Wiki`:"RAWeb Wiki"});const W=wt();await de(W).fetchData();const B=de(W);l||await import(`${B.iisBase}lib/assets/pagefind/pagefind.js?buildId=66a2b578-f6de-406b-90d4-d8f505c1fd9a`).then(async R=>{window.pagefind=R.default??R,await window.pagefind?.init()}).catch(R=>{console.error("Failed to load pagefind bundle:",R)}),l||St(),d.use(W),d.use(S);const{CodeBlock:P,PolicyDetails:J,InfoBar:$}=await Ot(async()=>{const{CodeBlock:T,PolicyDetails:R,InfoBar:b}=await import("./shared-DEFpb1Q4.js").then(N=>N.aL);return{CodeBlock:T,PolicyDetails:R,InfoBar:b}},__vite__mapDeps([0,1]));return d.component("CodeBlock",P),d.component("PolicyDetails",J),d.component("InfoBar",$),d.provide("docsNavigationContext",C),d.directive("swap",(T,R)=>{T.parentNode&&(T.outerHTML=R.value)}),d.config.globalProperties.docsNavigationContext=C,r&&await S.replace(r),await S.isReady(),{app:d,router:S}}if(typeof window<"u"){const{app:l}=await ya();l.mount("#app")}
