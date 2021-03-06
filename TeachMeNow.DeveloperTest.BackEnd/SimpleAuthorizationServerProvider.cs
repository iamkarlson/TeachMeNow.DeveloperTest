using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

using Microsoft.Owin.Security;
using Microsoft.Owin.Security.OAuth;

using TeachMeNow.DeveloperTest.BackEnd.Models;

namespace TeachMeNow.DeveloperTest.BackEnd {
    public class SimpleAuthorizationServerProvider: OAuthAuthorizationServerProvider {
        private readonly IBackendDb db;

        public SimpleAuthorizationServerProvider(IBackendDb db) {
            this.db = db;
        }

        public override async Task GrantResourceOwnerCredentials(OAuthGrantResourceOwnerCredentialsContext context) {
            var user = db.Users.SingleOrDefault(f => f.Email == context.UserName && f.Password == context.Password);
            if(user == default(User)) {
                context.SetError("invalid_grant", "The user name or password is incorrect.");
                return;
            }

            var identity = new ClaimsIdentity(context.Options.AuthenticationType);
            var claims = user.GetClaims();
            identity.AddClaims(claims);
            AuthenticationProperties properties = createProperties(new Dictionary<string, string>() {
                { nameof(User.IsTutor), user.IsTutor ? "true" : "false" },
                { $"{nameof(User)}{nameof(user.Id)}",user.Id.ToString() }
            });
            var ticket = new AuthenticationTicket(identity, properties);
            context.Validated(ticket);
        }

        public override async Task ValidateClientAuthentication(OAuthValidateClientAuthenticationContext context) {
            context.Validated();
        }

        public override Task MatchEndpoint(OAuthMatchEndpointContext context) {
            if(context.IsTokenEndpoint && context.Request.Method == "OPTIONS") {
                context.RequestCompleted();
                return Task.FromResult(0);
            }

            return base.MatchEndpoint(context);
        }

        public override Task TokenEndpoint(OAuthTokenEndpointContext context) {
            foreach(KeyValuePair<string, string> property in context.Properties.Dictionary) {
                context.AdditionalResponseParameters.Add(property.Key, property.Value);
            }
            return base.TokenEndpoint(context);
        }

        /// <summary>
        /// Create properties which will be returned to client in response.
        /// That's public information!
        /// </summary>
        /// <returns></returns>
        private static AuthenticationProperties createProperties(IDictionary<string, string> additionalPublicFields) {
            IDictionary<string, string> data = new Dictionary<string, string> {
                {
                    "ServerTime", DateTime.UtcNow.ToString("o")
                }
            };
            if(additionalPublicFields?.Count > 0) {
                foreach(var field in additionalPublicFields) {
                    data.Add(field.Key, field.Value);
                }
            }
            return new AuthenticationProperties(data);
        }
    }
}