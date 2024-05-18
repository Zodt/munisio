using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;

using Munisio.Models;

namespace Munisio
{
	/// <summary>
	/// This filter will execute all registered <see cref="IHateoasProvider{TModel}"/>
	/// and <see cref="IAsyncHateoasProvider{TModel}"/> classes to enrich the model with Hateoas links.
	/// </summary>
	/// <remarks>
	/// There are sync and async variants of the "ApplyHateoasOnX" methods because we support <see cref="IHateoasProvider{TModel}"/> and <see cref="IAsyncHateoasProvider{TModel}"/>
	/// and the async version can't executed from a sync context.
	/// </remarks>
	internal class HateoasActionFilter : IAsyncActionFilter
	{
		private readonly IAuthorizationService _authorizationService;
		private readonly LinkGenerator _linkGenerator;

		public HateoasActionFilter(IAuthorizationService authorizationService, LinkGenerator linkGenerator)
		{
			_authorizationService = authorizationService;
			_linkGenerator = linkGenerator;
		}

		public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
		{
			var executed = await next().ConfigureAwait(false);

			if (executed.Result is not ObjectResult objectResult)
			{
				return;
			}

			if (objectResult.Value is IHateoasCollection hateoasCollection && hateoasCollection.Items.Any())
			{
				await ApplyHateoasOnCollectionAsync(hateoasCollection.Items, context).ConfigureAwait(false);
			}

			if (objectResult.Value is IHateoasObject hateoasObject)
			{
				await ApplyHateoasOnObjectAsync(hateoasObject, context).ConfigureAwait(false);
			}
		}

		private void ApplyHateoasOnObject(IHateoasObject hateoasObject, ActionContext context)
		{
			var providerType = typeof(IHateoasProvider<>).MakeGenericType(hateoasObject.GetType());
			var provider = (IHateoasProvider?) context.HttpContext.RequestServices.GetService(providerType);

			if (provider is null)
			{
				return;
			}

			provider.Enrich(CreateHateoasContext(context), hateoasObject);
		}

		private async ValueTask ApplyHateoasOnObjectAsync(IHateoasObject hateoasObject, ActionContext context)
		{
			ApplyHateoasOnObject(hateoasObject, context);

			var providerType = typeof(IAsyncHateoasProvider<>).MakeGenericType(hateoasObject.GetType());
			var provider = (IAsyncHateoasProvider?) context.HttpContext.RequestServices.GetService(providerType);

			if (provider is null)
			{
				return;
			}

			await provider.EnrichAsync(CreateHateoasContext(context), hateoasObject).ConfigureAwait(false);
		}

		private void ApplyHateoasOnCollection(IEnumerable<IHateoasObject> collection, ActionContext context)
		{
			var immutableArray = collection.ToImmutableArray();
			
			var providerType = typeof(IHateoasProvider<>).MakeGenericType(immutableArray.First().GetType());
			var provider = (IHateoasProvider?) context.HttpContext.RequestServices.GetService(providerType);

			if (provider is null)
			{
				return;
			}

			foreach (var item in immutableArray)
			{
				provider.Enrich(CreateHateoasContext(context), item);
			}
		}

		private async ValueTask ApplyHateoasOnCollectionAsync(IEnumerable<IHateoasObject> collection, ActionContext context)
		{
			var immutableArray = collection.ToImmutableArray();
			ApplyHateoasOnCollection(immutableArray, context);

			var providerType = typeof(IAsyncHateoasProvider<>).MakeGenericType(immutableArray.First().GetType());
			var provider = (IAsyncHateoasProvider?) context.HttpContext.RequestServices.GetService(providerType);

			if (provider is null)
			{
				return;
			}

			foreach (var item in immutableArray)
			{
				await provider.EnrichAsync(CreateHateoasContext(context), item).ConfigureAwait(false);
			}
		}

		private HateoasContext CreateHateoasContext(ActionContext context) =>
			new(context, _authorizationService, _linkGenerator);
	}
}