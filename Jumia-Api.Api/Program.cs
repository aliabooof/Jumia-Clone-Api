
using Jumia_Api.Api.DependencyInjection.Application;
using Jumia_Api.Api.DependencyInjection.Domain;
using Jumia_Api.Api.DependencyInjection.Infrastructure;
using Jumia_Api.Application.Interfaces;
using Jumia_Api.Application.Services;

using Jumia_Api.Domain.Models;
using Jumia_Api.Infrastructure.Presistence.Context;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;

using Jumia_Api.Domain.Interfaces.UnitOfWork;
using Jumia_Api.Infrastructure.Presistence.UnitOfWork;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Jumia_Api.Infrastructure.Hubs;


namespace Jumia_Api.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();



            builder.Services.AddControllers();

            builder.Services.AddInfrastructure(builder.Configuration);
            builder.Services.AddDomain(builder.Configuration);
            builder.Services.AddApplication(builder.Configuration);


            var jwtConfig = builder.Configuration.GetSection("Jwt");
            //builder.Services.Configure<JwtOptions>(jwtConfig);

            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,

                    ValidIssuer = jwtConfig["Issuer"],
                    ValidAudience = jwtConfig["Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtConfig["Key"]!)),
                };

                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var token = context.Request.Cookies["JumiaAuthCookie"];
                        if (!string.IsNullOrEmpty(token))
                        {
                            context.Token = token;
                        }
                        return Task.CompletedTask;
                    }
                };

            });
            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
               .AddCookie(options =>
               {
                   options.Cookie.HttpOnly = true;
                   options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                   options.Cookie.SameSite = SameSiteMode.Strict;
                   options.Cookie.Name = "JumiaAuthCookie"; 
                   options.Cookie.MaxAge = TimeSpan.FromMinutes(int.Parse(jwtConfig["DurationInMinutes"]!));
               });
            builder.Services.Configure<JwtSettings>(jwtConfig);
            builder.Services.AddScoped<IJwtService, JwtService>();




            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new() { Title = "Your API", Version = "v1" });

                // Add JWT Auth support
                c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
                    Scheme = "Bearer",
                    BearerFormat = "JWT",
                    In = Microsoft.OpenApi.Models.ParameterLocation.Header,
                    Description = "Enter 'Bearer' [space] and then your valid token in the text input below.\r\n\r\nExample: \"Bearer eyJhbGciOiJI...\""
                });

                c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
            });

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAngularDev", policy =>
                {
                    policy.WithOrigins("http://localhost:4200")
                    .AllowCredentials()
                    .AllowAnyHeader()
                    .AllowAnyMethod();
                });
            });

            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            builder.Services.AddOpenApi();



            // Swagger/OpenAPI configuration
            builder.Services.AddEndpointsApiExplorer();

            
           


            //builder.Services.AddSwaggerGen(c =>
            //{
            //    c.SwaggerDoc("v1", new OpenApiInfo
            //    {
            //        Title = "Jumia API",
            //        Version = "v1",
            //        Description = "API for Jumia Application",
            //    });
 



            var app = builder.Build();
            app.UseCors("AllowAngularDev");
            app.UseStaticFiles();
            //Enable Swagger middleware
            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {

                app.UseDeveloperExceptionPage();

                app.UseSwagger();
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Jumia API v1");
                });

            }

            app.UseHttpsRedirection();

            app.UseAuthentication();
            app.UseAuthorization();


            app.MapControllers();
            app.MapHub<ChatHub>("/chathub");


            app.Run();
        }
    }
}
