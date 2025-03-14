using System.ComponentModel.DataAnnotations;

namespace OnPremisesWorkerService.Configuration;

public sealed class RabbitMqSettings
{
    public const string SectionName = "ApplicationServices:Persistence:RabbitMq";

    [Required]
    public string HostName { get; set; } = null!;

    [Required]
    public string UserName { get; set; } = null!;

    [Required]
    public string Password { get; set; } = null!;
}