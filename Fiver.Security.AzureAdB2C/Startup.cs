using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Fiver.Security.AzureAdB2C
{
    public class Startup
    {
        private readonly string TenantName = ""; // aka 'Initial Domain Name' in Azure
        private readonly string ClientId = ""; // aka 'Application Id' in Azure

        public void ConfigureServices(IServiceCollection services)
        {
            var signUpPolicy = "B2C_1_sign_up";
            var signInPolicy = "B2C_1_sign_in";
            var signUpInPolicy = "B2C_1_sign_up_in";
            var editProfilePolicy = "B2C_1_edit_profile";

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = signUpInPolicy;
            })
            .AddOpenIdConnect(signUpPolicy, GetOpenIdConnectOptions(signUpPolicy))
            .AddOpenIdConnect(signInPolicy, GetOpenIdConnectOptions(signInPolicy))
            .AddOpenIdConnect(signUpInPolicy, GetOpenIdConnectOptions(signUpInPolicy))
            .AddOpenIdConnect(editProfilePolicy, GetOpenIdConnectOptions(editProfilePolicy))
            .AddCookie();

            services.AddMvc();
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.UseDeveloperExceptionPage();

            app.UseAuthentication();

            app.UseMvcWithDefaultRoute();
        }

        #region " Private "

        private Action<OpenIdConnectOptions> GetOpenIdConnectOptions(string policy)
            => options =>
            {
                options.MetadataAddress = "https://login.microsoftonline.com/" + this.TenantName + "/v2.0/.well-known/openid-configuration?p=" + policy;
                options.ClientId = this.ClientId;
                options.ResponseType = OpenIdConnectResponseType.IdToken;
                options.CallbackPath = "/signin/" + policy;
                options.SignedOutCallbackPath = "/signout/" + policy;
                options.SignedOutRedirectUri = "/";
                options.TokenValidationParameters.NameClaimType = "name";

                options.Events.OnMessageReceived = context =>
                {
                    if (!string.IsNullOrEmpty(context.ProtocolMessage.Error) &&
                        !string.IsNullOrEmpty(context.ProtocolMessage.ErrorDescription) &&
                        context.ProtocolMessage.ErrorDescription.StartsWith("AADB2C90091"))
                    {
                        context.Response.Redirect("/");
                        context.HandleResponse();
                    }

                    return Task.FromResult(0);
                };
            };

        #endregion
    }
}
