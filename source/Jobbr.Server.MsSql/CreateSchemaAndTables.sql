CREATE SCHEMA [Jobbr]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [Jobbr].[JobRuns](
	[Id] [bigint] IDENTITY(1,1) NOT NULL,
	[JobId] [bigint] NOT NULL,
	[TriggerId] [bigint] NOT NULL,
	[JobParameters] [nvarchar](max) NULL,
	[InstanceParameters] [nvarchar](max) NULL,
	[Name] [nvarchar](50) NULL,
	[PlannedStartDateTimeUtc] [datetime2](7) NOT NULL,
	[ActualStartDateTimeUtc] [datetime2](7) NULL,
	[EstimatedEndDateTimeUtc] [datetime2](7) NULL,
	[ActualEndDateTimeUtc] [datetime2](7) NULL,
	[Progress] [float] NULL,
	[State] [nvarchar](15) NOT NULL,
	[Pid] [int] NULL,
  [Host] [nvarchar](100) NULL,
	[WorkingDir] [nvarchar](300) NULL,
	[TempDir] [nvarchar](300) NULL,
 CONSTRAINT [PK_JobRuns] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

GO
SET ANSI_PADDING OFF
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [Jobbr].[Jobs](
	[Id] [bigint] IDENTITY(1,1) NOT NULL,
	[UniqueName] [nvarchar](80) NOT NULL,
	[Title] [nvarchar](150) NULL,
	[Type] [nvarchar](200) NOT NULL,
	[Parameters] [nvarchar](max) NULL,
	[CreatedDateTimeUtc] [datetime2](7) NOT NULL,
	[UpdatedDateTimeUtc] [datetime2](7) NULL,
 CONSTRAINT [PK_Jobs] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

GO
SET ANSI_PADDING OFF
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [Jobbr].[Triggers](
	[Id] [bigint] IDENTITY(1,1) NOT NULL,
	[JobId] [bigint] NOT NULL,
	[TriggerType] [nvarchar](10) NOT NULL,
	[Definition] [nvarchar](20) NULL,
	[StartDateTimeUtc] [datetime2](7) NULL,
	[EndDateTimeUtc] [datetime2](7) NULL,
	[DelayedMinutes] [int] NULL,
	[IsActive] [bit] NOT NULL,
	[UserId] [nvarchar](100) NULL,
	[UserDisplayName] [nvarchar](100) NULL,
	[Parameters] [nvarchar](max) NULL,
	[Comment] [nvarchar](max) NULL,
	[CreatedDateTimeUtc] [datetime2](7) NOT NULL,
	[NoParallelExecution] [bit] DEFAULT 0 NOT NULL
 CONSTRAINT [PK_Triggers] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]