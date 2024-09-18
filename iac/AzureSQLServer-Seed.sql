-- noinspection SqlDialectInspectionForFile

USE
    [SaaStack]
GO

IF EXISTS(SELECT *
          FROM sys.objects
          WHERE object_id = OBJECT_ID(N'[dbo].[APIKey]')
            AND type in (N'U'))
    DROP TABLE [dbo].[APIKey]
GO

IF EXISTS(SELECT *
          FROM sys.objects
          WHERE object_id = OBJECT_ID(N'[dbo].[Audit]')
            AND type in (N'U'))
    DROP TABLE [dbo].[Audit]
GO

IF EXISTS(SELECT *
          FROM sys.objects
          WHERE object_id = OBJECT_ID(N'[dbo].[AuthToken]')
            AND type in (N'U'))
    DROP TABLE [dbo].[AuthToken]
GO

IF EXISTS(SELECT *
          FROM sys.objects
          WHERE object_id = OBJECT_ID(N'[dbo].[Booking]')
            AND type in (N'U'))
    DROP TABLE [dbo].[Booking]
GO

IF EXISTS(SELECT *
          FROM sys.objects
          WHERE object_id = OBJECT_ID(N'[dbo].[Car]')
            AND type in (N'U'))
    DROP TABLE [dbo].[Car]
GO

IF EXISTS(SELECT *
          FROM sys.objects
          WHERE object_id = OBJECT_ID(N'[dbo].[DomainEvent]')
            AND type in (N'U'))
    DROP TABLE [dbo].[DomainEvent]
GO

IF EXISTS(SELECT *
          FROM sys.objects
          WHERE object_id = OBJECT_ID(N'[dbo].[EmailDelivery]')
            AND type in (N'U'))
    DROP TABLE [dbo].[EmailDelivery]
GO

IF EXISTS(SELECT *
          FROM sys.objects
          WHERE object_id = OBJECT_ID(N'[dbo].[EndUser]')
            AND type in (N'U'))
    DROP TABLE [dbo].[EndUser]
GO

IF EXISTS(SELECT *
          FROM sys.objects
          WHERE object_id = OBJECT_ID(N'[dbo].[EventStore]')
            AND type in (N'U'))
    DROP TABLE [dbo].[EventStore]
GO

IF EXISTS(SELECT *
          FROM sys.objects
          WHERE object_id = OBJECT_ID(N'[dbo].[Invitation]')
            AND type in (N'U'))
    DROP TABLE [dbo].[Invitation]
GO

IF EXISTS(SELECT *
          FROM sys.objects
          WHERE object_id = OBJECT_ID(N'[dbo].[Image]')
            AND type in (N'U'))
    DROP TABLE [dbo].[Image]
GO

IF EXISTS(SELECT *
          FROM sys.objects
          WHERE object_id = OBJECT_ID(N'[dbo].[Membership]')
            AND type in (N'U'))
    DROP TABLE [dbo].[Membership]
GO

IF EXISTS(SELECT *
          FROM sys.objects
          WHERE object_id = OBJECT_ID(N'[dbo].[Organization]')
            AND type in (N'U'))
    DROP TABLE [dbo].[Organization]
GO

IF EXISTS(SELECT *
          FROM sys.objects
          WHERE object_id = OBJECT_ID(N'[dbo].[PasswordCredential]')
            AND type in (N'U'))
    DROP TABLE [dbo].[PasswordCredential]
GO

IF EXISTS(SELECT *
          FROM sys.objects
          WHERE object_id = OBJECT_ID(N'[dbo].[ProjectionCheckpoints]')
            AND type in (N'U'))
    DROP TABLE [dbo].[ProjectionCheckpoints]
GO

IF EXISTS(SELECT *
          FROM sys.objects
          WHERE object_id = OBJECT_ID(N'[dbo].[SSOUser]')
            AND type in (N'U'))
    DROP TABLE [dbo].[SSOUser]
GO

IF EXISTS(SELECT *
          FROM sys.objects
          WHERE object_id = OBJECT_ID(N'[dbo].[Subscription]')
            AND type in (N'U'))
    DROP TABLE [dbo].[Subscription]
GO

IF EXISTS(SELECT *
          FROM sys.objects
          WHERE object_id = OBJECT_ID(N'[dbo].[Unavailability]')
            AND type in (N'U'))
    DROP TABLE [dbo].[Unavailability]
GO

IF EXISTS(SELECT *
          FROM sys.objects
          WHERE object_id = OBJECT_ID(N'[dbo].[UserProfile]')
            AND type in (N'U'))
    DROP TABLE [dbo].[UserProfile]
GO

IF EXISTS(SELECT *
          FROM sys.objects
          WHERE object_id = OBJECT_ID(N'[dbo].[WebhookNotificationAudits]')
            AND type in (N'U'))
    DROP TABLE [dbo].[WebhookNotificationAudits]
GO

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[APIKey]
(
    [Id]                 [nvarchar](100) NOT NULL,
    [LastPersistedAtUtc] [datetime]      NULL,
    [IsDeleted]          [bit]           NULL,
    [Description]        [nvarchar](100) NULL,
    [ExpiresOn]          [datetime]      NULL,
    [KeyToken]           [nvarchar](450) NULL,
    [UserId]             [nvarchar](100) NULL,
) ON [PRIMARY]
GO

CREATE INDEX Id
    ON [dbo].[APIKey]
        (
         [Id]
            );
CREATE INDEX KeyToken
    ON [dbo].[APIKey]
        (
         [KeyToken]
            );
CREATE INDEX UserId
    ON [dbo].[APIKey]
        (
         [UserId]
            );

CREATE TABLE [dbo].[Audit]
(
    [Id]                 [nvarchar](100) NOT NULL,
    [LastPersistedAtUtc] [datetime]      NULL,
    [IsDeleted]          [bit]           NULL,
    [AgainstId]          [nvarchar](100) NULL,
    [AuditCode]          [nvarchar](max) NULL,
    [MessageTemplate]    [nvarchar](max) NULL,
    [OrganizationId]     [nvarchar](100) NULL,
    [TemplateArguments]  [nvarchar](max) NULL,
) ON [PRIMARY]
GO

CREATE INDEX Id
    ON [dbo].[Audit]
        (
         [Id]
            );
CREATE INDEX UserId
    ON [dbo].[Audit]
        (
         [AgainstId]
            );

CREATE TABLE [dbo].[AuthToken]
(
    [Id]                    [nvarchar](100) NOT NULL,
    [LastPersistedAtUtc]    [datetime]      NULL,
    [IsDeleted]             [bit]           NULL,
    [AccessToken]           [nvarchar](450) NULL,
    [AccessTokenExpiresOn]  [datetime]      NULL,
    [RefreshToken]          [nvarchar](450) NULL,
    [RefreshTokenExpiresOn] [datetime]      NULL,
    [UserId]                [nvarchar](100) NULL,
) ON [PRIMARY]
GO

CREATE INDEX Id
    ON [dbo].[AuthToken]
        (
         [Id]
            );
CREATE INDEX RefreshToken
    ON [dbo].[AuthToken]
        (
         [RefreshToken]
            );
CREATE INDEX UserId
    ON [dbo].[AuthToken]
        (
         [UserId]
            );

CREATE TABLE [dbo].[Booking]
(
    [Id]                 [nvarchar](100) NOT NULL,
    [LastPersistedAtUtc] [datetime]      NULL,
    [IsDeleted]          [bit]           NULL,
    [BorrowerId]         [nvarchar](100) NULL,
    [CarId]              [nvarchar](100) NULL,
    [End]                [datetime]      NULL,
    [OrganizationId]     [nvarchar](100) NULL,
    [Start]              [datetime]      NULL,
) ON [PRIMARY]
GO

CREATE INDEX Id
    ON [dbo].[Booking]
        (
         [Id]
            );
CREATE INDEX OrganizationId
    ON [dbo].[Booking]
        (
         [OrganizationId]
            );

CREATE TABLE [dbo].[Car]
(
    [Id]                  [nvarchar](100) NOT NULL,
    [LastPersistedAtUtc]  [datetime]      NULL,
    [IsDeleted]           [bit]           NULL,
    [LicenseJurisdiction] [nvarchar](max) NULL,
    [LicenseNumber]       [nvarchar](max) NULL,
    [ManagerIds]          [nvarchar](max) NULL,
    [ManufactureMake]     [nvarchar](max) NULL,
    [ManufactureModel]    [nvarchar](max) NULL,
    [ManufactureYear]     [int]           NULL,
    [OrganizationId]      [nvarchar](100) NULL,
    [Status]              [nvarchar](100) NULL,
    [VehicleOwnerId]      [nvarchar](100) NULL,
) ON [PRIMARY]
GO

CREATE INDEX Id
    ON [dbo].[Car]
        (
         [Id]
            );
CREATE INDEX OrganizationId
    ON [dbo].[Car]
        (
         [OrganizationId]
            );
CREATE INDEX Status
    ON [dbo].[Car]
        (
         [Status]
            );

CREATE TABLE [dbo].[DomainEvent]
(
    [Id]                 [nvarchar](100) NOT NULL,
    [LastPersistedAtUtc] [datetime]      NULL,
    [IsDeleted]          [bit]           NULL,
    [Data]               [nvarchar](max) NULL,
    [EventType]          [nvarchar](max) NULL,
    [Metadata]           [nvarchar](max) NULL,
    [RootAggregateType]  [nvarchar](max) NULL,
    [Version]            [int]           NULL,
) ON [PRIMARY]
GO

CREATE INDEX Id
    ON [dbo].[DomainEvent]
        (
         [Id]
            );

CREATE TABLE [dbo].[EmailDelivery]
(
    [Id]                   [nvarchar](100) NOT NULL,
    [LastPersistedAtUtc]   [datetime]      NULL,
    [IsDeleted]            [bit]           NULL,
    [Attempts]             [nvarchar](max) NULL,
    [Body]                 [nvarchar](max) NULL,
    [Delivered]            [datetime]      NULL,
    [DeliveryFailed]       [datetime]      NULL,
    [DeliveryFailedReason] [nvarchar](max) NULL,
    [LastAttempted]        [datetime]      NULL,
    [MessageId]            [nvarchar](450) NULL,
    [ReceiptId]            [nvarchar](450) NULL,
    [SendFailed]           [datetime]      NULL,
    [Sent]                 [datetime]      NULL,
    [Subject]              [nvarchar](max) NULL,
    [ToDisplayName]        [nvarchar](max) NULL,
    [ToEmailAddress]       [nvarchar](max) NULL,
) ON [PRIMARY]
GO

CREATE INDEX Id
    ON [dbo].[EmailDelivery]
        (
         [Id]
            );

CREATE INDEX MessageId
    ON [dbo].[EmailDelivery]
        (
         [MessageId]
            );

CREATE INDEX ReceiptId
    ON [dbo].[EmailDelivery]
        (
         [ReceiptId]
            );

CREATE TABLE [dbo].[EndUser]
(
    [Id]                 [nvarchar](100) NOT NULL,
    [LastPersistedAtUtc] [datetime]      NULL,
    [IsDeleted]          [bit]           NULL,
    [Access]             [nvarchar](max) NULL,
    [Classification]     [nvarchar](max) NULL,
    [Features]           [nvarchar](max) NULL,
    [Roles]              [nvarchar](max) NULL,
    [Status]             [nvarchar](max) NULL,
    [Username]           [nvarchar](max) NULL,
) ON [PRIMARY]
GO

CREATE INDEX Id
    ON [dbo].[EndUser]
        (
         [Id]
            );

CREATE TABLE [dbo].[EventStore]
(
    [Id]                 [nvarchar](100) NOT NULL,
    [LastPersistedAtUtc] [datetime]      NULL,
    [IsDeleted]          [bit]           NULL,
    [Data]               [nvarchar](max) NULL,
    [EntityName]         [nvarchar](max) NULL,
    [EntityType]         [nvarchar](max) NULL,
    [EventType]          [nvarchar](max) NULL,
    [Metadata]           [nvarchar](max) NULL,
    [StreamName]         [nvarchar](450) NULL,
    [Version]            [bigint]        NULL,
) ON [PRIMARY]
GO

CREATE INDEX Id
    ON [dbo].[EventStore]
        (
         [Id]
            );
CREATE INDEX StreamName
    ON [dbo].[EventStore]
        (
         [StreamName]
            );

CREATE TABLE [dbo].[Invitation]
(
    [Id]                   [nvarchar](100) NOT NULL,
    [LastPersistedAtUtc]   [datetime]      NULL,
    [IsDeleted]            [bit]           NULL,
    [AcceptedAt]           [datetime]      NULL,
    [AcceptedEmailAddress] [nvarchar](max) NULL,
    [InvitedById]          [nvarchar](100) NULL,
    [InvitedEmailAddress]  [nvarchar](450) NULL,
    [Status]               [nvarchar](100) NULL,
    [Token]                [nvarchar](450) NULL,
) ON [PRIMARY]
GO

CREATE INDEX Id
    ON [dbo].[Invitation]
        (
         [Id]
            );
CREATE INDEX InvitedEmailAddress
    ON [dbo].[Invitation]
        (
         [InvitedEmailAddress]
            );
CREATE INDEX Status
    ON [dbo].[Invitation]
        (
         [Status]
            );
CREATE INDEX Token
    ON [dbo].[Invitation]
        (
         [Token]
            );

CREATE TABLE [dbo].[Image]
(
    [Id]                 [nvarchar](100) NOT NULL,
    [LastPersistedAtUtc] [datetime]      NULL,
    [IsDeleted]          [bit]           NULL,
    [ContentType]        [nvarchar](max) NULL,
    [CreatedById]        [nvarchar](100) NULL,
    [Description]        [nvarchar](max) NULL,
    [Filename]           [nvarchar](max) NULL,
    [Size]               [bigint]        NULL,
) ON [PRIMARY]
GO

CREATE INDEX Id
    ON [dbo].[Image]
        (
         [Id]
            );

CREATE TABLE [dbo].[Membership]
(
    [Id]                 [nvarchar](100) NOT NULL,
    [LastPersistedAtUtc] [datetime]      NULL,
    [IsDeleted]          [bit]           NULL,
    [Features]           [nvarchar](max) NULL,
    [IsDefault]          [bit]           NULL,
    [OrganizationId]     [nvarchar](100) NULL,
    [Ownership]          [nvarchar](max) NULL,
    [Roles]              [nvarchar](max) NULL,
    [UserId]             [nvarchar](100) NULL,
) ON [PRIMARY]
GO

CREATE INDEX Id
    ON [dbo].[Membership]
        (
         [Id]
            );

CREATE TABLE [dbo].[Organization]
(
    [Id]                    [nvarchar](100) NOT NULL,
    [LastPersistedAtUtc]    [datetime]      NULL,
    [IsDeleted]             [bit]           NULL,
    [AvatarImageId]         [nvarchar](100) NULL,
    [AvatarUrl]             [nvarchar](max) NULL,
    [BillingSubscriberId]   [nvarchar](100) NULL,
    [BillingSubscriptionId] [nvarchar](100) NULL,
    [CreatedById]           [nvarchar](100) NULL,
    [Name]                  [nvarchar](max) NULL,
    [Ownership]             [nvarchar](max) NULL,
) ON [PRIMARY]
GO

CREATE INDEX Id
    ON [dbo].[Organization]
        (
         [Id]
            );
CREATE INDEX AvatarImageId
    ON [dbo].[Organization]
        (
         [AvatarImageId]
            );

CREATE TABLE [dbo].[PasswordCredential]
(
    [Id]                            [nvarchar](100) NOT NULL,
    [LastPersistedAtUtc]            [datetime]      NULL,
    [IsDeleted]                     [bit]           NULL,
    [AccountLocked]                 [bit]           NULL,
    [PasswordResetToken]            [nvarchar](450) NULL,
    [RegistrationVerificationToken] [nvarchar](max) NULL,
    [RegistrationVerified]          [bit]           NULL,
    [UserEmailAddress]              [nvarchar](max) NULL,
    [UserId]                        [nvarchar](100) NULL,
    [UserName]                      [nvarchar](450) NULL,
) ON [PRIMARY]
GO

CREATE INDEX Id
    ON [dbo].[PasswordCredential]
        (
         [Id]
            );
CREATE INDEX PasswordResetToken
    ON [dbo].[PasswordCredential]
        (
         [PasswordResetToken]
            );
CREATE INDEX UserId
    ON [dbo].[PasswordCredential]
        (
         [UserId]
            );
CREATE INDEX UserName
    ON [dbo].[PasswordCredential]
        (
         [UserName]
            );

CREATE TABLE [dbo].[ProjectionCheckpoints]
(
    [Id]                 [nvarchar](100) NOT NULL,
    [LastPersistedAtUtc] [datetime]      NULL,
    [IsDeleted]          [bit]           NULL,
    [Position]           [int]           NULL,
    [StreamName]         [nvarchar](450) NULL,
) ON [PRIMARY]
GO

CREATE INDEX Id
    ON [dbo].[ProjectionCheckpoints]
        (
         [Id]
            );
CREATE INDEX StreamName
    ON [dbo].[ProjectionCheckpoints]
        (
         [StreamName]
            );

CREATE TABLE [dbo].[Subscription]
(
    [Id]                    [nvarchar](100) NOT NULL,
    [LastPersistedAtUtc]    [datetime]      NULL,
    [IsDeleted]             [bit]           NULL,
    [BuyerId]               [nvarchar](100) NULL,
    [BuyerReference]        [nvarchar](450) NULL,
    [OwningEntityId]        [nvarchar](100) NULL,
    [ProviderName]          [nvarchar](max) NULL,
    [ProviderState]         [nvarchar](max) NULL,
    [SubscriptionReference] [nvarchar](450) NULL,
) ON [PRIMARY]
GO

CREATE INDEX Id
    ON [dbo].[Subscription]
        (
         [Id]
            );
CREATE INDEX BuyerReference
    ON [dbo].[Subscription]
        (
         [BuyerReference]
            );
CREATE INDEX OwningEntityId
    ON [dbo].[Subscription]
        (
         [OwningEntityId]
            );
CREATE INDEX SubscriptionReference
    ON [dbo].[Subscription]
        (
         [SubscriptionReference]
            );

CREATE TABLE [dbo].[SSOUser]
(
    [Id]                 [nvarchar](100) NOT NULL,
    [LastPersistedAtUtc] [datetime]      NULL,
    [IsDeleted]          [bit]           NULL,
    [CountryCode]        [nvarchar](max) NULL,
    [EmailAddress]       [nvarchar](max) NULL,
    [FirstName]          [nvarchar](max) NULL,
    [LastName]           [nvarchar](max) NULL,
    [ProviderName]       [nvarchar](max) NULL,
    [Timezone]           [nvarchar](max) NULL,
    [Tokens]             [nvarchar](max) NULL,
    [UserId]             [nvarchar](100) NULL,
) ON [PRIMARY]
GO

CREATE INDEX Id
    ON [dbo].[SSOUser]
        (
         [Id]
            );
CREATE INDEX UserId
    ON [dbo].[SSOUser]
        (
         [UserId]
            );

CREATE TABLE [dbo].[Unavailability]
(
    [Id]                 [nvarchar](100) NOT NULL,
    [LastPersistedAtUtc] [datetime]      NULL,
    [IsDeleted]          [bit]           NULL,
    [CarId]              [nvarchar](100) NULL,
    [CausedBy]           [nvarchar](max) NULL,
    [CausedByReference]  [nvarchar](max) NULL,
    [From]               [datetime]      NULL,
    [OrganizationId]     [nvarchar](100) NULL,
    [To]                 [datetime]      NULL,
) ON [PRIMARY]
GO

CREATE INDEX Id
    ON [dbo].[Unavailability]
        (
         [Id]
            );
CREATE INDEX OrganizationId
    ON [dbo].[Unavailability]
        (
         [OrganizationId]
            );

CREATE TABLE [dbo].[UserProfile]
(
    [Id]                    [nvarchar](100) NOT NULL,
    [LastPersistedAtUtc]    [datetime]      NULL,
    [IsDeleted]             [bit]           NULL,
    [AvatarImageId]         [nvarchar](100) NULL,
    [AvatarUrl]             [nvarchar](max) NULL,
    [CountryCode]           [nvarchar](max) NULL,
    [DefaultOrganizationId] [nvarchar](100) NULL,
    [DisplayName]           [nvarchar](max) NULL,
    [EmailAddress]          [nvarchar](450) NULL,
    [FirstName]             [nvarchar](max) NULL,
    [LastName]              [nvarchar](max) NULL,
    [PhoneNumber]           [nvarchar](max) NULL,
    [Timezone]              [nvarchar](max) NULL,
    [Type]                  [nvarchar](max) NULL,
    [UserId]                [nvarchar](100) NULL,
) ON [PRIMARY]
GO

CREATE INDEX Id
    ON [dbo].[UserProfile]
        (
         [Id]
            );
CREATE INDEX AvatarImageId
    ON [dbo].[UserProfile]
        (
         [AvatarImageId]
            );
CREATE INDEX EmailAddress
    ON [dbo].[UserProfile]
        (
         [EmailAddress]
            );
CREATE INDEX UserId
    ON [dbo].[UserProfile]
        (
         [UserId]
            );

CREATE TABLE [dbo].[WebhookNotificationAudits]
(
    [Id]                 [nvarchar](100) NOT NULL,
    [LastPersistedAtUtc] [datetime]      NULL,
    [IsDeleted]          [bit]           NULL,
    [EventId]            [nvarchar](max) NULL,
    [EventType]          [nvarchar](max) NULL,
    [JsonContent]        [nvarchar](max) NULL,
    [Source]             [nvarchar](max) NULL,
    [Status]             [nvarchar](max) NULL,
) ON [PRIMARY]
GO

CREATE INDEX Id
    ON [dbo].[WebhookNotificationAudits]
        (
         [Id]
            );