USE [AWT-WAAKIT-DB];


CREATE MESSAGE TYPE [HttpRequestMessage]
VALIDATION = WELL_FORMED_XML;

CREATE MESSAGE TYPE [HttpResponseMessage]
VALIDATION = WELL_FORMED_XML;

-- Create contract
CREATE CONTRACT [HttpRequestContract]
([HttpRequestMessage] SENT BY INITIATOR,
 [HttpResponseMessage] SENT BY TARGET);

-- Create queues
CREATE QUEUE HttpRequestQueue;
CREATE QUEUE HttpResponseQueue;

-- Create services
CREATE SERVICE [HttpRequestService]
ON QUEUE dbo.HttpRequestQueue
([HttpRequestContract]);


CREATE SERVICE [HttpResponseService]
ON QUEUE HttpResponseQueue
([HttpRequestContract]);

-- Enable worker

ALTER DATABASE [AWT-WAAKIT-DB]
SET ENABLE_BROKER
WITH ROLLBACK IMMEDIATE;