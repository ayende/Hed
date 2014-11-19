using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web.Http;
using Raven.Client.Connection;

namespace Hed.ConsoleHost.Controllers
{
    public class TopologyController : ApiController
    {
        [HttpGet]
        [Route("topology/view")]
        public object Get()
        {

            return HedConfiguration.Instance.Topology;
        }

        [HttpGet]
        [Route("topology/set")]
        public object Set(string from, string to, string behavior)
        {
            if (string.IsNullOrEmpty(from)) throw new ArgumentNullException("from");
            if (string.IsNullOrEmpty(to)) throw new ArgumentNullException("to");
            ProxyBehavior parsedBehavior = string.IsNullOrEmpty(behavior)
                ? ProxyBehavior.Optimal
                : Enum.TryParse(behavior, out parsedBehavior) ? parsedBehavior : ProxyBehavior.Optimal;
            HedConfiguration.Instance.Set(from, to, parsedBehavior);
            HedConfiguration.Instance.Flush();

            return Redirect(new Uri("/topology/view", UriKind.Relative));
        }

        [HttpGet]
        [Route("topology/del")]
        public object Del(string from, string to)
        {
            HedConfiguration.Instance.Delete(from, to);
            HedConfiguration.Instance.Flush();
            return Redirect(new Uri("/topology/view", UriKind.Relative));
        }

        [HttpGet]
        [Route("studio")]
        [Route("studio/{*path}")]
        public HttpResponseMessage GetStudioFile(string path = null)
        {
            return WriteEmbeddedFile(path);
        }

        public HttpResponseMessage WriteEmbeddedFile(string docPath)
        {
            /*var filePath = Path.Combine(ravenPath, docPath);
            if (File.Exists(filePath))
                return WriteFile(filePath);*/
            var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../Hed.Studio", docPath);
            if (File.Exists(filePath))
                return WriteFile(filePath);

            filePath = Path.Combine("~/../../../../../Hed.Studio", docPath);
            if (File.Exists(filePath))
                return WriteFile(filePath);


            /*if (string.IsNullOrEmpty(zipPath) == false)
            {
                var fullZipPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, zipPath + ".zip");
                if (File.Exists(fullZipPath) == false)
                    fullZipPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bin", zipPath + ".zip");

                if (File.Exists(fullZipPath))
                {
                    return WriteFileFromZip(fullZipPath, docPath);
                }
            }*/

            return null;
        }

        public HttpResponseMessage WriteFile(string filePath)
        {
            /*var etagValue = GetHeader("If-None-Match") ?? GetHeader("If-Match");
            if (etagValue != null)
            {
                // Bug fix: the etag header starts and ends with quotes, resulting in cache-busting; the Studio always receives new files, even if should be cached.
                etagValue = etagValue.Trim(new[] { '\"' });
            }

            var fileEtag = File.GetLastWriteTimeUtc(filePath).ToString("G");
            if (etagValue == fileEtag)
                return GetEmptyMessage(HttpStatusCode.NotModified);*/

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