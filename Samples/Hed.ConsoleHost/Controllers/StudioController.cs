using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using Raven.Client.Connection;

namespace Hed.ConsoleHost.Controllers
{
    [RoutePrefix("")]
    public class StudioController : ApiController
    {
        [HttpGet]
        [Route("studio")]
        [Route("studio/{*path}")]
        public HttpResponseMessage GetStudioFile(string path = "index.html")
        {
            return WriteEmbeddedFile(path);
        }

        public HttpResponseMessage WriteEmbeddedFile(string docPath)
        {
            var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../Hed.Studio", docPath);
            if (File.Exists(filePath))
                return WriteFile(filePath);

            filePath = Path.Combine("~/../../../../../Hed.Studio", docPath);
            if (File.Exists(filePath))
                return WriteFile(filePath);

            return null;
        }

        public HttpResponseMessage WriteFile(string filePath)
        {

            var msg = new HttpResponseMessage
            {
                Content = new CompressedStreamContent(new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite), false)
            };

            // WriteETag(fileEtag, msg);

            var type = GetContentType(filePath);
            msg.Content.Headers.ContentType = new MediaTypeHeaderValue(type);

            return msg;
        }

        private static string GetContentType(string docPath)
        {
            switch (Path.GetExtension(docPath))
            {
                case ".html":
                case ".htm":
                    return "text/html";
                case ".css":
                    return "text/css";
                case ".js":
                    return "text/javascript";
                case ".ico":
                    return "image/vnd.microsoft.icon";
                case ".jpg":
                    return "image/jpeg";
                case ".gif":
                    return "image/gif";
                case ".png":
                    return "image/png";
                case ".xap":
                    return "application/x-silverlight-2";
                default:
                    return "text/plain";
            }
        }
    }
}
