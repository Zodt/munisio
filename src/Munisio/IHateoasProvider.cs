using Munisio.Models;

namespace Munisio
{
    public interface IHateoasProvider
    {
        void Enrich(IHateoasContext context, object model);
    }
    
    public interface IHateoasProvider<in TModel> : IHateoasProvider where TModel : IHateoasObject
    {
        void Enrich(IHateoasContext context, TModel model);
    }
}
