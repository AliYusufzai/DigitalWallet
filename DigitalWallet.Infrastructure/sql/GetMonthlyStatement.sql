CREATE OR ALTER PROCEDURE dbo.GetMonthlyStatement
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

    -- Materialize once so totals and transaction rows use the same data.
    SELECT t.Id, t.ReferenceNumber, t.[Type], t.Direction, t.Amount,
        t.Description, t.CreatedAt,
        CASE WHEN t.Direction = 'Incoming' THEN t.Amount ELSE -t.Amount END AS SignedAmount
    INTO #Ledger
    FROM dbo.Transactions AS t
    INNER JOIN dbo.Wallets AS w ON w.Id = t.WalletId
    WHERE w.UserId = @UserId AND t.Status = 'Completed'
        AND t.Direction IN ('Incoming', 'Outgoing')
        AND t.CreatedAt < @EndUtc;

    SELECT
        COALESCE(SUM(CASE WHEN CreatedAt < @StartUtc THEN SignedAmount ELSE 0 END), 0) AS OpeningBalance,
        COALESCE(SUM(CASE WHEN CreatedAt >= @StartUtc AND Direction = 'Incoming' THEN Amount ELSE 0 END), 0) AS TotalIncoming,
        COALESCE(SUM(CASE WHEN CreatedAt >= @StartUtc AND Direction = 'Outgoing' THEN Amount ELSE 0 END), 0) AS TotalOutgoing,
        COALESCE(SUM(SignedAmount), 0) AS ClosingBalance
    FROM #Ledger;

    ;WITH RunningLedger AS
    (
        SELECT Id, ReferenceNumber, [Type], Direction, Amount, Description, CreatedAt,
            SUM(SignedAmount) OVER (
                ORDER BY CreatedAt, Id ROWS UNBOUNDED PRECEDING) AS BalanceAfter
        FROM #Ledger
    )
    SELECT Id, ReferenceNumber, [Type], Direction, Amount, Description,
        CreatedAt AS CreatedAtUtc, BalanceAfter
    FROM RunningLedger
    WHERE CreatedAt >= @StartUtc
    ORDER BY CreatedAt, Id;
END;
