using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace LicenseActivationApp
{
    public record ActivationRequest(string LicenseKey, string HardwareId);
    public record ActivationResponse(bool Success, string Message);

    public class Program
    {
        public static void Main(string[] args)
        {
            var activatedLicenses = new ConcurrentDictionary<string, string>();
            var validKeys = new List<string> { "ABC-123-XYZ", "DEF-456-UVW" };

            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            app.UseSwagger();
            app.UseSwaggerUI();

            app.MapPost("/activate", (ActivationRequest request) =>
            {
                if (!validKeys.Contains(request.LicenseKey))
                    return Results.Ok(new ActivationResponse(false, "Неверный лицензионный ключ"));

                if (activatedLicenses.TryGetValue(request.LicenseKey, out var existingHwId))
                {
                    if (existingHwId != request.HardwareId)
                        return Results.Ok(new ActivationResponse(false, "Ключ уже активирован на другом устройстве"));
                }
                else
                {
                    activatedLicenses[request.LicenseKey] = request.HardwareId;
                }

                return Results.Ok(new ActivationResponse(true, "Активация прошла успешно"));
            });

            app.Run();
        }
    }
}
