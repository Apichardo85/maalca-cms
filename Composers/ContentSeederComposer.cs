using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.ContentPublishing;
using Umbraco.Cms.Core.Services;

namespace MaalCaCMS.Composers;

/// <summary>
/// Seeds Pegote Barbershop content on startup if it doesn't exist.
/// Runs after DocumentTypeComposer creates the types.
/// </summary>
[ComposeAfter(typeof(DocumentTypeComposer))]
public class ContentSeederComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Components().Append<ContentSeederComponent>();
    }
}

public class ContentSeederComponent : IAsyncComponent
{
    private readonly IContentService _contentService;
    private readonly IContentTypeService _contentTypeService;
    private readonly IContentPublishingService _publishingService;

    public ContentSeederComponent(
        IContentService contentService,
        IContentTypeService contentTypeService,
        IContentPublishingService publishingService)
    {
        _contentService = contentService;
        _contentTypeService = contentTypeService;
        _publishingService = publishingService;
    }

    public async Task InitializeAsync(bool isRestarting, CancellationToken cancellationToken)
    {
        try
        {
            if (_contentTypeService.Get("baseAffiliate") == null) return;
        }
        catch
        {
            return;
        }

        await SeedPegoteBarbershopAsync();
    }

    public Task TerminateAsync(bool isRestarting, CancellationToken cancellationToken) => Task.CompletedTask;

    private async Task SeedPegoteBarbershopAsync()
    {
        var existing = _contentService.GetRootContent()?.FirstOrDefault(c => c.Name == "Pegote Barbershop");
        if (existing != null) return;

        var pegote = _contentService.Create("Pegote Barbershop", -1, "baseAffiliate");

        TrySet(pegote, "affiliateName", "Pegote Barbershop");
        TrySet(pegote, "affiliateDescription", "<p>La barbería dominicana #1 en el área. Cortes clásicos, fades modernos y la mejor experiencia de grooming. Más de 10 años sirviendo a la comunidad.</p>");
        TrySet(pegote, "website", "https://maalca.com/pegote-barbershop");
        TrySet(pegote, "displayInitials", "PB");
        TrySet(pegote, "contactName", "Carlos Pegote");
        TrySet(pegote, "contactEmail", "pegote@maalca.com");
        TrySet(pegote, "contactPhone", "+1 (809) 555-0101");
        TrySet(pegote, "address", "123 Main Street, Lawrence, MA 01840");
        TrySet(pegote, "instagram", "@pegotebarbershop");
        TrySet(pegote, "whatsapp", "+18095550101");
        TrySet(pegote, "facebook", "pegotebarbershop");
        TrySet(pegote, "tenantId", "a1000000-0000-0000-0000-000000000001");
        TrySet(pegote, "isActive", true);
        TrySet(pegote, "dashboardEnabled", true);

        _contentService.Save(pegote);
        await PublishAsync(pegote);

        await SeedServiceAsync(pegote.Id, "Corte Clásico", "Corte tradicional con tijera y máquina. Incluye lavado y estilizado.", 25m, 30, "Cortes");
        await SeedServiceAsync(pegote.Id, "Fade + Diseño", "Degradado con diseño personalizado. Precisión milimétrica.", 35m, 45, "Cortes");
        await SeedServiceAsync(pegote.Id, "Afeitado Clásico", "Afeitado tradicional con navaja caliente, toalla caliente y after-shave premium.", 20m, 25, "Grooming");

        await SeedTeamMemberAsync(pegote.Id, "Carlos Pegote", "Master Barber & Owner", "Más de 10 años de experiencia. Especialista en fades y diseños.", "Fades, Diseños, Corte clásico, Navaja");

        await SeedScheduleAsync(pegote.Id, "Lunes", "09:00", "19:00", false);
        await SeedScheduleAsync(pegote.Id, "Martes", "09:00", "19:00", false);
        await SeedScheduleAsync(pegote.Id, "Miércoles", "09:00", "19:00", false);
        await SeedScheduleAsync(pegote.Id, "Jueves", "09:00", "20:00", false);
        await SeedScheduleAsync(pegote.Id, "Viernes", "09:00", "20:00", false);
        await SeedScheduleAsync(pegote.Id, "Sábado", "08:00", "17:00", false);
        await SeedScheduleAsync(pegote.Id, "Domingo", "", "", true);
    }

    private async Task SeedServiceAsync(int parentId, string name, string desc, decimal price, int duration, string category)
    {
        var c = _contentService.Create(name, parentId, "affiliateService");
        TrySet(c, "serviceName", name);
        TrySet(c, "serviceDescription", desc);
        TrySet(c, "servicePrice", price);
        TrySet(c, "serviceDuration", duration);
        TrySet(c, "serviceCategory", category);
        TrySet(c, "isActive", true);
        _contentService.Save(c);
        await PublishAsync(c);
    }

    private async Task SeedTeamMemberAsync(int parentId, string name, string role, string bio, string specialties)
    {
        var c = _contentService.Create(name, parentId, "affiliateTeamMember");
        TrySet(c, "memberName", name);
        TrySet(c, "role", role);
        TrySet(c, "bio", bio);
        TrySet(c, "specialties", specialties);
        TrySet(c, "isAvailable", true);
        _contentService.Save(c);
        await PublishAsync(c);
    }

    private async Task SeedScheduleAsync(int parentId, string day, string open, string close, bool isClosed)
    {
        var c = _contentService.Create(day, parentId, "affiliateSchedule");
        TrySet(c, "dayOfWeek", day);
        TrySet(c, "openTime", open);
        TrySet(c, "closeTime", close);
        TrySet(c, "isClosed", isClosed);
        _contentService.Save(c);
        await PublishAsync(c);
    }

    private async Task PublishAsync(IContent content)
    {
        var cultures = new[] { new CulturePublishScheduleModel { Culture = "*" } };
        await _publishingService.PublishAsync(content.Key, cultures, Constants.Security.SuperUserKey);
    }

    private static void TrySet(IContent content, string alias, object value)
    {
        try { content.SetValue(alias, value); } catch { /* property not available */ }
    }
}
