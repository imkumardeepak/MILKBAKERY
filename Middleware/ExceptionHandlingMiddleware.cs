using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using System.Net;
using Newtonsoft.Json;
using Milk_Bakery.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Milk_Bakery.Middleware
{
	public class ExceptionHandlingMiddleware
	{
		private readonly RequestDelegate _next;

		public ExceptionHandlingMiddleware(RequestDelegate next)
		{
			_next = next;
		}

		public async Task InvokeAsync(HttpContext context)
		{
			try
			{
				await _next(context);
			}
			catch (Exception ex)
			{
				await HandleExceptionAsync(context, ex);
			}
		}

		private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
		{
			var error = new ErrorViewModel
			{
				RequestId = context.TraceIdentifier,
				ErrorMessage = "An unexpected error occurred.",
				ErrorDetails = "Please try again later or contact support if the problem persists.",
				RedirectUrl = "/Home/Index",
				RedirectText = "Go to Home"
			};

			// Handle specific exceptions
			switch (exception)
			{
				case DbUpdateException dbEx when dbEx.InnerException is SqlException sqlEx:
					if (sqlEx.Number == 547) // Foreign key constraint violation
					{
						error.ErrorMessage = "Cannot delete this record because it is being used by other records.";
						error.ErrorDetails = "Please remove or update all related records before deleting this item.";
						error.RedirectUrl = GetRefererPath(context) ?? "/Home/Index";
						error.RedirectText = "Go Back";
					}
					else
					{
						error.ErrorMessage = "Database error occurred.";
						error.ErrorDetails = "An error occurred while accessing the database.";
					}
					break;

				case SqlException sqlEx:
					if (sqlEx.Number == 547) // Foreign key constraint violation
					{
						error.ErrorMessage = "Cannot delete this record because it is being used by other records.";
						error.ErrorDetails = "Please remove or update all related records before deleting this item.";
						error.RedirectUrl = GetRefererPath(context) ?? "/Home/Index";
						error.RedirectText = "Go Back";
					}
					else
					{
						error.ErrorMessage = "Database error occurred.";
						error.ErrorDetails = "An error occurred while accessing the database.";
					}
					break;

				case UnauthorizedAccessException:
					error.ErrorMessage = "Access Denied";
					error.ErrorDetails = "You do not have permission to access this resource.";
					error.RedirectUrl = "/Home/Index";
					error.RedirectText = "Go to Home";
					break;

				case InvalidOperationException:
					error.ErrorMessage = "Invalid Operation";
					error.ErrorDetails = "An invalid operation was attempted.";
					error.RedirectUrl = GetRefererPath(context) ?? "/Home/Index";
					error.RedirectText = "Go Back";
					break;

				default:
					// Log the exception for debugging purposes
					// _logger.LogError(exception, "An unhandled exception occurred: {Message}", exception.Message);
					break;
			}

			// Set the response status code
			context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

			// Check if the request is AJAX
			if (IsAjaxRequest(context.Request))
			{
				// Return JSON for AJAX requests
				var jsonResponse = new
				{
					success = false,
					error = error.ErrorMessage,
					message = error.ErrorDetails
				};

				context.Response.ContentType = "application/json";
				await context.Response.WriteAsync(JsonConvert.SerializeObject(jsonResponse));
			}
			else
			{
				// Render error view for regular requests
				var viewResult = new ViewResult
				{
					ViewName = "~/Views/Shared/Error.cshtml",
					ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary())
					{
						Model = error
					}
				};

				// Create an ActionContext to render the view
				var actionContext = new ActionContext(
					context,
					context.GetRouteData() ?? new RouteData(),
					new ActionDescriptor()
				);

				// Render the view
				await RenderViewAsync(context, viewResult, actionContext);
			}
		}

		private static async Task RenderViewAsync(HttpContext context, ViewResult viewResult, ActionContext actionContext)
		{
			var viewEngine = context.RequestServices.GetService(typeof(ICompositeViewEngine)) as ICompositeViewEngine;
			var view = viewEngine.FindView(actionContext, viewResult.ViewName, false).View;

			if (view != null)
			{
				await using var writer = new StreamWriter(context.Response.Body);
				var viewContext = new ViewContext(
					actionContext,
					view,
					viewResult.ViewData,
					new TempDataDictionary(context, context.RequestServices.GetService(typeof(ITempDataProvider)) as ITempDataProvider),
					writer,
					new HtmlHelperOptions()
				);

				context.Response.ContentType = "text/html";
				await view.RenderAsync(viewContext);
			}
			else
			{
				// Fallback to simple error message if view cannot be rendered
				await context.Response.WriteAsync($"<h1>Error</h1><p>{viewResult.ViewData.Model}</p>");
			}
		}

		private static bool IsAjaxRequest(HttpRequest request)
		{
			return request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
				   request.Headers.ContentType.ToString().Contains("application/json");
		}

		private static string GetRefererPath(HttpContext context)
		{
			var referer = context.Request.Headers["Referer"].ToString();
			if (!string.IsNullOrEmpty(referer))
			{
				try
				{
					var uri = new Uri(referer);
					return uri.PathAndQuery;
				}
				catch
				{
					// Ignore invalid URI
				}
			}
			return null;
		}
	}
}