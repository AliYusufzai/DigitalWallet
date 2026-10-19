using DigitalWallet.Application.DTOs.Beneficiary;

namespace DigitalWallet.Application.Interfaces.Services;

public interface IBeneficiaryService
{
    Task<List<BeneficiaryResponseDto>> GetAllAsync(int userId);
    Task<BeneficiaryResponseDto> AddAsync(int userId, AddBeneficiaryDto dto);
    Task DeleteAsync(int userId, int beneficiaryId);
}
