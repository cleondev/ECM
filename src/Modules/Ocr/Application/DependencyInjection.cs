using ECM.Ocr.Application.Abstractions;
using ECM.Ocr.Application.Commands;
using ECM.Ocr.Application.Events;
using ECM.Ocr.Application.Queries;
using Microsoft.Extensions.DependencyInjection;

namespace ECM.Ocr.Application;

public static class OcrApplicationModuleExtensions
{
    public static IServiceCollection AddOcrApplication(this IServiceCollection services)
    {
        services.AddScoped<StartOcrCommandHandler>();
        services.AddScoped<SetOcrBoxValueCommandHandler>();

        services.AddScoped<GetOcrSampleResultQueryHandler>();
        services.AddScoped<GetOcrBoxingResultQueryHandler>();
        services.AddScoped<ListOcrBoxesQueryHandler>();

        services.AddScoped<OcrProcessingEventProcessor>();

        return services;
    }
}
