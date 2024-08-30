WITH RankedThresholds AS (
    SELECT 
        [Id],
        [Strategy],
        [Market],
        [CreatedAt],
        [Highscore],
        ROW_NUMBER() OVER (PARTITION BY [Strategy], [Market] ORDER BY [Highscore] DESC) AS rn
    FROM 
        [dbo].[IndicatorThresholds]
)
SELECT 
    [Id],
    [Strategy],
    [Market],
    [CreatedAt],
    [Highscore]
FROM 
    RankedThresholds
WHERE 
    rn = 1
ORDER BY 
    [Market],
    [Highscore] DESC;
