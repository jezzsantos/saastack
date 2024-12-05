using AncillaryApplication.Persistence;
using Application.Persistence.Shared;
using Common;
using Domain.Common.Identity;

namespace AncillaryApplication;

public partial class AncillaryApplication : IAncillaryApplication
{
    private readonly IAuditRepository _auditRepository;
    private readonly IEmailDeliveryRepository _emailDeliveryRepository;
    private readonly ISmsDeliveryRepository _smsDeliveryRepository;
    private readonly IEmailDeliveryService _emailDeliveryService;
    private readonly ISmsDeliveryService _smsDeliveryService;
    private readonly IIdentifierFactory _idFactory;
    private readonly IRecorder _recorder;
    private readonly IUsageDeliveryService _usageDeliveryService;
    private readonly IProvisioningNotificationService _provisioningNotificationService;
#if TESTINGONLY
    private readonly IAuditMessageQueueRepository _auditMessageQueueRepository;
    private readonly IEmailMessageQueue _emailMessageQueue;
    private readonly ISmsMessageQueue _smsMessageQueue;
    private readonly IUsageMessageQueue _usageMessageQueue;
    private readonly IProvisioningMessageQueue _provisioningMessageQueue;

    public AncillaryApplication(IRecorder recorder, IIdentifierFactory idFactory,
        IUsageMessageQueue usageMessageQueue, IUsageDeliveryService usageDeliveryService,
        IAuditMessageQueueRepository auditMessageQueueRepository, IAuditRepository auditRepository,
        IEmailMessageQueue emailMessageQueue, IEmailDeliveryService emailDeliveryService,
        IEmailDeliveryRepository emailDeliveryRepository,
        ISmsMessageQueue smsMessageQueue, ISmsDeliveryService smsDeliveryService,
        ISmsDeliveryRepository smsDeliveryRepository,
        IProvisioningMessageQueue provisioningMessageQueue,
        IProvisioningNotificationService provisioningNotificationService)
    {
        _recorder = recorder;
        _idFactory = idFactory;
        _usageMessageQueue = usageMessageQueue;
        _usageDeliveryService = usageDeliveryService;
        _auditMessageQueueRepository = auditMessageQueueRepository;
        _auditRepository = auditRepository;
        _emailMessageQueue = emailMessageQueue;
        _emailDeliveryService = emailDeliveryService;
        _emailDeliveryRepository = emailDeliveryRepository;
        _smsMessageQueue = smsMessageQueue;
        _smsDeliveryService = smsDeliveryService;
        _smsDeliveryRepository = smsDeliveryRepository;
        _provisioningMessageQueue = provisioningMessageQueue;
        _provisioningNotificationService = provisioningNotificationService;
    }
#else
    public AncillaryApplication(IRecorder recorder, IIdentifierFactory idFactory,
        // ReSharper disable once UnusedParameter.Local
        IUsageMessageQueue usageMessageQueue, IUsageDeliveryService usageDeliveryService,
        // ReSharper disable once UnusedParameter.Local
        IAuditMessageQueueRepository auditMessageQueueRepository, IAuditRepository auditRepository,
        // ReSharper disable once UnusedParameter.Local
        IEmailMessageQueue emailMessageQueue, IEmailDeliveryService emailDeliveryService,
        IEmailDeliveryRepository emailDeliveryRepository,
        // ReSharper disable once UnusedParameter.Local
        ISmsMessageQueue smsMessageQueue, ISmsDeliveryService smsDeliveryService,
        ISmsDeliveryRepository smsDeliveryRepository,
        // ReSharper disable once UnusedParameter.Local
        IProvisioningMessageQueue provisioningMessageQueue,
        IProvisioningNotificationService provisioningNotificationService)
    {
        _recorder = recorder;
        _idFactory = idFactory;
        _usageDeliveryService = usageDeliveryService;
        _auditRepository = auditRepository;
        _emailDeliveryService = emailDeliveryService;
        _emailDeliveryRepository = emailDeliveryRepository;
        _smsDeliveryService = smsDeliveryService;
        _smsDeliveryRepository = smsDeliveryRepository;
        _provisioningNotificationService = provisioningNotificationService;
    }
#endif
}