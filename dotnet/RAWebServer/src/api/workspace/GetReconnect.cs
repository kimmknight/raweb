using System.Net;
using System.Net.Http;
using System.Text;
using System.Web.Http;

namespace RAWebServer.Api {
  public partial class WorkspaceController : ApiController {

    [HttpGet]
    [HttpPost]
    [Route("reconnect")]
    [Route("~/RDWebService.asmx")]
    [RequireAuthentication(RedirectUrl = "~/api/auth/authenticate-workspace")]
    public IHttpActionResult GetReconnect() {
      var response = new HttpResponseMessage(HttpStatusCode.OK) {
        Content = new StringContent(workspaceReconnectStubXml, Encoding.UTF8, "text/xml")
      };

      return ResponseMessage(response);
    }

    /// <summary>
    /// A stub response for MS-RDWR. RAWeb cannot track which RDP files are in use
    /// across all terminal servers, so this just provides a valid empty response.
    /// 
    /// NOTE: there MUST NOT be any whitespace before the XML or MS-RDWR will reject it.
    /// </summary>
    public readonly string workspaceReconnectStubXml = @"<?xml version=""1.0"" encoding=""utf-8""?>
      <soap:Envelope xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"">
        <soap:Body>
          <GetRDPFilesResponse xmlns=""http://schemas.microsoft.com/ts/2010/09/rdweb"">
            <GetRDPFilesResult>
              <version>8.0</version>
              <wkspRC></wkspRC>
            </GetRDPFilesResult>
          </GetRDPFilesResponse>
        </soap:Body>
      </soap:Envelope>";
  }

}
