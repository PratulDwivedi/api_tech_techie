using System.ComponentModel.DataAnnotations;

namespace TechTechie.Services.Common.Models
{
    /// <summary>
    /// Configuration options for JWT authentication
    /// </summary>
    public class JwtOptions
    {
        /// <summary>
        /// Configuration section name
        /// </summary>
        public const string SectionName = "Jwt";

        /// <summary>
        /// Symmetric key for JWT signing (legacy support)
        /// </summary>
        public string Key { get; set; } = string.Empty;

        /// <summary>
        /// JWT token issuer
        /// </summary>
        [Required(ErrorMessage = "JWT Issuer is required")]
        public string Issuer { get; set; } = string.Empty;

        /// <summary>
        /// RSA private key in PEM format for signing tokens
        /// </summary>
        [Required(ErrorMessage = "JWT PrivateKey is required")]
        public string PrivateKey { get; set; } = string.Empty;

        /// <summary>
        /// RSA public key in PEM format for validating tokens
        /// </summary>
        [Required(ErrorMessage = "JWT PublicKey is required")]
        public string PublicKey { get; set; } = string.Empty;

        /// <summary>
        /// JWT token audience
        /// </summary>
        [Required(ErrorMessage = "JWT Audience is required")]
        public string Audience { get; set; } = string.Empty;

        /// <summary>
        /// Token expiry duration in minutes
        /// </summary>
        [Range(1, 43200, ErrorMessage = "ExpiryMinutes must be between 1 and 43200 (30 days)")]
        public int ExpiryMinutes { get; set; } = 200;

        /// <summary>
        /// Gets the token expiry as TimeSpan
        /// </summary>
        public TimeSpan ExpiryTimeSpan => TimeSpan.FromMinutes(ExpiryMinutes);
    }
}
