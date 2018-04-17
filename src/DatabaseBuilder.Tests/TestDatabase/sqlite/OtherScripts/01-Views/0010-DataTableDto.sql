drop view if exists DataTableDto;

create view DataTableDto
as
select 
    Id
    , [Text]
from DataTable
