using System;
using System.IO;
using System.Text;
using OSGeo.MapGuide;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Linq;

namespace MvcCoreSample.Controllers;

// This is a port of https://github.com/jumpinjackie/mapagent-dotnet-sample to MVC Core 2.1

public class MapAgentController : Controller
{
    public IActionResult Agent()
    {
        //mapagent only accepts GET or POST, so reject unsupported methods
        bool isGet = this.Request.Method == "GET";
        bool isPost = this.Request.Method == "POST";
        if (!isGet && !isPost)
        {
            return BadRequest("Unsupported method: " + this.Request.Method);
        }

        //We need the current request url as the mapagent may need to reference this URL for certain operations
        //(for example: GetMapKml/GetLayerKml/GetFeaturesKml)
        String uri = string.Empty;
        try
        {
            //This is the workhorse behind the mapagent handler, the previously mysterious MgHttpRequest class
            MgHttpRequest request = new MgHttpRequest(uri);

            //MgHttpRequestParam is the set of key/value parameter pairs that you need to set up for the
            //MgHttpRequest instance. We extract the relevant parameters from the HttpContext and pass it
            //down
            MgHttpRequestParam param = request.GetRequestParam();

            //Extract any parameters from the http authentication header if there is one
            bool bGotAuth = ParseAuthenticationHeader(param, Request);

            if (isGet)
            {
                PopulateGetRequest(param, Request);
            }
            else if (isPost)
            {
                PopulatePostRequest(param, Request);
            }

            //A request is valid if it contains any of the following:
            //
            // 1. A SESSION parameter
            // 2. A USERNAME parameter (PASSWORD optional). If not specified the http authentication header is checked and extracted if found
            //
            //Whether these values are valid will be determined by MgSiteConnection in the MgHttpRequest handler when we come to execute it
            bool bValid = param.ContainsParameter("SESSION");
            if (!bValid)
                bValid = param.ContainsParameter("USERNAME");

            if (!bValid)
            {
                return MgUnauthorized();
            }

            return SendRequest(request);
        }
        catch (MgException ex)
        {
            return HandleMgException(ex);
        }
    }

    private static bool ParseAuthenticationHeader(MgHttpRequestParam param, HttpRequest request)
    {
        //This method decodes and extracts the username and password from the http authentication
        //header (if it exists) and packs the values into the MgHttpRequestParam object if they
        //exist
        String auth = request.Headers["authorization"];
        if (auth != null && auth.Length > 6)
        {
            auth = auth.Substring(6);
            byte[] decoded = Convert.FromBase64String(auth);
            String decodedStr = Encoding.UTF8.GetString(decoded);
            String[] decodedTokens = decodedStr.Split(':');
            if (decodedTokens.Length == 1 || decodedTokens.Length == 2)
            {
                String username = decodedTokens[0];
                String password = "";
                if (decodedTokens.Length == 2)
                    password = decodedTokens[1];

                param.AddParameter("USERNAME", username);
                param.AddParameter("PASSWORD", password);
                return true;
            }
        }
        return false;
    }

    IActionResult HandleMgHttpError(MgHttpResult result)
    {
        String statusMessage = result.GetHttpStatusMessage();
        //These are 401-class errors
        if (statusMessage.Equals("MgAuthenticationFailedException") || statusMessage.Equals("MgUnauthorizedAccessException"))
        {
            Response.Headers.Add("WWW-Authenticate", "Basic realm=\"mapguide\"");
            return Unauthorized();
        }
        else
        {
            String errHtml = String.Format(
                "\r\n" +
                "<html>\n<head>\n" +
                "<title>{0}</title>\n" +
                "<meta http-equiv=\"Content-Type\" content=\"text/html; charset=utf-8\">\n" +
                "</head>\n" +
                "<body>\n<h2>{1}</h2>\n{2}\n</body>\n</html>\n",
                statusMessage,
                result.GetErrorMessage(),
                result.GetDetailedErrorMessage());

            return Content(errHtml, "text/html");
        }
    }

    IActionResult SendRequest(MgHttpRequest request)
    {
        MgHttpRequestParam param = request.GetRequestParam();
        //This next line does all the grunt work. It's why you never have to actually set up a 
        //MgSiteConnection/MgResourceService/MgFeatureService and do all this stuff yourself
        MgHttpResponse response = request.Execute();

        MgHttpResult result = response.GetResult();
        Response.StatusCode = result.GetStatusCode();
        if (Response.StatusCode == 200)
        {
            //MgDisposable is MapGuide's "object" class, so we need to do type
            //testing to find out the underlying derived type. The list of expected
            //types is small, so there isn't too much of this checking to do
            MgDisposable resultObj = result.GetResultObject();
            if (resultObj != null)
            {
                Response.ContentType = result.GetResultContentType();

                //Most of the applicable types have logic to return their content as XML in the form of a MgByteReader
                MgByteReader outputReader = null;
                if (resultObj is MgByteReader)
                {
                    outputReader = (MgByteReader)resultObj;
                    return OutputReaderContent(outputReader);
                }
                else if (resultObj is MgFeatureReader)
                {
                    //NOTE: This code path is not actually reached in MGOS 2.4 and if this code path ever
                    //does get reached, calling ToXml() on it is a potentially memory expensive operation.
                    //
                    //But we're doing this because this is how the official mapagent handler does it
                    //
                    //RFC 130 (http://trac.osgeo.org/mapguide/wiki/MapGuideRfc130) will hopefully address this problem
                    outputReader = ((MgFeatureReader)resultObj).ToXml();
                    return OutputReaderContent(outputReader);
                }
                else if (resultObj is MgSqlDataReader)
                {
                    //NOTE: This code path is not actually reached in MGOS 2.4 and if this code path ever
                    //does get reached, calling ToXml() on it is a potentially memory expensive operation.
                    //
                    //But we're doing this because this is how the official mapagent handler does it
                    //
                    //RFC 130 (http://trac.osgeo.org/mapguide/wiki/MapGuideRfc130) will hopefully address this problem
                    outputReader = ((MgSqlDataReader)resultObj).ToXml();
                    return OutputReaderContent(outputReader);
                }
                else if (resultObj is MgDataReader)
                {
                    //NOTE: This code path is not actually reached in MGOS 2.4 and if this code path ever
                    //does get reached, calling ToXml() on it is a potentially memory expensive operation.
                    //
                    //But we're doing this because this is how the official mapagent handler does it
                    //
                    //RFC 130 (http://trac.osgeo.org/mapguide/wiki/MapGuideRfc130) will hopefully address this problem
                    outputReader = ((MgDataReader)resultObj).ToXml();
                    return OutputReaderContent(outputReader);
                }
                else if (resultObj is MgStringCollection)
                {
                    outputReader = ((MgStringCollection)resultObj).ToXml();
                    return OutputReaderContent(outputReader);
                }
                else if (resultObj is MgSpatialContextReader)
                {
                    outputReader = ((MgSpatialContextReader)resultObj).ToXml();
                    return OutputReaderContent(outputReader);
                }
                else if (resultObj is MgLongTransactionReader)
                {
                    outputReader = ((MgSpatialContextReader)resultObj).ToXml();
                    return OutputReaderContent(outputReader);
                }
                else if (resultObj is MgHttpPrimitiveValue)
                {
                    return Content(((MgHttpPrimitiveValue)resultObj).ToString());
                }
                else //Shouldn't get here
                {
                    return BadRequest("Not sure how to output: " + resultObj.ToString());
                }
            }
            else
            {
                //The operation may not return any content at all, so we do nothing
                return Ok();
            }
        }
        else
        {
            return HandleMgHttpError(result);
        }
    }

    IActionResult OutputReaderContent(MgByteReader outputReader)
    {
        using (MemoryStream memBuf = new MemoryStream())
        {
            byte[] byteBuffer = new byte[1024];
            int numBytes = outputReader.Read(byteBuffer, 1024);
            while (numBytes > 0)
            {
                memBuf.Write(byteBuffer, 0, numBytes);
                numBytes = outputReader.Read(byteBuffer, 1024);
            }
            byte[] content = memBuf.ToArray();
            return File(content, outputReader.MimeType);
        }
    }

    private static void PopulateGetRequest(MgHttpRequestParam param, HttpRequest request)
    {
        foreach (var key in request.Query.Keys)
        {
            var value = request.Query[key].FirstOrDefault();
            if (value != null)
                param.AddParameter(key, value);
        }
    }

    static void PopulatePostRequest(MgHttpRequestParam param, HttpRequest request)
    {
        foreach (var key in request.Form.Keys)
        {
            var value = request.Form[key].FirstOrDefault();
            if (value != null)
                param.AddParameter(key, value);
        }

        //TODO: Dunno how to get file names from any files in the form yet in MVC6

        /*
        //NOTE: To ensure package loading operations work, set the maxRequestLength property in web.config
        //as appropriate.
        foreach (var postedFile in request.Form.Files)
        {
            //We have to dump this file content to a temp location so that the mapagent handler
            //can create a file-based MgByteSource from it
            var tempPath = Path.GetTempFileName();
            postedFile.SaveAs(tempPath);
            param.AddParameter(postedFile., tempPath);
            //tempfile is a hint to the MgHttpRequest for it to create a MgByteSource from it
            param.SetParameterType(file, "tempfile");
        }
        */
    }

    IActionResult HandleMgException(MgException ex)
    {
        String msg = string.Format("{0}\n{1}", ex.GetExceptionMessage(), ex.GetStackTrace());
        if (ex.GetExceptionCode() == MgExceptionCodes.MgResourceNotFoundException || ex.GetExceptionCode() == MgExceptionCodes.MgResourceDataNotFoundException)
        {
            return NotFound();
        }
        else if (ex.GetExceptionCode() == MgExceptionCodes.MgAuthenticationFailedException || ex.GetExceptionCode() == MgExceptionCodes.MgUnauthorizedAccessException || ex.GetExceptionCode() == MgExceptionCodes.MgUserNotFoundException)
        {
            return MgUnauthorized();
        }
        throw new Exception(ex.ToString());
    }

    IActionResult MgUnauthorized()
    {
        Response.Headers.Add("WWW-Authenticate", "Basic realm=\"mapguide\"");
        return Unauthorized();
    }
}