CREATE TABLE [dbo].[Board] (
    [Id]             INT      IDENTITY (1, 1) NOT NULL,
    [DealerPlayerId] INT      NULL,
    [TrumpSuitId]    INT      NULL,
    [GameId]         INT      NOT NULL,
    [Team1Wager]     INT      NULL,
    [Team2Wager]     INT      NULL,
    [DateTime]       DATETIME CONSTRAINT [DF_Board_DateTime] DEFAULT (getdate()) NOT NULL,
    CONSTRAINT [PK__Board__3214EC073D786CAE] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_Board_Game] FOREIGN KEY ([GameId]) REFERENCES [dbo].[Game] ([Id]),
    CONSTRAINT [FK_Board_Player] FOREIGN KEY ([DealerPlayerId]) REFERENCES [dbo].[Player] ([Id]),
    CONSTRAINT [FK_Board_Suit] FOREIGN KEY ([TrumpSuitId]) REFERENCES [dbo].[Suit] ([Id])
);











