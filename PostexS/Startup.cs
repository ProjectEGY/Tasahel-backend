using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using PostexS.Interfaces;
using PostexS.Models;
using PostexS.Models.Data;
using PostexS.Models.Domain;
using PostexS.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace PostexS
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSession(options =>
            {
                options.Cookie.Name = "MySession";
                options.IdleTimeout = TimeSpan.FromDays(30);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });
            services.AddControllersWithViews();
            services.AddDbContext<ApplicationDbContext>(x => x.UseSqlServer(Configuration.GetConnectionString("default")));
            services.AddIdentity<ApplicationUser, IdentityRole>(option =>
            {
                option.Password.RequireNonAlphanumeric = false;
                option.Password.RequireUppercase = false;
                option.Password.RequireLowercase = false;
                option.Password.RequiredLength = 2;
            })
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();
            services.Configure<IdentityOptions>(options =>
            {
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireLowercase = false;
                options.Password.RequiredLength = 2;
            });
            services.AddAuthentication(/*options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;

            }*/)
           // Adding Jwt Bearer  
           .AddJwtBearer(options =>
           {
               options.SaveToken = true;
               options.RequireHttpsMetadata = false;
               options.TokenValidationParameters = new TokenValidationParameters()
               {

                   ValidateIssuer = true,
                   ValidateAudience = true,
                   ValidAudience = Configuration["JWT:ValidAudience"],
                   ValidIssuer = Configuration["JWT:ValidIssuer"],
                   IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Configuration["JWT:Secret"]))
               };
           })
           .Services.ConfigureApplicationCookie(e =>
           {
               e.Cookie.Name = "Postex-Admin-Token";
               e.LoginPath = new PathString("/Home/Login");
               e.LogoutPath = new PathString("/Home/Logout");
               e.AccessDeniedPath = new PathString("/Home/AccessDenied");
               e.ExpireTimeSpan = TimeSpan.FromDays(999);
               e.Cookie.HttpOnly = true;
               e.SlidingExpiration = true;
               e.ReturnUrlParameter = CookieAuthenticationDefaults.ReturnUrlParameter;
               e.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
               e.Cookie.SameSite = SameSiteMode.Unspecified;
           });
            services.AddScoped(typeof(IGeneric<>), typeof(Generic<>));
            services.AddScoped(typeof(ICRUD<>), typeof(CRUD<>));
            services.AddScoped<IOrderService, OrderService>();
            services.AddScoped<IWalletService, WalletService>();

            // Wapilot WhatsApp Integration
            services.AddHttpClient();
            services.AddScoped<IWapilotService, WapilotService>();
            services.AddScoped<IWhatsAppBotCloudService, WhatsAppBotCloudService>();
            services.AddScoped<IWhaStackService, WhaStackService>();
            services.AddScoped<IWhatsAppProviderService, WhatsAppProviderService>();
            services.AddHostedService<WhatsAppQueueProcessor>();

            // Bind Firebase configuration from appsettings.json
            var firebaseConfig = Configuration.GetSection("FireBase").Get<FirebaseConfig>();

            if (firebaseConfig == null)
            {
                throw new Exception("Firebase configuration is missing in appsettings.json");
            }

            // Serialize the FirebaseConfig object to JSON
            var firebaseConfigJson = JsonConvert.SerializeObject(firebaseConfig);

            // Initialize Firebase App
            FirebaseApp.Create(new AppOptions()
            {
                Credential = GoogleCredential.FromJson(firebaseConfigJson)
            });

            // Register Firebase Messaging as a singleton if needed
            services.AddSingleton(FirebaseMessaging.DefaultInstance);

            // Swagger Configuration - single document that contains all APIs
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "Tasahel Express - API",
                    Version = "v1",
                    Description =
                        "جميع واجهات Tasahel Express في صفحة واحدة:\n\n" +
                        "- Sender API (مفاتيح API: X-Public-Key, X-Private-Key)\n" +
                        "- Driver & Mobile API (JWT Bearer Token عبر /api/Account/Login)\n\n" +
                        "استخدم زر **Authorize** لإرسال الـ Headers أو الـ Bearer Token تلقائياً.",
                    Contact = new OpenApiContact
                    {
                        Name = "Tasahel Express Support",
                        Url = new Uri("https://tasahel-eg.com")
                    }
                });

                // توثيق API Controllers فقط (استبعاد MVC Controllers التي تسبب 500 عند توليد swagger.json)
                c.DocInclusionPredicate((docName, apiDesc) =>
                {
                    if (apiDesc.ActionDescriptor is Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor cad)
                        return cad.ControllerTypeInfo?.Namespace == "PostexS.Controllers.API";
                    return false;
                });

                // Resolve conflicting actions and schema IDs
                c.ResolveConflictingActions(apiDescriptions => apiDescriptions.First());
                c.CustomSchemaIds(type => type.FullName);

                // Header parameters filter (Latitude/Longitude with default values)
                c.OperationFilter<Filters.AddHeaderParametersFilter>();

                // Security: API Key (for Sender APIs)
                c.AddSecurityDefinition("X-Public-Key", new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.ApiKey,
                    In = ParameterLocation.Header,
                    Name = "X-Public-Key",
                    Description = "المفتاح العام (Public Key) - احصل عليه من /developer/api-keys"
                });

                c.AddSecurityDefinition("X-Private-Key", new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.ApiKey,
                    In = ParameterLocation.Header,
                    Name = "X-Private-Key",
                    Description = "المفتاح الخاص (Private Key) - احصل عليه من /developer/api-keys"
                });

                // Security: JWT Bearer (for Driver/Mobile APIs)
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Name = "Authorization",
                    Description = "أدخل الـ JWT Token هنا.\n\nمثال: `eyJhbGciOiJIUzI1NiIs...`\n\n(لا تكتب كلمة Bearer - هتتضاف تلقائياً)"
                });

                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "X-Public-Key"
                            }
                        },
                        new string[] {}
                    },
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "X-Private-Key"
                            }
                        },
                        new string[] {}
                    },
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        new string[] {}
                    }
                });
            });

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            app.UseHttpsRedirection();
            app.UseStaticFiles();

            // Swagger UI - Enable in all environments
            app.UseSwagger(c =>
            {
                c.SerializeAsV2 = false;
                c.RouteTemplate = "swagger/{documentName}/swagger.json";
            });
            app.UseSwaggerUI(c =>
            {
                // Single combined Swagger document for all APIs
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Tasahel Express - API");
                c.RoutePrefix = "swagger";
                c.DocumentTitle = "Tasahel Express - API Documentation";
            });

            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
