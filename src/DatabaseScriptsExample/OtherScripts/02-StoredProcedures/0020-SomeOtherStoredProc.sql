IF EXISTS (SELECT * FROM SYSOBJECTS WHERE ID = OBJECT_ID(N'SomeOtherStoredProc') and OBJECTPROPERTY(id, N'IsProcedure') = 1)
DROP PROCEDURE SomeOtherStoredProc
GO

CREATE PROCEDURE SomeOtherStoredProc
(
    @someParam int
)
AS
BEGIN

-- stored procedure sql

END
GO
