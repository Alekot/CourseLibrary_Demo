using CourseLibrary.API.DbContexts;
using CourseLibrary.API.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;

namespace CourseLibrary.API
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
            services.AddControllers(setupAction =>
            {
                setupAction.ReturnHttpNotAcceptable = true;
            })
                 .AddXmlDataContractSerializerFormatters()
                 .ConfigureApiBehaviorOptions(setupAction =>
                 {
                     setupAction.InvalidModelStateResponseFactory = context =>
                     {
                         //create a problem details object
                         var problemDetailsFatory = context.HttpContext.RequestServices
                              .GetRequiredService<ProblemDetailsFactory>();
                         var problemDetails = problemDetailsFatory.CreateValidationProblemDetails(
                             context.HttpContext,
                             context.ModelState);

                         //add additional info not added by default
                         problemDetails.Detail = "See the error field for details.";
                         problemDetails.Instance = context.HttpContext.Request.Path;

                         //find out whih status code to use
                         var actionExecutingContext = context as ActionExecutingContext;

                         if ((context.ModelState.ErrorCount > 0) &&
                            (actionExecutingContext?.ActionArguments.Count ==
                            context.ActionDescriptor.Parameters.Count))
                         {
                             problemDetails.Type = "https://something.com";
                             problemDetails.Status = StatusCodes.Status422UnprocessableEntity;
                             problemDetails.Title = "One or more validation errors occured.";
                             return new UnprocessableEntityObjectResult(problemDetails)
                             {
                                 ContentTypes = { "application/problem+json" }
                             };
                         }
                         //if one of the arguments wasn't correctly found/couldn't be parsed
                         //we're dealing with null/unparseable input
                         problemDetails.Status = StatusCodes.Status400BadRequest;
                         problemDetails.Title = "One or more errors on input occured.";
                         return new BadRequestObjectResult(problemDetails)
                         {
                             ContentTypes = { "application/problem+json" }
                         };
                     };
                 });

            services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

            services.AddScoped<ICourseLibraryRepository, CourseLibraryRepository>();

            services.AddDbContext<CourseLibraryContext>(options =>
            {
                options.UseSqlServer(
                    @"Server=(localdb)\mssqllocaldb;Database=CourseLibraryDB;Trusted_Connection=True;");
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
