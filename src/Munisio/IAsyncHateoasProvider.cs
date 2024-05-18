using System.Threading.Tasks;
using Munisio.Models;

namespace Munisio
{
    public interface IAsyncHateoasProvider
    {
        ValueTask EnrichAsync(IHateoasContext context, object model);
    }
    
    public interface IAsyncHateoasProvider<in TModel> : IAsyncHateoasProvider where TModel : IHateoasObject
    {
        ValueTask EnrichAsync(IHateoasContext context, TModel model);
    }
}
