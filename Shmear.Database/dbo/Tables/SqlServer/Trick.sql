CREATE TABLE [dbo].[Trick] (
    [Id]              INT      IDENTITY (1, 1) NOT NULL,
    [GameId]          INT      NOT NULL,
    [WinningPlayerId] INT      NULL,
    [Sequence]        INT      NOT NULL,
    [CreatedDate]     DATETIME NOT NULL,
    [CompletedDate]   DATETIME NULL,
    CONSTRAINT [PK__Trick__3214EC076A315D17] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_Trick_Game] FOREIGN KEY ([GameId]) REFERENCES [dbo].[Game] ([Id]),
    CONSTRAINT [FK_Trick_Player] FOREIGN KEY ([WinningPlayerId]) REFERENCES [dbo].[Player] ([Id])
);





