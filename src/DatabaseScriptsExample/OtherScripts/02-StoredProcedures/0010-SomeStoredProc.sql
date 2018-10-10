IF EXISTS (SELECT * FROM SYSOBJECTS WHERE ID = OBJECT_ID(N'SomeStoredProc') and OBJECTPROPERTY(id, N'IsProcedure') = 1)
DROP PROCEDURE SomeStoredProc
GO

CREATE PROCEDURE SomeStoredProc
(
    @someParam int
)
AS
BEGIN

-- stored procedure sql

END
GO
