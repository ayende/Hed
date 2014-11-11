using System.Threading.Tasks;
using Hed.Server.Context;
using Hed.Server.Request;
using Hed.Server.Response;

namespace Hed.Server.Handlers
{
    public interface IHedRequestHandler
    {
        Task<HedResponse> GetResponseAsync(HedContext context, HedRequest request);
    }
}
