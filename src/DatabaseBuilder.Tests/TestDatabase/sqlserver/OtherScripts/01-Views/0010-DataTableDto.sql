IF OBJECT_ID(N'DataTableDto') IS NOT NULL
DROP VIEW DataTableDto
go

create view DataTableDto
as
select 
    Id
    , [Text]
from DataTable
