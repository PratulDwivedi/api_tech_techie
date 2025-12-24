CREATE OR ALTER PROCEDURE dbo.usp_StringToBase64
    @body NVARCHAR(MAX),
    @response NVARCHAR(MAX) OUTPUT,
    @statusCode INT OUTPUT
AS
/*                                                                   
========================================================================================================                                                                      
Copyright : Tech-Techie, 2025                                                                     
Author  : Pratul Dwivedi                                                                      
Create date : 18/Dec/2025                                                                     
Description : Service broker procedure to decrypt data                                                                
========================================================================================================                                                                      

DECLARE @response NVARCHAR(MAX),
        @statusCode INT;
EXEC dbo.usp_StringToBase64 @body = N'user:password', 
                     @response = @response OUTPUT, 
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
            SELECT '' AS Url,
                   'BASE64' AS Method,
                   '' AS Headers,
                   '' AS ContentType,
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
            TIMEOUT 1000;

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
        SELECT @response = dbo.StringToBase64(@body);
    END;

END;
GO