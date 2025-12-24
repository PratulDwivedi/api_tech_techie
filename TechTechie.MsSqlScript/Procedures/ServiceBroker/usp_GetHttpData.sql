CREATE OR ALTER PROCEDURE dbo.usp_GetHttpData
    @url NVARCHAR(MAX),
    @header NVARCHAR(MAX) = NULL, -- Optional headers, format: Name:Value;Name2:Value2  
    @response NVARCHAR(MAX) OUTPUT,
    @timeout INT = 100000
AS
/*                                                                     
========================================================================================================                                                                        
Copyright : Tech-Techie, 2025                                                                       
Author  : Pratul Dwivedi                                                                        
Create date : 19/Dec/2025                                                                       
Description : Service broker procedure to call the get api                                                                  
========================================================================================================                                                                        
  
DECLARE @RequestUrl NVARCHAR(MAX)  
    = N'',  
        @UserName NVARCHAR(100) = N'',  
        @PassWord NVARCHAR(100) = N'',  
        @AccessToken NVARCHAR(MAX) = N'',  
        @Headers NVARCHAR(MAX) = N'',  
        @result NVARCHAR(MAX),  
        @Data NVARCHAR(MAX);  
  
SELECT @AccessToken = dbo.StringToBase64(@UserName + ':' + @PassWord);  
SET @Headers = N'Authorization: Basic ' + @AccessToken;  
  
EXEC dbo.usp_GetHttpData @RequestUrl, @Headers, @result OUTPUT;  
SELECT @result;  
  
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
                   'GET' AS Method,
                   @header AS Headers,
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
                SET @response = @message_body.value('(/Response/Body)[1]', 'NVARCHAR(MAX)');
            ELSE
                SET @response = 'ERROR: Timeout or no response received';

            -- End conversation  
            IF @handle IS NOT NULL
                END CONVERSATION @handle;

        END TRY
        BEGIN CATCH
            SET @response = 'ERROR: ' + ERROR_MESSAGE();
            IF @handle IS NOT NULL
                END CONVERSATION @handle
                WITH ERROR = 500 DESCRIPTION = 'Internal Error';
        END CATCH;
    END;
    ELSE
    BEGIN
        IF @header IS NULL
        BEGIN
            SELECT @response = dbo.GetHttpData(@url);
        END;
        ELSE
        BEGIN
            SELECT @response = dbo.GetHttpDataHeaders(@url, @header);
        END;
    END;

END;
