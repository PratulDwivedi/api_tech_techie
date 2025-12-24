CREATE PROCEDURE dbo.usp_ExecuteServiceSqlAsync
    @sqlCommand NVARCHAR(MAX),
    @requestId UNIQUEIDENTIFIER = NULL OUTPUT
AS
/*                                                                   
========================================================================================================                                                                      
Copyright : Tech-Techie, 2025                                                                     
Author  : Pratul Dwivedi                                                                      
Create date : 23/Dec/2025                                                                     
Description : Fire-and-forget async SQL execution via Service Broker                                                                
========================================================================================================                                                                      

-- Fire and forget - just queue it
EXEC dbo.usp_ExecuteServiceSqlAsync 
    @sqlCommand = N'EXEC dbo.usp_ProcessLargeDataSet';

-- Or get request ID for later tracking
DECLARE @reqId UNIQUEIDENTIFIER;
EXEC dbo.usp_ExecuteServiceSqlAsync 
    @sqlCommand = N'EXEC dbo.usp_ProcessLargeDataSet',
    @requestId = @reqId OUTPUT;
PRINT 'Queued with ID: ' + CAST(@reqId AS NVARCHAR(50));

========================================================================================================             
*/
BEGIN
    SET NOCOUNT ON;

    DECLARE @handle UNIQUEIDENTIFIER,
            @message_body XML,
            @is_broker_enabled BIT;

    -- Generate request ID if not provided
    IF @requestId IS NULL
        SET @requestId = NEWID();

    SELECT @is_broker_enabled = is_broker_enabled
    FROM sys.databases
    WHERE name = DB_NAME();

    IF @is_broker_enabled = 1
    BEGIN
        -- Build request XML
        SET @message_body =
        (
            SELECT '' AS Url,
                   'SQL' AS Method,
                   '' AS Headers,
                   '' AS ContentType,
                   @sqlCommand AS Body,
                   @requestId AS RequestId
            FOR XML PATH(''), ROOT('Request')
        );

        BEGIN TRY
            -- Start conversation
            BEGIN DIALOG @handle
            FROM SERVICE [HttpRequestService]
            TO SERVICE 'HttpResponseService'
            ON CONTRACT [HttpRequestContract]
            WITH ENCRYPTION = OFF;

            -- Send request and return immediately (fire-and-forget)
            SEND ON CONVERSATION @handle
            MESSAGE TYPE [HttpRequestMessage]
            (@message_body);

            -- No WAITFOR - just return
            -- The background service will process it and the stored procedure
            -- will handle success/failure tracking

        END TRY
        BEGIN CATCH
            IF @handle IS NOT NULL
                END CONVERSATION @handle
                WITH ERROR = 500 DESCRIPTION = 'Internal Error';
                
            -- Optionally re-throw or just return
            THROW;
        END CATCH;
    END;
    ELSE
    BEGIN
        RAISERROR('Service Broker is not enabled. Async execution requires Service Broker.', 16, 1);
    END;

END;
GO