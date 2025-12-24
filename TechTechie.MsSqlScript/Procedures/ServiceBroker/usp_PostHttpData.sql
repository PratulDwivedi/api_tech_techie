CREATE OR ALTER PROCEDURE dbo.usp_PostHttpData
    @url NVARCHAR(MAX),
    @body NVARCHAR(MAX),
    @header NVARCHAR(MAX) = NULL,
    @response NVARCHAR(MAX) OUTPUT,
    @statusCode INT OUTPUT,
    @timeout INT = 100000
AS
/*                                                                   
========================================================================================================                                                                      
Copyright : Tech-Techie, 2025                                                                     
Author  : Pratul Dwivedi                                                                      
Create date : 16/Dec/2025                                                                     
Description : Service broker procedure to call the POST api                                                                
========================================================================================================                                                                      
DECLARE @response NVARCHAR(MAX),
        @statusCode INT;


EXEC dbo.usp_PostHttpData @url = N',                       -- nvarchar(500)
                          @body = N'{}',                      -- nvarchar(max)
                          @header = N'Authorization: Bearer eyJhbGciOiJSUzI1NiIsInR5cCI', 
                          @response = @response OUTPUT,     -- nvarchar(max)
                          @statusCode = @statusCode OUTPUT; 
                         
PRINT @response;
========================================================================================================             
Change Log:                                                                       
dd-MMM-yyyy, Name, #CCxx, Description     
=======================================================================================================                                                                                                                                 
*/
BEGIN
    SET NOCOUNT ON;

    DECLARE @handle UNIQUEIDENTIFIER,
            @message_body XML,
            @contentType NVARCHAR(100) = N'application/json',
            @message_type_name NVARCHAR(256),
            @is_broker_enabled BIT;

    SELECT @is_broker_enabled = is_broker_enabled
    FROM sys.databases
    WHERE name = DB_NAME();


    IF @is_broker_enabled = 1
    BEGIN
        -- Build request XML
        SET @message_body =
        (
            SELECT @url AS Url,
                   'POST' AS Method,
                   @header AS Headers,
                   @contentType AS ContentType,
                   @body AS Body,
                   NEWID() AS RequestId
            FOR XML PATH(''), ROOT('Request')
        );

        BEGIN TRY
            -- Start conversation
            BEGIN DIALOG @handle
            FROM SERVICE [HttpRequestService]
            TO SERVICE 'HttpResponseService'
            ON CONTRACT [HttpRequestContract]
            WITH ENCRYPTION = OFF;

            -- Send request
            SEND ON CONVERSATION @handle
            MESSAGE TYPE [HttpRequestMessage]
            (@message_body);

            -- Wait for response
            WAITFOR
            (
                RECEIVE TOP (1) @message_type_name = message_type_name,
                                @message_body = CAST(message_body AS XML)
                FROM dbo.HttpRequestQueue
                WHERE CONVERSATION_HANDLE = @handle
            ),
            TIMEOUT @timeout;

            -- Extract response
            IF @message_type_name = 'HttpResponseMessage'
               AND @message_body IS NOT NULL
            BEGIN
                SET @response = @message_body.value('(/Response/Body)[1]', 'NVARCHAR(MAX)');
                SET @statusCode = @message_body.value('(/Response/StatusCode)[1]', 'INT');
            END;
            ELSE
            BEGIN
                SET @response = 'ERROR: Timeout or no response received';
                SET @statusCode = -1;
            END;

            -- End conversation
            IF @handle IS NOT NULL
                END CONVERSATION @handle;

        END TRY
        BEGIN CATCH
            SET @response = 'ERROR: ' + ERROR_MESSAGE();
            SET @statusCode = -2;

            IF @handle IS NOT NULL
                END CONVERSATION @handle
                WITH ERROR = 500 DESCRIPTION = 'Internal Error';
        END CATCH;


    END;
    ELSE
    BEGIN
        SELECT @response = dbo.PostHttpData(@url, @header, @body);
    END;

END;
GO