using DigitalWallet.Domain.Entities;

namespace DigitalWallet.Application.Interfaces.Repositories;

public interface IBeneficiaryRepository
{
    Task<List<Beneficiary>> GetAllByUserIdAsync(int userId);
    Task<Beneficiary?> GetByIdAsync(int beneficiaryId);
    Task<bool> ExistsAsync(int userId, string walletNumber);
    Task<Beneficiary> AddAsync(Beneficiary dto);
    Task DeleteAsync(Beneficiary beneficiary);
}
