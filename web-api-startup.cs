
public class Startup
{
    public Startup(IConfiguration configuration, IHostingEnvironment env)
    {
        var builder = new ConfigurationBuilder()
         .SetBasePath(env.ContentRootPath)
         .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
         .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
         .AddEnvironmentVariables();
        Configuration = builder.Build();
    }

    public IConfiguration Configuration { get; }

    // This method gets called by the runtime. Use this method to add services to the container.
    public void ConfigureServices(IServiceCollection services)
    {
        string conn = Configuration.GetSection("Logging:ConnectionStrings:connDB").Value;
        services.AddDbContext<Scholars.Data.Business.AppDbContext>(options =>
        options.UseSqlServer(conn));

        //Adding Cors Config
        services.AddCors(options =>
        {
            options.AddPolicy("AllowAll",
                p => p.AllowAnyOrigin().
                AllowAnyMethod().
                AllowAnyHeader().
                AllowCredentials());
        });
        string Secret = Configuration.GetSection("Logging:AppConfiguration:SecretKey").Value;
        var key = Encoding.ASCII.GetBytes(Secret);
        // configure strongly typed settings objects
        var appSettingsSection = Configuration.GetSection("Logging:AppConfiguration");
        services.Configure<AppSettings>(appSettingsSection);
        var dbSettingsSection = Configuration.GetSection("Logging:ConnectionStrings");
        services.Configure<Scholars.Data.DbSettings>(dbSettingsSection);
        var connDB = Configuration.GetSection("Logging:ConnectionStrings:connDB").Value;
        Scholars.Data.DataLayerAsync._StrSqlConn = connDB;
        services.AddAuthentication(x =>
        {
            x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        //.AddCookie()
        .AddJwtBearer(x =>
        {
            x.RequireHttpsMetadata = false;
            x.SaveToken = true;
            x.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = false,
                ValidateAudience = false
            };
        });

        // Add Global Filter Service            
        services.AddMvc(
            config =>
            {
                config.Filters.Add(typeof(CustomExceptionFilter));
            }
        )
        .AddSessionStateTempDataProvider()
        .AddJsonOptions(options =>
        {
                //options.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
                options.SerializerSettings.ContractResolver = new DefaultContractResolver();
            options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            options.SerializerSettings.Formatting = Formatting.Indented;
        });
        // Adds a default in-memory implementation of IDistributedCache.
        services.AddDistributedMemoryCache();
        services.AddSession(o =>
        {
            o.Cookie.Name = ".BCS.Session";
            o.IdleTimeout = TimeSpan.FromMinutes(20);
            o.Cookie.HttpOnly = true;
            o.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        });
        services.AddTransient<Scholars.Data.Business.ILoginCls, Scholars.Data.Business.LoginCls>();
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
    {
        loggerFactory.AddConsole(Configuration.GetSection("Logging"));
        loggerFactory.AddDebug();

        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
            app.UseBrowserLink();
        }
        else
        {
            app.UseExceptionHandler(options =>
            {
                options.Run(
                async context =>
                {
                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    context.Response.ContentType = "text/html";

                    var ex = context.Features.Get<IExceptionHandlerFeature>();

                    if (ex != null)
                    {
                        var err = $"<h1>Error: {ex.Error.Message}</h1>{ex.Error.StackTrace }";
                        await context.Response.WriteAsync(err).ConfigureAwait(false);
                    }
                });
            });
        }
        // Return static files and end pipeline.
        app.UseStaticFiles();
        app.UseSession();
        //Middleware Call            
        app.UseCors(x => x
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials());

        // Authenticate before you access secure resources.
        app.UseAuthentication();
        // app.UseStatusCodePagesWithReExecute("/Error/PageNotFound");
        // app.UseMvc();

        app.UseMvc(routes =>
        {
            routes.MapRoute(
                name: "default",
                template: "{controller=Home}/{action=Index}/{id?}"
                );
                //routes.MapSpaFallbackRoute(
                //   name: "spa-fallback",
                //   defaults: new { controller = "Home", action = "Index" });
            });
    }
}

public class AppSettings
{
    public string SenderEmailID { get; set; }
    public string SenderEmailPassword { get; set; }
    public string SenderSMTPHost { get; set; }
    public string SenderSMTPPortNo { get; set; }
    public string EnableSSL { get; set; }
    public string ftpHostName { get; set; }
    public string ftpUserName { get; set; }
    public string ftpPassword { get; set; }
    public string ftpSshHostKeyFingerprint { get; set; }
    public string ftpIsSecure { get; set; }
    public string ftpPortNo { get; set; }
    public string ftpRemotePath { get; set; }
    public string csvDelimeter { get; set; }
    public string SecretKey { get; set; }
}