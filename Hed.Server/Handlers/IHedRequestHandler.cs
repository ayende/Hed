using System.Threading.Tasks;

namespace Switchboard.Server
{
    public interface IHedRequestHandler
    {
        Task<SwitchboardResponse> GetResponseAsync(SwitchboardContext context, HedRequest request);
    }
}
