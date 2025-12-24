CREATE OR ALTER PROCEDURE dbo.usp_PostHttpFormData
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
Description : Service broker procedure to call the Post form data api                                                                
========================================================================================================                                                                      
DECLARE @response NVARCHAR(MAX),
        @statusCode INT;
EXEC dbo.usp_PostHttpFormData @url = N'https://xxxxx/oauth/v2/token', 
                              @body = N'grant_type=refresh_token&client_id=1000.4DWVEU1ZDEAR4S7L01HFIE0S22SI4P',   
                              @header = N'', 
                              @response = @response OUTPUT,  
                              @statusCode = @statusCode OUTPUT; 
                              
SELECT @response;

========================================================================================================             
Change Log:                                                                       
dd-MMM-yyyy, Name, #CCxx, Description     
=======================================================================================================                                                                                                                                 
*/
BEGIN
    SET NOCOUNT ON;

    DECLARE @handle UNIQUEIDENTIFIER,
            @message_body XML,
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
                   'application/x-www-form-urlencoded' AS ContentType,
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
        SELECT @response = dbo.PostHttpFormData(@url, @header, @body);
    END;
END;
GO