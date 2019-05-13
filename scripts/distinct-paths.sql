-- Run this with LogParser:
-- LogParser -i:CSV file:"C:\Users\peter\Projects\ToDoCommentAnalyzer\scripts\distinct-paths.sql" -o:CSV
-- Adjust the paths to match your locations
--
-- I used this script because the formula to find the number of unique paths in Excel was too slow
select distinct [Path]
into C:\Users\peter\Projects\ToDoCommentAnalyzer\data\distinct-paths.csv
from C:\Users\peter\Projects\ToDoCommentAnalyzer\data\bq-results-20190409-112119-si88464mntq-results(2).csv
