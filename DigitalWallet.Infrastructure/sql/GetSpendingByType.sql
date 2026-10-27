CREATE OR ALTER PROCEDURE dbo.GetSpendingByType
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

    IF @StartUtc IS NULL
       OR @EndUtc IS NULL
       OR @StartUtc >= @EndUtc
    BEGIN
        ;THROW 50002, 'A valid reporting period is required.', 1;
    END;

    SELECT
        t.[Type] AS [Type],
        COUNT_BIG(*) AS TransactionCount,
        SUM(t.Amount) AS TotalAmount
    FROM dbo.Transactions AS t
    INNER JOIN dbo.Wallets AS w
        ON w.Id = t.WalletId
    WHERE
        w.UserId = @UserId
        AND t.Status = 'Completed'
        AND t.Direction = 'Outgoing'
        AND t.[Type] IN ('Withdrawal', 'Transfer')
        AND t.CreatedAt >= @StartUtc
        AND t.CreatedAt < @EndUtc
    GROUP BY t.[Type]
    ORDER BY t.[Type];
END;