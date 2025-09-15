using medical_be.DTOs;

namespace medical_be.Services
{
    /// <summary>
    /// Interface for data seeding operations to generate test medical data
    /// </summary>
    public interface IDataSeedingService
    {
        /// <summary>
        /// Check if large dataset has already been seeded
        /// </summary>
        Task<bool> IsDataAlreadySeededAsync();

        /// <summary>
        /// Seed a large dataset with specified number of users and comprehensive medical data
        /// </summary>
        /// <param name="userCount">Number of users to create (default: 10,000)</param>
        Task<DataSeedingResultDto> SeedLargeDatasetAsync(int userCount = 10000);

        /// <summary>
        /// Get seeding progress and statistics
        /// </summary>
        Task<DataSeedingStatusDto> GetSeedingStatusAsync();

        /// <summary>
        /// Clear all seeded data (for testing purposes)
        /// </summary>
        Task ClearSeedDataAsync();
    }
}
