using Microsoft.AspNetCore.Mvc;
using TechTechie.Common;
using TechTechie.Services.AI.Interfaces;
using TechTechie.Services.AI.Models;
using TechTechie.Services.Common.Models;


namespace TechTechie.WebApi.Controllers
{

    //  [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [ApiController]
    public class AIController : ControllerBase
    {
        private readonly IAIService _aiService;
        private readonly ILogger<AIController> _logger;

        public AIController(IAIService aiService, ILogger<AIController> logger)
        {
            _aiService = aiService;
            _logger = logger;
        }

        /// <summary>
        /// Generate text completion from a prompt.
        /// </summary>
        [HttpPost("~/api/ai/generate")]
        public async Task<IActionResult> GenerateAsync([FromBody] AIGenerateRequestModel request)
        {

            try
            {
                var result = await _aiService.GenerateAsync(request.ConfigId, request.Prompt);
                return Ok(new ResponseMessageModel() { IsSuccess = true, StatusCode = 200, Message = "Success", Data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating AI response");
                return StatusCode(500, new ResponseMessageModel() { IsSuccess = false, StatusCode = 400, Message = ex.Message });
            }
        }

        /// <summary>
        /// Run a chat completion with optional conversation history.
        /// </summary>
        [HttpPost("~/api/ai/chat")]
        public async Task<IActionResult> ChatAsync([FromBody] AIChatRequestModel request)
        {
            try
            {
                var historyTuples = request.History?
                    .Select(h => (h.Role, h.Content))
                    .ToList();

                var result = await _aiService.ChatAsync(request.ConfigId, request.Message, historyTuples);
                return Ok(new ResponseMessageModel() { IsSuccess = true, StatusCode = 200, Message = "Success", Data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error running chat completion");
                return StatusCode(500, new ResponseMessageModel() { IsSuccess = false, StatusCode = 400, Message = ex.Message });
            }
        }

        /// <summary>
        /// Generate embeddings for semantic search or RAG.
        /// </summary>
        [HttpPost("~/api/ai/embed")]
        public async Task<IActionResult> GetEmbeddingAsync([FromBody] AIEmbeddingRequestModel request)
        {
            try
            {
                var result = await _aiService.GetEmbeddingAsync(request.ConfigId, request.Input);
                return Ok(new ResponseMessageModel() { IsSuccess = true, StatusCode = 200, Message = "Success", Data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating embeddings");
                return StatusCode(500, new ResponseMessageModel() { IsSuccess = false, StatusCode = 400, Message = ex.Message });
            }
        }


        /// <summary>
        /// Generate speech from text.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("~/api/ai/speak")]
        public async Task<IActionResult> SpeakAsync([FromBody] AISpeakRequestModel request)
        {
            try
            {

                var audioBytes = await _aiService.SpeakAsync(request.ConfigId, request);
                var contentType = Utils.GetMimeType(request.Format);

                return File(audioBytes, contentType, "speech-output.mp3");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating speech");
                return StatusCode(500, new ResponseMessageModel
                {
                    IsSuccess = false,
                    StatusCode = 500,
                    Message = ex.Message
                });
            }
        }


        /// <summary>
        /// Stream speech synthesis from text.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("~/api/ai/speak/stream")]
        public async Task<IActionResult> SpeakStreamAsync([FromBody] AISpeakRequestModel request)
        {
            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.Text))
                    return BadRequest("Text is required for speech synthesis.");

                var responseStream = new MemoryStream();

                // Assuming configId is in request.ConfigId
                await _aiService.SpeakStreamAsync(request.ConfigId, request, async chunk =>
                {
                    await responseStream.WriteAsync(chunk);
                });

                responseStream.Position = 0;
                var contentType = Utils.GetMimeType(request.Format ?? "audio/mp3");

                return File(responseStream, contentType, "speech-stream-output.mp3");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error streaming speech");
                return StatusCode(500, new ResponseMessageModel
                {
                    IsSuccess = false,
                    StatusCode = 500,
                    Message = ex.Message
                });
            }
        }



    }
}
