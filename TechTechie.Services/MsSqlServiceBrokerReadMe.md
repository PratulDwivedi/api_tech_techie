# Service Broker Setup

## Create message types

USE TechTechieDB;


CREATE MESSAGE TYPE [HttpRequestMessage]
VALIDataION = WELL_FORMED_XML;

CREATE MESSAGE TYPE [HttpResponseMessage]
VALIDataION = WELL_FORMED_XML;

## Create contract
CREATE CONTRACT [HttpRequestContract]
([HttpRequestMessage] SENT BY INITIATOR,
 [HttpResponseMessage] SENT BY TARGET);

## Create queues
CREATE QUEUE HttpRequestQueue;
CREATE QUEUE HttpResponseQueue;

## Create services
CREATE SERVICE [HttpRequestService]
ON QUEUE dbo.HttpRequestQueue
([HttpRequestContract]);


CREATE SERVICE [HttpResponseService]
ON QUEUE HttpResponseQueue
([HttpRequestContract]);

## Enable worker for TechTechieDB

ALTER DataBASE TechTechieDB
SET ENABLE_BROKER
WITH ROLLBACK IMMEDIATE;


## CLR funtion to Service Broker supported Procedure

CREATE OR ALTER PROCEDURE dbo.usp_GetHttpData
    @url NVARCHAR(MAX),
    @header NVARCHAR(MAX) = NULL, -- Optional headers, format: Name:Value;Name2:Value2  
    @response NVARCHAR(MAX) OUTPUT,
    @timeout INT = 100000
AS
/*                                                                     
========================================================================================================                                                                        
Copyright : Tech Techie, 2025                                                                       
Author  : Pratul Dwivedi                                                                        
Create Date : 16/Dec/2025                                                                       
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
  
SELECT @AccessToken = Databaselog.dbo.StringToBase64(@UserName + ':' + @PassWord);  
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
    FROM sys.Databases
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



## usp_PostHttpData Procedure to call post api


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
Copyright : Tech Techie, 2025                                                                     
Author  : Pratul Dwivedi                                                                      
Create Date : 16/Dec/2025                                                                     
Description : Service broker procedure to call the POST api                                                                
========================================================================================================                                                                      
DECLARE @response NVARCHAR(MAX),
        @statusCode INT;


EXEC dbo.usp_PostHttpData @url = N'',                       -- nvarchar(500)
                          @body = N'{',                      -- nvarchar(max)
                          @header = N'Authorization: Bearer eyJhbGciOiJSUzI', 
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
    FROM sys.Databases
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


## usp_PostHttpFormData procedure to call formData api

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
Copyright : Tech Techie, 2025                                                                     
Author  : Pratul Dwivedi                                                                      
Create Date : 16/Dec/2025                                                                     
Description : Service broker procedure to call the Post form Data api                                                                
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
    FROM sys.Databases
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



## usp_Encrypt procedure to encrypt the Data


CREATE OR ALTER PROCEDURE dbo.usp_Encrypt
    @body NVARCHAR(MAX),
    @response NVARCHAR(MAX) OUTPUT,
    @statusCode INT OUTPUT
AS
/*                                                                   
========================================================================================================                                                                      
Copyright : Tech Techie, 2025                                                                     
Author  : Pratul Dwivedi                                                                      
Create Date : 18/Dec/2025                                                                     
Description : Service broker procedure to encrypt Data                                                                
========================================================================================================                                                                      

DECLARE @response NVARCHAR(MAX),
        @statusCode INT;
EXEC dbo.usp_Encrypt @body = N'MyPassword', 
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
    FROM sys.Databases
    WHERE name = DB_NAME();


    IF @is_broker_enabled = 1
    BEGIN
        -- Build request XML
        SET @message_body =
        (
            SELECT '' AS Url,
                   'ENCRYPT' AS Method,
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
        SELECT @response = dbo.Encrypt(@body);
    END;

END;
GO

## usp_Decrypt procedure to encrypt the Data


CREATE OR ALTER PROCEDURE dbo.usp_Decrypt
    @body NVARCHAR(MAX),
    @response NVARCHAR(MAX) OUTPUT,
    @statusCode INT OUTPUT
AS
/*                                                                   
========================================================================================================                                                                      
Copyright : Tech Techie, 2025                                                                     
Author  : Pratul Dwivedi                                                                      
Create Date : 18/Dec/2025                                                                     
Description : Service broker procedure to decrypt Data                                                                
========================================================================================================                                                                      

DECLARE @response NVARCHAR(MAX),
        @statusCode INT;
EXEC dbo.usp_Decrypt @body = N'fbAODQbt/3EWKG4PvsiYfQ==', 
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
    FROM sys.Databases
    WHERE name = DB_NAME();


    IF @is_broker_enabled = 1
    BEGIN
        -- Build request XML
        SET @message_body =
        (
            SELECT '' AS Url,
                   'DECRYPT' AS Method,
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
        SELECT @response = dbo.Decrypt(@body);
    END;

END;
GO

## usp_StringToBase64 procedure to convert string to base64

CREATE OR ALTER PROCEDURE dbo.usp_StringToBase64
    @body NVARCHAR(MAX),
    @response NVARCHAR(MAX) OUTPUT,
    @statusCode INT OUTPUT
AS
/*                                                                   
========================================================================================================                                                                      
Copyright : Tech Techie, 2025                                                                     
Author  : Pratul Dwivedi                                                                      
Create Date : 18/Dec/2025                                                                     
Description : Service broker procedure to decrypt Data                                                                
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
    FROM sys.Databases
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
