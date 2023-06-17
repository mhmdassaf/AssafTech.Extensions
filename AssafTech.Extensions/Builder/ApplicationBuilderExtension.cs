namespace AssafTech.Extensions.Builder;

public static class ApplicationBuilderExtension
{
    public static WebApplication UseCheckAPI(this WebApplication app, string serviceName)
    {
        app.MapGet("/api/check", () => $"{serviceName} is working!");
        return app;
    }

    public static WebApplicationBuilder UseNLog(this WebApplicationBuilder builder)
    {
        builder.Logging.ClearProviders();
        builder.Host.UseNLog();

        return builder;
    }

    public static WebApplication UseGlobalExceptionHandler(this WebApplication app, Logger _logger)
    {
        app.UseExceptionHandler(errorApp =>
        {
            errorApp.Run(async context =>
            {
                var responseModel = new ResponseModel();
                var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;

                switch (exception)
                {
                    case NotImplementedException ex:
                        responseModel.HttpStatusCode = HttpStatusCode.NotImplemented;
                        responseModel.Errors.Add(new ErrorModel
                        {
                            Code = HttpStatusCode.NotImplemented.ToString(),
                            Message = ex.Message
                        });
                        break;
                    default:
                        responseModel.HttpStatusCode = HttpStatusCode.InternalServerError;
                        responseModel.Errors.Add(new ErrorModel
                        {
                            Code = HttpStatusCode.InternalServerError.ToString(),
                            Message = "Internal server error!"
                        });
                        break;
                }

                _logger.Error(exception, exception?.Message);

                context.Response.ContentType = "application/json";
                context.Response.StatusCode = (int)responseModel.HttpStatusCode;
                await context.Response.WriteAsync(JsonConvert.SerializeObject(responseModel));
            });
        });
        return app;
    }
}
