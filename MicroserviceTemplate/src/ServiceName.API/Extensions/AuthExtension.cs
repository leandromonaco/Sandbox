﻿using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using ServiceName.Core.Common.Security;

namespace ServiceName.API.Extensions
{
    public static class SecurityExtension
    {
        public static void AddAuthSupport(this IServiceCollection services, ConfigurationManager configurationManager)
        {
            services.AddAuthentication(o =>
             {
                 o.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                 o.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                 o.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
             }).AddJwtBearer(o =>
             {
                 o.TokenValidationParameters = new TokenValidationParameters
                 {
                     ValidateIssuerSigningKey = true,
                     ValidateIssuer = true,
                     ValidateAudience = true,
                     ValidIssuer = configurationManager["ModuleConfiguration:Jwt:Issuer"],
                     ValidAudience = configurationManager["ModuleConfiguration:Jwt:Audience"],
                     IssuerSigningKey = SecurityHelper.GetRsaSecurityKey(configurationManager["ModuleConfiguration:AwsServices:KMS:PublicKey"])
                 };
             });

            services.AddAuthorization();
        }

        public static void UseAuth(this WebApplication app) {

            app.UseAuthentication();
            app.UseAuthorization();
        }
    }
}
