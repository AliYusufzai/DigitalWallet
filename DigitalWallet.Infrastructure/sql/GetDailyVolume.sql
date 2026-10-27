CREATE OR ALTER PROCEDURE dbo.GetDailyVolume
    @UserId INT,
    @StartUtc DATETIME2,
    @EndUtc DATETIME2
AS
BEGIN
    SET NOCOUNT ON;
    IF @UserId IS NULL OR @UserId <= 0
    BEGIN
        ;THROW 50001, 'A valid user ID is required.', 1;
    END;
    IF @StartUtc IS NULL OR @EndUtc IS NULL OR @StartUtc >= @EndUtc
    BEGIN
        ;THROW 50002, 'A valid reporting period is required.', 1;
    END;

    SELECT
        CONVERT(date, t.CreatedAt AT TIME ZONE 'UTC'
            AT TIME ZONE 'Pakistan Standard Time') AS [Date],
        COUNT_BIG(*) AS TransactionCount,
        SUM(CASE WHEN t.Direction = 'Incoming' THEN t.Amount ELSE 0 END) AS IncomingAmount,
        SUM(CASE WHEN t.Direction = 'Outgoing' THEN t.Amount ELSE 0 END) AS OutgoingAmount
    FROM dbo.Transactions AS t
    INNER JOIN dbo.Wallets AS w ON w.Id = t.WalletId
    WHERE w.UserId = @UserId
        AND t.Status = 'Completed'
        AND t.Direction IN ('Incoming', 'Outgoing')
        AND t.CreatedAt >= @StartUtc AND t.CreatedAt < @EndUtc
    GROUP BY CONVERT(date, t.CreatedAt AT TIME ZONE 'UTC'
        AT TIME ZONE 'Pakistan Standard Time')
    ORDER BY [Date];
END;
