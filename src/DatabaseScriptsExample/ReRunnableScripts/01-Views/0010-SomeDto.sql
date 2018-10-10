IF OBJECT_ID(N'SomeDto') IS NOT NULL
DROP VIEW SomeDto
GO

create view SomeDto
as
select 
    *
from SomeTable
