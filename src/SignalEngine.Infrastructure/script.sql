IF SCHEMA_ID(N'identity') IS NULL EXEC(N'CREATE SCHEMA [identity];');
GO


CREATE TABLE [AspNetRoles] (
    [Id] nvarchar(450) NOT NULL,
    [Description] nvarchar(500) NULL,
    [CreatedAt] datetime2 NOT NULL,
    [Name] nvarchar(256) NULL,
    [NormalizedName] nvarchar(256) NULL,
    [ConcurrencyStamp] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetRoles] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [LookupTypes] (
    [Id] int NOT NULL IDENTITY,
    [Code] nvarchar(50) NOT NULL,
    [Description] nvarchar(200) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [CreatedBy] int NULL,
    [ModifiedAt] datetime2 NULL,
    [ModifiedBy] int NULL,
    CONSTRAINT [PK_LookupTypes] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [identity].[OpenIddictApplications] (
    [Id] nvarchar(450) NOT NULL,
    [ApplicationType] nvarchar(max) NULL,
    [ClientId] nvarchar(max) NULL,
    [ClientSecret] nvarchar(max) NULL,
    [ClientType] nvarchar(max) NULL,
    [ConcurrencyToken] nvarchar(max) NULL,
    [ConsentType] nvarchar(max) NULL,
    [DisplayName] nvarchar(max) NULL,
    [DisplayNames] nvarchar(max) NULL,
    [JsonWebKeySet] nvarchar(max) NULL,
    [Permissions] nvarchar(max) NULL,
    [PostLogoutRedirectUris] nvarchar(max) NULL,
    [Properties] nvarchar(max) NULL,
    [RedirectUris] nvarchar(max) NULL,
    [Requirements] nvarchar(max) NULL,
    [Settings] nvarchar(max) NULL,
    CONSTRAINT [PK_OpenIddictApplications] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [identity].[OpenIddictScopes] (
    [Id] nvarchar(450) NOT NULL,
    [ConcurrencyToken] nvarchar(max) NULL,
    [Description] nvarchar(max) NULL,
    [Descriptions] nvarchar(max) NULL,
    [DisplayName] nvarchar(max) NULL,
    [DisplayNames] nvarchar(max) NULL,
    [Name] nvarchar(max) NULL,
    [Properties] nvarchar(max) NULL,
    [Resources] nvarchar(max) NULL,
    CONSTRAINT [PK_OpenIddictScopes] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [AspNetRoleClaims] (
    [Id] int NOT NULL IDENTITY,
    [RoleId] nvarchar(450) NOT NULL,
    [ClaimType] nvarchar(max) NULL,
    [ClaimValue] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetRoleClaims] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AspNetRoleClaims_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [LookupValues] (
    [Id] int NOT NULL IDENTITY,
    [LookupTypeId] int NOT NULL,
    [Code] nvarchar(50) NOT NULL,
    [Name] nvarchar(100) NOT NULL,
    [SortOrder] int NOT NULL,
    [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
    [CreatedAt] datetime2 NOT NULL,
    [CreatedBy] int NULL,
    [ModifiedAt] datetime2 NULL,
    [ModifiedBy] int NULL,
    CONSTRAINT [PK_LookupValues] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_LookupValues_LookupTypes_LookupTypeId] FOREIGN KEY ([LookupTypeId]) REFERENCES [LookupTypes] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [identity].[OpenIddictAuthorizations] (
    [Id] nvarchar(450) NOT NULL,
    [ApplicationId] nvarchar(450) NULL,
    [ConcurrencyToken] nvarchar(max) NULL,
    [CreationDate] datetime2 NULL,
    [Properties] nvarchar(max) NULL,
    [Scopes] nvarchar(max) NULL,
    [Status] nvarchar(max) NULL,
    [Subject] nvarchar(max) NULL,
    [Type] nvarchar(max) NULL,
    CONSTRAINT [PK_OpenIddictAuthorizations] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_OpenIddictAuthorizations_OpenIddictApplications_ApplicationId] FOREIGN KEY ([ApplicationId]) REFERENCES [identity].[OpenIddictApplications] ([Id])
);
GO


CREATE TABLE [Plans] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(100) NOT NULL,
    [PlanCodeId] int NOT NULL,
    [MaxRules] int NOT NULL,
    [MaxAssets] int NOT NULL,
    [MaxNotificationsPerDay] int NOT NULL,
    [AllowWebhook] bit NOT NULL,
    [AllowSlack] bit NOT NULL,
    [MonthlyPrice] decimal(18,2) NOT NULL,
    [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
    [CreatedAt] datetime2 NOT NULL,
    [CreatedBy] int NULL,
    [ModifiedAt] datetime2 NULL,
    [ModifiedBy] int NULL,
    CONSTRAINT [PK_Plans] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Plans_LookupValues_PlanCodeId] FOREIGN KEY ([PlanCodeId]) REFERENCES [LookupValues] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [identity].[OpenIddictTokens] (
    [Id] nvarchar(450) NOT NULL,
    [ApplicationId] nvarchar(450) NULL,
    [AuthorizationId] nvarchar(450) NULL,
    [ConcurrencyToken] nvarchar(max) NULL,
    [CreationDate] datetime2 NULL,
    [ExpirationDate] datetime2 NULL,
    [Payload] nvarchar(max) NULL,
    [Properties] nvarchar(max) NULL,
    [RedemptionDate] datetime2 NULL,
    [ReferenceId] nvarchar(max) NULL,
    [Status] nvarchar(max) NULL,
    [Subject] nvarchar(max) NULL,
    [Type] nvarchar(max) NULL,
    CONSTRAINT [PK_OpenIddictTokens] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_OpenIddictTokens_OpenIddictApplications_ApplicationId] FOREIGN KEY ([ApplicationId]) REFERENCES [identity].[OpenIddictApplications] ([Id]),
    CONSTRAINT [FK_OpenIddictTokens_OpenIddictAuthorizations_AuthorizationId] FOREIGN KEY ([AuthorizationId]) REFERENCES [identity].[OpenIddictAuthorizations] ([Id])
);
GO


CREATE TABLE [Tenants] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(200) NOT NULL,
    [Subdomain] nvarchar(100) NOT NULL,
    [TenantTypeId] int NOT NULL,
    [PlanId] int NOT NULL,
    [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
    [DefaultNotificationEmail] nvarchar(256) NULL,
    [CreatedAt] datetime2 NOT NULL,
    [CreatedBy] int NULL,
    [ModifiedAt] datetime2 NULL,
    [ModifiedBy] int NULL,
    CONSTRAINT [PK_Tenants] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Tenants_LookupValues_TenantTypeId] FOREIGN KEY ([TenantTypeId]) REFERENCES [LookupValues] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Tenants_Plans_PlanId] FOREIGN KEY ([PlanId]) REFERENCES [Plans] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [AspNetUsers] (
    [Id] nvarchar(450) NOT NULL,
    [TenantId] int NOT NULL,
    [FirstName] nvarchar(100) NULL,
    [LastName] nvarchar(100) NULL,
    [CreatedAt] datetime2 NOT NULL,
    [LastLoginAt] datetime2 NULL,
    [IsActive] bit NOT NULL,
    [UserName] nvarchar(256) NULL,
    [NormalizedUserName] nvarchar(256) NULL,
    [Email] nvarchar(256) NULL,
    [NormalizedEmail] nvarchar(256) NULL,
    [EmailConfirmed] bit NOT NULL,
    [PasswordHash] nvarchar(max) NULL,
    [SecurityStamp] nvarchar(max) NULL,
    [ConcurrencyStamp] nvarchar(max) NULL,
    [PhoneNumber] nvarchar(max) NULL,
    [PhoneNumberConfirmed] bit NOT NULL,
    [TwoFactorEnabled] bit NOT NULL,
    [LockoutEnd] datetimeoffset NULL,
    [LockoutEnabled] bit NOT NULL,
    [AccessFailedCount] int NOT NULL,
    CONSTRAINT [PK_AspNetUsers] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AspNetUsers_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [Assets] (
    [Id] int NOT NULL IDENTITY,
    [TenantId] int NOT NULL,
    [Name] nvarchar(200) NOT NULL,
    [Identifier] nvarchar(500) NOT NULL,
    [AssetTypeId] int NOT NULL,
    [DataSourceId] int NOT NULL,
    [Description] nvarchar(1000) NULL,
    [Metadata] nvarchar(max) NULL,
    [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
    [IngestionIntervalSeconds] int NOT NULL DEFAULT 60,
    [LastIngestedAtUtc] datetime2 NULL,
    [NextIngestionAtUtc] datetime2 NULL,
    [CreatedAt] datetime2 NOT NULL,
    [CreatedBy] int NULL,
    [ModifiedAt] datetime2 NULL,
    [ModifiedBy] int NULL,
    CONSTRAINT [PK_Assets] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Assets_LookupValues_AssetTypeId] FOREIGN KEY ([AssetTypeId]) REFERENCES [LookupValues] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Assets_LookupValues_DataSourceId] FOREIGN KEY ([DataSourceId]) REFERENCES [LookupValues] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Assets_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [AspNetUserClaims] (
    [Id] int NOT NULL IDENTITY,
    [UserId] nvarchar(450) NOT NULL,
    [ClaimType] nvarchar(max) NULL,
    [ClaimValue] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetUserClaims] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AspNetUserClaims_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [AspNetUserLogins] (
    [LoginProvider] nvarchar(450) NOT NULL,
    [ProviderKey] nvarchar(450) NOT NULL,
    [ProviderDisplayName] nvarchar(max) NULL,
    [UserId] nvarchar(450) NOT NULL,
    CONSTRAINT [PK_AspNetUserLogins] PRIMARY KEY ([LoginProvider], [ProviderKey]),
    CONSTRAINT [FK_AspNetUserLogins_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [AspNetUserRoles] (
    [UserId] nvarchar(450) NOT NULL,
    [RoleId] nvarchar(450) NOT NULL,
    CONSTRAINT [PK_AspNetUserRoles] PRIMARY KEY ([UserId], [RoleId]),
    CONSTRAINT [FK_AspNetUserRoles_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_AspNetUserRoles_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [AspNetUserTokens] (
    [UserId] nvarchar(450) NOT NULL,
    [LoginProvider] nvarchar(450) NOT NULL,
    [Name] nvarchar(450) NOT NULL,
    [Value] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetUserTokens] PRIMARY KEY ([UserId], [LoginProvider], [Name]),
    CONSTRAINT [FK_AspNetUserTokens_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [Metrics] (
    [Id] int NOT NULL IDENTITY,
    [TenantId] int NOT NULL,
    [AssetId] int NOT NULL,
    [Name] nvarchar(100) NOT NULL,
    [MetricTypeId] int NOT NULL,
    [Unit] nvarchar(50) NULL,
    [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
    [CreatedAt] datetime2 NOT NULL,
    [CreatedBy] int NULL,
    [ModifiedAt] datetime2 NULL,
    [ModifiedBy] int NULL,
    CONSTRAINT [PK_Metrics] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Metrics_Assets_AssetId] FOREIGN KEY ([AssetId]) REFERENCES [Assets] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Metrics_LookupValues_MetricTypeId] FOREIGN KEY ([MetricTypeId]) REFERENCES [LookupValues] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Metrics_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [Rules] (
    [Id] int NOT NULL IDENTITY,
    [TenantId] int NOT NULL,
    [AssetId] int NOT NULL,
    [Name] nvarchar(200) NOT NULL,
    [Description] nvarchar(1000) NULL,
    [MetricName] nvarchar(100) NOT NULL,
    [OperatorId] int NOT NULL,
    [Threshold] decimal(18,6) NOT NULL,
    [SeverityId] int NOT NULL,
    [EvaluationFrequencyId] int NOT NULL,
    [ConsecutiveBreachesRequired] int NOT NULL DEFAULT 1,
    [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
    [CreatedAt] datetime2 NOT NULL,
    [CreatedBy] int NULL,
    [ModifiedAt] datetime2 NULL,
    [ModifiedBy] int NULL,
    CONSTRAINT [PK_Rules] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Rules_Assets_AssetId] FOREIGN KEY ([AssetId]) REFERENCES [Assets] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Rules_LookupValues_EvaluationFrequencyId] FOREIGN KEY ([EvaluationFrequencyId]) REFERENCES [LookupValues] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Rules_LookupValues_OperatorId] FOREIGN KEY ([OperatorId]) REFERENCES [LookupValues] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Rules_LookupValues_SeverityId] FOREIGN KEY ([SeverityId]) REFERENCES [LookupValues] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Rules_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [MetricData] (
    [Id] int NOT NULL IDENTITY,
    [TenantId] int NOT NULL,
    [MetricId] int NOT NULL,
    [Value] decimal(18,6) NOT NULL,
    [Timestamp] datetime2 NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_MetricData] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_MetricData_Metrics_MetricId] FOREIGN KEY ([MetricId]) REFERENCES [Metrics] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_MetricData_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [Signals] (
    [Id] int NOT NULL IDENTITY,
    [TenantId] int NOT NULL,
    [RuleId] int NOT NULL,
    [AssetId] int NOT NULL,
    [SignalStatusId] int NOT NULL,
    [Title] nvarchar(500) NOT NULL,
    [Description] nvarchar(2000) NULL,
    [TriggerValue] decimal(18,6) NOT NULL,
    [ThresholdValue] decimal(18,6) NOT NULL,
    [TriggeredAt] datetime2 NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [CreatedBy] int NULL,
    [ModifiedAt] datetime2 NULL,
    [ModifiedBy] int NULL,
    CONSTRAINT [PK_Signals] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Signals_Assets_AssetId] FOREIGN KEY ([AssetId]) REFERENCES [Assets] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Signals_LookupValues_SignalStatusId] FOREIGN KEY ([SignalStatusId]) REFERENCES [LookupValues] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Signals_Rules_RuleId] FOREIGN KEY ([RuleId]) REFERENCES [Rules] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Signals_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [SignalStates] (
    [Id] int NOT NULL IDENTITY,
    [TenantId] int NOT NULL,
    [RuleId] int NOT NULL,
    [ConsecutiveBreaches] int NOT NULL DEFAULT 0,
    [LastEvaluatedAt] datetime2 NOT NULL,
    [LastMetricValue] decimal(18,6) NULL,
    [IsBreached] bit NOT NULL DEFAULT CAST(0 AS bit),
    [CreatedAt] datetime2 NOT NULL,
    [CreatedBy] int NULL,
    [ModifiedAt] datetime2 NULL,
    [ModifiedBy] int NULL,
    CONSTRAINT [PK_SignalStates] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_SignalStates_Rules_RuleId] FOREIGN KEY ([RuleId]) REFERENCES [Rules] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_SignalStates_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [Notifications] (
    [Id] int NOT NULL IDENTITY,
    [TenantId] int NOT NULL,
    [SignalId] int NOT NULL,
    [ChannelTypeId] int NOT NULL,
    [Recipient] nvarchar(500) NOT NULL,
    [Subject] nvarchar(500) NOT NULL,
    [Body] nvarchar(max) NOT NULL,
    [SentAt] datetime2 NULL,
    [IsSent] bit NOT NULL DEFAULT CAST(0 AS bit),
    [ErrorMessage] nvarchar(2000) NULL,
    [RetryCount] int NOT NULL DEFAULT 0,
    [CreatedAt] datetime2 NOT NULL,
    [CreatedBy] int NULL,
    [ModifiedAt] datetime2 NULL,
    [ModifiedBy] int NULL,
    CONSTRAINT [PK_Notifications] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Notifications_LookupValues_ChannelTypeId] FOREIGN KEY ([ChannelTypeId]) REFERENCES [LookupValues] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Notifications_Signals_SignalId] FOREIGN KEY ([SignalId]) REFERENCES [Signals] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Notifications_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [SignalResolutions] (
    [Id] int NOT NULL IDENTITY,
    [TenantId] int NOT NULL,
    [SignalId] int NOT NULL,
    [ResolutionStatusId] int NOT NULL,
    [ResolvedByUserId] int NOT NULL,
    [Notes] nvarchar(2000) NULL,
    [ResolvedAt] datetime2 NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [CreatedBy] int NULL,
    [ModifiedAt] datetime2 NULL,
    [ModifiedBy] int NULL,
    CONSTRAINT [PK_SignalResolutions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_SignalResolutions_LookupValues_ResolutionStatusId] FOREIGN KEY ([ResolutionStatusId]) REFERENCES [LookupValues] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_SignalResolutions_Signals_SignalId] FOREIGN KEY ([SignalId]) REFERENCES [Signals] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_SignalResolutions_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION
);
GO


CREATE INDEX [IX_AspNetRoleClaims_RoleId] ON [AspNetRoleClaims] ([RoleId]);
GO


CREATE UNIQUE INDEX [RoleNameIndex] ON [AspNetRoles] ([NormalizedName]) WHERE [NormalizedName] IS NOT NULL;
GO


CREATE INDEX [IX_AspNetUserClaims_UserId] ON [AspNetUserClaims] ([UserId]);
GO


CREATE INDEX [IX_AspNetUserLogins_UserId] ON [AspNetUserLogins] ([UserId]);
GO


CREATE INDEX [IX_AspNetUserRoles_RoleId] ON [AspNetUserRoles] ([RoleId]);
GO


CREATE INDEX [EmailIndex] ON [AspNetUsers] ([NormalizedEmail]);
GO


CREATE INDEX [IX_AspNetUsers_TenantId] ON [AspNetUsers] ([TenantId]);
GO


CREATE UNIQUE INDEX [UserNameIndex] ON [AspNetUsers] ([NormalizedUserName]) WHERE [NormalizedUserName] IS NOT NULL;
GO


CREATE INDEX [IX_Assets_AssetTypeId] ON [Assets] ([AssetTypeId]);
GO


CREATE INDEX [IX_Assets_DataSource_Identifier] ON [Assets] ([DataSourceId], [Identifier]);
GO


CREATE INDEX [IX_Assets_Ingestion_Due] ON [Assets] ([IsActive], [NextIngestionAtUtc]);
GO


CREATE INDEX [IX_Assets_TenantId] ON [Assets] ([TenantId]);
GO


CREATE INDEX [IX_Assets_TenantId_DataSourceId] ON [Assets] ([TenantId], [DataSourceId]);
GO


CREATE UNIQUE INDEX [IX_Assets_TenantId_Identifier] ON [Assets] ([TenantId], [Identifier]);
GO


CREATE UNIQUE INDEX [IX_LookupTypes_Code] ON [LookupTypes] ([Code]);
GO


CREATE UNIQUE INDEX [IX_LookupValues_LookupTypeId_Code] ON [LookupValues] ([LookupTypeId], [Code]);
GO


CREATE INDEX [IX_MetricData_MetricId_Timestamp] ON [MetricData] ([MetricId], [Timestamp] DESC);
GO


CREATE INDEX [IX_MetricData_TenantId] ON [MetricData] ([TenantId]);
GO


CREATE INDEX [IX_MetricData_Timestamp] ON [MetricData] ([Timestamp] DESC);
GO


CREATE UNIQUE INDEX [IX_Metrics_AssetId_Name] ON [Metrics] ([AssetId], [Name]);
GO


CREATE INDEX [IX_Metrics_MetricTypeId] ON [Metrics] ([MetricTypeId]);
GO


CREATE INDEX [IX_Metrics_TenantId] ON [Metrics] ([TenantId]);
GO


CREATE INDEX [IX_Notifications_ChannelTypeId] ON [Notifications] ([ChannelTypeId]);
GO


CREATE INDEX [IX_Notifications_SignalId] ON [Notifications] ([SignalId]);
GO


CREATE INDEX [IX_Notifications_TenantId] ON [Notifications] ([TenantId]);
GO


CREATE INDEX [IX_Notifications_TenantId_IsSent_CreatedAt] ON [Notifications] ([TenantId], [IsSent], [CreatedAt]);
GO


CREATE INDEX [IX_OpenIddictAuthorizations_ApplicationId] ON [identity].[OpenIddictAuthorizations] ([ApplicationId]);
GO


CREATE INDEX [IX_OpenIddictTokens_ApplicationId] ON [identity].[OpenIddictTokens] ([ApplicationId]);
GO


CREATE INDEX [IX_OpenIddictTokens_AuthorizationId] ON [identity].[OpenIddictTokens] ([AuthorizationId]);
GO


CREATE INDEX [IX_Plans_PlanCodeId] ON [Plans] ([PlanCodeId]);
GO


CREATE INDEX [IX_Rules_AssetId] ON [Rules] ([AssetId]);
GO


CREATE INDEX [IX_Rules_EvaluationFrequencyId] ON [Rules] ([EvaluationFrequencyId]);
GO


CREATE INDEX [IX_Rules_IsActive] ON [Rules] ([IsActive]);
GO


CREATE INDEX [IX_Rules_OperatorId] ON [Rules] ([OperatorId]);
GO


CREATE INDEX [IX_Rules_SeverityId] ON [Rules] ([SeverityId]);
GO


CREATE INDEX [IX_Rules_TenantId] ON [Rules] ([TenantId]);
GO


CREATE INDEX [IX_SignalResolutions_ResolutionStatusId] ON [SignalResolutions] ([ResolutionStatusId]);
GO


CREATE UNIQUE INDEX [IX_SignalResolutions_SignalId] ON [SignalResolutions] ([SignalId]);
GO


CREATE INDEX [IX_SignalResolutions_TenantId] ON [SignalResolutions] ([TenantId]);
GO


CREATE INDEX [IX_Signals_AssetId] ON [Signals] ([AssetId]);
GO


CREATE INDEX [IX_Signals_RuleId] ON [Signals] ([RuleId]);
GO


CREATE INDEX [IX_Signals_SignalStatusId] ON [Signals] ([SignalStatusId]);
GO


CREATE INDEX [IX_Signals_TenantId] ON [Signals] ([TenantId]);
GO


CREATE INDEX [IX_Signals_TenantId_TriggeredAt] ON [Signals] ([TenantId], [TriggeredAt] DESC);
GO


CREATE UNIQUE INDEX [IX_SignalStates_RuleId] ON [SignalStates] ([RuleId]);
GO


CREATE INDEX [IX_SignalStates_TenantId] ON [SignalStates] ([TenantId]);
GO


CREATE INDEX [IX_Tenants_PlanId] ON [Tenants] ([PlanId]);
GO


CREATE UNIQUE INDEX [IX_Tenants_Subdomain] ON [Tenants] ([Subdomain]);
GO


CREATE INDEX [IX_Tenants_TenantTypeId] ON [Tenants] ([TenantTypeId]);
GO


