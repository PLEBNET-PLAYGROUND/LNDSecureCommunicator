using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Funq;
using ServiceStack;
using ServiceStack.Configuration;
using LNDSecureCommunicator.ServiceInterface;
using LNDroneController.LND;
using ServiceStack.OrmLite;
using ServiceStack.OrmLite.Sqlite;
using ServiceStack.Data;
using System;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Hosting.Server;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Hosting.Server.Features;
using TorHiddenServiceHelper;
using TorHiddenServiceHelper.ExtentionMethods;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

namespace LNDSecureCommunicator
{

    public static class GlobalThing
    {
        public static int NonSecurePort { get; internal set; }
    }
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }
        public IServiceProvider Provider { get; private set; }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public new void ConfigureServices(IServiceCollection services)
        {
            services.AddDbContext<LNDSecureContext>(ServiceLifetime.Transient, ServiceLifetime.Transient);

            var lndSettings = new LNDSettings();
            Configuration.GetSection("LNDSettings").Bind(lndSettings);

            var Alice = new LNDNodeConnection(lndSettings);

            services.AddControllers();
            services.AddHttpContextAccessor();
            services.AddSingleton(Alice);

            TorHSHelperOptions torHS = new TorHSHelperOptions();
            Configuration.GetSection("Tor").Bind(torHS);
            services.AddInjectableHostedService<ITorHSHelper, TorHSHelper>(x => new TorHSHelper(x.GetRequiredService<ILogger<TorHSHelper>>(), torHS, getBootupOnionConfig, saveOnionConfig));
            services.AddInjectableHostedService<ILightningBackgroundService, LightningBackgroundService>();

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Libre.Lightning", Version = "v1" });
                c.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
                {
                    Description = "ApiKey must appear in header",
                    Type = SecuritySchemeType.ApiKey,
                    Name = "ApiKey",
                    In = ParameterLocation.Header
                });
                var key = new OpenApiSecurityScheme()
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "ApiKey"
                    },
                    Type = SecuritySchemeType.ApiKey,
                    In = ParameterLocation.Header
                };
                var requirement = new OpenApiSecurityRequirement
                {
                   { key, new List<string>() }
                };
                c.AddSecurityRequirement(requirement);
            });


        }

        private (string onionPublicAddress, string keyType, string onionPrivateKeyBase32, int onionPort, string mapToHostAndPort, string clientAuthBase64PrivateKey, IList<string> clientAuthPubkeys, IDictionary<string, string> remoteRegistrations) getBootupOnionConfig()
        {
            var db = new LNDSecureContext();
            var conf = db.LNDSecureCommunicatorSettings.SingleOrDefault();
            if (conf == null)
            {
                return (null, null, null, 80, $"localhost:{GlobalThing.NonSecurePort}", null, null, null);
            }
            else
            {
                var clientAuthPubkeys = new List<string>();
                var remoteRegistrations = new Dictionary<string, string>();

                var remoteNodes = db.RemoteNodes;
                foreach (var remote in remoteNodes.Where(x=>x != null))
                {
                    clientAuthPubkeys.Add(remote.ClientAuthBase32PublicKey);
                    remoteRegistrations.Add(remote.OnionAddress, conf.ClientAuthBase64PrivateKey);
                }

                return (conf.OnionPublicAddress, conf.KeyType, conf.OnionPrivateKeyBase32, 80, $"localhost:{GlobalThing.NonSecurePort}", conf.ClientAuthBase64PrivateKey, clientAuthPubkeys, remoteRegistrations);
            }
        }
        private void saveOnionConfig(string keyType, string onionPrivateKeyBase32, string onionPublicAddress, string privateKeyBase64, string pubKeyBase32)
        {
            var db = new LNDSecureContext();
            var conf = db.LNDSecureCommunicatorSettings.SingleOrDefault();
            if (conf == null)
            {
                var timeStamp = DateTime.UtcNow;
                conf = new LNDSecureCommunicatorSetting()
                {
                    KeyType = keyType,
                    OnionPrivateKeyBase32 = onionPrivateKeyBase32,
                    OnionPublicAddress = onionPublicAddress,
                    Id = 1,
                    ClientAuthBase64PrivateKey = privateKeyBase64,
                    ClientAuthBase32PublicKey = pubKeyBase32,
                    UpdateDate = timeStamp,
                    CreatedDate = timeStamp,
                };
                db.LNDSecureCommunicatorSettings.Add(conf);
                db.SaveChanges();
            }
            else
            {
                conf.KeyType = keyType;
                conf.OnionPrivateKeyBase32 = onionPrivateKeyBase32;
                conf.OnionPublicAddress = onionPublicAddress;
                conf.UpdateDate = DateTime.UtcNow;
                db.SaveChanges();
            }

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, LNDSecureContext dbContext, IServiceProvider provider)
        {
            Provider = provider;
            dbContext.Database.Migrate(); //autocreate DB or apply new migrations

            var server = provider.GetRequiredService<IServer>();
            var addresses = server.Features.Get<IServerAddressesFeature>();
            GlobalThing.NonSecurePort = addresses.Addresses.First(t => t.StartsWith("http://")).LastRightPart(':').Replace("/", string.Empty).ToInt();
            
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Libre.Lightning v1"));
            }
            else
            {
                app.UseExceptionHandler("/Error");
            }
            var webSocketOptions = new WebSocketOptions()
            {
                KeepAliveInterval = TimeSpan.FromSeconds(120),
                ReceiveBufferSize = 4096,
            };
            app.UseWebSockets(webSocketOptions);

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
           
            
            DefaultFilesOptions options = new DefaultFilesOptions();
            options.DefaultFileNames.Clear();
            options.DefaultFileNames.Add("index.html");
            options.DefaultFileNames.Add("index.htm");
            app.UseDefaultFiles(options);
            app.UseStaticFiles();
        }
    }

}
