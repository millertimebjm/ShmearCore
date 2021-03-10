CREATE TABLE [dbo].[Player] (
    [Id]           INT            IDENTITY (1, 1) NOT NULL,
    [Name]         VARCHAR (50)   NOT NULL,
    [ConnectionId] VARCHAR (1000) NOT NULL,
    [KeepAlive]    DATETIME       NOT NULL,
    CONSTRAINT [PK__Player__3214EC0779BF3402] PRIMARY KEY CLUSTERED ([Id] ASC)
);



