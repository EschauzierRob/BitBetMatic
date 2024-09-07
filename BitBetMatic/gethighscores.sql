WITH RankedThresholds AS (
    SELECT 
        [Id],
        [Strategy],
        [Market],
        [CreatedAt],
        [Highscore],
        ROW_NUMBER() OVER (PARTITION BY [Strategy], [Market] ORDER BY [CreatedAt] DESC) AS rn
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



	-- delete from IndicatorThresholds where Highscore > 1000