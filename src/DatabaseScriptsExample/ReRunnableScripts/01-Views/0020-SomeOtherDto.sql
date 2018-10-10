IF OBJECT_ID(N'SomeOtherDto') IS NOT NULL
DROP VIEW SomeOtherDto
GO

create view SomeOtherDto
as
select 
    *
from SomeOtherTable
