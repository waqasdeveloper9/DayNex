-- Read-only checks. Run each section against the corresponding service database.

-- Identity database: subscription rows that would violate the new foreign key.
SELECT s.Id, s.UserId
FROM dbo.Subscriptions AS s
LEFT JOIN dbo.Users AS u ON u.Id = s.UserId
WHERE u.Id IS NULL;

-- Identity database: legacy, unverified identifiers require explicit reconciliation.
SELECT Id, ExternalId
FROM dbo.Users
WHERE LEN(ExternalId) <> 73
   OR TRY_CONVERT(uniqueidentifier, LEFT(ExternalId, 36)) IS NULL
   OR SUBSTRING(ExternalId, 37, 1) <> ':'
   OR TRY_CONVERT(uniqueidentifier, RIGHT(ExternalId, 36)) IS NULL;

-- Holiday database: duplicates must be reviewed before adding the unique index.
SELECT Region, Date, Title COLLATE Latin1_General_100_BIN2 AS Title, COUNT(*) AS DuplicateCount
FROM dbo.BankHoliday
GROUP BY Region, Date, Title COLLATE Latin1_General_100_BIN2
HAVING COUNT(*) > 1;
