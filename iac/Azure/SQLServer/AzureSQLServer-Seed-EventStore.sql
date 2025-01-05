-- To be used to use your SQL database as the EventStore for all event-sourcing aggregates.
--
-- Note: Column Definitions:
--     We are deliberately defining most textual columns (in most of the read-model tables) as "string nvarchar(max)" by default
--       this is to allow for future expansion of the content of the column as you code evolves, without having to change the sizes of the columns.
--       you are free to modify that default to some nominal value (across the board) as you wish (i.e. "nvarchar(800)").
--       columns with indexes cannot be "nvarchar(max)" because of the index size limit of 900 bytes.
--     We are only specifically using other datatypes, where we know they are very un-likely to change over time.
--       if you want to be more specific about data types and want to optimize column design early, you will need to be very careful not to change the code in the future.
--       we recommend optimizing for change, rather than optimizing for performance, until your product has matured and fully developed, or has scaled dramatically.
--       most read-model columns will change from string to JSON(ValueObject) as things change in your domain models, so limiting them too early to specific datatypes can backfire later on in production workloads.
--     We are deliberately defining most columns as NULL for the same reason. To avoid, as much as possible, having to make changes to the schema in the future, when the code changes.
--       clearly there are limits to this strategy, so this strategy is simply minimizing them, since we don't care at this stage about optimizing SQL storage in the cloud (i.e. no longer depend on spinning hard disks, tracks and sectors).  
--
-- noinspection SqlDialectInspectionForFile

USE
    [saastack-sqldatabase]
GO

IF EXISTS(SELECT *
          FROM sys.objects
          WHERE object_id = OBJECT_ID(N'[dbo].[EventStore]')
            AND type in (N'U'))
    DROP TABLE [dbo].[EventStore]
GO

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

--

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