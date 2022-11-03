using Microsoft.AspNetCore.Mvc;
using MvcCoreSample.Models;
using OSGeo.MapGuide;

namespace MvcCoreSample.Controllers;

public abstract class MgBaseController : Controller
{
    protected MgSiteConnection CreateConnection(MapGuideCommandModel input)
    {
        MgSiteConnection conn = new MgSiteConnection();
        MgUserInformation user = new MgUserInformation(input.Session);
        conn.Open(user);
        return conn;
    }

    protected (MgSiteConnection conn, MgMap map) OpenMap(MapGuideCommandModel input)
    {
        var conn = CreateConnection(input);
        var map = new MgMap(conn);
        map.Open(input.MapName);
        return (conn, map);
    }

    protected ActionResult ByteReaderResult(MgByteReader rdr, string downloadName = null)
    {
        if (string.IsNullOrEmpty(downloadName))
            return new FileStreamResult(new MgReadOnlyStream(rdr), rdr.MimeType);
        else
            return File(new MgReadOnlyStream(rdr), rdr.MimeType, downloadName);
    }
}