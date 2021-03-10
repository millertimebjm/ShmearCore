CREATE TABLE [dbo].[GamePlayer] (
    [Id]         INT IDENTITY (1, 1) NOT NULL,
    [PlayerId]   INT NOT NULL,
    [GameId]     INT NOT NULL,
    [SeatNumber] INT NOT NULL,
    [Wager]      INT NULL,
    [Ready]      BIT NOT NULL,
    CONSTRAINT [PK__GamePlay__3214EC07C6A01F52] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_GamePlayer_Game] FOREIGN KEY ([GameId]) REFERENCES [dbo].[Game] ([Id]),
    CONSTRAINT [FK_GamePlayer_Player] FOREIGN KEY ([PlayerId]) REFERENCES [dbo].[Player] ([Id])
);









