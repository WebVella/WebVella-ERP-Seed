using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System.Globalization;
using Microsoft.Net.Http.Headers;
using Microsoft.AspNetCore.ResponseCompression;
using System.IO.Compression;
using WebVella.Erp.Web;
using Microsoft.AspNetCore.Http;
using WebVella.Erp.Web.Middleware;
using Microsoft.AspNetCore.Authentication.Cookies;
using WebVella.Erp.Plugins.SDK;

namespace Site
{
	public class Startup
	{
		public void ConfigureServices(IServiceCollection services)
		{
			services.Configure<GzipCompressionProviderOptions>(options => options.Level = CompressionLevel.Optimal);
			services.AddResponseCompression(options => { options.Providers.Add<GzipCompressionProvider>(); });
			services.AddRouting(options =>
			{
				options.LowercaseUrls = true;
			});

			services.AddDetectionCore().AddDevice();

			services.AddMvc()
				.AddRazorPagesOptions(options =>
				{
					options.Conventions.AuthorizeFolder("/");
					options.Conventions.AllowAnonymousToPage("/login");
				})
				.AddJsonOptions(options =>
				{
					options.SerializerSettings.Converters.Add(new ErpDateTimeJsonConverter());
				});


			services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
					.AddCookie(options =>
					{
						options.Cookie.HttpOnly = true;
						options.Cookie.Name = "erp_auth";
						options.LoginPath = new PathString("/login");
						options.LogoutPath = new PathString("/logout");
						options.AccessDeniedPath = new PathString("/error?access_denied");
						options.ReturnUrlParameter = "ret_url";
					});

			services.AddErp();
		}

		// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
		public void Configure(IApplicationBuilder app, IHostingEnvironment env)
		{
			app.UseRequestLocalization(new RequestLocalizationOptions
			{
				DefaultRequestCulture = new Microsoft.AspNetCore.Localization.RequestCulture(CultureInfo.GetCultureInfo("en-US"))
			});

			app.UseAuthentication();

			app
			.UseErpPlugin<SdkPlugin>()
			.UseErp()
			.UseErpMiddleware();

			// Add the following to the request pipeline only in development environment.
			if (string.Equals(env.EnvironmentName, "Development", StringComparison.OrdinalIgnoreCase))
			{
				app.UseDeveloperExceptionPage();
			}
			else
			{
				// Add Error handling middleware which catches all application specific errors and
				// send the request to the following path or controller action.
				app.UseErrorHandlingMiddleware();
				app.UseExceptionHandler("/error");
				app.UseStatusCodePagesWithReExecute("/error");
			}

			//Should be before Static files
			app.UseResponseCompression();

			app.UseStaticFiles(new StaticFileOptions
			{
				OnPrepareResponse = ctx =>
				{
					const int durationInSeconds = 60 * 60 * 24 * 30; //30 days caching of these resources
					ctx.Context.Response.Headers[HeaderNames.CacheControl] =
						"public,max-age=" + durationInSeconds;
				}
			});

			app.UseMvc(routes =>
			{
				routes.MapRoute(name: "default", template: "{controller=Home}/{action=Index}/{id?}");
			});
		}
	}
}
